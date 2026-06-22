namespace CloudComponents.Grid.Models;

/// <summary>
/// Raised by <c>CloudGrid</c> after the user drags a row to a new position.
/// Contains the full row order so the consumer can persist it.
/// </summary>
public sealed class CloudGridRowReorder
{
    /// <summary>The id of the row that was dragged.</summary>
    public required Guid RecordId { get; init; }

    /// <summary>Zero-based index the row was moved from (within the loaded rows).</summary>
    public required int OldIndex { get; init; }

    /// <summary>Zero-based index the row was moved to (within the loaded rows).</summary>
    public required int NewIndex { get; init; }

    /// <summary>Every loaded row id in its new display order.</summary>
    public required List<Guid> OrderedRecordIds { get; init; }
}
