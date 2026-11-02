using AngleSharp.Dom;
using AngryMonkey.CloudComponents.TextEditor.Components;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace CloudComponents.Tests.TextEditor;

/// <summary>The HTML source view: formatting, live validation, applying and cancelling.</summary>
public class EditorCodeViewTests : EditorTestBase
{
    private static IElement SourceBox(IRenderedComponent<CloudTextEditor> cut) => cut.Find("textarea.cloudeditor-code");

    private static string SourceText(IRenderedComponent<CloudTextEditor> cut) => (SourceBox(cut).GetAttribute("value") ?? string.Empty).TrimStart('\n', '\r');

    private static IElement Apply(IRenderedComponent<CloudTextEditor> cut) => cut.Find(".cloudeditor-codeactions .cloudeditor-action:not(._secondary)");

    private static IElement Cancel(IRenderedComponent<CloudTextEditor> cut) => cut.Find(".cloudeditor-codeactions .cloudeditor-action._secondary");

    private static string Status(IRenderedComponent<CloudTextEditor> cut) => cut.Find(".cloudeditor-codestatus").TextContent.Trim();

    private static Task Edit(IRenderedComponent<CloudTextEditor> cut, string html) =>
        SourceBox(cut).TriggerEventAsync("oninput", new ChangeEventArgs { Value = html });

    private async Task<IRenderedComponent<CloudTextEditor>> OpenCodeViewAsync(string currentHtml = "<p>a</p><p>b</p>", Action<ComponentParameterCollectionBuilder<CloudTextEditor>>? configure = null)
    {
        Js.Controller.Results["getHtml"] = currentHtml;
        IRenderedComponent<CloudTextEditor> cut = RenderEditor(configure);
        await Select(cut, "Code view");
        return cut;
    }

    // ----- Opening ------------------------------------------------------------------------

    [Fact]
    public async Task The_code_view_shows_the_current_html_one_block_per_line()
    {
        IRenderedComponent<CloudTextEditor> cut = await OpenCodeViewAsync("<p>a</p><p>b</p>");

        Assert.Equal("<p>a</p>\n<p>b</p>", SourceText(cut));
        Assert.Single(Js.ControllerCalls("getHtml"));
        Assert.Contains("_codeview", cut.Find(".cloudeditor").ClassList);
    }

    [Fact]
    public async Task The_button_turns_into_design_view_while_the_source_is_shown()
    {
        IRenderedComponent<CloudTextEditor> cut = await OpenCodeViewAsync();

        Assert.False(HasButton(cut, "Code view"));
        Assert.Contains("_active", Button(cut, "Design view").ClassList);
    }

    [Fact]
    public async Task An_empty_document_opens_an_empty_source_that_is_valid()
    {
        IRenderedComponent<CloudTextEditor> cut = await OpenCodeViewAsync("");

        Assert.Equal(string.Empty, SourceText(cut).Trim());
        Assert.Equal("Valid HTML", Status(cut));
        Assert.False(IsDisabled(Apply(cut)));
    }

    [Fact]
    public async Task While_the_source_is_shown_the_surface_and_the_tools_are_locked()
    {
        IRenderedComponent<CloudTextEditor> cut = await OpenCodeViewAsync();

        Assert.Equal("false", cut.Find(".cloudeditor-content").GetAttribute("contenteditable"));
        Assert.True(IsDisabled(Button(cut, "Bold (Ctrl+B)")));
        Assert.True(IsDisabled(Button(cut, "Insert image")));
        Assert.True(cut.Find("select[title='Block format']").HasAttribute("disabled"));

        await Select(cut, "Insert link (Ctrl+K)");
        await cut.InvokeAsync(() => cut.Instance.HandleLinkShortcutAsync());

        Assert.Empty(cut.FindAll(".cloudeditor-dialog"));
        Assert.Empty(Js.ControllerCalls("exec"));
    }

    [Fact]
    public async Task Opening_the_source_closes_an_open_colour_palette()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor();
        await Select(cut, "Text color");

        await Select(cut, "Code view");

