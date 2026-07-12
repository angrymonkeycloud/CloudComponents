namespace AngryMonkey.CloudComponents.DataGrid.Models;

/// <summary>
/// Determines how a <see cref="CloudDataGridAction"/> is rendered and what happens when it is activated.
/// </summary>
public enum CloudDataGridActionType
{
    /// <summary>
    /// Renders an anchor tag (<c>&lt;a&gt;</c>) navigating to <see cref="CloudDataGridAction.Href"/>.
    /// </summary>
    Link,

    /// <summary>
    /// Renders a button that fires <see cref="CloudDataGridAction.OnClicked"/> when pressed.
    /// </summary>
    Button,

    /// <summary>
    /// Renders a toggle button. When activated the button is replaced in-place by
    /// <see cref="CloudDataGridAction.ChildContent"/> with an automatic "cancel" button appended.
    /// Cancelling collapses the element and calls <see cref="CloudDataGridAction.OnDeactivated"/>.
    /// Only one Element per scope (header or row) can be active at a time.
    /// </summary>
    Element
}
