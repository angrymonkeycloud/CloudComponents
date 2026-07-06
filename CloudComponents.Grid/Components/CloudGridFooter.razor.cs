using AngryMonkey.CloudComponents.Grid.Models;
using Microsoft.AspNetCore.Components;

namespace AngryMonkey.CloudComponents.Grid.Components;

/// <summary>
/// Presentational footer bar for <see cref="CloudGrid"/>: shows the current range
/// ("1 - 20 out of 100") and, for <see cref="CloudGridPagingMode.Pages"/>, previous/next
/// page navigation buttons.
///
/// All state is owned by <see cref="CloudGrid"/>; this component only renders and
/// forwards button clicks via <see cref="OnPageChanged"/>.
/// </summary>
public partial class CloudGridFooter
{
    /// <summary>The current page of data.</summary>
    [Parameter] public CloudGridDataResult? Data { get; set; }

    /// <summary>How the grid navigates between pages.</summary>
    [Parameter] public CloudGridPagingMode PagingMode { get; set; } = CloudGridPagingMode.Pages;

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
    [Parameter] public EventCallback<CloudGridPaginationType> OnPageChanged { get; set; }

    private bool HasData => Data is { Rows.Count: > 0 };
    private bool ShowPager => HasData && PagingMode == CloudGridPagingMode.Pages;
    private bool CanGoPrevious => HasData && HasPreviousPage;
    private bool CanGoNext => HasData && HasNextPage;
    private string PageLabel => HasData ? $"Page {Data!.Page}" : "\u00A0";
    private string RangeLabel => HasData ? $"{RangeStart} - {RangeEnd} out of {Data!.Total}" : "\u00A0";

    private Task PreviousPageAsync() => OnPageChanged.InvokeAsync(CloudGridPaginationType.LeftArrow);
    private Task NextPageAsync() => OnPageChanged.InvokeAsync(CloudGridPaginationType.RightArrow);
}