        Assert.Empty(cut.FindAll(".cloudeditor-popover"));
    }

    [Fact]
    public void A_read_only_editor_cannot_open_the_source()
    {
        IRenderedComponent<CloudTextEditor> cut = RenderEditor(p => p.Add(x => x.ReadOnly, true));

        Assert.True(IsDisabled(Button(cut, "Code view")));
        Assert.Empty(cut.FindAll(".cloudeditor-codeview"));
    }

    // ----- Live validation ----------------------------------------------------------------

    [Fact]
    public async Task Well_formed_source_is_reported_valid_and_can_be_applied()
    {
        IRenderedComponent<CloudTextEditor> cut = await OpenCodeViewAsync();

        Assert.Equal("Valid HTML", Status(cut));
        Assert.False(IsDisabled(Apply(cut)));
    }

    [Fact]
    public async Task A_broken_edit_shows_the_first_error_with_its_position_and_blocks_apply()
    {
        IRenderedComponent<CloudTextEditor> cut = await OpenCodeViewAsync();

        await Edit(cut, "<p>a</p>\n<div><span>x</div>");

        Assert.Contains("Error", Status(cut));
        Assert.Contains("2:6", Status(cut));
        Assert.Contains("Element <span> is not closed before </div>.", Status(cut));
        Assert.Contains("1 issue", Status(cut));
        Assert.True(IsDisabled(Apply(cut)));
        Assert.Contains("_error", cut.Find(".cloudeditor-message").ClassList);
    }

    [Fact]
    public async Task Fixing_the_error_makes_the_source_valid_again()
    {
        IRenderedComponent<CloudTextEditor> cut = await OpenCodeViewAsync();
        await Edit(cut, "<div>");
        Assert.True(IsDisabled(Apply(cut)));

        await Edit(cut, "<div></div>");

        Assert.Equal("Valid HTML", Status(cut));
        Assert.False(IsDisabled(Apply(cut)));
    }

    [Fact]
    public async Task A_warning_is_shown_but_does_not_block_apply()
    {
        IRenderedComponent<CloudTextEditor> cut = await OpenCodeViewAsync();

        await Edit(cut, "<p onclick=\"x()\">a</p>");

        Assert.Contains("Warning", Status(cut));
        Assert.Contains("1 issue", Status(cut));
        Assert.Contains("_warning", cut.Find(".cloudeditor-message").ClassList);
        Assert.False(IsDisabled(Apply(cut)));
    }

    [Fact]
    public async Task An_error_is_shown_before_a_warning_and_the_count_is_plural()
    {
        IRenderedComponent<CloudTextEditor> cut = await OpenCodeViewAsync();

        await Edit(cut, "<p onclick=\"x()\">a</p><div>");

        Assert.Contains("Error", Status(cut));
        Assert.Contains("2 issues", Status(cut));
    }

    [Fact]
    public async Task The_issue_list_opens_and_closes_from_the_count_and_the_back_button()
    {
        IRenderedComponent<CloudTextEditor> cut = await OpenCodeViewAsync();
        await Edit(cut, "<p onclick=\"x()\">a</p>\n<div>");
        Assert.Empty(cut.FindAll(".cloudeditor-issuepanel"));

        await cut.Find(".cloudeditor-issuecount").ClickAsync(new());

        Assert.Equal(2, cut.FindAll(".cloudeditor-issue").Count);
        Assert.Contains("_active", cut.Find(".cloudeditor-issuecount").ClassList);
        Assert.Equal(["1:4", "2:1"], cut.FindAll(".cloudeditor-issueposition").Select(position => position.TextContent.Trim()));
        Assert.Contains("_warning", cut.FindAll(".cloudeditor-issue")[0].ClassList);
        Assert.Contains("_error", cut.FindAll(".cloudeditor-issue")[1].ClassList);

        await cut.Find(".cloudeditor-issuetoggle").ClickAsync(new());

        Assert.Empty(cut.FindAll(".cloudeditor-issuepanel"));
    }

    [Fact]
    public async Task The_issue_list_closes_by_itself_once_there_are_no_issues()
    {
        IRenderedComponent<CloudTextEditor> cut = await OpenCodeViewAsync();
        await Edit(cut, "<div>");
        await cut.Find(".cloudeditor-issuecount").ClickAsync(new());
        Assert.NotEmpty(cut.FindAll(".cloudeditor-issuepanel"));

        await Edit(cut, "<div></div>");
        await Edit(cut, "<div>");

        Assert.Empty(cut.FindAll(".cloudeditor-issuepanel"));
    }

    // ----- Apply and cancel ---------------------------------------------------------------

    [Fact]
    public async Task Apply_loads_the_source_rereads_the_cleaned_html_and_publishes_it()
    {
        List<string?> published = [];
        IRenderedComponent<CloudTextEditor> cut = await OpenCodeViewAsync("<p>original</p>", p => p.Add(x => x.ValueChanged, value => { published.Add(value); }));
        await Edit(cut, "<p onclick=\"x()\">edited</p>");
        Js.Controller.Results["getHtml"] = "<p>edited</p>";

        await Apply(cut).ClickAsync(new());

        Assert.Equal("<p onclick=\"x()\">edited</p>", Js.ControllerCalls("setHtml").Last().Arguments[0]);
        Assert.Equal(["<p>edited</p>"], published);
        Assert.Empty(cut.FindAll(".cloudeditor-codeview"));
        Assert.DoesNotContain("_codeview", cut.Find(".cloudeditor").ClassList);
    }

    [Fact]
    public async Task Applying_returns_focus_to_the_surface()
    {
        IRenderedComponent<CloudTextEditor> cut = await OpenCodeViewAsync();

        await Apply(cut).ClickAsync(new());

        cut.WaitForAssertion(() => Assert.Single(Js.ControllerCalls("focus")));
    }

    [Fact]
    public async Task The_design_view_button_applies_valid_source()
    {
        IRenderedComponent<CloudTextEditor> cut = await OpenCodeViewAsync("<p>x</p>");
        await Edit(cut, "<p>y</p>");

        await Select(cut, "Design view");

        Assert.Empty(cut.FindAll(".cloudeditor-codeview"));
        Assert.Equal("<p>y</p>", Js.ControllerCalls("setHtml").Last().Arguments[0]);
    }

    [Fact]
    public async Task The_design_view_button_does_nothing_while_the_source_has_errors()
    {
        IRenderedComponent<CloudTextEditor> cut = await OpenCodeViewAsync();
        await Edit(cut, "<div>");
        int setHtmlCalls = Js.ControllerCalls("setHtml").Count();

        await Select(cut, "Design view");

        Assert.Single(cut.FindAll(".cloudeditor-codeview"));
        Assert.Equal(setHtmlCalls, Js.ControllerCalls("setHtml").Count());
    }

    [Fact]
    public async Task Apply_does_nothing_for_invalid_source_even_when_forced()
    {
        IRenderedComponent<CloudTextEditor> cut = await OpenCodeViewAsync();
        await Edit(cut, "<b><i></b>");
        int setHtmlCalls = Js.ControllerCalls("setHtml").Count();

        await cut.InvokeAsync(() => Apply(cut).Click());

        Assert.Equal(setHtmlCalls, Js.ControllerCalls("setHtml").Count());
        Assert.Single(cut.FindAll(".cloudeditor-codeview"));
    }

    [Fact]
    public async Task Cancel_discards_the_edits_and_leaves_the_content_alone()
    {
        List<string?> published = [];
        IRenderedComponent<CloudTextEditor> cut = await OpenCodeViewAsync(configure: p => p.Add(x => x.ValueChanged, value => { published.Add(value); }));
        int setHtmlCalls = Js.ControllerCalls("setHtml").Count();
        await Edit(cut, "<p>changed</p>");

        await Cancel(cut).ClickAsync(new());

        Assert.Empty(cut.FindAll(".cloudeditor-codeview"));
        Assert.Equal(setHtmlCalls, Js.ControllerCalls("setHtml").Count());
        Assert.Empty(published);
        cut.WaitForAssertion(() => Assert.Single(Js.ControllerCalls("focus")));
    }

    [Fact]
    public async Task Reopening_after_cancel_shows_the_script_content_not_the_discarded_edit()
    {
        IRenderedComponent<CloudTextEditor> cut = await OpenCodeViewAsync("<p>real</p>");
        await Edit(cut, "<p>discarded</p>");
        await Cancel(cut).ClickAsync(new());

        await Select(cut, "Code view");

        Assert.Equal("<p>real</p>", SourceText(cut).Trim());
    }

    // ----- Keyboard -----------------------------------------------------------------------

    [Fact]
    public async Task Ctrl_enter_applies_valid_source()
    {
        IRenderedComponent<CloudTextEditor> cut = await OpenCodeViewAsync();

        await SourceBox(cut).KeyDownAsync(new KeyboardEventArgs { Key = "Enter", CtrlKey = true });

        Assert.Empty(cut.FindAll(".cloudeditor-codeview"));
    }

    [Fact]
    public async Task Ctrl_enter_does_not_apply_invalid_source()
    {
        IRenderedComponent<CloudTextEditor> cut = await OpenCodeViewAsync();
        await Edit(cut, "<div>");

        await SourceBox(cut).KeyDownAsync(new KeyboardEventArgs { Key = "Enter", CtrlKey = true });

        Assert.Single(cut.FindAll(".cloudeditor-codeview"));
    }

    [Fact]
    public async Task Enter_alone_only_types_a_new_line()
    {
        IRenderedComponent<CloudTextEditor> cut = await OpenCodeViewAsync();

        await SourceBox(cut).KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.Single(cut.FindAll(".cloudeditor-codeview"));
    }

    [Fact]
    public async Task Escape_cancels_even_when_the_source_is_invalid()
    {
        IRenderedComponent<CloudTextEditor> cut = await OpenCodeViewAsync();
        await Edit(cut, "<div>");

        await SourceBox(cut).KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        Assert.Empty(cut.FindAll(".cloudeditor-codeview"));
    }
}
