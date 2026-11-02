using AngryMonkey.CloudComponents;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace CloudComponents.Tests.Core.Interactive;

public class PopupCompTests : InteractiveTestBase
{
    private IRenderedComponent<PopupComp> RenderPopup(Action<ComponentParameterCollectionBuilder<PopupComp>>? configure = null, bool isOpen = true) =>
        Render<PopupComp>(parameters =>
        {
            parameters.Add(p => p.IsOpen, isOpen);
            configure?.Invoke(parameters);
        });

    [Fact]
    public void A_closed_popup_renders_nothing()
    {
        IRenderedComponent<PopupComp> cut = RenderPopup(p => p.Add(x => x.Title, "Hidden").AddChildContent("<p>secret</p>"), isOpen: false);

        Assert.Empty(cut.FindAll(".amc-popup"));
        Assert.DoesNotContain("secret", cut.Markup);
        Assert.DoesNotContain("Hidden", cut.Markup);
    }

    [Fact]
    public void An_open_popup_renders_a_modal_dialog_in_the_top_layer_with_the_small_size_by_default()
    {
        IRenderedComponent<PopupComp> cut = RenderPopup();

        AngleSharp.Dom.IElement popup = cut.Find(".amc-popup");
        Assert.True(popup.HasAttribute("popover"));
        Assert.Contains("modal", popup.ClassList);
        Assert.Contains("_open", popup.ClassList);
        Assert.Contains("_small", popup.ClassList);

        AngleSharp.Dom.IElement content = cut.Find(".modal-content");
        Assert.Equal("dialog", content.GetAttribute("role"));
        Assert.Equal("true", content.GetAttribute("aria-modal"));
    }

    [Theory]
    [InlineData("_medium")]
    [InlineData("_large")]
    [InlineData("_xlarge")]
    [InlineData("_form")]
    public void The_size_parameter_becomes_a_class_instead_of_the_default_size(string size)
    {
        IRenderedComponent<PopupComp> cut = RenderPopup(p => p.Add(x => x.Size, size));

        AngleSharp.Dom.IElement popup = cut.Find(".amc-popup");
        Assert.Contains(size, popup.ClassList);
        Assert.DoesNotContain("_small", popup.ClassList);
    }

    [Fact]
    public void Additional_classes_are_added_to_the_popup_root()
    {
        IRenderedComponent<PopupComp> cut = RenderPopup(p => p.Add(x => x.AdditionalClasses, "my-popup wide"));

        AngleSharp.Dom.IElement popup = cut.Find(".amc-popup");
        Assert.Contains("my-popup", popup.ClassList);
        Assert.Contains("wide", popup.ClassList);
        Assert.Contains("modal", popup.ClassList);
    }

    [Fact]
    public void Child_content_is_rendered_in_the_body()
    {
        IRenderedComponent<PopupComp> cut = RenderPopup(p => p.AddChildContent("<span id=\"inner\">hello</span>"));

        Assert.Equal("hello", cut.Find(".modal-content .body #inner").TextContent);
    }

