using AngryMonkey.CloudComponents;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace CloudComponents.Tests.Core.Interactive;

public class TabsTests : InteractiveTestBase
{
    /// <summary>A small component a tab can host, so the dynamic-content path is covered.</summary>
    public sealed class Badge : ComponentBase
    {
        [Parameter] public string? Label { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "b");
            builder.AddAttribute(1, "class", "badge");
            builder.AddContent(2, Label);
            builder.CloseElement();
        }
    }

    private IRenderedComponent<Tabs> RenderTabs(List<TabItem> tabs) => Render<Tabs>(parameters => parameters.Add(p => p.TabsList, tabs));

    private static string[] ActiveTabs(IRenderedComponent<Tabs> cut) =>
        [.. cut.FindAll("button.amc-tab").Select((button, index) => (button, index)).Where(pair => pair.button.ClassList.Contains("_active")).Select(pair => pair.button.TextContent.Trim())];

    [Fact]
    public void Each_tab_is_rendered_as_a_button_with_its_text()
    {
        IRenderedComponent<Tabs> cut = RenderTabs([new TabItem("One"), new TabItem("Two"), new TabItem("Three")]);

        Assert.Equal(["One", "Two", "Three"], cut.FindAll(".amc-tabs > button.amc-tab").Select(button => button.TextContent.Trim()));
        Assert.All(cut.FindAll("button.amc-tab"), button => Assert.NotNull(button.QuerySelector("span")));
    }

    [Fact]
    public void An_empty_tab_list_renders_an_empty_tab_strip_without_failing()
    {
        IRenderedComponent<Tabs> cut = RenderTabs([]);

        Assert.Empty(cut.FindAll("button"));
        Assert.NotNull(cut.Find(".amc-tabs"));
    }

    [Fact]
    public void The_first_tab_is_activated_when_none_is_active()
    {
        IRenderedComponent<Tabs> cut = RenderTabs([new TabItem("One"), new TabItem("Two")]);

        Assert.Equal(["One"], ActiveTabs(cut));
    }

    [Fact]
    public void Activating_the_first_tab_by_default_runs_its_activation_callback()
    {
        List<string> log = [];
        TabItem first = new("One") { OnActivated = Record(log, "one") };
        TabItem second = new("Two") { OnActivated = Record(log, "two") };

        RenderTabs([first, second]);

        Assert.Equal(["one"], log);
        Assert.True(first.IsActive);
        Assert.False(second.IsActive);
    }

    [Fact]
    public void A_tab_that_is_already_active_is_kept_and_no_other_tab_is_activated()
    {
        List<string> log = [];
        TabItem first = new("One") { OnActivated = Record(log, "one") };
        TabItem second = new("Two") { IsActive = true, OnActivated = Record(log, "two") };

        IRenderedComponent<Tabs> cut = RenderTabs([first, second]);

        Assert.Equal(["Two"], ActiveTabs(cut));
        Assert.Empty(log);
    }

    [Fact]
    public void Clicking_a_tab_activates_it_and_deactivates_the_previous_one()
    {
        IRenderedComponent<Tabs> cut = RenderTabs([new TabItem("One"), new TabItem("Two"), new TabItem("Three")]);

        cut.FindAll("button.amc-tab")[2].Click();

        Assert.Equal(["Three"], ActiveTabs(cut));
    }

    [Fact]
    public void Clicking_a_tab_runs_only_that_tabs_activation_callback()
    {
        List<string> log = [];
        List<TabItem> tabs =
        [
            new("One") { OnActivated = Record(log, "one") },
            new("Two") { OnActivated = Record(log, "two") }
        ];
        IRenderedComponent<Tabs> cut = RenderTabs(tabs);
        log.Clear();

        cut.FindAll("button.amc-tab")[1].Click();

        Assert.Equal(["two"], log);
    }

    [Fact]
    public void Clicking_the_active_tab_keeps_it_active()
    {
        IRenderedComponent<Tabs> cut = RenderTabs([new TabItem("One"), new TabItem("Two")]);

        cut.FindAll("button.amc-tab")[0].Click();

        Assert.Equal(["One"], ActiveTabs(cut));
    }

    [Fact]
    public void Switching_back_and_forth_always_leaves_exactly_one_tab_active()
    {
        IRenderedComponent<Tabs> cut = RenderTabs([new TabItem("One"), new TabItem("Two"), new TabItem("Three")]);

        foreach (int index in new[] { 1, 2, 0, 2, 1 })
        {
            cut.FindAll("button.amc-tab")[index].Click();

            Assert.Single(ActiveTabs(cut));
        }

        Assert.Equal(["Two"], ActiveTabs(cut));
    }

    [Fact]
    public void A_component_tab_renders_the_component_with_its_parameters()
    {
        TabItem tab = new(typeof(Badge), new Dictionary<string, object> { ["Label"] = "New" });

        IRenderedComponent<Tabs> cut = RenderTabs([tab]);

        Assert.Equal("New", cut.Find("button.amc-tab b.badge").TextContent);
        Assert.Empty(cut.FindAll("button.amc-tab > span"));
    }

    [Fact]
    public void A_component_tab_without_parameters_renders_the_component()
    {
        IRenderedComponent<Tabs> cut = RenderTabs([new TabItem(typeof(Badge))]);

        Assert.NotNull(cut.Find("button.amc-tab b.badge"));
    }

    [Fact]
    public void Text_and_component_tabs_can_be_mixed()
    {
        IRenderedComponent<Tabs> cut = RenderTabs([new TabItem("Plain"), new TabItem(typeof(Badge), new Dictionary<string, object> { ["Label"] = "Rich" })]);

        AngleSharp.Dom.IElement[] buttons = [.. cut.FindAll("button.amc-tab")];
        Assert.Equal("Plain", buttons[0].TextContent.Trim());
        Assert.Equal("Rich", buttons[1].TextContent.Trim());
    }
}

