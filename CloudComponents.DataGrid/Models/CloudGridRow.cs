namespace AngryMonkey.CloudComponents.DataGrid.Models;

public class CloudDataGridRow
{
    public Guid Id { get; set; }

    /// <summary>
    /// Optional navigation link for the row. When null the row is not clickable.
    /// </summary>
    public string? Link { get; set; }

    /// <summary>
    /// Pre-resolved cell values, one per column. Null values are rendered as "--".
    /// </summary>
    public List<object?> Cells { get; set; } = [];

    /// <summary>Optional CSS class applied to the complete row.</summary>
    public string? CssClass { get; set; }

    /// <summary>Optional inline CSS applied to the complete row.</summary>
    public string? Style { get; set; }

    /// <summary>
    /// Optional viewport-anchored text shown beneath the row. It spans the visible grid width
    /// and remains at the left edge while columns scroll horizontally.
    /// </summary>
    public string? Note { get; set; }

    public string? NoteCssClass { get; set; }
    public string? NoteStyle { get; set; }

    /// <summary>Optional stable key used to group this row beneath a collapsible category header.</summary>
    public string? CategoryKey { get; set; }

    /// <summary>Optional display label for <see cref="CategoryKey"/>. Defaults to the key.</summary>
    public string? CategoryLabel { get; set; }

    /// <summary>
    /// Additional attributes (e.g. data-*) rendered on the row element.
    /// </summary>
    public Dictionary<string, object>? Attributes { get; set; }
}
