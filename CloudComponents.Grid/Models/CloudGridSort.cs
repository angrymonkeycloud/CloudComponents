namespace CloudComponents.Grid.Models;

/// <summary>
/// Describes the active sort of a <c>CloudGrid</c>. Passed to the
/// <c>OnSortChanged</c> callback so the consumer can reload data sorted server side.
/// </summary>
public sealed class CloudGridSort
{
    /// <summary>The column being sorted.</summary>
    public required CloudGridColumn Column { get; init; }

    /// <summary>Zero-based index of the column within the grid's column list.</summary>
    public required int ColumnIndex { get; init; }

    /// <summary>The requested sort direction.</summary>
    public required CloudGridSortDirection Direction { get; init; }

    /// <summary>The column's <see cref="CloudGridColumn.Key"/>, falling back to its label.</summary>
    public string Key => Column.Key ?? Column.Label;
}
