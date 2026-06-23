using Microsoft.AspNetCore.Components;

namespace CloudComponents.Grid.Models;

/// <summary>
/// Options for the built-in <c>CloudGridHeader</c> rendered above a <c>CloudGrid</c>.
/// When this is passed to <see cref="Components.CloudGrid.Header"/> the header is shown;
/// leave it null to hide the header entirely.
/// </summary>
public class CloudGridHeaderOptions
{
    /// <summary>Title displayed on the left of the header.</summary>
    public string? Label { get; set; }

    /// <summary>
    /// All header actions in display order.
    /// <list type="bullet">
    ///   <item>Actions with <see cref="CloudGridAction.ShowOnHeader"/> appear on the right.</item>
    ///   <item>Actions with <see cref="CloudGridAction.ShowOnBulkHeader"/> appear on the left
    ///     (next to the label) only while rows are selected.</item>
    /// </list>
    /// </summary>
    public List<CloudGridAction> Actions { get; set; } = [];

    /// <summary>
    /// Raised when any <see cref="CloudGridAction"/> of type <see cref="CloudGridActionType.Button"/>
    /// is clicked. The event args carry the action and any relevant record ids.
    /// </summary>
    public EventCallback<CloudGridActionEventArgs> OnActionClicked { get; set; }

    // ── Built-in helper shortcuts ────────────────────────────────────────────

    /// <summary>Adds the built-in Search element action when true.</summary>
    public bool AllowSearch { get; set; }

    /// <summary>Debounce delay in ms before <see cref="OnSearchChanged"/> fires.</summary>
    public int SearchDebounceMilliseconds { get; set; } = 300;

    /// <summary>Raised when the debounced search query changes. Null = cleared.</summary>
    public EventCallback<string?> OnSearchChanged { get; set; }

    /// <summary>Adds the built-in Refresh button when true. Defaults to <c>true</c>.</summary>
    public bool AllowRefresh { get; set; } = true;

    /// <summary>Raised when the built-in Refresh button is clicked.</summary>
    public EventCallback OnRefresh { get; set; }

    /// <summary>
    /// Adds the built-in Export action (behind the More ⋯ menu) when true.
    /// Clicking it opens an inline focus panel with Export current page / Export all /
    /// Export selection options and downloads a <c>.csv</c> file.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool AllowExport { get; set; } = true;

    /// <summary>
    /// Optional extra content rendered at the end of the right-side action slot row.
    /// Use for custom buttons (e.g. reorder save/cancel) that don't fit the
    /// <see cref="CloudGridAction"/> model.
    /// </summary>
    public RenderFragment? ExtraActions { get; set; }
}

