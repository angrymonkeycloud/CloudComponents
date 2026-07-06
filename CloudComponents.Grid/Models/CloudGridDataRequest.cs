namespace AngryMonkey.CloudComponents.Grid.Models;

/// <summary>
/// Describes the parameters the <c>CloudGrid</c> passes to its
/// <c>DataProvider</c> callback whenever it needs fresh data.
/// </summary>
public sealed class CloudGridDataRequest
{
    /// <summary>
    /// 1-based page number being requested.
    /// Always 1 on an initial load, search change, or sort change.
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Number of rows the grid is currently sized for (its page size).
    /// Pass this to the server's <c>Count</c> / <c>PageSize</c> parameter.
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Active search query, or <c>null</c> when the search box is empty.
    /// </summary>
    public string? Search { get; init; }

    /// <summary>
    /// The active sort, or <c>null</c> when the grid is in its natural order.
    /// </summary>
    public CloudGridSort? Sort { get; init; }

    /// <summary>
    /// <c>true</c> when the grid is in infinite-scroll / load-more mode and
    /// the request is for an additional page (rows should be appended).
    /// <c>false</c> on the first page or on a page-mode grid (rows replace).
    /// </summary>
    public bool IsAppend { get; init; }

    /// <summary>
    /// Total number of records known from the previous response.
    /// Zero on the first request. Useful when a sort is applied and the
    /// provider needs to fetch all records in one call.
    /// </summary>
    public int Total { get; init; }
}
