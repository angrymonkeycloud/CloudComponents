using AngryMonkey.CloudComponents.TextEditor.Components;
using AngryMonkey.CloudComponents.TextEditor.Models;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace CloudComponents.Tests.TextEditor;

public class EditorToolbarTests : EditorTestBase
{
    /// <summary>The buttons each toolbar option owns, by their title. Headings owns the block-format select instead.</summary>
    private static readonly Dictionary<string, string[]> ButtonsByOption = new()
    {
        [nameof(CloudTextEditorToolbarOptions.Headings)] = [],
        [nameof(CloudTextEditorToolbarOptions.BasicFormatting)] = ["Bold (Ctrl+B)", "Italic (Ctrl+I)", "Underline (Ctrl+U)", "Strikethrough"],
        [nameof(CloudTextEditorToolbarOptions.Script)] = ["Subscript", "Superscript"],
        [nameof(CloudTextEditorToolbarOptions.InlineCode)] = ["Inline code"],
        [nameof(CloudTextEditorToolbarOptions.TextColor)] = ["Text color"],
        [nameof(CloudTextEditorToolbarOptions.HighlightColor)] = ["Highlight color"],
        [nameof(CloudTextEditorToolbarOptions.Alignment)] = ["Align left", "Align center", "Align right", "Justify"],
        [nameof(CloudTextEditorToolbarOptions.Lists)] = ["Bulleted list", "Numbered list"],
        [nameof(CloudTextEditorToolbarOptions.Indentation)] = ["Decrease indent", "Increase indent"],
        [nameof(CloudTextEditorToolbarOptions.Blockquote)] = ["Blockquote"],
        [nameof(CloudTextEditorToolbarOptions.CodeBlock)] = ["Code block"],
        [nameof(CloudTextEditorToolbarOptions.Link)] = ["Insert link (Ctrl+K)", "Remove link"],
        [nameof(CloudTextEditorToolbarOptions.Image)] = ["Insert image"],
        [nameof(CloudTextEditorToolbarOptions.Video)] = ["Insert video"],
        [nameof(CloudTextEditorToolbarOptions.HorizontalRule)] = ["Horizontal rule"],
        [nameof(CloudTextEditorToolbarOptions.ClearFormatting)] = ["Clear formatting"],
        [nameof(CloudTextEditorToolbarOptions.UndoRedo)] = ["Undo (Ctrl+Z)", "Redo (Ctrl+Y)"],
        [nameof(CloudTextEditorToolbarOptions.CodeView)] = ["Code view"],
        [nameof(CloudTextEditorToolbarOptions.Fullscreen)] = ["Fullscreen"],
    };

    public static IEnumerable<object[]> OptionNames => ButtonsByOption.Keys.Select(name => new object[] { name });

    private static CloudTextEditorToolbarOptions Without(params string[] options)
    {
        CloudTextEditorToolbarOptions toolbar = new();

        foreach (string option in options)
            typeof(CloudTextEditorToolbarOptions).GetProperty(option)!.SetValue(toolbar, false);

        return toolbar;
    }

    // ----- Which tools are rendered -------------------------------------------------------

    [Fact]
    public void The_test_table_covers_every_toolbar_option()
    {
        string[] real = [.. typeof(CloudTextEditorToolbarOptions).GetProperties().Select(property => property.Name)];

        Assert.Equal(real.Order(), ButtonsByOption.Keys.Order());
    }

    [Fact]
    public void The_default_toolbar_shows_every_tool()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();

        string[] expected = [.. ButtonsByOption.Values.SelectMany(titles => titles)];

