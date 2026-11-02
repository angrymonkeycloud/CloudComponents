using AngryMonkey.CloudComponents.TextEditor.Models;
using AngryMonkey.CloudComponents.TextEditor.Services;

namespace CloudComponents.Tests.TextEditor;

public class HtmlValidatorTests
{
    private static CloudTextEditorHtmlValidationResult Validate(string? html) => CloudTextEditorHtmlValidator.Validate(html);

    private static string[] Errors(string html) => [.. Validate(html).Errors.Select(message => message.Message)];

    private static string[] Warnings(string html) => [.. Validate(html).Warnings.Select(message => message.Message)];

    // ----- Valid input --------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\r\n\t")]
    public void Nothing_to_validate_is_valid_and_has_no_messages(string? html)
    {
        CloudTextEditorHtmlValidationResult result = Validate(html);

        Assert.True(result.IsValid);
        Assert.Empty(result.Messages);
    }

    [Theory]
    [InlineData("plain text with no tags at all")]
    [InlineData("<p>Hello <strong>bold</strong> and <em>italic</em></p>")]
    [InlineData("<h1>Title</h1><p>Body</p>")]
    [InlineData("<ul><li>one</li><li>two</li></ul>")]
    [InlineData("<ol start=\"3\"><li>three</li></ol>")]
    [InlineData("<blockquote><p>quoted</p></blockquote>")]
    [InlineData("<pre><code>var x = 1;</code></pre>")]
    [InlineData("<a href=\"https://example.com\" target=\"_blank\">link</a>")]
    [InlineData("<table><thead><tr><th>A</th></tr></thead><tbody><tr><td>1</td></tr></tbody></table>")]
    [InlineData("<figure class=\"cloudeditor-embed\"><iframe src=\"https://player.vimeo.com/video/1\" allowfullscreen></iframe></figure>")]
    [InlineData("<DIV><P>Upper</P></DIV>")]
    [InlineData("<my-element data-x=\"1\">custom</my-element>")]
    public void Well_formed_html_is_valid(string html)
    {
        CloudTextEditorHtmlValidationResult result = Validate(html);

        Assert.True(result.IsValid, string.Join("; ", result.Messages));
    }

    [Theory]
    [InlineData("<br>")]
    [InlineData("<br/>")]
    [InlineData("<br />")]
    [InlineData("<hr>")]
    [InlineData("<img src=\"a.png\" alt=\"A\">")]
    [InlineData("<img src=\"a.png\"/>")]
    [InlineData("<p>line<br>break</p>")]
    [InlineData("<input type=\"checkbox\" disabled>")]
    public void Void_elements_need_no_closing_tag(string html)
    {
        Assert.True(Validate(html).IsValid);
    }

    [Fact]
    public void A_self_closed_non_void_element_is_accepted()
    {
        Assert.True(Validate("<div/><p>after</p>").IsValid);
    }

    [Theory]
    [InlineData("<p>one<p>two")]
    [InlineData("<p>text<div>block</div>")]
    [InlineData("<ul><li>a<li>b</ul>")]
    [InlineData("<dl><dt>t<dd>d<dt>t2<dd>d2</dl>")]
    [InlineData("<table><tr><td>a<td>b<tr><td>c</table>")]
    [InlineData("<select><option>a<option>b</select>")]
    public void Elements_whose_end_tag_is_optional_may_be_left_open(string html)
    {
        CloudTextEditorHtmlValidationResult result = Validate(html);

        Assert.True(result.IsValid, string.Join("; ", result.Messages));
    }

    [Theory]
    [InlineData("<a href=https://example.com>link</a>")]
    [InlineData("<a href='single'>link</a>")]
    [InlineData("<a href = \"spaced\">link</a>")]
    [InlineData("<video controls src=\"a.mp4\"></video>")]
    [InlineData("<p title=\"a > b\">gt inside a quoted value</p>")]
    [InlineData("<p title=\"x\" class=\"y\">two</p>")]
    public void Attribute_syntax_variants_are_accepted(string html)
    {
        CloudTextEditorHtmlValidationResult result = Validate(html);

        Assert.True(result.IsValid, string.Join("; ", result.Messages));
    }

    [Fact]
    public void Comments_and_doctypes_are_skipped()
    {
        CloudTextEditorHtmlValidationResult result = Validate("<!DOCTYPE html><!-- <div> unclosed inside a comment --><p>ok</p>");

        Assert.True(result.IsValid);
        Assert.Empty(result.Messages);
    }

    // ----- Errors -------------------------------------------------------------------------