    [Fact]
    public void A_title_is_rendered_in_a_header()
    {
        IRenderedComponent<PopupComp> cut = RenderPopup(p => p.Add(x => x.Title, "Settings"));

        Assert.Equal("Settings", cut.Find(".header strong").TextContent);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_missing_or_blank_title_renders_no_header(string? title)
    {
        IRenderedComponent<PopupComp> cut = RenderPopup(p => p.Add(x => x.Title, title));

        Assert.Empty(cut.FindAll(".header"));
    }

    [Fact]
    public void A_header_close_button_replaces_the_bottom_close_button()
    {
        IRenderedComponent<PopupComp> cut = RenderPopup(p => p.Add(x => x.Title, "T").Add(x => x.HeaderClose, true));

        Assert.Single(cut.FindAll(".header .closemodal"));
        Assert.Empty(cut.FindAll(".actions"));
        Assert.Empty(cut.FindAll("button.close"));
    }

    [Fact]
    public void Without_a_header_close_button_or_footer_a_centered_close_button_is_rendered_at_the_bottom()
    {
        IRenderedComponent<PopupComp> cut = RenderPopup();

        AngleSharp.Dom.IElement actions = cut.Find(".actions");
        Assert.Contains("_center", actions.ClassList);
        Assert.Equal("Close", actions.QuerySelector("button.close")!.GetAttribute("aria-label"));
        Assert.Empty(cut.FindAll(".closemodal"));
    }

    [Fact]
    public void A_footer_is_rendered_in_the_actions_area_and_suppresses_the_default_close_button()
    {
        IRenderedComponent<PopupComp> cut = RenderPopup(p => p.Add(x => x.Footer, "<button id=\"ok\">OK</button>"));

        AngleSharp.Dom.IElement actions = cut.Find(".actions");
        Assert.DoesNotContain("_center", actions.ClassList);
        Assert.NotNull(actions.QuerySelector("#ok"));
        Assert.Empty(cut.FindAll("button.close"));
    }

    [Fact]
    public void A_footer_is_rendered_even_when_the_header_close_button_is_used()
    {
        IRenderedComponent<PopupComp> cut = RenderPopup(p => p.Add(x => x.Footer, "<i id=\"foot\"></i>").Add(x => x.HeaderClose, true).Add(x => x.Title, "T"));

        Assert.NotNull(cut.Find(".actions #foot"));
        Assert.Single(cut.FindAll(".closemodal"));
    }

    [Fact]
    public void Without_the_default_chrome_only_a_slot_with_the_content_is_rendered()
    {
        IRenderedComponent<PopupComp> cut = RenderPopup(p => p
            .Add(x => x.UseDefaultChrome, false)
            .Add(x => x.Title, "Ignored")
            .Add(x => x.HeaderClose, true)
            .AddChildContent("<em id=\"custom\">custom</em>"));

        Assert.NotNull(cut.Find(".modal-slot #custom"));
        Assert.Empty(cut.FindAll(".modal-content"));
        Assert.Empty(cut.FindAll(".header"));
        Assert.Empty(cut.FindAll("button"));
    }

    [Fact]
    public void Opening_a_popup_reports_true_through_IsOpenChanged_and_adds_a_popup_fragment_to_the_address()
    {
        string before = Navigation.Uri;
        List<bool> changes = [];

        RenderPopup(p => p.Add(x => x.IsOpenChanged, Record(changes)));

        Assert.Equal([true], changes);
        Assert.Equal(before + "#popup", Navigation.Uri);
    }

    [Fact]
    public void Opening_a_popup_pushes_a_history_entry_instead_of_replacing_the_current_one()
    {
        RenderPopup();

        NavigationHistory entry = Assert.Single(Services.GetRequiredService<BunitNavigationManager>().History);
        Assert.EndsWith("#popup", entry.Uri);
        Assert.False(entry.Options.ReplaceHistoryEntry);
    }

    [Fact]
    public void The_close_button_closes_the_popup_reports_false_and_goes_back_in_history()
    {
        List<bool> changes = [];
        IRenderedComponent<PopupComp> cut = RenderPopup(p => p.Add(x => x.IsOpenChanged, Record(changes)));

        cut.Find("button.close").Click();

        Assert.Empty(cut.FindAll(".amc-popup"));
        Assert.Equal([true, false], changes);
        Assert.Single(Js.RuntimeCalls("history.back"));
    }

    [Fact]
    public void The_header_close_button_closes_the_popup()
    {
        IRenderedComponent<PopupComp> cut = RenderPopup(p => p.Add(x => x.Title, "T").Add(x => x.HeaderClose, true));

        cut.Find(".closemodal").Click();

        Assert.Empty(cut.FindAll(".amc-popup"));
    }

    [Fact]
    public void Clicking_the_overlay_closes_the_popup()
    {
        List<bool> changes = [];
        IRenderedComponent<PopupComp> cut = RenderPopup(p => p.Add(x => x.IsOpenChanged, Record(changes)));

        cut.Find(".amc-popup").Click();

        Assert.Empty(cut.FindAll(".amc-popup"));
        Assert.False(changes.Last());
    }

    [Fact]
    public void Clicking_the_overlay_does_nothing_when_closing_on_click_is_prevented()
    {
        IRenderedComponent<PopupComp> cut = RenderPopup(p => p.Add(x => x.PreventCloseOnClick, true));

        cut.Find(".amc-popup").Click();

        Assert.Single(cut.FindAll(".amc-popup"));
        Assert.Empty(Js.RuntimeCalls("history.back"));
    }

    [Fact]
    public void Clicking_content_inside_the_dialog_does_not_close_the_popup()
    {
        int clicks = 0;
        IRenderedComponent<PopupComp> cut = RenderPopup(p => p.AddChildContent(ClickableContent(() => clicks++)));

        cut.Find("#inside").Click();

        Assert.Equal(1, clicks);
        Assert.Single(cut.FindAll(".amc-popup"));
    }

    [Fact]
    public void Clicking_content_inside_a_chromeless_popup_does_not_close_it()
    {
        int clicks = 0;
        IRenderedComponent<PopupComp> cut = RenderPopup(p => p.Add(x => x.UseDefaultChrome, false).AddChildContent(ClickableContent(() => clicks++)));

        cut.Find("#inside").Click();

        Assert.Equal(1, clicks);
        Assert.Single(cut.FindAll(".amc-popup"));
    }

    private static RenderFragment ClickableContent(Action onClick) => builder =>
    {
        builder.OpenElement(0, "button");
        builder.AddAttribute(1, "id", "inside");
        builder.AddAttribute(2, "onclick", EventCallback.Factory.Create<MouseEventArgs>(onClick.Target ?? new object(), _ => onClick()));
        builder.AddContent(3, "inside");
        builder.CloseElement();
    };

    [Fact]
    public void A_parent_can_close_the_popup_by_setting_IsOpen_to_false()
    {
        List<bool> changes = [];
        IRenderedComponent<PopupComp> cut = RenderPopup(p => p.Add(x => x.IsOpenChanged, Record(changes)));

        cut.Render(p => p.Add(x => x.IsOpen, false));

        Assert.Empty(cut.FindAll(".amc-popup"));
        Assert.Equal([true, false], changes);
    }

    [Fact]
    public void A_parent_can_open_the_popup_later_by_setting_IsOpen_to_true()
    {
        List<bool> changes = [];
        IRenderedComponent<PopupComp> cut = RenderPopup(p => p.Add(x => x.IsOpenChanged, Record(changes)), isOpen: false);
        Assert.Empty(changes);

        cut.Render(p => p.Add(x => x.IsOpen, true));

        Assert.Single(cut.FindAll(".amc-popup"));
        Assert.Equal([true], changes);
    }

    [Fact]
    public async Task Open_and_Close_can_be_called_directly()
    {
        IRenderedComponent<PopupComp> cut = RenderPopup(isOpen: false);

        await cut.InvokeAsync(() => cut.Instance.Open());
        Assert.Single(cut.FindAll(".amc-popup"));
        Assert.True(cut.Instance.IsOpen);

        await cut.InvokeAsync(() => cut.Instance.Close());
        Assert.Empty(cut.FindAll(".amc-popup"));
        Assert.False(cut.Instance.IsOpen);
    }

    [Fact]
    public async Task SoftClose_closes_without_touching_the_browser_history()
    {
        List<bool> changes = [];
        IRenderedComponent<PopupComp> cut = RenderPopup(p => p.Add(x => x.IsOpenChanged, Record(changes)));

        await cut.InvokeAsync(() => cut.Instance.SoftClose());

        Assert.Empty(cut.FindAll(".amc-popup"));
        Assert.False(changes.Last());
        Assert.Empty(Js.RuntimeCalls("history.back"));
    }

    [Fact]
    public async Task Navigating_back_to_the_page_closes_the_popup_without_another_history_step()
    {
        string page = Navigation.Uri;
        List<bool> changes = [];
        IRenderedComponent<PopupComp> cut = RenderPopup(p => p.Add(x => x.IsOpenChanged, Record(changes)));

        await cut.InvokeAsync(() => Navigation.NavigateTo(page));

        Assert.Empty(cut.FindAll(".amc-popup"));
        Assert.False(changes.Last());
        Assert.Empty(Js.RuntimeCalls("history.back"));
    }

    [Fact]
    public async Task Navigating_to_another_page_closes_the_popup()
    {
        IRenderedComponent<PopupComp> cut = RenderPopup();

        await cut.InvokeAsync(() => Navigation.NavigateTo("/somewhere-else"));

        Assert.Empty(cut.FindAll(".amc-popup"));
    }

    [Fact]
    public async Task Navigating_back_keeps_the_popup_open_when_closing_on_back_is_off()
    {
        string page = Navigation.Uri;
        IRenderedComponent<PopupComp> cut = RenderPopup(p => p.Add(x => x.CloseOnBack, false));

        await cut.InvokeAsync(() => Navigation.NavigateTo(page));

        Assert.Single(cut.FindAll(".amc-popup"));
    }

    [Fact]
    public void Navigation_does_not_report_a_close_for_a_popup_that_is_not_open()
    {
        List<bool> changes = [];
        RenderPopup(p => p.Add(x => x.IsOpenChanged, Record(changes)), isOpen: false);

        Navigation.NavigateTo("/elsewhere");

        Assert.Empty(changes);
    }

    [Fact]
    public async Task Disposing_an_open_popup_reports_it_as_closed()
    {
        List<bool> changes = [];
        RenderPopup(p => p.Add(x => x.IsOpenChanged, Record(changes)));

        await DisposeComponentsAsync();

        Assert.False(changes.Last());
    }

}
