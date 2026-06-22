namespace CloudComponents.Grid.Models;

public class CloudGridDataResult
{
    public List<CloudGridRow> Rows { get; set; } = [];

    /// <summary>
    /// Current page number (1-based).
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of records per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of records across all pages.
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// Optional human-readable error or status message returned by the data provider.
    /// When non-null CloudGrid displays this instead of rows (unless IsLoading is true).
    /// </summary>
    public string? ErrorMessage { get; set; }
}
