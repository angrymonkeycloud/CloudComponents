namespace CloudComponents.Grid.Models;

/// <summary>
/// How <c>CloudGrid</c> navigates between pages of data.
/// </summary>
public enum CloudGridPagingMode
{
    /// <summary>
    /// Footer pager with previous/next buttons. Each page replaces the rows.
    /// </summary>
    Pages,

    /// <summary>
    /// A "load more" button is rendered after the last row. Clicking it raises
    /// <c>OnPageChanged</c> with <c>RightArrow</c>; the consumer should append
    /// the next page's rows to <c>Data.Rows</c>.
    /// </summary>
    LoadMore,

    /// <summary>
    /// The next page is requested automatically when the user scrolls near the
    /// end of the loaded rows (no button, no custom JavaScript). The consumer
    /// should append the next page's rows to <c>Data.Rows</c>.
    /// </summary>
    InfiniteScroll
}
