namespace CloudComponents.Grid.Models;

/// <summary>
/// Defines a custom action button rendered by <c>CloudGrid</c>, either on each
/// row, in the selection toolbar (bulk actions), or both. Buttons can show
/// text, an icon, or both.
/// </summary>
public class CloudGridRowButton
{
    /// <summary>Stable identifier surfaced in the <c>OnRowButtonClicked</c> callback.</summary>
    public required string Key { get; set; }

    /// <summary>Optional text displayed inside the button.</summary>
    public string? Text { get; set; }

    /// <summary>Optional raw inline SVG markup rendered as the button icon.</summary>
    public string? IconSvg { get; set; }

    /// <summary>Optional tooltip (title attribute). Falls back to <see cref="Text"/>.</summary>
    public string? Tooltip { get; set; }

    /// <summary>Additional CSS class(es) appended to the button element.</summary>
    public string? CssClass { get; set; }

    /// <summary>Renders the button on each row when true (default).</summary>
    public bool ShowOnRow { get; set; } = true;

    /// <summary>
    /// When true the button also renders in the selection toolbar and applies
    /// to every selected row at once (e.g. bulk delete).
    /// </summary>
    public bool AllowMultiple { get; set; }

    /// <summary>
    /// When true the button is only visible while at least one row is selected.
    /// Applies to both the row buttons and the selection toolbar.
    /// </summary>
    public bool VisibleOnSelectionOnly { get; set; }
}
