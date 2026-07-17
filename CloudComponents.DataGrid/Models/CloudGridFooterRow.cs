namespace AngryMonkey.CloudComponents.DataGrid.Models;

/// <summary>A summary row rendered beneath the data rows and aligned with the grid columns.</summary>
public class CloudDataGridFooterRow
{
    /// <summary>Cells in column order. Use empty cells to preserve alignment.</summary>
    public List<object?> Cells { get; set; } = [];
    public string? CssClass { get; set; }
    public string? Style { get; set; }
    public Dictionary<string, object>? Attributes { get; set; }
}
