using AngryMonkey.CloudComponents.DataGrid.Models;
using Microsoft.AspNetCore.Components;

namespace AngryMonkey.CloudComponents.DataGrid.Components;

/// <summary>
/// Presentational footer bar for <see cref="CloudDataGrid"/>: shows the current range
/// ("1 - 20 out of 100") and, for <see cref="CloudDataGridPagingMode.Pages"/>, previous/next
/// page navigation buttons.
///
/// All state is owned by <see cref="CloudDataGrid"/>; this component only renders and
/// forwards button clicks via <see cref="OnPageChanged"/>.
/// </summary>
public partial class CloudDataGridFooter
{
    /// <summary>The current page of data.</summary>
    [Parameter] public CloudDataGridDataResult? Data { get; set; }

    /// <summary>How the grid navigates between pages.</summary>
    [Parameter] public CloudDataGridPagingMode PagingMode { get; set; } = CloudDataGridPagingMode.Pages;

    /// <summary>First row number shown in the range label.</summary>
    [Parameter] public int RangeStart { get; set; }

    /// <summary>Last row number shown in the range label.</summary>
    [Parameter] public int RangeEnd { get; set; }

    /// <summary>Whether a previous page exists.</summary>
    [Parameter] public bool HasPreviousPage { get; set; }

    /// <summary>Whether a next page exists.</summary>
    [Parameter] public bool HasNextPage { get; set; }

    /// <summary>How many rows are currently selected.</summary>
    [Parameter] public int SelectedCount { get; set; }

    /// <summary>Raised when the user clicks the previous or next page button.</summary>
    [Parameter] public EventCallback<CloudDataGridPaginationType> OnPageChanged { get; set; }

    private bool HasData => Data is { Rows.Count: > 0 };
    private bool ShowPager => HasData && PagingMode == CloudDataGridPagingMode.Pages;
    private bool CanGoPrevious => HasData && HasPreviousPage;
    private bool CanGoNext => HasData && HasNextPage;
    private string PageLabel => HasData ? $"Page {Data!.Page}" : "\u00A0";
    private string RangeLabel => HasData ? $"{RangeStart} - {RangeEnd} out of {Data!.Total}" : "\u00A0";

    private Task PreviousPageAsync() => OnPageChanged.InvokeAsync(CloudDataGridPaginationType.LeftArrow);
    private Task NextPageAsync() => OnPageChanged.InvokeAsync(CloudDataGridPaginationType.RightArrow);
}