    [Fact]
    public void An_element_that_is_never_closed_is_an_error_at_its_opening_tag()
    {
        CloudTextEditorHtmlValidationResult result = Validate("<div>text");

        Assert.False(result.IsValid);
        CloudTextEditorHtmlValidationMessage error = Assert.Single(result.Errors);
        Assert.Equal("Element <div> is never closed.", error.Message);
        Assert.Equal((1, 1), (error.Line, error.Column));
    }

    [Fact]
    public void An_inline_element_left_open_by_its_parent_closing_is_an_error()
    {
        string[] errors = Errors("<div><span>text</div>");

        Assert.Equal(["Element <span> is not closed before </div>."], errors);
    }

    [Fact]
    public void Crossed_tags_report_both_the_unclosed_and_the_stray_closing_tag()
    {
        string[] errors = Errors("<b><i>text</b></i>");

        Assert.Equal(["Element <i> is not closed before </b>.", "Closing tag </i> has no matching opening tag."], errors);
    }

    [Fact]
    public void A_closing_tag_without_an_opening_tag_is_an_error()
    {
        Assert.Equal(["Closing tag </p> has no matching opening tag."], Errors("text</p>"));
    }

    [Fact]
    public void A_closing_tag_is_matched_ignoring_case()
    {
        Assert.True(Validate("<Div>x</dIV>").IsValid);
    }

    [Theory]
    [InlineData("</>", "Malformed closing tag.")]
    [InlineData("<p>x</p", "Closing tag </p> is never closed. Expected '>'.")]
    public void A_malformed_closing_tag_is_an_error(string html, string expected)
    {
        Assert.Contains(expected, Errors(html));
    }

    [Fact]
    public void An_unterminated_comment_is_an_error_and_stops_the_scan()
    {
        CloudTextEditorHtmlValidationResult result = Validate("<p>a</p><!-- never ends <div>");

        Assert.Equal(["Comment is never closed. Expected '-->'."], result.Errors.Select(error => error.Message));
        Assert.Equal(9, result.Errors.Single().Column);
    }

    [Fact]
    public void An_unterminated_markup_declaration_is_an_error()
    {
        Assert.Equal(["Markup declaration is never closed. Expected '>'."], Errors("<!DOCTYPE html"));
    }

    [Theory]
    [InlineData("<p")]
    [InlineData("<p class=\"a\"")]
    [InlineData("<p <b>text</b>")]
    public void A_start_tag_that_never_ends_is_an_error(string html)
    {
        Assert.Contains("Tag <p> is never closed. Expected '>'.", Errors(html));
    }

    [Fact]
    public void An_attribute_quote_that_is_never_closed_is_an_error()
    {
        Assert.Contains("Attribute 'href' value quote is never closed.", Errors("<a href=\"https://example.com>link</a>"));
    }

    [Theory]
    [InlineData("<a href=>x</a>")]
    [InlineData("<a href=")]
    public void An_attribute_with_an_equals_sign_but_no_value_is_an_error(string html)
    {
        Assert.Contains("Attribute 'href' has no value.", Errors(html));
    }

    [Fact]
    public void A_stray_character_inside_a_tag_is_an_error()
    {
        Assert.Contains("Unexpected character '\"' inside tag <p>.", Errors("<p \"x\">text</p>"));
    }

    [Fact]
    public void An_unclosed_script_is_an_error_as_well_as_a_warning()
    {
        CloudTextEditorHtmlValidationResult result = Validate("<script>var a = 1;");

        Assert.Equal(["Element <script> is never closed."], result.Errors.Select(error => error.Message));
        Assert.Single(result.Warnings);
    }

    [Fact]
    public void Several_problems_are_all_reported()
    {
        string[] errors = Errors("<div><span></p>");

        Assert.Equal(3, errors.Length);
    }

    // ----- Warnings -----------------------------------------------------------------------

    [Theory]
    [InlineData("<script>alert(1)</script>", "<script> is discouraged inside editor content and may be removed.")]
    [InlineData("<style>p{}</style>", "<style> is discouraged inside editor content and may be removed.")]
    [InlineData("<iframe src=\"x\"></iframe>", "<iframe> is discouraged inside editor content and may be removed.")]
    public void Active_or_embedding_elements_are_warnings_not_errors(string html, string expected)
    {
        CloudTextEditorHtmlValidationResult result = Validate(html);

        Assert.True(result.IsValid);
        Assert.Equal([expected], result.Warnings.Select(warning => warning.Message));
    }

    [Fact]
    public void The_content_of_script_and_style_is_not_parsed_as_markup()
    {
        CloudTextEditorHtmlValidationResult result = Validate("<script>if (a < b && c > d) { document.write('<p>'); }</script>");

        Assert.True(result.IsValid);
        Assert.Single(result.Messages);
    }

