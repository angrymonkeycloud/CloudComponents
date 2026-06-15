using Microsoft.AspNetCore.Components;

namespace CloudComponents.Grid.Models;

/// <summary>
/// Options for the built-in <c>CloudGridHeader</c> rendered above a <c>CloudGrid</c>.
/// When this is passed to <see cref="Components.CloudGrid.Header"/> the header is shown;
/// leave it null to hide the header entirely.
/// </summary>
public class CloudGridHeaderOptions
{
    /// <summary>Title displayed on the left.</summary>
    public string? Label { get; set; }

    /// <summary>Optional link to open the full view. Hidden when null or empty.</summary>
    public string? ViewUrl { get; set; }

    /// <summary>Optional link for the "new" button. Hidden when null or empty.</summary>
    public string? NewUrl { get; set; }

    /// <summary>Text displayed on the "new" button.</summary>
    public string NewButtonText { get; set; } = "new";

    /// <summary>Whether to render the search button/box.</summary>
    public bool AllowSearch { get; set; } = true;

    /// <summary>Delay applied while typing before <see cref="OnSearchChanged"/> is raised.</summary>
    public int SearchDebounceMilliseconds { get; set; } = 300;

    /// <summary>
    /// Raised when the (debounced) search query changes. Null means the search was cleared.
    /// </summary>
    public EventCallback<string?> OnSearchChanged { get; set; }

    /// <summary>Optional extra action buttons rendered after the built-in search button.</summary>
    public RenderFragment? ExtraActions { get; set; }
}
