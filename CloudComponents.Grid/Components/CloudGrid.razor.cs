using CloudComponents.Grid.Models;
using Microsoft.AspNetCore.Components;
using System.Globalization;

namespace CloudComponents.Grid.Components;

/// <summary>
/// Single entry point for the full grid experience.
///
/// Orchestrates <see cref="CloudGridHeader"/> (optional), <see cref="CloudGridBody"/>
/// and <see cref="CloudGridFooter"/> internally. Developers only use this component
/// and configure each section through the corresponding parameters.
///
/// Owns: selection state, bulk action bar, root CSS adjectives, footer range maths.
/// All table / row / sorting / paging behavior lives in <see cref="CloudGridBody"/>.
/// </summary>
public partial class CloudGrid
{
    #region Parameters

    /// <summary>
    /// When set, renders a <see cref="CloudGridHeader"/> above the table with the
    /// provided options (title, search, new/view links, extra actions).
    /// Leave null to hide the header entirely.
    /// </summary>
    [Parameter] public CloudGridHeaderOptions? Header { get; set; }

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

    /// <summary>
    /// Renders a drag handle on each row letting the user re-arrange records when true.
    /// The new order is surfaced through <see cref="OnRowsReordered"/>.
    /// </summary>
    [Parameter] public bool AllowReordering { get; set; }

    /// <summary>Raised after the user drops a dragged row at a new position.</summary>
    [Parameter] public EventCallback<CloudGridRowReorder> OnRowsReordered { get; set; }

    /// <summary>
    /// Custom action buttons rendered on each row and/or in the selection toolbar.
    /// See <see cref="CloudGridRowButton"/> for per-button options.
    /// </summary>
    [Parameter] public List<CloudGridRowButton> RowButtons { get; set; } = [];

    /// <summary>
    /// Optional per-row filter deciding whether a button is rendered on a given row.
    /// Returning true keeps the button visible.
    /// </summary>
    [Parameter] public Func<CloudGridRow, CloudGridRowButton, bool>? RowButtonFilter { get; set; }

    /// <summary>Raised when a custom row/bulk button is clicked.</summary>
    [Parameter] public EventCallback<CloudGridRowButtonEventArgs> OnRowButtonClicked { get; set; }

    /// <summary>Additional CSS class(es) appended to the root element.</summary>
    [Parameter] public string? CssClass { get; set; }

    /// <summary>Target of row links (<c>_parent</c> by default).</summary>
    [Parameter] public string LinkTarget { get; set; } = "_parent";

    [Parameter] public string LoadingText { get; set; } = "retrieving data...";
    [Parameter] public string SearchingText { get; set; } = "searching...";

    /// <summary>Placeholder rendered for null cell values.</summary>
    [Parameter] public string EmptyCellText { get; set; } = "--";

    /// <summary>
    /// Fixed row height in pixels. Overrides the <c>--cloudgrid-row-height</c> CSS variable.
    /// </summary>
    [Parameter] public double? RowHeight { get; set; }

    /// <summary>Number of body rows the grid is sized for.</summary>
    [Parameter] public int? RowsPerPage { get; set; }

    /// <summary>How the grid navigates between pages. Defaults to <see cref="CloudGridPagingMode.Pages"/>.</summary>
    [Parameter] public CloudGridPagingMode PagingMode { get; set; } = CloudGridPagingMode.Pages;

    /// <summary>Text of the load-more button (<see cref="CloudGridPagingMode.LoadMore"/>).</summary>
    [Parameter] public string LoadMoreText { get; set; } = "load more";

    /// <summary>Raised when the user navigates between pages using the footer.</summary>
    [Parameter] public EventCallback<CloudGridPaginationType> OnPageChanged { get; set; }

    /// <summary>
    /// Raised when the user sorts a column. When no handler is attached the grid
    /// falls back to sorting the current page locally.
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

    private async Task OnBodySelectionChanged(List<Guid> updated)
    {
        _selectedRecords = updated;
        await SelectedRecordsChanged.InvokeAsync(_selectedRecords);
    }

    #endregion

    #region Bulk bar

    /// <summary>The selection toolbar shows when bulk buttons exist and rows are selected.</summary>
    private bool ShowBulkBar => AllowSelection && _selectedRecords.Count > 0 && RowButtons.Any(b => b.AllowMultiple);

    private IEnumerable<CloudGridRowButton> BulkButtons => RowButtons.Where(b => b.AllowMultiple);

    private Task BulkButtonClickAsync(CloudGridRowButton button) =>
        _selectedRecords.Count == 0
            ? Task.CompletedTask
            : OnRowButtonClicked.InvokeAsync(new CloudGridRowButtonEventArgs
            {
                Button = button,
                RecordIds = [.. _selectedRecords]
            });

    #endregion

    #region Body state callbacks (resize / drag adjectives on root div)

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

    #region Footer helpers

    private int FooterRangeStart => Data == null ? 0
        : PagingMode == CloudGridPagingMode.Pages ? (Data.PageSize * (Data.Page - 1)) + 1
        : 1;

    private int FooterRangeEnd => Data == null ? 0
        : PagingMode == CloudGridPagingMode.Pages ? (Data.PageSize * (Data.Page - 1)) + Data.Rows.Count
        : Data.Rows.Count;

    private bool FooterHasPreviousPage => Data != null && FooterRangeStart > 1;
    private bool FooterHasNextPage => Data != null && FooterRangeEnd < Data.Total;

    #endregion

    #region CSS helpers

    private string RootClass
    {
        get
        {
            List<string> classes = [];

            if (AllowSelection)
                classes.Add("_selectable");

            if (AllowReordering)
                classes.Add("_reorderable");

            if (IsLoading || IsSearching)
                classes.Add("_busy");

            if (_isResizing)
                classes.Add("_resizing");

            if (_isDragging)
                classes.Add("_rowdragging");

            if (!string.IsNullOrWhiteSpace(CssClass))
                classes.Add(CssClass);

            return string.Join(' ', classes);
        }
    }

    /// <summary>Inline CSS variables on the root element (currently the row height).</summary>
    private string? RootStyle =>
        RowHeight.HasValue ? $"--cloudgrid-row-height: {RowHeight.Value.ToString(CultureInfo.InvariantCulture)}px" : null;

    #endregion
}

