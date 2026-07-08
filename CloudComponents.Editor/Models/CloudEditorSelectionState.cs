using System.Text.Json.Serialization;

namespace AngryMonkey.CloudComponents.Editor.Models;

/// <summary>
/// Snapshot of the formatting active at the current selection, pushed from
/// JavaScript whenever the caret moves so toolbar buttons can reflect state.
/// </summary>
public sealed class CloudEditorSelectionState
{
    [JsonPropertyName("bold")] public bool Bold { get; set; }
    [JsonPropertyName("italic")] public bool Italic { get; set; }
    [JsonPropertyName("underline")] public bool Underline { get; set; }
    [JsonPropertyName("strikethrough")] public bool Strikethrough { get; set; }
    [JsonPropertyName("subscript")] public bool Subscript { get; set; }
    [JsonPropertyName("superscript")] public bool Superscript { get; set; }
    [JsonPropertyName("inlineCode")] public bool InlineCode { get; set; }

    /// <summary>Lower-case tag of the current block, e.g. <c>p</c>, <c>h2</c>, <c>blockquote</c>, <c>pre</c>.</summary>
    [JsonPropertyName("blockTag")] public string BlockTag { get; set; } = "p";

    /// <summary>One of <c>left</c>, <c>center</c>, <c>right</c>, <c>justify</c>.</summary>
    [JsonPropertyName("alignment")] public string Alignment { get; set; } = "left";

    [JsonPropertyName("unorderedList")] public bool UnorderedList { get; set; }
    [JsonPropertyName("orderedList")] public bool OrderedList { get; set; }
    [JsonPropertyName("link")] public bool Link { get; set; }
    [JsonPropertyName("linkUrl")] public string? LinkUrl { get; set; }

    /// <summary>Plain text of the current selection (used to pre-fill the link dialog).</summary>
    [JsonPropertyName("selectedText")] public string SelectedText { get; set; } = string.Empty;
}
