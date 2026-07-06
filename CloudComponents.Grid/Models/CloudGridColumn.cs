namespace AngryMonkey.CloudComponents.Grid.Models;

/// <summary>
/// Defines a single column of a <c>CloudGrid</c>.
/// </summary>
public class CloudGridColumn
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
}
