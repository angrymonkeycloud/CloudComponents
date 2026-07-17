using Microsoft.AspNetCore.Components;

namespace AngryMonkey.CloudComponents.DataGrid.Models;

/// <summary>
/// Optional rich cell value. Plain objects in <see cref="CloudDataGridRow.Cells"/> remain supported;
/// use this wrapper only when an individual cell needs styling, attributes, or a template.
/// </summary>
public class CloudDataGridCell
{
    public object? Value { get; set; }
    public string? CssClass { get; set; }
    public string? Style { get; set; }
    public Dictionary<string, object>? Attributes { get; set; }
    public RenderFragment<CloudDataGridCellContext>? Template { get; set; }
}

/// <summary>Context supplied to column- and cell-level Razor templates.</summary>
public sealed class CloudDataGridCellContext
{
    public required object? Value { get; init; }
    public CloudDataGridRow? Row { get; init; }
    public required CloudDataGridColumn Column { get; init; }
    public required int ColumnIndex { get; init; }
    public CloudDataGridCell? Cell { get; init; }
    public bool IsFooter { get; init; }
}
