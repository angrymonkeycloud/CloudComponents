using CloudComponents.Grid.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using System.Globalization;

namespace CloudComponents.Grid.Components;

/// <summary>
/// Inner table component for <see cref="CloudGrid"/>: renders the column header row,
/// data rows (with paging / infinite scroll / load-more), column-resize and row-drag
/// shields — all without JavaScript interop.
///
/// Owns: sorting, column resizing, row drag-and-drop, paging.
/// Selection state is owned by <see cref="CloudGrid"/> and passed in as parameters.
/// </summary>
public partial class CloudGridBody
{
    #region Parameters

    /// <summary>Column definitions, in display order.</summary>
    [Parameter] public List<CloudGridColumn> Columns { get; set; } = [];

    /// <summary>The current page of data. Null while nothing has been loaded.</summary>
    [Parameter] public CloudGridDataResult? Data { get; set; }

    /// <summary>Shows <see cref="LoadingText"/> instead of rows while true.</summary>
    [Parameter] public bool IsLoading { get; set; }

    /// <summary>Shows <see cref="SearchingText"/> instead of rows while true.</summary>
    [Parameter] public bool IsSearching { get; set; }

    /// <summary>Optional message displayed instead of the rows (e.g. "No records.").</summary>
    [Parameter] public string? Message { get; set; }

    /// <summary>Renders a checkbox column for row selection when true.</summary>
    [Parameter] public bool AllowSelection { get; set; }

    /// <summary>Ids of the currently selected rows (owned by CloudGrid).</summary>
    [Parameter] public List<Guid> SelectedRecords { get; set; } = [];

    /// <summary>Raised whenever the selection changes via a row checkbox or select-all.</summary>
    [Parameter] public EventCallback<List<Guid>> SelectedRecordsChanged { get; set; }

    /// <summary>Renders a drag handle on each row when true.</summary>
    [Parameter] public bool AllowReordering { get; set; }

    /// <summary>Raised after the user drops a dragged row at a new position.</summary>
    [Parameter] public EventCallback<CloudGridRowReorder> OnRowsReordered { get; set; }

    /// <summary>Actions rendered on each row (those with <see cref="CloudGridAction.ShowOnRow"/> = true).</summary>
    [Parameter] public List<CloudGridAction> RowActions { get; set; } = [];

    /// <summary>Optional per-row filter; return false to hide an action on a specific row.</summary>
    [Parameter] public Func<CloudGridRow, CloudGridAction, bool>? ActionFilter { get; set; }

    /// <summary>Raised when a row <see cref="CloudGridActionType.Button"/> action is clicked.</summary>
    [Parameter] public EventCallback<CloudGridActionEventArgs> OnActionClicked { get; set; }

    /// <summary>Target of row links (<c>_parent</c> by default).</summary>
    [Parameter] public string LinkTarget { get; set; } = "_parent";

    [Parameter] public string LoadingText { get; set; } = "retrieving data...";
    [Parameter] public string SearchingText { get; set; } = "searching...";

    /// <summary>Placeholder rendered for null cell values.</summary>
    [Parameter] public string EmptyCellText { get; set; } = "--";

    /// <summary>Fixed row height in pixels. Overrides the <c>--cloudgrid-row-height</c> CSS variable.</summary>
    [Parameter] public double? RowHeight { get; set; }

    /// <summary>Number of body rows the grid is sized for.</summary>
    [Parameter] public int? RowsPerPage { get; set; }

    /// <summary>How the grid navigates between pages.</summary>
    [Parameter] public CloudGridPagingMode PagingMode { get; set; } = CloudGridPagingMode.Pages;

    /// <summary>Text of the load-more button (<see cref="CloudGridPagingMode.LoadMore"/>).</summary>
    [Parameter] public string LoadMoreText { get; set; } = "load more";

    /// <summary>Raised when the user navigates between pages.</summary>
    [Parameter] public EventCallback<CloudGridPaginationType> OnPageChanged { get; set; }

    /// <summary>
    /// Raised when the user sorts a column. When no handler is attached the grid
    /// falls back to sorting the current page locally.
    /// </summary>
    [Parameter] public EventCallback<CloudGridSort> OnSortChanged { get; set; }

    /// <summary>
    /// Raised when column-resize starts or ends, so the parent can apply the
    /// <c>._resizing</c> CSS adjective on the root element.
    /// </summary>
    [Parameter] public EventCallback<bool> OnResizingChanged { get; set; }

