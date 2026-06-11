using CloudComponents.Grid.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using System.Globalization;

namespace CloudComponents.Grid.Components;

/// <summary>
/// Generic, dependency-free data grid with paging, column sorting, column resizing
/// and row selection — all implemented in Blazor/C#, without JavaScript interop.
///
/// The grid is purely presentational: data is supplied through <see cref="Data"/> and
/// user interactions are surfaced through callbacks (<see cref="OnPageChanged"/>,
/// <see cref="OnSortChanged"/>, <see cref="SelectedRecordsChanged"/>).
/// </summary>
public partial class CloudGrid
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

    /// <summary>Ids of the currently selected rows. Supports two-way binding.</summary>
    [Parameter] public List<Guid>? SelectedRecords { get; set; }

    /// <summary>Raised whenever the selection changes (row checkbox or select-all).</summary>
    [Parameter] public EventCallback<List<Guid>> SelectedRecordsChanged { get; set; }

    /// <summary>Additional CSS class(es) appended to the root element.</summary>
    [Parameter] public string? CssClass { get; set; }

    /// <summary>Target of row links (<c>_parent</c> by default).</summary>
    [Parameter] public string LinkTarget { get; set; } = "_parent";

    [Parameter] public string LoadingText { get; set; } = "retrieving data...";
    [Parameter] public string SearchingText { get; set; } = "searching...";

    /// <summary>Placeholder rendered for null cell values.</summary>
    [Parameter] public string EmptyCellText { get; set; } = "--";

    /// <summary>
    /// Fixed row height in pixels (header and body rows). Overrides the
    /// <c>--cloudgrid-row-height</c> CSS variable. Defaults to 32 when neither
    /// this parameter nor the CSS variable is set.
    /// </summary>
    [Parameter] public double? RowHeight { get; set; }

    /// <summary>
    /// Number of body rows the grid is sized for. When set, the table gets an
    /// exact height of <c>(RowsPerPage + 1) × row height</c> (the +1 is the
    /// header) so a full page fits without a vertical scrollbar or leftover
    /// pixels. With <see cref="CloudGridPagingMode.LoadMore"/> or
    /// <see cref="CloudGridPagingMode.InfiniteScroll"/> it defines the visible
    /// viewport instead, and additional rows scroll within it.
    /// </summary>
    [Parameter] public int? RowsPerPage { get; set; }

    /// <summary>How the grid navigates between pages. Defaults to <see cref="CloudGridPagingMode.Pages"/>.</summary>
    [Parameter] public CloudGridPagingMode PagingMode { get; set; } = CloudGridPagingMode.Pages;

    /// <summary>Text of the load-more button (<see cref="CloudGridPagingMode.LoadMore"/>).</summary>
    [Parameter] public string LoadMoreText { get; set; } = "load more";

    /// <summary>Raised when the user navigates between pages using the footer.</summary>
    [Parameter] public EventCallback<CloudGridPaginationType> OnPageChanged { get; set; }

    /// <summary>
    /// Raised when the user sorts a column. When no handler is attached the grid
    /// falls back to sorting the current page locally; attach a handler to reload
    /// data sorted server side instead.
    /// </summary>
    [Parameter] public EventCallback<CloudGridSort> OnSortChanged { get; set; }

    /// <summary>Unmatched attributes (e.g. data-*) rendered on the root element.</summary>
    [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object>? AdditionalAttributes { get; set; }

    #endregion

    #region Selection

    private List<Guid> _selectedRecords = [];
    private List<Guid>? _lastSelectedRecordsParameter;

    protected override void OnParametersSet()
    {
        // Re-sync internal selection only when the parameter instance changes,
        // so user toggles are not lost on unrelated re-renders.
        if (!ReferenceEquals(SelectedRecords, _lastSelectedRecordsParameter))
        {
            _lastSelectedRecordsParameter = SelectedRecords;
            _selectedRecords = SelectedRecords is null ? [] : [.. SelectedRecords];
        }
    }

    private bool IsRowSelected(CloudGridRow row) => _selectedRecords.Contains(row.Id);

    private bool AllRowsSelected =>
        Data is { Rows.Count: > 0 } && Data.Rows.All(row => _selectedRecords.Contains(row.Id));

    private async Task ToggleRowAsync(CloudGridRow row)
    {
        if (!_selectedRecords.Remove(row.Id))
            _selectedRecords.Add(row.Id);

        await SelectedRecordsChanged.InvokeAsync(_selectedRecords);
    }

    private async Task ToggleSelectAllAsync(ChangeEventArgs e)
    {
        bool selectAll = e.Value is bool value && value;

        if (Data != null)
            foreach (CloudGridRow row in Data.Rows)
            {
                _selectedRecords.Remove(row.Id);

                if (selectAll)
                    _selectedRecords.Add(row.Id);
            }

        await SelectedRecordsChanged.InvokeAsync(_selectedRecords);
    }

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

    private void BeginResize(int columnIndex, PointerEventArgs e)
    {
        _resizeIndex = columnIndex;
        _resizeStartX = e.ClientX;
        _resizeStartWidth = GetColumnWidth(columnIndex);
    }

    private void ResizeMove(PointerEventArgs e)
    {
        if (_resizeIndex is not int columnIndex)
            return;

        double minWidth = columnIndex < Columns.Count ? Columns[columnIndex].MinWidth : 40;

        _columnWidths[columnIndex] = Math.Max(minWidth, _resizeStartWidth + e.ClientX - _resizeStartX);
    }

    private void EndResize() => _resizeIndex = null;

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

    private int RangeStart => Data == null ? 0
        : PagingMode == CloudGridPagingMode.Pages ? (Data.PageSize * (Data.Page - 1)) + 1
        : 1;

    private int RangeEnd => Data == null ? 0
        : PagingMode == CloudGridPagingMode.Pages ? (Data.PageSize * (Data.Page - 1)) + Data.Rows.Count
        : Data.Rows.Count;

    private bool HasPreviousPage => Data != null && RangeStart > 1;
    private bool HasNextPage => Data != null && RangeEnd < Data.Total;

    private Task PreviousPageAsync() => OnPageChanged.InvokeAsync(CloudGridPaginationType.LeftArrow);
    private Task NextPageAsync() => OnPageChanged.InvokeAsync(CloudGridPaginationType.RightArrow);

    /// <summary>
    /// Requests the next page (load-more button or infinite scroll). The
    /// consumer is expected to append the new rows to <see cref="Data"/>.
    /// When a request completes without growing the row list, loading stops
    /// until the data changes again.
    /// </summary>
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
    /// Virtualize asks for the row range near the scroll position; when that
    /// range extends past the loaded rows the next page is fetched first.
    /// </summary>
    private async ValueTask<ItemsProviderResult<CloudGridRow>> ProvideRowsAsync(ItemsProviderRequest request)
    {
        if (request.StartIndex + request.Count > LoadedRows.Count && HasMoreRows)
            await LoadMoreAsync();

        List<CloudGridRow> rows = [.. DisplayRows];
        int total = HasMoreRows ? Math.Max(Data?.Total ?? 0, rows.Count) : rows.Count;

        return new ItemsProviderResult<CloudGridRow>(rows.Skip(request.StartIndex).Take(request.Count), total);
    }

    /// <summary>Re-queries Virtualize after the data changed outside of scrolling (search, reload, sort).</summary>
    private Task RefreshVirtualizeAsync() =>
        PagingMode == CloudGridPagingMode.InfiniteScroll && _virtualize != null
            ? _virtualize.RefreshDataAsync()
            : Task.CompletedTask;

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

    private string RootClass
    {
        get
        {
            List<string> classes = [];

            if (AllowSelection)
                classes.Add("_selectable");

            if (IsLoading || IsSearching)
                classes.Add("_busy");

            if (_resizeIndex.HasValue)
                classes.Add("_resizing");

            if (!string.IsNullOrWhiteSpace(CssClass))
                classes.Add(CssClass);

            return string.Join(' ', classes);
        }
    }

    private string HeadCellClass(int columnIndex)
    {
        List<string> classes = ["cloudgrid-headcell"];

        if (Columns[columnIndex].Sortable)
            classes.Add("_sortable");

        if (_sortIndex == columnIndex)
            classes.Add(_sortDirection == CloudGridSortDirection.Ascending ? "_sorted-ascending" : "_sorted-descending");

        return string.Join(' ', classes);
    }

    private string RowClass(CloudGridRow row) =>
        IsRowSelected(row) ? "cloudgrid-row _selected" : "cloudgrid-row";

    /// <summary>Inline CSS variables on the root element (currently the row height).</summary>
    private string? RootStyle =>
        RowHeight.HasValue ? $"--cloudgrid-row-height: {Px(RowHeight.Value)}" : null;

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
    /// Exact table height: header + <see cref="RowsPerPage"/> body rows. Rows and
    /// header use border-box sizing, so no extra pixels and no vertical scrollbar.
    /// </summary>
    private string? BodyStyle =>
        RowsPerPage.HasValue
            ? $"height: calc(var(--cloudgrid-row-height) * {RowsPerPage.Value})"
            : null;

    private static string Px(double value) => value.ToString(CultureInfo.InvariantCulture) + "px";

    #endregion
}