    [Theory]
    [InlineData("<p onclick=\"x()\">a</p>", "onclick")]
    [InlineData("<img src=\"a.png\" ONERROR=\"x()\">", "ONERROR")]
    public void Inline_event_handlers_are_warnings(string html, string attribute)
    {
        CloudTextEditorHtmlValidationResult result = Validate(html);

        Assert.True(result.IsValid);
        Assert.Equal([$"Inline event handler '{attribute}' is not allowed and will be removed."], result.Warnings.Select(warning => warning.Message));
    }

    [Fact]
    public void An_attribute_named_just_on_is_not_an_event_handler()
    {
        Assert.Empty(Warnings("<p on=\"1\">a</p>"));
    }

    [Fact]
    public void A_duplicate_attribute_is_a_warning_ignoring_case()
    {
        Assert.Equal(["Duplicate attribute 'HREF' on <a>."], Warnings("<a href=\"1\" HREF=\"2\">x</a>"));
    }

    [Fact]
    public void A_closing_tag_on_a_void_element_is_a_warning()
    {
        CloudTextEditorHtmlValidationResult result = Validate("<p>a<br></br>b</p>");

        Assert.True(result.IsValid);
        Assert.Equal(["Void element <br> should not have a closing tag."], result.Warnings.Select(warning => warning.Message));
    }

    [Theory]
    [InlineData("a < b")]
    [InlineData("1 <2")]
    [InlineData("x <")]
    [InlineData("< p>")]
    public void A_less_than_sign_that_does_not_start_a_tag_is_a_warning(string html)
    {
        CloudTextEditorHtmlValidationResult result = Validate(html);

        Assert.True(result.IsValid);
        Assert.Equal(["Unescaped '<' treated as text. Use '&lt;' instead."], result.Warnings.Select(warning => warning.Message));
    }

    // ----- Positions and result members ---------------------------------------------------

    [Fact]
    public void Messages_carry_one_based_line_and_column_numbers()
    {
        CloudTextEditorHtmlValidationResult result = Validate("<p>first</p>\n  <div>\n<span>x</span>");

        CloudTextEditorHtmlValidationMessage error = Assert.Single(result.Errors);
        Assert.Equal((2, 3), (error.Line, error.Column));
    }

    [Fact]
    public void Windows_line_endings_count_one_line_each()
    {
        CloudTextEditorHtmlValidationResult result = Validate("<p>a</p>\r\n<p>b</p>\r\n<div>");

        Assert.Equal(3, result.Errors.Single().Line);
        Assert.Equal(1, result.Errors.Single().Column);
    }

    [Fact]
    public void Messages_are_returned_in_document_order()
    {
        CloudTextEditorHtmlValidationResult result = Validate("<div>\n<p onclick=\"x\">a</p>\n</span>\n<script>");

        int[] lines = [.. result.Messages.Select(message => message.Line)];

        Assert.Equal(lines.Order(), lines);
        Assert.True(result.Messages.Count >= 4);
    }

    [Fact]
    public void Is_valid_is_false_only_when_there_is_an_error()
    {
        Assert.True(Validate("<script>x</script>").IsValid);
        Assert.False(Validate("<div>").IsValid);
        Assert.False(Validate("<script>x</script><div>").IsValid);
    }

    [Fact]
    public void Errors_and_warnings_partition_the_messages()
    {
        CloudTextEditorHtmlValidationResult result = Validate("<div onclick=\"x\"><script>1</script>");

        Assert.Equal(result.Messages.Count, result.Errors.Count() + result.Warnings.Count());
        Assert.All(result.Errors, message => Assert.Equal(CloudTextEditorValidationSeverity.Error, message.Severity));
        Assert.All(result.Warnings, message => Assert.Equal(CloudTextEditorValidationSeverity.Warning, message.Severity));
    }

    [Fact]
    public void A_message_prints_its_severity_position_and_text()
    {
        CloudTextEditorHtmlValidationMessage message = new(CloudTextEditorValidationSeverity.Error, 3, 8, "Element <div> is never closed.");

        Assert.Equal("Error (3,8): Element <div> is never closed.", message.ToString());
        Assert.Equal("Warning (1,1): x", new CloudTextEditorHtmlValidationMessage(CloudTextEditorValidationSeverity.Warning, 1, 1, "x").ToString());
    }

    [Fact]
    public void A_new_result_is_valid_and_empty()
    {
        CloudTextEditorHtmlValidationResult result = new();

        Assert.True(result.IsValid);
        Assert.Empty(result.Messages);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void A_very_long_document_is_validated_without_trouble()
    {
        string html = string.Concat(Enumerable.Repeat("<p>paragraph <strong>bold</strong> text</p>\n", 5000));

        Assert.True(Validate(html).IsValid);
    }
}