    /// <summary>
    /// Raised when row dragging starts or ends, so the parent can apply the
    /// <c>._rowdragging</c> CSS adjective on the root element.
    /// </summary>
    [Parameter] public EventCallback<bool> OnDraggingChanged { get; set; }

    #endregion

    #region Selection (owned by CloudGrid, forwarded here)

    private bool IsRowSelected(CloudGridRow row) => SelectedRecords.Contains(row.Id);

    private bool AllRowsSelected =>
        Data is { Rows.Count: > 0 } && Data.Rows.All(row => SelectedRecords.Contains(row.Id));

    private async Task ToggleRowAsync(CloudGridRow row)
    {
        List<Guid> updated = [.. SelectedRecords];

        if (!updated.Remove(row.Id))
            updated.Add(row.Id);

        await SelectedRecordsChanged.InvokeAsync(updated);
    }

    private async Task ToggleSelectAllAsync(ChangeEventArgs e)
    {
        bool selectAll = e.Value is bool value && value;
        List<Guid> updated = [.. SelectedRecords];

        if (Data != null)
            foreach (CloudGridRow row in Data.Rows)
            {
                updated.Remove(row.Id);

                if (selectAll)
                    updated.Add(row.Id);
            }

        await SelectedRecordsChanged.InvokeAsync(updated);
    }

    #endregion

    #region Row reordering

    private Guid? _dragRowId;
    private int _dragStartIndex;
    private int _dragCurrentIndex;
    private double _dragStartY;

    private bool IsRowDragging(CloudGridRow row) => _dragRowId == row.Id;

    private async Task BeginRowDrag(CloudGridRow row, PointerEventArgs e)
    {
        if (!AllowReordering || Data == null)
            return;

        int index = Data.Rows.FindIndex(r => r.Id == row.Id);

        if (index < 0)
            return;

        _dragRowId = row.Id;
        _dragStartIndex = index;
        _dragCurrentIndex = index;
        _dragStartY = e.ClientY;

        await OnDraggingChanged.InvokeAsync(true);
    }

    private async Task RowDragMove(PointerEventArgs e)
    {
        if (!_dragRowId.HasValue || Data == null)
            return;

        int offset = (int)Math.Round((e.ClientY - _dragStartY) / EffectiveRowHeight);
        int targetIndex = Math.Clamp(_dragStartIndex + offset, 0, Data.Rows.Count - 1);

        if (targetIndex == _dragCurrentIndex)
            return;

        CloudGridRow dragged = Data.Rows[_dragCurrentIndex];
        Data.Rows.RemoveAt(_dragCurrentIndex);
        Data.Rows.Insert(targetIndex, dragged);
        _dragCurrentIndex = targetIndex;

        await RefreshVirtualizeAsync();
    }

    private async Task EndRowDrag()
    {
        if (_dragRowId is not Guid rowId || Data == null)
        {
            _dragRowId = null;
            await OnDraggingChanged.InvokeAsync(false);
            return;
        }

        int oldIndex = _dragStartIndex;
        int newIndex = _dragCurrentIndex;

        _dragRowId = null;

        await OnDraggingChanged.InvokeAsync(false);

        if (oldIndex != newIndex && OnRowsReordered.HasDelegate)
            await OnRowsReordered.InvokeAsync(new CloudGridRowReorder
            {
                RecordId = rowId,
                OldIndex = oldIndex,
                NewIndex = newIndex,
                OrderedRecordIds = [.. Data.Rows.Select(r => r.Id)]
            });
    }

    #endregion

    #region Row actions

    /// <summary>Key of the currently expanded Element action per row (rowId, actionKey).</summary>
    private (Guid RowId, string ActionKey)? _activeRowElement;

    private IEnumerable<CloudGridAction> RowVisibleActions(CloudGridRow row) =>
        RowActions.Where(a => a.ShowOnRow
            && (!a.VisibleOnSelectionOnly || IsRowSelected(row))
            && (ActionFilter?.Invoke(row, a) ?? true));

    private bool IsRowElementActive(CloudGridRow row, CloudGridAction action) =>
        _activeRowElement is { } state && state.RowId == row.Id && state.ActionKey == action.Key;