        Assert.Equal(expected.Order(), ToolbarTitles(cut).Order());
        Assert.Single(cut.FindAll("select[title='Block format']"));
    }

    [Theory]
    [MemberData(nameof(OptionNames))]
    public void Switching_an_option_off_removes_exactly_its_tools(string option)
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor(p => p.Add(x => x.Toolbar, Without(option)));

        string[] expected = [.. ButtonsByOption.Where(pair => pair.Key != option).SelectMany(pair => pair.Value)];

        Assert.Equal(expected.Order(), ToolbarTitles(cut).Order());
        Assert.Equal(option != nameof(CloudTextEditorToolbarOptions.Headings), cut.FindAll("select[title='Block format']").Count == 1);
    }

    [Fact]
    public void A_toolbar_with_everything_off_renders_no_tools_but_keeps_the_editing_surface()
    {
        string[] all = [.. ButtonsByOption.Keys];

        IRenderedComponent<CloudTextEditor> cut = RenderEditor(p => p.Add(x => x.Toolbar, Without(all)));

        Assert.Empty(cut.FindAll(".cloudeditor-toolbar button"));
        Assert.Empty(cut.FindAll(".cloudeditor-toolbar select"));
        Assert.Single(cut.FindAll(".cloudeditor-content"));
    }

    [Fact]
    public void A_tool_group_is_not_rendered_when_all_of_its_tools_are_off()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor(p => p.Add(x => x.Toolbar, Without(
            nameof(CloudTextEditorToolbarOptions.Headings), nameof(CloudTextEditorToolbarOptions.BasicFormatting), nameof(CloudTextEditorToolbarOptions.Script),
            nameof(CloudTextEditorToolbarOptions.InlineCode), nameof(CloudTextEditorToolbarOptions.TextColor), nameof(CloudTextEditorToolbarOptions.HighlightColor),
            nameof(CloudTextEditorToolbarOptions.Alignment), nameof(CloudTextEditorToolbarOptions.Lists), nameof(CloudTextEditorToolbarOptions.Indentation),
            nameof(CloudTextEditorToolbarOptions.Blockquote), nameof(CloudTextEditorToolbarOptions.CodeBlock), nameof(CloudTextEditorToolbarOptions.Link),
            nameof(CloudTextEditorToolbarOptions.Image), nameof(CloudTextEditorToolbarOptions.Video), nameof(CloudTextEditorToolbarOptions.HorizontalRule),
            nameof(CloudTextEditorToolbarOptions.ClearFormatting), nameof(CloudTextEditorToolbarOptions.CodeView), nameof(CloudTextEditorToolbarOptions.Fullscreen))));

        Assert.Equal(["Undo (Ctrl+Z)", "Redo (Ctrl+Y)"], ToolbarTitles(cut));
        Assert.Single(cut.FindAll(".cloudeditor-group"));
    }

    // ----- Commands -----------------------------------------------------------------------

    [Theory]
    [InlineData("Undo (Ctrl+Z)", "undo")]
    [InlineData("Redo (Ctrl+Y)", "redo")]
    [InlineData("Bold (Ctrl+B)", "bold")]
    [InlineData("Italic (Ctrl+I)", "italic")]
    [InlineData("Underline (Ctrl+U)", "underline")]
    [InlineData("Strikethrough", "strikeThrough")]
    [InlineData("Subscript", "subscript")]
    [InlineData("Superscript", "superscript")]
    [InlineData("Align left", "justifyLeft")]
    [InlineData("Align center", "justifyCenter")]
    [InlineData("Align right", "justifyRight")]
    [InlineData("Justify", "justifyFull")]
    [InlineData("Bulleted list", "insertUnorderedList")]
    [InlineData("Numbered list", "insertOrderedList")]
    [InlineData("Decrease indent", "outdent")]
    [InlineData("Increase indent", "indent")]
    [InlineData("Horizontal rule", "insertHorizontalRule")]
    [InlineData("Clear formatting", "removeFormat")]
    public async Task A_formatting_button_runs_its_editing_command(string title, string command)
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();

        await Select(cut, title);

        Assert.Equal(command, OnlyCall("exec")[0]);
    }

    [Theory]
    [InlineData("Blockquote", "blockquote")]
    [InlineData("Code block", "pre")]
    public async Task The_block_buttons_toggle_their_block_tag(string title, string tag)
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();

        await Select(cut, title);

        Assert.Equal(tag, OnlyCall("toggleBlock")[0]);
    }

    [Fact]
    public async Task The_inline_code_button_toggles_inline_code()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();

        await Select(cut, "Inline code");

        Assert.Single(Js.ControllerCalls("toggleInlineCode"));
    }

    [Fact]
    public async Task Remove_link_is_only_available_on_a_link()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();
        Assert.True(IsDisabled(Button(cut, "Remove link")));

        await PushState(cut, state => { state.Link = true; state.LinkUrl = "https://example.com"; });

        Assert.False(IsDisabled(Button(cut, "Remove link")));

        await Select(cut, "Remove link");

        Assert.Single(Js.ControllerCalls("unlink"));
    }

    [Fact]
    public void The_buttons_keep_the_editor_selection_when_pressed()
    {
        // Pressing a toolbar button must not take focus from the editing surface (mousedown is default-prevented).
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();

        Assert.All(cut.FindAll(".cloudeditor-toolbar button.cloudeditor-btn"),
            button => Assert.Contains(button.Attributes, attribute => attribute.Name.Contains("mousedown", StringComparison.Ordinal)));
    }

    // ----- Read-only and code view lock the commands --------------------------------------

    [Fact]
    public async Task A_read_only_editor_disables_every_tool_except_fullscreen()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor(p => p.Add(x => x.ReadOnly, true));

        IEnumerable<string> enabled = cut.FindAll(".cloudeditor-toolbar button").Where(button => !IsDisabled(button)).Select(button => button.GetAttribute("title")!);

        Assert.Equal(["Fullscreen"], enabled);
        Assert.True(cut.Find("select[title='Block format']").HasAttribute("disabled"));

        await Button(cut, "Bold (Ctrl+B)").ClickAsync(new());
        Assert.Empty(Js.ControllerCalls("exec"));
    }

    [Fact]
    public async Task A_read_only_editor_does_not_run_link_shortcuts()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor(p => p.Add(x => x.ReadOnly, true));

        await cut.InvokeAsync(() => cut.Instance.HandleLinkShortcutAsync());

        Assert.Empty(cut.FindAll(".cloudeditor-dialog"));
        Assert.Empty(Js.ControllerCalls("saveSelection"));
    }

    [Fact]
    public void The_content_surface_is_editable_unless_read_only()
    {
        Assert.Equal("true", RenderEditor().Find(".cloudeditor-content").GetAttribute("contenteditable"));
        Assert.Equal("false", RenderEditor(p => p.Add(x => x.ReadOnly, true)).Find(".cloudeditor-content").GetAttribute("contenteditable"));
    }

    // ----- Reflecting the selection -------------------------------------------------------

    public static IEnumerable<object[]> ActiveStates =>
    [
        ["Bold (Ctrl+B)", (Action<CloudTextEditorSelectionState>)(s => s.Bold = true)],
        ["Italic (Ctrl+I)", (Action<CloudTextEditorSelectionState>)(s => s.Italic = true)],
        ["Underline (Ctrl+U)", (Action<CloudTextEditorSelectionState>)(s => s.Underline = true)],
        ["Strikethrough", (Action<CloudTextEditorSelectionState>)(s => s.Strikethrough = true)],
        ["Subscript", (Action<CloudTextEditorSelectionState>)(s => s.Subscript = true)],
        ["Superscript", (Action<CloudTextEditorSelectionState>)(s => s.Superscript = true)],
        ["Inline code", (Action<CloudTextEditorSelectionState>)(s => s.InlineCode = true)],
        ["Align center", (Action<CloudTextEditorSelectionState>)(s => s.Alignment = "center")],
        ["Align right", (Action<CloudTextEditorSelectionState>)(s => s.Alignment = "right")],
        ["Justify", (Action<CloudTextEditorSelectionState>)(s => s.Alignment = "justify")],
        ["Bulleted list", (Action<CloudTextEditorSelectionState>)(s => s.UnorderedList = true)],
        ["Numbered list", (Action<CloudTextEditorSelectionState>)(s => s.OrderedList = true)],
        ["Blockquote", (Action<CloudTextEditorSelectionState>)(s => s.BlockTag = "blockquote")],
        ["Code block", (Action<CloudTextEditorSelectionState>)(s => s.BlockTag = "pre")],
        ["Insert link (Ctrl+K)", (Action<CloudTextEditorSelectionState>)(s => s.Link = true)],
    ];

    [Theory]
    [MemberData(nameof(ActiveStates))]
    public async Task A_button_is_highlighted_when_its_format_is_active_at_the_caret(string title, Action<CloudTextEditorSelectionState> configure)
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();
        Assert.DoesNotContain("_active", Button(cut, title).ClassList);

        await PushState(cut, configure);

        Assert.Contains("_active", Button(cut, title).ClassList);
    }

    [Fact]
    public async Task Only_the_buttons_for_the_active_formats_are_highlighted()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();

        await PushState(cut, state => { state.Bold = true; state.Alignment = "center"; });

        Assert.Equal(["Bold (Ctrl+B)", "Align center"], cut.FindAll(".cloudeditor-toolbar button._active").Select(button => button.GetAttribute("title")));
    }

    [Fact]
    public async Task Plain_left_aligned_text_highlights_align_left_only()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();

        await PushState(cut, _ => { });

        Assert.Equal(["Align left"], cut.FindAll(".cloudeditor-toolbar button._active").Select(button => button.GetAttribute("title")));
    }

    [Fact]
    public async Task The_formats_clear_again_when_the_caret_moves_to_plain_text()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();
        await PushState(cut, state => state.Bold = true);

        await PushState(cut, _ => { });

        Assert.DoesNotContain("_active", Button(cut, "Bold (Ctrl+B)").ClassList);
    }

    // ----- Block format -------------------------------------------------------------------

    private static (string Value, string Label)[] Choices(IRenderedComponent<CloudTextEditor> cut) =>
        [.. cut.FindAll("select[title='Block format'] option").Select(option => (option.GetAttribute("value")!, option.TextContent))];

    [Fact]
    public void The_block_format_offers_paragraph_and_four_headings_by_default()
    {
        Assert.Equal(
            [("p", "Paragraph"), ("h1", "Heading 1"), ("h2", "Heading 2"), ("h3", "Heading 3"), ("h4", "Heading 4")],
            Choices(RenderEditor()));
    }

    [Fact]
    public void The_first_heading_level_shifts_which_html_heading_each_entry_produces()
    {
        // The README example: Heading 1 -> h2, Heading 2 -> h3, Heading 3 -> h4.
        IRenderedComponent<CloudTextEditor> cut = RenderEditor(p => p.Add(x => x.FirstHeadingLevel, 2).Add(x => x.HeadingCount, 3));

        Assert.Equal([("p", "Paragraph"), ("h2", "Heading 1"), ("h3", "Heading 2"), ("h4", "Heading 3")], Choices(cut));
    }

    [Theory]
    [InlineData(5, 4, new[] { "h5", "h6" })]
    [InlineData(6, 4, new[] { "h6" })]
    [InlineData(1, 10, new[] { "h1", "h2", "h3", "h4", "h5", "h6" })]
    [InlineData(2, 5, new[] { "h2", "h3", "h4", "h5", "h6" })]
    [InlineData(0, 2, new[] { "h1", "h2" })]
    [InlineData(-3, 1, new[] { "h1" })]
    [InlineData(9, 2, new[] { "h6" })]
    [InlineData(1, 0, new[] { "h1" })]
    [InlineData(3, -2, new[] { "h3" })]
    public void Heading_entries_never_go_beyond_h6_or_below_one_entry(int first, int count, string[] expected)
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor(p => p.Add(x => x.FirstHeadingLevel, first).Add(x => x.HeadingCount, count));

        Assert.Equal(["p", .. expected], Choices(cut).Select(choice => choice.Value));
    }

    [Fact]
    public async Task The_select_shows_the_block_at_the_caret()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();

        await PushState(cut, state => state.BlockTag = "h3");

        Assert.Equal("h3", cut.Find("select[title='Block format']").GetAttribute("value"));
    }

    [Theory]
    [InlineData("blockquote")]
    [InlineData("pre")]
    [InlineData("h6")]
    public async Task A_block_that_is_not_in_the_list_shows_as_paragraph(string blockTag)
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();

        await PushState(cut, state => state.BlockTag = blockTag);

        Assert.Equal("p", cut.Find("select[title='Block format']").GetAttribute("value"));
    }

    [Fact]
    public async Task Choosing_a_block_format_tells_the_script()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();

        await cut.Find("select[title='Block format']").ChangeAsync(new ChangeEventArgs { Value = "h2" });

        Assert.Equal("h2", OnlyCall("formatBlock")[0]);
    }

    [Fact]
    public async Task An_empty_block_format_choice_is_ignored()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();

        await cut.Find("select[title='Block format']").ChangeAsync(new ChangeEventArgs { Value = "" });

        Assert.Empty(Js.ControllerCalls("formatBlock"));
    }

    // ----- Colours ------------------------------------------------------------------------

    [Fact]
    public void No_colour_popover_is_open_until_asked()
    {
        Assert.Empty(RenderEditor().FindAll(".cloudeditor-popover"));
    }

    [Theory]
    [InlineData("Text color", "foreColor")]
    [InlineData("Highlight color", "hiliteColor")]
    public async Task A_colour_button_opens_a_palette_and_remembers_the_selection(string title, string command)
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();

        await Select(cut, title);

        Assert.Equal(18, cut.FindAll(".cloudeditor-popover .cloudeditor-swatch").Count);
        Assert.Contains("_active", Button(cut, title).ClassList);
        Assert.Single(Js.ControllerCalls("saveSelection"));

        await cut.Find(".cloudeditor-swatch[title='#0078d4']").ClickAsync(new());

        object?[] arguments = OnlyCall("execColor");
        Assert.Equal(command, arguments[0]);
        Assert.Equal("#0078d4", arguments[1]);
        Assert.Empty(cut.FindAll(".cloudeditor-popover"));
    }

    [Fact]
    public async Task The_palette_swatches_are_all_distinct_hex_colours()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();
        await Select(cut, "Text color");

        string[] colours = [.. cut.FindAll(".cloudeditor-swatch").Select(swatch => swatch.GetAttribute("title")!)];

        Assert.Equal(colours.Length, colours.Distinct().Count());
        Assert.All(colours, colour => Assert.Matches("^#[0-9a-f]{6}$", colour));
        Assert.All(cut.FindAll(".cloudeditor-swatch"), swatch => Assert.Contains(swatch.GetAttribute("title")!, swatch.GetAttribute("style")!));
    }

    [Fact]
    public async Task Pressing_the_colour_button_again_closes_the_palette()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();
        await Select(cut, "Text color");

        await Select(cut, "Text color");

        Assert.Empty(cut.FindAll(".cloudeditor-popover"));
        Assert.Single(Js.ControllerCalls("saveSelection"));
    }

    [Fact]
    public async Task Opening_one_palette_closes_the_other()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();
        await Select(cut, "Text color");

        await Select(cut, "Highlight color");

        Assert.Single(cut.FindAll(".cloudeditor-popover"));
        Assert.DoesNotContain("_active", Button(cut, "Text color").ClassList);
        Assert.Contains("_active", Button(cut, "Highlight color").ClassList);
    }

    [Theory]
    [InlineData("Text color", "foreColor", "inherit")]
    [InlineData("Highlight color", "hiliteColor", "transparent")]
    public async Task Clear_removes_the_colour_by_applying_the_neutral_value(string title, string command, string neutral)
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();
        await Select(cut, title);

        await cut.Find(".cloudeditor-popoverrow button").ClickAsync(new());

        Assert.Equal([command, neutral], OnlyCall("execColor").Select(argument => (string)argument!));
        Assert.Empty(cut.FindAll(".cloudeditor-popover"));
    }

    [Fact]
    public async Task The_custom_colour_input_applies_the_picked_colour()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();
        await Select(cut, "Text color");

        await cut.Find(".cloudeditor-popoverrow input[type='color']").ChangeAsync(new ChangeEventArgs { Value = "#123456" });

        Assert.Equal(["foreColor", "#123456"], OnlyCall("execColor").Select(argument => (string)argument!));
    }

    [Fact]
    public async Task Running_another_command_closes_an_open_palette()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();
        await Select(cut, "Text color");

        await Select(cut, "Bold (Ctrl+B)");

        Assert.Empty(cut.FindAll(".cloudeditor-popover"));
    }

    // ----- Fullscreen ---------------------------------------------------------------------

    [Fact]
    public async Task Fullscreen_toggles_the_class_and_the_button()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();
        Assert.DoesNotContain("_fullscreen", cut.Find(".cloudeditor").ClassList);

        await Select(cut, "Fullscreen");

        Assert.Contains("_fullscreen", cut.Find(".cloudeditor").ClassList);
        Assert.False(HasButton(cut, "Fullscreen"));
        Assert.Contains("_active", Button(cut, "Exit fullscreen").ClassList);

        await Select(cut, "Exit fullscreen");

        Assert.DoesNotContain("_fullscreen", cut.Find(".cloudeditor").ClassList);
        Assert.True(HasButton(cut, "Fullscreen"));
    }

    [Fact]
    public async Task Fullscreen_works_for_a_read_only_editor_too()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor(p => p.Add(x => x.ReadOnly, true));

        await Select(cut, "Fullscreen");

        Assert.Contains("_fullscreen", cut.Find(".cloudeditor").ClassList);
    }
}
