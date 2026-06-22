using CloudComponents.Grid.Models;
using Microsoft.AspNetCore.Components;
using System.Globalization;

namespace CloudComponents.Grid.Components;

/// <summary>
/// Single entry point for the full grid experience.
/// Composes <see cref="CloudGridHeader"/> (optional), <see cref="CloudGridBody"/>,
/// and <see cref="CloudGridFooter"/> internally.
///
/// Owns all data-fetching state (loading, searching, pagination, sort, error/empty
/// messages) via the required <see cref="DataProvider"/> callback.
/// </summary>
public partial class CloudGrid
{
    #region Parameters

    /// <summary>When set, renders a <see cref="CloudGridHeader"/> above the table.</summary>
    [Parameter] public CloudGridHeaderOptions? Header { get; set; }

    /// <summary>Column definitions, in display order.</summary>
    [Parameter] public List<CloudGridColumn> Columns { get; set; } = [];

    /// <summary>
    /// Called whenever the grid needs data — on initial load, page change, sort change,
    /// or search change. Return <see cref="CloudGridDataResult.ErrorMessage"/> to surface
    /// a soft error instead of rows.
    /// </summary>
    [Parameter] public required Func<CloudGridDataRequest, Task<CloudGridDataResult?>> DataProvider { get; set; }

    /// <summary>Renders a checkbox column for row selection when true.</summary>
    [Parameter] public bool AllowSelection { get; set; }

    /// <summary>Ids of the currently selected rows. Supports two-way binding.</summary>
    [Parameter] public List<Guid>? SelectedRecords { get; set; }

    /// <summary>Raised whenever the selection changes.</summary>
    [Parameter] public EventCallback<List<Guid>> SelectedRecordsChanged { get; set; }

    /// <summary>Renders a drag handle on each row for reordering when true.</summary>
    [Parameter] public bool AllowReordering { get; set; }

    /// <summary>Raised after the user drops a dragged row at a new position.</summary>
    [Parameter] public EventCallback<CloudGridRowReorder> OnRowsReordered { get; set; }

    /// <summary>Raised when any button action is activated (row or header).</summary>
    [Parameter] public EventCallback<CloudGridActionEventArgs> OnActionClicked { get; set; }

    /// <summary>Explicit row actions merged with any from <see cref="Header"/>.</summary>
    [Parameter] public List<CloudGridAction>? RowActions { get; set; }

    /// <summary>Optional per-row filter; return false to hide an action on a specific row.</summary>
    [Parameter] public Func<CloudGridRow, CloudGridAction, bool>? ActionFilter { get; set; }

    /// <summary>Additional CSS class(es) appended to the root element.</summary>
    [Parameter] public string? CssClass { get; set; }

    /// <summary>Target of row links. Defaults to <c>_parent</c>.</summary>
    [Parameter] public string LinkTarget { get; set; } = "_parent";

    [Parameter] public string LoadingText { get; set; } = "retrieving data...";
    [Parameter] public string SearchingText { get; set; } = "searching...";

    /// <summary>Placeholder rendered for null cell values.</summary>
    [Parameter] public string EmptyCellText { get; set; } = "--";

    /// <summary>Fixed row height in pixels. Overrides the <c>--cloudgrid-row-height</c> CSS variable.</summary>
    [Parameter] public double? RowHeight { get; set; }

    /// <summary>Number of body rows the grid is sized for (also the page size sent to <see cref="DataProvider"/>).</summary>
    [Parameter] public int? RowsPerPage { get; set; }

    /// <summary>How the grid navigates between pages. Defaults to <see cref="CloudGridPagingMode.Pages"/>.</summary>
    [Parameter] public CloudGridPagingMode PagingMode { get; set; } = CloudGridPagingMode.Pages;

    /// <summary>Text of the load-more button when <see cref="PagingMode"/> is <see cref="CloudGridPagingMode.LoadMore"/>.</summary>
    [Parameter] public string LoadMoreText { get; set; } = "load more";