    private async Task ToggleRowElementAsync(CloudGridRow row, CloudGridAction action)
    {
        if (_activeRowElement is { } state && state.RowId == row.Id && state.ActionKey == action.Key)
        {
            _activeRowElement = null;
            if (action.OnDeactivated != null)
                await action.OnDeactivated();
        }
        else
        {
            if (_activeRowElement is { } prev)
            {
                CloudGridAction? prevAction = RowActions.FirstOrDefault(a => a.Key == prev.ActionKey);
                if (prevAction?.OnDeactivated != null)
                    await prevAction.OnDeactivated();
            }
            _activeRowElement = (row.Id, action.Key);
        }
    }

    private async Task CancelRowElementAsync()
    {
        if (_activeRowElement is not { } state)
            return;

        CloudGridAction? action = RowActions.FirstOrDefault(a => a.Key == state.ActionKey);
        _activeRowElement = null;

        if (action?.OnDeactivated != null)
            await action.OnDeactivated();
    }

    private Task RowActionClickAsync(CloudGridAction action, CloudGridRow row) =>
        OnActionClicked.InvokeAsync(new CloudGridActionEventArgs
        {
            Action = action,
            RecordIds = [row.Id]
        });

    #endregion

    #region Sorting

    private int? _sortIndex;
    private CloudGridSortDirection _sortDirection = CloudGridSortDirection.Ascending;

    private IEnumerable<CloudGridRow> DisplayRows
    {
        get
        {
            if (Data == null)
                return [];

            // When the consumer handles sorting (server side), render rows as provided.
            if (!_sortIndex.HasValue || OnSortChanged.HasDelegate)
                return Data.Rows;

            int index = _sortIndex.Value;

            return _sortDirection == CloudGridSortDirection.Ascending
                ? Data.Rows.OrderBy(row => row.Cells.ElementAtOrDefault(index), CellComparer.Instance)
                : Data.Rows.OrderByDescending(row => row.Cells.ElementAtOrDefault(index), CellComparer.Instance);
        }
    }

    private async Task SortByAsync(int columnIndex)
    {
        if (_resizeIndex.HasValue || columnIndex >= Columns.Count)
            return;

        CloudGridColumn column = Columns[columnIndex];

        if (!column.Sortable)
            return;

        if (_sortIndex == columnIndex)
            _sortDirection = _sortDirection == CloudGridSortDirection.Ascending
                ? CloudGridSortDirection.Descending
                : CloudGridSortDirection.Ascending;
        else
        {
            _sortIndex = columnIndex;
            _sortDirection = CloudGridSortDirection.Ascending;
        }

        if (OnSortChanged.HasDelegate)
            await OnSortChanged.InvokeAsync(new CloudGridSort
            {
                Column = column,
                ColumnIndex = columnIndex,
                Direction = _sortDirection
            });
        else
            await RefreshVirtualizeAsync();
    }

    /// <summary>
    /// Compares cell values of the same type natively (numbers, dates, ...) and
    /// everything else as case-insensitive text. Nulls sort last.
    /// </summary>
    private sealed class CellComparer : IComparer<object?>
    {
        public static readonly CellComparer Instance = new();

        public int Compare(object? x, object? y)
        {
            if (ReferenceEquals(x, y))
                return 0;

            if (x is null)
                return 1;

            if (y is null)
                return -1;

            if (x.GetType() == y.GetType() && x is IComparable comparable)
                return comparable.CompareTo(y);

            return string.Compare(x.ToString(), y.ToString(), StringComparison.OrdinalIgnoreCase);
        }
    }

    #endregion

    #region Column resizing

    private readonly Dictionary<int, double> _columnWidths = [];
    private int? _resizeIndex;
    private double _resizeStartX;
    private double _resizeStartWidth;

    private double GetColumnWidth(int columnIndex)
    {
        if (_columnWidths.TryGetValue(columnIndex, out double width))
            return width;

        return columnIndex < Columns.Count ? Columns[columnIndex].Width : 100;
    }

    private async Task BeginResize(int columnIndex, PointerEventArgs e)
    {
        _resizeIndex = columnIndex;
        _resizeStartX = e.ClientX;
        _resizeStartWidth = GetColumnWidth(columnIndex);

        await OnResizingChanged.InvokeAsync(true);
    }

    private void ResizeMove(PointerEventArgs e)
    {
        if (_resizeIndex is not int columnIndex)
            return;

        double minWidth = columnIndex < Columns.Count ? Columns[columnIndex].MinWidth : 40;

        _columnWidths[columnIndex] = Math.Max(minWidth, _resizeStartWidth + e.ClientX - _resizeStartX);
    }

