namespace AngryMonkey.CloudComponents.DataGrid.Models;

/// <summary>
/// Describes the active sort of a <c>CloudDataGrid</c>. Passed to the
/// <c>OnSortChanged</c> callback so the consumer can reload data sorted server side.
/// </summary>
public sealed class CloudDataGridSort
{
    /// <summary>The column being sorted.</summary>
    public required CloudDataGridColumn Column { get; init; }

    /// <summary>Zero-based index of the column within the grid's column list.</summary>
    public required int ColumnIndex { get; init; }

    /// <summary>The requested sort direction.</summary>
    public required CloudDataGridSortDirection Direction { get; init; }

    /// <summary>The column's <see cref="CloudDataGridColumn.Key"/>, falling back to its label.</summary>
    public string Key => Column.Key ?? Column.Label;
}
