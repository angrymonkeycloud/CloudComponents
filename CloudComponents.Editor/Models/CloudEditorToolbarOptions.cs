namespace AngryMonkey.CloudComponents.Editor.Models;

/// <summary>
/// Feature toggles controlling which tool groups are rendered in the
/// <c>CloudEditor</c> toolbar. All features are enabled by default.
/// </summary>
public sealed class CloudEditorToolbarOptions
{
    /// <summary>Paragraph / heading block format dropdown.</summary>
    public bool Headings { get; set; } = true;

    /// <summary>Bold, italic, underline and strikethrough buttons.</summary>
    public bool BasicFormatting { get; set; } = true;

    /// <summary>Subscript and superscript buttons.</summary>
    public bool Script { get; set; } = true;

    /// <summary>Inline <c>&lt;code&gt;</c> toggle button.</summary>
    public bool InlineCode { get; set; } = true;

    /// <summary>Text color picker.</summary>
    public bool TextColor { get; set; } = true;

    /// <summary>Text highlight (background) color picker.</summary>
    public bool HighlightColor { get; set; } = true;

    /// <summary>Left / center / right / justify alignment buttons.</summary>
    public bool Alignment { get; set; } = true;

    /// <summary>Bulleted and numbered list buttons.</summary>
    public bool Lists { get; set; } = true;

    /// <summary>Increase / decrease indentation buttons.</summary>
    public bool Indentation { get; set; } = true;

    /// <summary>Blockquote toggle button.</summary>
    public bool Blockquote { get; set; } = true;

    /// <summary>Preformatted code block toggle button.</summary>
    public bool CodeBlock { get; set; } = true;

    /// <summary>Insert / remove hyperlink buttons.</summary>
    public bool Link { get; set; } = true;

    /// <summary>Insert image button (URL and file upload).</summary>
    public bool Image { get; set; } = true;

    /// <summary>Insert video button (YouTube, Vimeo or direct file URL).</summary>
    public bool Video { get; set; } = true;

    /// <summary>Insert horizontal rule button.</summary>
    public bool HorizontalRule { get; set; } = true;

    /// <summary>Clear formatting button.</summary>
    public bool ClearFormatting { get; set; } = true;

    /// <summary>Undo / redo buttons.</summary>
    public bool UndoRedo { get; set; } = true;

    /// <summary>HTML code view toggle button.</summary>
    public bool CodeView { get; set; } = true;

    /// <summary>Fullscreen toggle button.</summary>
    public bool Fullscreen { get; set; } = true;
}
