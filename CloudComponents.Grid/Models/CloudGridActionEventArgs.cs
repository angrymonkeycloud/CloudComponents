namespace CloudComponents.Grid.Models;

/// <summary>
/// Raised by <c>CloudGridHeader</c> or <c>CloudGridBody</c> when a <see cref="CloudGridAction"/> is activated.
/// </summary>
public sealed class CloudGridActionEventArgs
{
    /// <summary>The action that was activated.</summary>
    public required CloudGridAction Action { get; init; }

    /// <summary>
    /// The affected row ids.
    /// <list type="bullet">
    ///   <item>Row button click — single id of the clicked row.</item>
    ///   <item>Bulk header action — ids of every currently selected row.</item>
    ///   <item>Header-only action — empty.</item>
    /// </list>
    /// </summary>
    public List<Guid> RecordIds { get; init; } = [];
}
