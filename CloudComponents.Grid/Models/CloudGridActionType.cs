namespace AngryMonkey.CloudComponents.Grid.Models;

/// <summary>
/// Determines how a <see cref="CloudGridAction"/> is rendered and what happens when it is activated.
/// </summary>
public enum CloudGridActionType
{
    /// <summary>
    /// Renders an anchor tag (<c>&lt;a&gt;</c>) navigating to <see cref="CloudGridAction.Href"/>.
    /// </summary>
    Link,

    /// <summary>
    /// Renders a button that fires <see cref="CloudGridAction.OnClicked"/> when pressed.
    /// </summary>
    Button,

    /// <summary>
    /// Renders a toggle button. When activated the button is replaced in-place by
    /// <see cref="CloudGridAction.ChildContent"/> with an automatic "cancel" button appended.
    /// Cancelling collapses the element and calls <see cref="CloudGridAction.OnDeactivated"/>.
    /// Only one Element per scope (header or row) can be active at a time.
    /// </summary>
    Element
}
