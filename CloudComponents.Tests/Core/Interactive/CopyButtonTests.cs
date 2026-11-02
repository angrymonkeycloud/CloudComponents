using AngryMonkey.CloudComponents;
using Bunit;
using Microsoft.JSInterop;

namespace CloudComponents.Tests.Core.Interactive;

public class CopyButtonTests : InteractiveTestBase
{
    private IRenderedComponent<CopyButton> RenderCopy(string text = "secret-code", Action<ComponentParameterCollectionBuilder<CopyButton>>? configure = null) =>
        Render<CopyButton>(parameters =>
        {
            parameters.Add(p => p.Text, text);
            configure?.Invoke(parameters);
        });

    private static bool IsSuccess(IRenderedComponent<CopyButton> cut) => cut.Find(".amc-copybutton").ClassList.Contains("_success");

    [Fact]
    public void A_copy_button_renders_a_titled_and_labelled_button_with_the_default_icons()
    {
        IRenderedComponent<CopyButton> cut = RenderCopy();

        AngleSharp.Dom.IElement button = cut.Find("button.amc-copybutton-button");
        Assert.Equal("button", button.GetAttribute("type"));
        Assert.Equal("Copy", button.GetAttribute("title"));
        Assert.Equal("Copy", button.GetAttribute("aria-label"));
        Assert.Equal(2, button.QuerySelectorAll("svg").Length);
        Assert.False(IsSuccess(cut));
    }

    [Fact]
    public void Without_child_content_no_content_area_is_rendered()
    {
        IRenderedComponent<CopyButton> cut = RenderCopy();

        Assert.Empty(cut.FindAll(".amc-copybutton-content"));
    }

    [Fact]
    public void Child_content_is_rendered_next_to_the_button_and_is_not_clickable_by_default()
    {
        IRenderedComponent<CopyButton> cut = RenderCopy(configure: p => p.AddChildContent("<code>abc</code>"));

        AngleSharp.Dom.IElement content = cut.Find(".amc-copybutton-content");
        Assert.Equal("abc", content.TextContent.Trim());
        Assert.DoesNotContain("_clickable", content.ClassList);
    }

    [Fact]
    public void Copying_on_content_click_marks_the_content_as_clickable()
    {
        IRenderedComponent<CopyButton> cut = RenderCopy(configure: p => p.AddChildContent("abc").Add(x => x.CopyOnContentClick, true));

        Assert.Contains("_clickable", cut.Find(".amc-copybutton-content").ClassList);
    }

    [Fact]
    public void Custom_button_content_replaces_the_default_icons()
    {
        IRenderedComponent<CopyButton> cut = RenderCopy(configure: p => p.Add(x => x.ButtonContent, "<em id=\"mine\">Copy it</em>"));

        AngleSharp.Dom.IElement button = cut.Find("button.amc-copybutton-button");
        Assert.NotNull(button.QuerySelector("#mine"));
        Assert.Empty(button.QuerySelectorAll("svg"));
    }

    [Fact]
    public void The_button_can_be_hidden()
    {
        IRenderedComponent<CopyButton> cut = RenderCopy(configure: p => p.Add(x => x.ShowButton, false).AddChildContent("abc"));

        Assert.Empty(cut.FindAll("button"));
        Assert.NotNull(cut.Find(".amc-copybutton-content"));
    }

    [Fact]
    public void Clicking_the_button_writes_the_text_to_the_clipboard()
    {
        IRenderedComponent<CopyButton> cut = RenderCopy("hello world");

        cut.Find("button.amc-copybutton-button").Click();

        JsCall call = Assert.Single(Js.RuntimeCalls("navigator.clipboard.writeText"));
        Assert.Equal(["hello world"], call.Args);
    }

    [Fact]
    public void Clicking_the_button_shows_the_copied_state_in_the_class_and_title()
    {
        IRenderedComponent<CopyButton> cut = RenderCopy();

        cut.Find("button.amc-copybutton-button").Click();

        Assert.True(IsSuccess(cut));
        AngleSharp.Dom.IElement button = cut.Find("button.amc-copybutton-button");
        Assert.Equal("Copied!", button.GetAttribute("title"));
        Assert.Equal("Copied!", button.GetAttribute("aria-label"));
    }

