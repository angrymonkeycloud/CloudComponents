namespace AngryMonkey.CloudComponents.DataGrid.Models;

/// <summary>Context supplied to category-header Razor templates.</summary>
public sealed class CloudDataGridCategoryContext
{
    public required string Key { get; init; }
    public required string Label { get; init; }
    public required int VisibleRowCount { get; init; }
    public required bool IsExpanded { get; init; }
}
