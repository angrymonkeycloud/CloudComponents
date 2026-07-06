namespace AngryMonkey.CloudComponents.Grid.Models;

public class CloudGridRow
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

    /// <summary>
    /// Additional attributes (e.g. data-*) rendered on the row element.
    /// </summary>
    public Dictionary<string, object>? Attributes { get; set; }
}