    private async Task EndResize()
    {
        _resizeIndex = null;
        await OnResizingChanged.InvokeAsync(false);
    }

    #endregion

    #region Paging

    private Virtualize<CloudGridRow>? _virtualize;
    private bool _loadMorePending;
    private bool _loadExhausted;
    private (int Total, int Count, int Page) _dataSignature;

    private double EffectiveRowHeight => RowHeight ?? 43;

    private List<CloudGridRow> LoadedRows => Data?.Rows ?? [];

    /// <summary>More rows exist beyond the ones currently loaded (accumulating modes).</summary>
    private bool HasMoreRows => Data != null && !_loadExhausted && Data.Rows.Count < Data.Total;

    /// <summary>Re-queries Virtualize after the data changed outside of scrolling (search, reload, sort).</summary>
    public Task RefreshVirtualizeAsync() =>
        PagingMode == CloudGridPagingMode.InfiniteScroll && _virtualize != null
            ? _virtualize.RefreshDataAsync()
            : Task.CompletedTask;

    private async Task LoadMoreAsync()
    {
        if (_loadMorePending || !HasMoreRows || !OnPageChanged.HasDelegate)
            return;

        _loadMorePending = true;
        int rowsBefore = LoadedRows.Count;

        try
        {
            await OnPageChanged.InvokeAsync(CloudGridPaginationType.RightArrow);
        }
        finally
        {
            _loadMorePending = false;
        }

        // The consumer did not append any rows; stop requesting to avoid a loop.
        if (LoadedRows.Count == rowsBefore)
            _loadExhausted = true;
    }

    /// <summary>
    /// Items provider for <see cref="CloudGridPagingMode.InfiniteScroll"/>.
    /// </summary>
    private async ValueTask<ItemsProviderResult<CloudGridRow>> ProvideRowsAsync(ItemsProviderRequest request)
    {
        if (request.StartIndex + request.Count > LoadedRows.Count && HasMoreRows)
            await LoadMoreAsync();

        List<CloudGridRow> rows = [.. DisplayRows];
        int total = HasMoreRows ? Math.Max(Data?.Total ?? 0, rows.Count) : rows.Count;

        return new ItemsProviderResult<CloudGridRow>(rows.Skip(request.StartIndex).Take(request.Count), total);
    }

    protected override async Task OnParametersSetAsync()
    {
        (int, int, int) signature = (Data?.Total ?? 0, Data?.Rows.Count ?? 0, Data?.Page ?? 0);

        if (signature != _dataSignature)
        {
            _dataSignature = signature;
            _loadExhausted = false;

            if (!_loadMorePending)
                await RefreshVirtualizeAsync();
        }
    }

    #endregion

    #region CSS helpers

    private string HeadCellClass(int columnIndex)
    {
        List<string> classes = ["cloudgrid-headcell"];

        if (Columns[columnIndex].Sortable)
            classes.Add("_sortable");

        if (_sortIndex == columnIndex)
            classes.Add(_sortDirection == CloudGridSortDirection.Ascending ? "_sorted-ascending" : "_sorted-descending");

        return string.Join(' ', classes);
    }

    private string RowClass(CloudGridRow row)
    {
        List<string> classes = ["cloudgrid-row"];

        if (IsRowSelected(row))
            classes.Add("_selected");

        if (IsRowDragging(row))
            classes.Add("_dragging");

        return string.Join(' ', classes);
    }

    private string TableClass
    {
        get
        {
            List<string> classes = [];

            if (RowsPerPage.HasValue)
                classes.Add("_fixed");

            // Accumulating modes need a vertical scrollbar inside the fixed viewport.
            if (RowsPerPage.HasValue && PagingMode != CloudGridPagingMode.Pages)
                classes.Add("_scroll");

            return string.Join(' ', classes);
        }
    }

    /// <summary>
    /// Exact table height: <see cref="RowsPerPage"/> body rows.
    /// </summary>
    private string? BodyStyle =>
        RowsPerPage.HasValue
            ? $"height: calc(var(--cloudgrid-row-height) * {RowsPerPage.Value})"
            : null;

    private static string Px(double value) => value.ToString(CultureInfo.InvariantCulture) + "px";

    #endregion
}
