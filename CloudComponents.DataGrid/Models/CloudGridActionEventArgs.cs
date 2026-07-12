namespace AngryMonkey.CloudComponents.DataGrid.Models;

/// <summary>
/// Raised by <c>CloudDataGridHeader</c> or <c>CloudDataGridBody</c> when a <see cref="CloudDataGridAction"/> is activated.
/// </summary>
public sealed class CloudDataGridActionEventArgs
{
    /// <summary>The action that was activated.</summary>
    public required CloudDataGridAction Action { get; init; }

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
