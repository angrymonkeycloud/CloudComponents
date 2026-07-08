using AngryMonkey.CloudComponents.Editor.Models;

namespace AngryMonkey.CloudComponents.Editor.Services;

/// <summary>
/// Lightweight HTML fragment validator used by the <c>CloudEditor</c> code view.
/// Checks well-formedness (tag matching, attribute quoting, comment closure)
/// with HTML5 awareness for void elements and implied end tags, and reports
/// line/column positioned messages.
/// </summary>
public static class CloudEditorHtmlValidator
{
    private static readonly HashSet<string> VoidElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "area", "base", "br", "col", "embed", "hr", "img", "input",
        "link", "meta", "param", "source", "track", "wbr"
    };

    private static readonly HashSet<string> RawTextElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "script", "style"
    };

    /// <summary>Elements whose end tag may be omitted per the HTML5 spec (pragmatic subset).</summary>
    private static readonly HashSet<string> OptionalEndElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "p", "li", "dd", "dt", "option", "td", "th", "tr", "thead", "tbody", "tfoot"
    };

    /// <summary>Block-level elements whose start tag implicitly closes an open paragraph.</summary>
    private static readonly HashSet<string> ParagraphClosers = new(StringComparer.OrdinalIgnoreCase)
    {
        "address", "article", "aside", "blockquote", "details", "div", "dl", "fieldset",
        "figcaption", "figure", "footer", "form", "h1", "h2", "h3", "h4", "h5", "h6",
        "header", "hr", "main", "menu", "nav", "ol", "p", "pre", "section", "table", "ul"
    };

    /// <summary>Elements flagged with a warning because the editor strips or discourages them.</summary>
    private static readonly HashSet<string> DiscouragedElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "script", "style", "iframe"
    };

    /// <summary>Validates an HTML fragment and returns all findings.</summary>
    public static CloudEditorHtmlValidationResult Validate(string? html)
    {
        List<CloudEditorHtmlValidationMessage> messages = [];

        if (string.IsNullOrWhiteSpace(html))
            return new() { Messages = messages };

        int[] lineStarts = BuildLineStarts(html);
        List<OpenTag> stack = [];
        int index = 0;

        while (index < html.Length)
        {
            if (html[index] != '<')
            {
                index++;
                continue;
            }

            if (Matches(html, index, "<!--"))
            {
                int end = html.IndexOf("-->", index + 4, StringComparison.Ordinal);

                if (end < 0)
                {
                    AddMessage(messages, CloudEditorValidationSeverity.Error, lineStarts, index, "Comment is never closed. Expected '-->'.");
                    break;
                }

                index = end + 3;
                continue;
            }

            if (Matches(html, index, "<!"))
            {
                int end = html.IndexOf('>', index + 2);

                if (end < 0)
                {
                    AddMessage(messages, CloudEditorValidationSeverity.Error, lineStarts, index, "Markup declaration is never closed. Expected '>'.");
                    break;
                }

                index = end + 1;
                continue;
            }

            if (Matches(html, index, "</"))
            {
                index = ParseCloseTag(html, index, lineStarts, stack, messages);
                continue;
            }

            if (index + 1 < html.Length && char.IsAsciiLetter(html[index + 1]))
            {
                index = ParseOpenTag(html, index, lineStarts, stack, messages);
                continue;
            }

            AddMessage(messages, CloudEditorValidationSeverity.Warning, lineStarts, index, "Unescaped '<' treated as text. Use '&lt;' instead.");
            index++;
        }

        foreach (OpenTag open in stack.Where(tag => !OptionalEndElements.Contains(tag.Name)))
            messages.Add(new(CloudEditorValidationSeverity.Error, open.Line, open.Column, $"Element <{open.Name}> is never closed."));

        return new() { Messages = [.. messages.OrderBy(m => m.Line).ThenBy(m => m.Column)] };
    }

    private static int ParseOpenTag(string html, int start, int[] lineStarts, List<OpenTag> stack, List<CloudEditorHtmlValidationMessage> messages)
    {
        int index = start + 1;
        string name = ReadName(html, ref index);
        bool selfClosed = false;
        HashSet<string> seenAttributes = new(StringComparer.OrdinalIgnoreCase);

        while (true)
        {
            SkipWhitespace(html, ref index);

            if (index >= html.Length)
            {
                AddMessage(messages, CloudEditorValidationSeverity.Error, lineStarts, start, $"Tag <{name}> is never closed. Expected '>'.");
                return index;
            }

            if (html[index] == '>')
            {
                index++;
                break;
            }

            if (html[index] == '/' && index + 1 < html.Length && html[index + 1] == '>')
            {
                selfClosed = true;
                index += 2;
                break;
            }

            if (html[index] == '<')
            {
                AddMessage(messages, CloudEditorValidationSeverity.Error, lineStarts, start, $"Tag <{name}> is never closed. Expected '>'.");
                return index;
            }

            int attributeStart = index;
            string attributeName = ReadAttributeName(html, ref index);

            if (attributeName.Length == 0)
            {
                AddMessage(messages, CloudEditorValidationSeverity.Error, lineStarts, index, $"Unexpected character '{html[index]}' inside tag <{name}>.");
                index++;
                continue;
            }

            if (!seenAttributes.Add(attributeName))
                AddMessage(messages, CloudEditorValidationSeverity.Warning, lineStarts, attributeStart, $"Duplicate attribute '{attributeName}' on <{name}>.");

            if (attributeName.StartsWith("on", StringComparison.OrdinalIgnoreCase) && attributeName.Length > 2)
                AddMessage(messages, CloudEditorValidationSeverity.Warning, lineStarts, attributeStart, $"Inline event handler '{attributeName}' is not allowed and will be removed.");

            SkipWhitespace(html, ref index);

            if (index < html.Length && html[index] == '=')
            {
                index++;
                SkipWhitespace(html, ref index);

                if (index >= html.Length)
                {
                    AddMessage(messages, CloudEditorValidationSeverity.Error, lineStarts, attributeStart, $"Attribute '{attributeName}' has no value.");
                    return index;
                }

                if (html[index] is '"' or '\'')
                {
                    char quote = html[index];
                    int valueEnd = html.IndexOf(quote, index + 1);

                    if (valueEnd < 0)
                    {
                        AddMessage(messages, CloudEditorValidationSeverity.Error, lineStarts, index, $"Attribute '{attributeName}' value quote is never closed.");
                        return html.Length;
                    }

                    index = valueEnd + 1;
                }
                else
                {
                    int valueStart = index;

                    while (index < html.Length && !char.IsWhiteSpace(html[index]) && html[index] is not ('>' or '<' or '"' or '\''))
                        index++;

                    if (index == valueStart)
                        AddMessage(messages, CloudEditorValidationSeverity.Error, lineStarts, attributeStart, $"Attribute '{attributeName}' has no value.");
                }
            }
        }

        if (DiscouragedElements.Contains(name))
            AddMessage(messages, CloudEditorValidationSeverity.Warning, lineStarts, start, $"<{name}> is discouraged inside editor content and may be removed.");

        if (VoidElements.Contains(name) || selfClosed)
            return index;

        ApplyImpliedEndTags(stack, name);
        stack.Add(new(name, LineOf(lineStarts, start), ColumnOf(lineStarts, start)));

        if (RawTextElements.Contains(name))
        {
            int close = html.IndexOf($"</{name}", index, StringComparison.OrdinalIgnoreCase);

            if (close < 0)
            {
                AddMessage(messages, CloudEditorValidationSeverity.Error, lineStarts, start, $"Element <{name}> is never closed.");
                stack.RemoveAt(stack.Count - 1);
                return html.Length;
            }

            return close;
        }

        return index;
    }

    private static int ParseCloseTag(string html, int start, int[] lineStarts, List<OpenTag> stack, List<CloudEditorHtmlValidationMessage> messages)
    {
        int index = start + 2;
        string name = ReadName(html, ref index);
        int end = html.IndexOf('>', index);

        if (name.Length == 0)
        {
            AddMessage(messages, CloudEditorValidationSeverity.Error, lineStarts, start, "Malformed closing tag.");
            return end < 0 ? html.Length : end + 1;
        }

        if (end < 0)
        {
            AddMessage(messages, CloudEditorValidationSeverity.Error, lineStarts, start, $"Closing tag </{name}> is never closed. Expected '>'.");
            return html.Length;
        }

        index = end + 1;

        if (VoidElements.Contains(name))
        {
            AddMessage(messages, CloudEditorValidationSeverity.Warning, lineStarts, start, $"Void element <{name}> should not have a closing tag.");
            return index;
        }

        int matchIndex = stack.FindLastIndex(tag => tag.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (matchIndex < 0)
        {
            AddMessage(messages, CloudEditorValidationSeverity.Error, lineStarts, start, $"Closing tag </{name}> has no matching opening tag.");
            return index;
        }

        for (int i = stack.Count - 1; i > matchIndex; i--)
        {
            OpenTag unclosed = stack[i];

            if (!OptionalEndElements.Contains(unclosed.Name))
                messages.Add(new(CloudEditorValidationSeverity.Error, unclosed.Line, unclosed.Column, $"Element <{unclosed.Name}> is not closed before </{name}>."));

            stack.RemoveAt(i);
        }

        stack.RemoveAt(matchIndex);
        return index;
    }

    private static void ApplyImpliedEndTags(List<OpenTag> stack, string newTag)
    {
        while (stack.Count > 0)
        {
            string top = stack[^1].Name;

            bool implied =
                (top.Equals("p", StringComparison.OrdinalIgnoreCase) && ParagraphClosers.Contains(newTag)) ||
                (top.Equals("li", StringComparison.OrdinalIgnoreCase) && newTag.Equals("li", StringComparison.OrdinalIgnoreCase)) ||
                (top is "dd" or "dt" && newTag is "dd" or "dt") ||
                (top.Equals("option", StringComparison.OrdinalIgnoreCase) && newTag.Equals("option", StringComparison.OrdinalIgnoreCase)) ||
                ((top.Equals("td", StringComparison.OrdinalIgnoreCase) || top.Equals("th", StringComparison.OrdinalIgnoreCase)) && newTag is "td" or "th" or "tr") ||
                (top.Equals("tr", StringComparison.OrdinalIgnoreCase) && newTag.Equals("tr", StringComparison.OrdinalIgnoreCase));

            if (!implied)
                break;

            stack.RemoveAt(stack.Count - 1);
        }
    }

    private static string ReadName(string html, ref int index)
    {
        int start = index;

        while (index < html.Length && (char.IsAsciiLetterOrDigit(html[index]) || html[index] == '-'))
            index++;

        return html[start..index];
    }

    private static string ReadAttributeName(string html, ref int index)
    {
        int start = index;

        while (index < html.Length && !char.IsWhiteSpace(html[index]) && html[index] is not ('=' or '>' or '/' or '<' or '"' or '\''))
            index++;

        return html[start..index];
    }

    private static void SkipWhitespace(string html, ref int index)
    {
        while (index < html.Length && char.IsWhiteSpace(html[index]))
            index++;
    }

    private static bool Matches(string html, int index, string token) =>
        index + token.Length <= html.Length && html.AsSpan(index, token.Length).Equals(token, StringComparison.Ordinal);

    private static void AddMessage(List<CloudEditorHtmlValidationMessage> messages, CloudEditorValidationSeverity severity, int[] lineStarts, int index, string message) =>
        messages.Add(new(severity, LineOf(lineStarts, index), ColumnOf(lineStarts, index), message));

    private static int[] BuildLineStarts(string html)
    {
        List<int> starts = [0];

        for (int i = 0; i < html.Length; i++)
            if (html[i] == '\n')
                starts.Add(i + 1);

        return [.. starts];
    }

    private static int LineOf(int[] lineStarts, int index)
    {
        int line = Array.BinarySearch(lineStarts, index);
        return (line < 0 ? ~line - 1 : line) + 1;
    }

    private static int ColumnOf(int[] lineStarts, int index)
    {
        int line = Array.BinarySearch(lineStarts, index);
        int lineStart = lineStarts[line < 0 ? ~line - 1 : line];
        return index - lineStart + 1;
    }

    private sealed record OpenTag(string Name, int Line, int Column);
}
