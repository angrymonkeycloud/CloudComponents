namespace CloudComponents.Grid.Models;

/// <summary>
/// Raised by <c>CloudGrid</c> when a custom row/bulk button is clicked.
/// </summary>
public sealed class CloudGridRowButtonEventArgs
{
    /// <summary>The button that was clicked.</summary>
    public required CloudGridRowButton Button { get; init; }

    /// <summary>
    /// The affected row ids: a single id for a row button, or every selected
    /// id when the button was clicked from the selection toolbar.
    /// </summary>
    public required List<Guid> RecordIds { get; init; }
}