    [Fact]
    public void The_copied_state_goes_back_to_normal_after_about_a_second()
    {
        IRenderedComponent<CopyButton> cut = RenderCopy();

        cut.Find("button.amc-copybutton-button").Click();
        Assert.True(IsSuccess(cut));

        cut.WaitForAssertion(() => Assert.False(IsSuccess(cut)), TimeSpan.FromSeconds(5));
        Assert.Equal("Copy", cut.Find("button.amc-copybutton-button").GetAttribute("title"));
    }

    [Fact]
    public async Task The_copied_state_is_not_cleared_before_the_second_is_up()
    {
        IRenderedComponent<CopyButton> cut = RenderCopy();

        cut.Find("button.amc-copybutton-button").Click();
        await Task.Delay(400);

        Assert.True(IsSuccess(cut));
    }

    [Fact]
    public async Task Clicking_again_restarts_the_copied_period()
    {
        IRenderedComponent<CopyButton> cut = RenderCopy();

        cut.Find("button.amc-copybutton-button").Click();
        await Task.Delay(500);
        cut.Find("button.amc-copybutton-button").Click();
        await Task.Delay(700);

        // 1200 ms after the first click, 700 ms after the second: the first timer must not have cleared the state.
        Assert.True(IsSuccess(cut));
        Assert.Equal(2, Js.RuntimeCalls("navigator.clipboard.writeText").Length);

        cut.WaitForAssertion(() => Assert.False(IsSuccess(cut)), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void There_is_nothing_to_copy_for_an_empty_text()
    {
        IRenderedComponent<CopyButton> cut = RenderCopy("");

        cut.Find("button.amc-copybutton-button").Click();

        Assert.Empty(Js.RuntimeCalls("navigator.clipboard.writeText"));
        Assert.False(IsSuccess(cut));
    }

    [Fact]
    public void Clicking_the_content_does_nothing_when_copy_on_content_click_is_off()
    {
        IRenderedComponent<CopyButton> cut = RenderCopy(configure: p => p.AddChildContent("abc"));

        cut.Find(".amc-copybutton-content").Click();

        Assert.Empty(Js.RuntimeCalls("navigator.clipboard.writeText"));
        Assert.False(IsSuccess(cut));
    }

    [Fact]
    public void Clicking_the_content_copies_when_copy_on_content_click_is_on()
    {
        IRenderedComponent<CopyButton> cut = RenderCopy("payload", p => p.AddChildContent("abc").Add(x => x.CopyOnContentClick, true));

        cut.Find(".amc-copybutton-content").Click();

        Assert.Equal(["payload"], Assert.Single(Js.RuntimeCalls("navigator.clipboard.writeText")).Args);
        Assert.True(IsSuccess(cut));
    }

    [Fact]
    public void Content_click_copying_works_without_a_visible_button()
    {
        IRenderedComponent<CopyButton> cut = RenderCopy("payload", p => p
            .Add(x => x.ShowButton, false)
            .Add(x => x.CopyOnContentClick, true)
            .AddChildContent("abc"));

        cut.Find(".amc-copybutton-content").Click();

        Assert.Single(Js.RuntimeCalls("navigator.clipboard.writeText"));
    }

    [Fact]
    public void A_text_change_is_what_the_next_click_copies()
    {
        IRenderedComponent<CopyButton> cut = RenderCopy("first");
        cut.Render(p => p.Add(x => x.Text, "second"));

        cut.Find("button.amc-copybutton-button").Click();

        Assert.Equal(["second"], Assert.Single(Js.RuntimeCalls("navigator.clipboard.writeText")).Args);
    }

    [Fact]
    public void A_refused_clipboard_write_does_not_break_the_component()
    {
        Js.FailWith("navigator.clipboard.writeText", new JSException("NotAllowedError"));
        IRenderedComponent<CopyButton> cut = RenderCopy();

        cut.Find("button.amc-copybutton-button").Click();

        Assert.NotNull(cut.Find("button.amc-copybutton-button"));
    }

    [Fact]
    public async Task Disposing_while_the_copied_state_is_showing_does_not_throw()
    {
        IRenderedComponent<CopyButton> cut = RenderCopy();
        cut.Find("button.amc-copybutton-button").Click();

        await DisposeComponentsAsync();
    }

    [Fact]
    public void The_defaults_show_the_button_and_do_not_copy_on_content_click()
    {
        IRenderedComponent<CopyButton> cut = Render<CopyButton>();

        Assert.True(cut.Instance.ShowButton);
        Assert.False(cut.Instance.CopyOnContentClick);
        Assert.Equal(string.Empty, cut.Instance.Text);
    }
}
