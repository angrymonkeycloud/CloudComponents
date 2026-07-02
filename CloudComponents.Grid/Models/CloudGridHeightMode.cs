namespace CloudComponents.Grid.Models;

/// <summary>
/// How the <c>CloudGrid</c> body is sized vertically.
/// </summary>
public enum CloudGridHeightMode
{
    /// <summary>
    /// Default. The body is fixed to <c>RowsPerPage</c> rows tall
    /// (<c>height: calc(var(--cloudgrid-row-height) * RowsPerPage)</c>).
    /// </summary>
    RowHeight,

    /// <summary>
    /// The body fills the available space of its container (<c>height: 100%</c>).
    /// </summary>
    FullHeight,

    /// <summary>
    /// The body grows to fit its content (<c>height: auto</c>).
    /// </summary>
    Auto
}