public class TabItemTests
{
    [Fact]
    public void A_text_tab_holds_the_text_and_no_component_type()
    {
        TabItem tab = new("Home");

        Assert.Equal("Home", tab.Content);
        Assert.Null(tab.ContentType);
    }

    [Fact]
    public void A_component_tab_holds_the_type_and_no_content()
    {
        TabItem tab = new(typeof(TabsTests.Badge));

        Assert.Equal(typeof(TabsTests.Badge), tab.ContentType);
        Assert.Null(tab.Content);
    }

    [Fact]
    public void A_component_tab_with_parameters_keeps_them_as_its_content()
    {
        Dictionary<string, object> parameters = new() { ["Label"] = "x" };

        TabItem tab = new(typeof(TabsTests.Badge), parameters);

        Assert.Equal(typeof(TabsTests.Badge), tab.ContentType);
        Assert.Same(parameters, tab.Content);
    }

    [Fact]
    public void A_new_tab_is_not_active_and_has_only_the_base_class()
    {
        TabItem tab = new("x");

        Assert.False(tab.IsActive);
        Assert.Equal("amc-tab", tab.CssClass);
    }

    [Fact]
    public void An_active_tab_adds_the_active_class()
    {
        TabItem tab = new("x") { IsActive = true };

        Assert.Equal("amc-tab _active", tab.CssClass);
    }

    [Fact]
    public async Task Invoking_activation_marks_the_tab_active_and_runs_the_callback()
    {
        int calls = 0;
        TabItem tab = new("x") { OnActivated = EventCallback.Factory.Create(new object(), () => calls++) };

        await tab.InvokeOnActivated();

        Assert.True(tab.IsActive);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Invoking_activation_without_a_callback_still_marks_the_tab_active()
    {
        TabItem tab = new("x");

        await tab.InvokeOnActivated();

        Assert.True(tab.IsActive);
    }
}