    /// <summary>Unmatched attributes (e.g. <c>data-*</c>) rendered on the root element.</summary>
    [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object>? AdditionalAttributes { get; set; }

    #endregion

    #region Data state

    private CloudGridDataResult? _data;
    private bool _loading;
    private bool _searching;
    private string? _search;
    private CloudGridSort? _sort;

    private int PageSize => RowsPerPage ?? 30;

    private string? DataMessage
    {
        get
        {
            if (_data?.ErrorMessage is { Length: > 0 } err)
                return err;

            if (_data != null && _data.Rows.Count == 0)
                return string.IsNullOrWhiteSpace(_search)
                    ? "No records."
                    : $"No records match \"{_search}\".";

            return null;
        }
    }

    protected override Task OnInitializedAsync() =>
        ExecuteAsync(page: 1, isAppend: false);

    /// <summary>Reloads from page 1, clearing any active search and sort.</summary>
    public Task ReloadAsync()
    {
        _sort = null;
        _search = null;
        _data = null;
        return ExecuteAsync(page: 1, isAppend: false);
    }

    private async Task ExecuteAsync(int page, bool isAppend, bool isSearch = false)
    {
        _searching = isSearch;
        _loading = !isSearch && !isAppend;
        await InvokeAsync(StateHasChanged);

        CloudGridDataRequest request = new()
        {
            Page = page,
            PageSize = PageSize,
            Search = _search,
            Sort = _sort,
            IsAppend = isAppend,
            Total = _data?.Total ?? 0
        };

        CloudGridDataResult? result = null;

        try
        {
            result = await DataProvider(request);
        }
        catch (Exception ex)
        {
            result = new CloudGridDataResult { ErrorMessage = ex.Message };
        }
        finally
        {
            if (result != null && isAppend && _data != null)
            {
                _data.Rows.AddRange(result.Rows);
                _data.Page = result.Page;
                _data.Total = result.Total;
                _data.ErrorMessage = result.ErrorMessage;
            }
            else
            {
                _data = result ?? new CloudGridDataResult { ErrorMessage = "Failed to load data." };
            }

            _loading = false;
            _searching = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private Task OnPageChanged(CloudGridPaginationType type)
    {
        if (_data == null) return Task.CompletedTask;

        if (type == CloudGridPaginationType.LeftArrow)
        {
            int prev = _data.Page - 1;
            return prev < 1 ? Task.CompletedTask : ExecuteAsync(prev, isAppend: false);
        }

        bool append = PagingMode is CloudGridPagingMode.InfiniteScroll or CloudGridPagingMode.LoadMore;
        return ExecuteAsync(_data.Page + 1, isAppend: append);
    }

    private Task OnSortChanged(CloudGridSort sort)
    {
        _sort = sort;
        _data = null;
        return ExecuteAsync(page: 1, isAppend: false);
    }

    private Task OnSearchChanged(string? query)
    {
        _search = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
        _sort = null;
        _data = null;
        return ExecuteAsync(page: 1, isAppend: false, isSearch: true);
    }

    private Task OnRefreshAsync() => ReloadAsync();

    #endregion

    #region Header

    /// <summary>
    /// Wraps <see cref="Header"/> so the grid always owns the search callback,
    /// regardless of what the caller passes in.
    /// </summary>
    private CloudGridHeaderOptions? ActiveHeader
    {
        get
        {
            if (Header == null) return null;

            return new CloudGridHeaderOptions
            {
                Label = Header.Label,
                Actions = Header.Actions,
                OnActionClicked = Header.OnActionClicked,
                AllowSearch = Header.AllowSearch,
                SearchDebounceMilliseconds = Header.SearchDebounceMilliseconds,
                OnSearchChanged = EventCallback.Factory.Create<string?>(this, OnSearchChanged),
                AllowRefresh = Header.AllowRefresh,
                OnRefresh = EventCallback.Factory.Create(this, OnRefreshAsync),
                ExtraActions = Header.ExtraActions
            };
        }
    }

    #endregion

    #region Selection

    private List<Guid> _selectedRecords = [];
    private List<Guid>? _lastSelectedRecordsParameter;

    protected override void OnParametersSet()
    {
        // Re-sync only when the parameter instance changes so user toggles survive re-renders.
        if (!ReferenceEquals(SelectedRecords, _lastSelectedRecordsParameter))
        {
            _lastSelectedRecordsParameter = SelectedRecords;
            _selectedRecords = SelectedRecords is null ? [] : [.. SelectedRecords];
        }
    }

    private async Task OnBodySelectionChanged(List<Guid> updated)
    {
        _selectedRecords = updated;
        await SelectedRecordsChanged.InvokeAsync(_selectedRecords);
    }

    #endregion

    #region Row actions

    private List<CloudGridAction> AllRowActions
    {
        get
        {
            IEnumerable<CloudGridAction> fromHeader = Header?.Actions.Where(a => a.ShowOnRow) ?? [];
            IEnumerable<CloudGridAction> explicit_ = RowActions ?? [];
            return [.. fromHeader, .. explicit_.Where(a => !fromHeader.Any(h => h.Key == a.Key))];
        }
    }

    private List<Guid> HeaderSelectedRecords => AllowSelection ? _selectedRecords : [];

    #endregion

    #region Body state (resize / drag adjectives on root)

    private bool _isResizing;
    private bool _isDragging;

    private async Task OnBodyResizingChanged(bool resizing)
    {
        _isResizing = resizing;
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnBodyDraggingChanged(bool dragging)
    {
        _isDragging = dragging;
        await InvokeAsync(StateHasChanged);
    }

    #endregion

    #region Footer

    private int FooterRangeStart => _data == null ? 0
        : PagingMode == CloudGridPagingMode.Pages ? (_data.PageSize * (_data.Page - 1)) + 1
        : 1;

    private int FooterRangeEnd => _data == null ? 0
        : PagingMode == CloudGridPagingMode.Pages ? (_data.PageSize * (_data.Page - 1)) + _data.Rows.Count
        : _data.Rows.Count;

    private bool FooterHasPreviousPage => _data != null && FooterRangeStart > 1;
    private bool FooterHasNextPage => _data != null && FooterRangeEnd < _data.Total;
    private int FooterSelectedCount => AllowSelection ? _selectedRecords.Count : 0;

    #endregion

    #region CSS

    private string RootClasses
    {
        get
        {
            List<string> classes = [];

            if (AllowSelection) classes.Add("_selectable");
            if (AllowReordering) classes.Add("_reorderable");
            if (_loading || _searching) classes.Add("_busy");
            if (_isResizing) classes.Add("_resizing");
            if (_isDragging) classes.Add("_rowdragging");
            if (!string.IsNullOrWhiteSpace(CssClass)) classes.Add(CssClass);

            return string.Join(' ', classes);
        }
    }

    private string? RootStyle =>
        RowHeight.HasValue ? $"--cloudgrid-row-height: {RowHeight.Value.ToString(CultureInfo.InvariantCulture)}px" : null;

    #endregion
}
