using Microsoft.AspNetCore.Components;

namespace AngryMonkey.CloudComponents.DataGrid.Models;

/// <summary>
/// Defines a single column of a <c>CloudDataGrid</c>.
/// </summary>
public class CloudDataGridColumn
{
    /// <summary>Text displayed in the column header.</summary>
    public required string Label { get; set; }

    /// <summary>
    /// Optional stable identifier for the column, surfaced in sort callbacks
    /// (e.g. a field schema name). Falls back to <see cref="Label"/> when null.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Initial column width in pixels. When <see cref="Resizable"/> is enabled the
    /// user can change the rendered width at runtime without affecting this value.
    /// </summary>
    public double Width { get; set; } = 100;

    /// <summary>Minimum width in pixels the column can be resized to.</summary>
    public double MinWidth { get; set; } = 60;

    /// <summary>Whether the user can sort the grid by clicking this column's header.</summary>
    public bool Sortable { get; set; } = true;

    /// <summary>Whether the user can resize this column by dragging its header edge.</summary>
    public bool Resizable { get; set; } = true;

    /// <summary>
    /// When true, cell values of this column are treated as image URLs and
    /// rendered as thumbnails instead of text.
    /// </summary>
    public bool IsImage { get; set; }

    /// <summary>Keeps the column visible at the left or right edge while scrolling horizontally.</summary>
    public CloudDataGridPinnedPosition Pinned { get; set; }

    /// <summary>CSS class applied to every body and column-footer cell in this column.</summary>
    public string? CssClass { get; set; }

    /// <summary>Inline CSS applied to every body and column-footer cell in this column.</summary>
    public string? Style { get; set; }

    /// <summary>CSS class applied only to this column's header cell.</summary>
    public string? HeaderCssClass { get; set; }

    /// <summary>Inline CSS applied only to this column's header cell.</summary>
    public string? HeaderStyle { get; set; }

    /// <summary>
    /// Optional Razor template for body cells. The context exposes the raw value, row,
    /// column and cell-level metadata, so a component can receive <c>context.Value</c>.
    /// </summary>
    public RenderFragment<CloudDataGridCellContext>? CellTemplate { get; set; }
}
