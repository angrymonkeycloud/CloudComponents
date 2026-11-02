using AngryMonkey.CloudComponents.TextEditor.Components;
using Bunit;

namespace CloudComponents.Tests.TextEditor;

/// <summary>The markup produced for the layout parameters.</summary>
public class EditorRenderTests : EditorTestBase
{
    private static string Style(IRenderedComponent<CloudTextEditor> cut) => cut.Find(".cloudeditor-content").GetAttribute("style") ?? string.Empty;

    [Fact]
    public void The_editor_renders_a_toolbar_and_an_editable_textbox()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();

        Assert.Equal("toolbar", cut.Find(".cloudeditor-toolbar").GetAttribute("role"));
        var content = cut.Find(".cloudeditor-content");
        Assert.Equal("textbox", content.GetAttribute("role"));
        Assert.Equal("true", content.GetAttribute("aria-multiline"));
        Assert.Equal("true", content.GetAttribute("contenteditable"));
    }

    [Fact]
    public void A_plain_editor_has_no_state_classes()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();

        Assert.Equal(["cloudeditor"], cut.Find(".cloudeditor").ClassList);
    }

    [Fact]
    public void A_read_only_editor_is_marked_with_a_class()
    {
        Assert.Contains("_readonly", RenderEditor(p => p.Add(x => x.ReadOnly, true)).Find(".cloudeditor").ClassList);
    }

    [Fact]
    public void The_placeholder_is_exposed_to_styles_and_used_as_the_accessible_name()
    {
        var content = RenderEditor(p => p.Add(x => x.Placeholder, "Start writing…")).Find(".cloudeditor-content");

        Assert.Equal("Start writing…", content.GetAttribute("data-placeholder"));
        Assert.Equal("Start writing…", content.GetAttribute("aria-label"));
    }

    [Fact]
    public void Without_a_placeholder_the_accessible_name_is_generic()
    {
        var content = RenderEditor().Find(".cloudeditor-content");

        Assert.False(content.HasAttribute("data-placeholder"));
        Assert.Equal("Rich text editor", content.GetAttribute("aria-label"));
    }

    [Fact]
    public void The_default_height_is_a_250_pixel_minimum()
    {
        Assert.Equal("min-height:250px", Style(RenderEditor()));
    }

    [Fact]
    public void A_custom_minimum_height_is_used()
    {
        Assert.Equal("min-height:10rem", Style(RenderEditor(p => p.Add(x => x.MinHeight, "10rem"))));
    }

    [Fact]
    public void A_maximum_height_adds_scrolling_beyond_it()
    {
        Assert.Equal("min-height:250px;max-height:600px;overflow-y:auto", Style(RenderEditor(p => p.Add(x => x.MaxHeight, "600px"))));
    }

    [Fact]
    public void A_fixed_height_overrides_the_minimum_and_maximum()
    {
        string style = Style(RenderEditor(p => p.Add(x => x.Height, "320px").Add(x => x.MinHeight, "100px").Add(x => x.MaxHeight, "900px")));

        Assert.Equal("height:320px;overflow-y:auto", style);
    }

    [Fact]
    public void An_empty_maximum_height_is_ignored()
    {
        Assert.Equal("min-height:250px", Style(RenderEditor(p => p.Add(x => x.MaxHeight, ""))));
    }

    [Fact]
    public async Task The_code_view_uses_the_same_height_as_the_surface()
    {
        Js.Controller.Results["getHtml"] = "<p>x</p>";
        IRenderedComponent<CloudTextEditor> cut = RenderEditor(p => p.Add(x => x.Height, "320px"));

        await Select(cut, "Code view");

        Assert.Equal("height:320px;overflow-y:auto", cut.Find(".cloudeditor-codeview").GetAttribute("style"));
    }

    [Fact]
    public void Spellcheck_is_on_by_default()
    {
        Assert.True(RenderEditor().Find(".cloudeditor-content").HasAttribute("spellcheck"));
    }

    [Fact(Skip = "BUG: spellcheck=\"@Spellcheck\" is a bool, which Blazor renders as a bare attribute when true and omits when false; the attribute is enumerated (\"true\"/\"false\"), so Spellcheck=false renders nothing and the browser still spell-checks")]
    public void Turning_spellcheck_off_renders_spellcheck_false()
    {
        var content = RenderEditor(p => p.Add(x => x.Spellcheck, false)).Find(".cloudeditor-content");

        Assert.Equal("false", content.GetAttribute("spellcheck"));
    }

    [Fact(Skip = "BUG: aria-expanded=\"@_showCodeIssues\" is a bool, so it renders as a bare attribute (empty value, invalid for ARIA) when the issue list is open and is missing when closed instead of \"true\"/\"false\"")]
    public async Task The_issue_count_button_reports_expanded_state_as_true_or_false()
    {
        Js.Controller.Results["getHtml"] = "<div>";
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();
        await Select(cut, "Code view");

        Assert.Equal("false", cut.Find(".cloudeditor-issuecount").GetAttribute("aria-expanded"));

        await cut.Find(".cloudeditor-issuecount").ClickAsync(new());

        Assert.Equal("true", cut.Find(".cloudeditor-issuecount").GetAttribute("aria-expanded"));
    }
}
