namespace AngryMonkey.CloudComponents.Grid.Models;

/// <summary>
/// How the <c>CloudGrid</c> body is sized vertically.
/// </summary>
public enum CloudGridHeightMode
{
    /// <summary>
    /// The body is fixed to <c>RowsPerPage</c> rows tall
    /// (<c>height: calc(var(--cloudgrid-row-height) * RowsPerPage)</c>) and scrolls internally
    /// once content exceeds that height.
    /// </summary>
    RowHeight,

    /// <summary>
    /// Default. The grid fills the full height of its parent container: the header and footer
    /// stay fixed and only the row area between them scrolls. Requires the parent element to
    /// have a definite height (e.g. a fixed-height wrapper or a flex/grid item that stretches).
    /// </summary>
    FullHeight,

    /// <summary>
    /// The body grows to fit its content (no forced height, no internal scrollbar). An optional
    /// <c>MaxHeight</c> can be supplied to cap the grid's height, after which it scrolls
    /// internally like the other modes.
    /// </summary>
    Auto
}
