using Microsoft.AspNetCore.Components;

namespace AngryMonkey.CloudComponents.Grid.Models;

/// <summary>
/// Defines a single interactive action rendered inside the <c>CloudGrid</c> toolbar or on a row.
///
/// <para><b>Types</b></para>
/// <list type="bullet">
///   <item><see cref="CloudGridActionType.Link"/> — renders an <c>&lt;a&gt;</c> navigating to <see cref="Href"/>.</item>
///   <item><see cref="CloudGridActionType.Button"/> — renders a <c>&lt;button&gt;</c> that fires <see cref="OnClicked"/>.</item>
///   <item><see cref="CloudGridActionType.Element"/> — renders a toggle button; when activated the button slot is replaced
///     by <see cref="ChildContent"/> with an automatic cancel button. Cancelling calls <see cref="OnDeactivated"/>.</item>
/// </list>
///
/// <para><b>Placement flags</b> (combinable)</para>
/// <list type="bullet">
///   <item><see cref="ShowOnHeader"/> — appears on the right side of the header toolbar (default <c>true</c>).</item>
///   <item><see cref="ShowOnBulkHeader"/> — appears on the left side of the header next to the label,
///     only visible while rows are selected (bulk operations).</item>
///   <item><see cref="ShowOnRow"/> — appears in each row's action cell.</item>
/// </list>
/// </summary>
public class CloudGridAction
{
    /// <summary>Stable identifier used to track state and surfaced in event args.</summary>
    public required string Key { get; set; }

    /// <summary>How the action is rendered and behaves. Defaults to <see cref="CloudGridActionType.Button"/>.</summary>
    public CloudGridActionType Type { get; set; } = CloudGridActionType.Button;

    /// <summary>Optional label text displayed inside the action element.</summary>
    public string? Text { get; set; }

    /// <summary>
    /// Icon rendered as a Razor component (preferred over <see cref="IconSvg"/>).
    /// When set, this takes precedence and the icon is rendered via Blazor's
    /// component pipeline so scoped CSS classes are applied correctly.
    /// </summary>
    public RenderFragment? Icon { get; set; }

    /// <summary>Optional raw inline SVG markup rendered as the action icon (legacy fallback).</summary>
    public string? IconSvg { get; set; }

    /// <summary>Optional tooltip (title attribute). Falls back to <see cref="Text"/> when not set.</summary>
    public string? Tooltip { get; set; }

    /// <summary>Additional CSS class(es) appended to the rendered element.</summary>
    public string? CssClass { get; set; }

    /// <summary>
    /// When <c>true</c> the action renders only its icon and never a text label,
    /// regardless of <see cref="Text"/> / <see cref="Tooltip"/>. Defaults to <c>false</c>.
    /// </summary>
    public bool IconOnly { get; set; }

    // ── Placement ────────────────────────────────────────────────────────────

    /// <summary>
    /// Renders this action on the right side of the header toolbar.
    /// Defaults to <c>true</c>; set to <c>false</c> to hide from the header.
    /// </summary>
    public bool ShowOnHeader { get; set; } = true;

    /// <summary>
    /// When <c>true</c> the action is placed inside the More (⋯) dropdown menu
    /// instead of being rendered as a direct icon button in the header toolbar.
    /// Implies <see cref="ShowOnHeader"/> is effectively ignored for direct rendering.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool ShowInMore { get; set; }

    /// <summary>
    /// Renders this action on the left side of the header (next to the label) as a bulk operation.
    /// The action is only visible while at least one row is selected.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool ShowOnBulkHeader { get; set; }

    /// <summary>
    /// Renders this action inside each row's action cell.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool ShowOnRow { get; set; }

    /// <summary>
    /// When <c>true</c> the action is hidden on a row until that row is selected.
    /// Only meaningful when <see cref="ShowOnRow"/> is also <c>true</c>.
    /// </summary>
    public bool VisibleOnSelectionOnly { get; set; }

    // ── Link ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Destination URL for <see cref="CloudGridActionType.Link"/> actions.
    /// </summary>
    public string? Href { get; set; }

    /// <summary>
    /// Anchor <c>target</c> attribute (e.g. <c>_blank</c>, <c>_parent</c>).
    /// Only used for <see cref="CloudGridActionType.Link"/> actions.
    /// </summary>
    public string? Target { get; set; }

    // ── Element ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Content rendered in-place of the button when the action is activated.
    /// Only used for <see cref="CloudGridActionType.Element"/> actions.
    /// </summary>
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Called when the user cancels an active <see cref="CloudGridActionType.Element"/> action
    /// (via the automatic cancel button). Use this to reset any state driven by the element
    /// (e.g. clearing a search query and notifying the parent).
    /// </summary>
    public Func<Task>? OnDeactivated { get; set; }

    /// <summary>
    /// Called by the header when an <see cref="CloudGridActionType.Element"/> action is activated.
    /// The argument is a <c>Func&lt;Task&gt;</c> that, when invoked, programmatically closes
    /// the focus panel (equivalent to the user clicking cancel).
    /// Use this to give child content a handle to self-close — e.g. an export button that
    /// closes the panel after triggering a download.
    /// </summary>
    public Action<Func<Task>>? OnActivated { get; set; }

    // ── Events ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Raised when the action is activated:
    /// <list type="bullet">
    ///   <item><see cref="CloudGridActionType.Button"/> — on click.</item>
    ///   <item><see cref="CloudGridActionType.Link"/> — not fired (browser navigation handles it).</item>
    ///   <item><see cref="CloudGridActionType.Element"/> — not fired (use <see cref="ChildContent"/> interactions).</item>
    /// </list>
    /// <para>
    /// When <see cref="ShowOnBulkHeader"/> is <c>true</c> the event args carry the ids of all
    /// currently selected rows. When <see cref="ShowOnRow"/> is <c>true</c> the args carry the
    /// single row's id. Both can be true simultaneously.
    /// </para>
    /// </summary>
    public EventCallback<CloudGridActionEventArgs> OnClicked { get; set; }
}

