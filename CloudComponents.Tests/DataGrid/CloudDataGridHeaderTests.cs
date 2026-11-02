using AngleSharp.Dom;
using AngryMonkey.CloudComponents.DataGrid.Components;
using AngryMonkey.CloudComponents.DataGrid.Models;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace CloudComponents.Tests.DataGrid;

/// <summary>The toolbar above the grid: label, direct and bulk actions, the More menu, built-in Refresh and Search.</summary>
public class CloudDataGridHeaderTests : DataGridTestBase
{
    private static CloudDataGridAction Button(string key, string? text = null, string? tooltip = null) =>
        new() { Key = key, Type = CloudDataGridActionType.Button, Text = text, Tooltip = tooltip };

    private static RenderFragment Markup(string html) => builder => builder.AddMarkupContent(0, html);

    private IRenderedComponent<CloudDataGridHeader> RenderHeader(Action<ComponentParameterCollectionBuilder<CloudDataGridHeader>>? configure = null) =>
        Render<CloudDataGridHeader>(parameters =>
        {
            parameters.Add(header => header.AllowRefresh, false);
            configure?.Invoke(parameters);
        });

    private static IReadOnlyList<IElement> DirectActions(IRenderedComponent<CloudDataGridHeader> cut) =>
        cut.FindAll(".cloudgridheader-actions .cloudgridheader-action");

    private static IReadOnlyList<IElement> BulkActions(IRenderedComponent<CloudDataGridHeader> cut) =>
        cut.FindAll(".cloudgridheader-bulkactions .cloudgridheader-action");

    private static IReadOnlyList<IElement> MoreItems(IRenderedComponent<CloudDataGridHeader> cut) =>
        cut.FindAll(".cloudgridheader-more-dropdown .cloudgridheader-more-item");

    // ----- label and built-ins ----------------------------------------------------------------------------------------------

    [Fact]
    public void The_label_is_rendered_as_the_heading()
    {
        IRenderedComponent<CloudDataGridHeader> cut = RenderHeader(parameters => parameters.Add(header => header.Label, "Customers"));

        Assert.Equal("Customers", Text(cut.Find("h2")));
    }

    [Fact]
    public void A_header_without_a_label_has_an_empty_heading()
    {
        IRenderedComponent<CloudDataGridHeader> cut = RenderHeader();

        Assert.Equal(string.Empty, Text(cut.Find("h2")));
    }

    [Fact]
    public void The_refresh_button_is_on_by_default()
    {
        IRenderedComponent<CloudDataGridHeader> cut = Render<CloudDataGridHeader>();

        Assert.Single(cut.FindAll("button[title='Refresh']"));
    }

    [Fact]
    public void Turning_refresh_off_removes_the_refresh_button()
    {
        IRenderedComponent<CloudDataGridHeader> cut = RenderHeader();

        Assert.Empty(cut.FindAll("button[title='Refresh']"));
    }

    [Fact]
    public void The_refresh_button_shows_only_an_icon()
    {
        IRenderedComponent<CloudDataGridHeader> cut = Render<CloudDataGridHeader>();

        IElement refresh = cut.Find("button[title='Refresh']");

        Assert.NotNull(refresh.QuerySelector("svg"));
        Assert.Empty(refresh.QuerySelectorAll("span"));
    }

    [Fact]
    public void Clicking_refresh_raises_OnRefresh_and_not_OnActionClicked()
    {
        int refreshes = 0;
        List<CloudDataGridActionEventArgs> actions = [];
        IRenderedComponent<CloudDataGridHeader> cut = Render<CloudDataGridHeader>(parameters =>
        {
            parameters.Add(header => header.OnRefresh, () => { refreshes++; });
            parameters.Add(header => header.OnActionClicked, (CloudDataGridActionEventArgs args) => actions.Add(args));
        });

        cut.Find("button[title='Refresh']").Click();

        Assert.Equal(1, refreshes);
        Assert.Empty(actions);
    }

    [Fact]
    public void Built_in_actions_follow_the_consumer_actions_with_refresh_before_search()
    {
        IRenderedComponent<CloudDataGridHeader> cut = Render<CloudDataGridHeader>(parameters =>
        {
            parameters.Add(header => header.Actions, [Button("a", tooltip: "First"), Button("b", tooltip: "Second")]);
            parameters.Add(header => header.AllowSearch, true);
        });

        Assert.Equal(["First", "Second", "Refresh", "Search"], DirectActions(cut).Select(action => action.GetAttribute("title")));
    }

    [Fact]
    public void Search_is_off_by_default()
    {
        IRenderedComponent<CloudDataGridHeader> cut = Render<CloudDataGridHeader>();

        Assert.Empty(cut.FindAll("button[title='Search']"));
    }

    // ----- button and link actions ------------------------------------------------------------------------------------------

    [Fact]
    public void A_button_action_is_rendered_with_its_tooltip_text_and_css_class()
    {
        CloudDataGridAction action = Button("add", text: "Add", tooltip: "Add a record");
        action.CssClass = "primary";

        IRenderedComponent<CloudDataGridHeader> cut = RenderHeader(parameters => parameters.Add(header => header.Actions, [action]));

        IElement button = Assert.Single(DirectActions(cut));
        Assert.Equal("button", button.TagName.ToLowerInvariant());
        Assert.Equal("Add a record", button.GetAttribute("title"));
        Assert.Contains("primary", button.ClassList);
        Assert.Equal("Add", Text(button.QuerySelector("span")!));
    }

    [Fact]
    public void An_action_without_a_tooltip_uses_its_text_as_the_title()
    {
        IRenderedComponent<CloudDataGridHeader> cut = RenderHeader(parameters =>
            parameters.Add(header => header.Actions, [Button("add", text: "Add")]));

        Assert.Equal("Add", Assert.Single(DirectActions(cut)).GetAttribute("title"));
    }

    [Fact]
    public void An_icon_only_action_does_not_render_its_text()
    {
        CloudDataGridAction action = Button("add", text: "Add");
        action.IconOnly = true;
        action.IconSvg = "<svg class='mine'></svg>";

        IRenderedComponent<CloudDataGridHeader> cut = RenderHeader(parameters => parameters.Add(header => header.Actions, [action]));

        IElement button = Assert.Single(DirectActions(cut));
        Assert.Empty(button.QuerySelectorAll("span"));
        Assert.NotNull(button.QuerySelector("svg.mine"));
    }

    [Fact]
    public void An_icon_fragment_wins_over_icon_svg_markup()
    {
        CloudDataGridAction action = Button("add");
        action.Icon = Markup("<i class='fragment'></i>");
        action.IconSvg = "<svg class='legacy'></svg>";

        IRenderedComponent<CloudDataGridHeader> cut = RenderHeader(parameters => parameters.Add(header => header.Actions, [action]));

        IElement button = Assert.Single(DirectActions(cut));
        Assert.NotNull(button.QuerySelector("i.fragment"));
        Assert.Null(button.QuerySelector("svg.legacy"));
    }

    [Fact]
    public void Clicking_a_button_action_raises_OnActionClicked_with_the_action_and_no_ids()
    {
        CloudDataGridAction action = Button("add", text: "Add");
        List<CloudDataGridActionEventArgs> events = [];
        IRenderedComponent<CloudDataGridHeader> cut = RenderHeader(parameters =>
        {
            parameters.Add(header => header.Actions, [action]);
            parameters.Add(header => header.OnActionClicked, (CloudDataGridActionEventArgs args) => events.Add(args));
        });

        Assert.Single(DirectActions(cut)).Click();

        CloudDataGridActionEventArgs raised = Assert.Single(events);
        Assert.Same(action, raised.Action);
        Assert.Empty(raised.RecordIds);
    }

    [Fact]
    public void An_action_hidden_from_the_header_is_not_rendered_there()
    {
        CloudDataGridAction hidden = Button("hidden", text: "Hidden");
        hidden.ShowOnHeader = false;

        IRenderedComponent<CloudDataGridHeader> cut = RenderHeader(parameters => parameters.Add(header => header.Actions, [hidden]));

        Assert.Empty(DirectActions(cut));
        Assert.Empty(cut.FindAll(".cloudgridheader-more"));
    }

    [Fact]
    public void A_link_action_is_an_anchor_with_href_target_and_title()
    {
        CloudDataGridAction link = new()
        {
            Key = "docs",
            Type = CloudDataGridActionType.Link,
            Href = "/docs",
            Target = "_blank",
            Text = "Docs"
        };

        IRenderedComponent<CloudDataGridHeader> cut = RenderHeader(parameters => parameters.Add(header => header.Actions, [link]));

        IElement anchor = Assert.Single(DirectActions(cut));
        Assert.Equal("a", anchor.TagName.ToLowerInvariant());
        Assert.Equal("/docs", anchor.GetAttribute("href"));
        Assert.Equal("_blank", anchor.GetAttribute("target"));
        Assert.Equal("Docs", anchor.GetAttribute("title"));
    }

    // ----- bulk actions -------------------------------------------------------------------------------------------------------

    private static CloudDataGridAction BulkDelete()
    {
        CloudDataGridAction action = Button("delete", text: "Delete");
        action.ShowOnBulkHeader = true;
        action.ShowOnHeader = false;
        return action;
    }

    [Fact]
    public void Bulk_actions_are_hidden_while_nothing_is_selected()
    {
        IRenderedComponent<CloudDataGridHeader> cut = RenderHeader(parameters => parameters.Add(header => header.Actions, [BulkDelete()]));

        Assert.Empty(cut.FindAll(".cloudgridheader-bulkactions"));
        Assert.Empty(DirectActions(cut));
    }

    [Fact]
    public void Bulk_actions_appear_once_rows_are_selected_and_are_marked_bulk()
    {
        IRenderedComponent<CloudDataGridHeader> cut = RenderHeader(parameters =>
        {
            parameters.Add(header => header.Actions, [BulkDelete()]);
            parameters.Add(header => header.SelectedRecords, [Id(1)]);
        });

        IElement bulk = Assert.Single(BulkActions(cut));
        Assert.Contains("_bulk", bulk.ClassList);
        Assert.Equal("Delete", bulk.GetAttribute("title"));
    }

    [Fact]
    public void Clicking_a_bulk_action_raises_it_with_the_ids_of_every_selected_row()
    {
        List<CloudDataGridActionEventArgs> events = [];
        List<Guid> selected = [Id(1), Id(2), Id(3)];
        IRenderedComponent<CloudDataGridHeader> cut = RenderHeader(parameters =>
        {
            parameters.Add(header => header.Actions, [BulkDelete()]);
            parameters.Add(header => header.SelectedRecords, selected);
            parameters.Add(header => header.OnActionClicked, (CloudDataGridActionEventArgs args) => events.Add(args));
        });

        Assert.Single(BulkActions(cut)).Click();
        selected.Clear();

        CloudDataGridActionEventArgs raised = Assert.Single(events);
        Assert.Equal([Id(1), Id(2), Id(3)], raised.RecordIds);
    }

    [Fact]
    public void An_action_can_be_both_a_direct_and_a_bulk_action()
    {
        CloudDataGridAction both = Button("both", text: "Both");
        both.ShowOnBulkHeader = true;

        IRenderedComponent<CloudDataGridHeader> cut = RenderHeader(parameters =>
        {
            parameters.Add(header => header.Actions, [both]);
            parameters.Add(header => header.SelectedRecords, [Id(1)]);
        });

        Assert.Single(BulkActions(cut));
        Assert.Single(DirectActions(cut));
    }

    // ----- More menu ------------------------------------------------------------------------------------------------------------

    private static CloudDataGridAction InMore(string key, string text, CloudDataGridActionType type = CloudDataGridActionType.Button)
    {
        CloudDataGridAction action = Button(key, text: text);
        action.ShowInMore = true;
        action.Type = type;
        return action;
    }

    [Fact]
    public void Without_more_actions_there_is_no_more_button()
    {
        IRenderedComponent<CloudDataGridHeader> cut = RenderHeader(parameters => parameters.Add(header => header.Actions, [Button("a", text: "A")]));

        Assert.Empty(cut.FindAll("button[title='More']"));
    }

    [Fact]
    public void A_more_action_is_not_rendered_as_a_direct_action()
    {
        IRenderedComponent<CloudDataGridHeader> cut = RenderHeader(parameters => parameters.Add(header => header.Actions, [InMore("a", "A")]));

        Assert.Empty(DirectActions(cut));
        Assert.Single(cut.FindAll("button[title='More']"));
    }

    [Fact]
    public void The_more_menu_starts_closed_and_opens_and_closes_with_its_button()
    {
        IRenderedComponent<CloudDataGridHeader> cut = RenderHeader(parameters => parameters.Add(header => header.Actions, [InMore("a", "A"), InMore("b", "B")]));
        Assert.Empty(cut.FindAll(".cloudgridheader-more-dropdown"));

        cut.Find("button[title='More']").Click();
        Assert.Equal(["A", "B"], Texts(MoreItems(cut)));
        Assert.Contains("_more-open", cut.Find("button[title='More']").ClassList);

        cut.Find("button[title='More']").Click();
        Assert.Empty(cut.FindAll(".cloudgridheader-more-dropdown"));
    }

    [Fact]
    public void Clicking_a_more_button_item_raises_the_action_and_closes_the_menu()
    {
        CloudDataGridAction item = InMore("export", "Export CSV");
        List<CloudDataGridActionEventArgs> events = [];
        IRenderedComponent<CloudDataGridHeader> cut = RenderHeader(parameters =>
        {
            parameters.Add(header => header.Actions, [item]);
            parameters.Add(header => header.OnActionClicked, (CloudDataGridActionEventArgs args) => events.Add(args));
        });
        cut.Find("button[title='More']").Click();

        MoreItems(cut).Single().Click();

        Assert.Same(item, Assert.Single(events).Action);
        Assert.Empty(cut.FindAll(".cloudgridheader-more-dropdown"));
    }

    [Fact]
    public void A_more_link_item_is_an_anchor()
    {
        CloudDataGridAction link = InMore("docs", "Docs", CloudDataGridActionType.Link);
        link.Href = "/docs";

        IRenderedComponent<CloudDataGridHeader> cut = RenderHeader(parameters => parameters.Add(header => header.Actions, [link]));
        cut.Find("button[title='More']").Click();

        IElement anchor = Assert.Single(MoreItems(cut));
        Assert.Equal("a", anchor.TagName.ToLowerInvariant());
        Assert.Equal("/docs", anchor.GetAttribute("href"));
    }

    [Fact]
    public void A_more_element_item_opens_its_panel_instead_of_raising_a_click()
    {
        CloudDataGridAction element = InMore("panel", "Panel", CloudDataGridActionType.Element);
        element.ChildContent = Markup("<span class='inside'>hello</span>");
        List<CloudDataGridActionEventArgs> events = [];
        IRenderedComponent<CloudDataGridHeader> cut = RenderHeader(parameters =>
        {
            parameters.Add(header => header.Actions, [element]);
            parameters.Add(header => header.OnActionClicked, (CloudDataGridActionEventArgs args) => events.Add(args));
        });
        cut.Find("button[title='More']").Click();

        MoreItems(cut).Single().Click();

        Assert.Equal("hello", Text(cut.Find(".inside")));
        Assert.Empty(events);
        Assert.Empty(cut.FindAll(".cloudgridheader-more-dropdown"));
    }

    // ----- element actions --------------------------------------------------------------------------------------------------------

    private static CloudDataGridAction ElementAction(string key = "panel") => new()
    {
        Key = key,
        Type = CloudDataGridActionType.Element,
        Tooltip = "Open panel",
        ChildContent = Markup("<span class='inside'>hello</span>")
    };

    [Fact]
    public void An_element_action_starts_collapsed()
    {
        IRenderedComponent<CloudDataGridHeader> cut = RenderHeader(parameters => parameters.Add(header => header.Actions, [ElementAction()]));

        Assert.Empty(cut.FindAll(".inside"));
        Assert.Empty(cut.FindAll("._cancel"));
        Assert.Single(DirectActions(cut));
    }

    [Fact]
    public void Clicking_an_element_action_shows_its_content_and_a_cancel_button_in_place_of_the_actions()
    {
        IRenderedComponent<CloudDataGridHeader> cut = RenderHeader(parameters => parameters.Add(header => header.Actions, [ElementAction()]));

        cut.Find("button[title='Open panel']").Click();

        Assert.Equal("hello", Text(cut.Find(".inside")));
        Assert.Equal("cancel", Text(cut.Find("._cancel")));
        Assert.Empty(cut.FindAll("button[title='Open panel']"));
        Assert.Contains("_mode-active", cut.Find(".cloudgridheader-actions").ClassList);
    }

    [Fact]
    public void Cancelling_an_element_action_restores_the_actions_and_calls_OnDeactivated()
    {
        int deactivated = 0;
        CloudDataGridAction element = ElementAction();
        element.OnDeactivated = () =>
        {
            deactivated++;
            return Task.CompletedTask;
        };
        IRenderedComponent<CloudDataGridHeader> cut = RenderHeader(parameters => parameters.Add(header => header.Actions, [element]));
        cut.Find("button[title='Open panel']").Click();

        cut.Find("._cancel").Click();

        Assert.Equal(1, deactivated);
        Assert.Empty(cut.FindAll(".inside"));
        Assert.Single(cut.FindAll("button[title='Open panel']"));
        Assert.DoesNotContain("_mode-active", cut.Find(".cloudgridheader-actions").ClassList);
    }

    [Fact]
    public void Activating_an_element_action_hands_it_a_callback_that_closes_the_panel()
    {
        Func<Task>? close = null;
        int deactivated = 0;
        CloudDataGridAction element = ElementAction();
        element.OnActivated = cancel => close = cancel;
        element.OnDeactivated = () =>
        {
            deactivated++;
            return Task.CompletedTask;
        };
        IRenderedComponent<CloudDataGridHeader> cut = RenderHeader(parameters => parameters.Add(header => header.Actions, [element]));

        cut.Find("button[title='Open panel']").Click();
        Assert.NotNull(close);
        cut.InvokeAsync(close!).GetAwaiter().GetResult();
        cut.Render(_ => { }); // the callback itself does not re-render; the grid re-renders the header after its own click handlers

        Assert.Equal(1, deactivated);
        Assert.Empty(cut.FindAll(".inside"));
    }

    [Fact]
    public void Opening_an_element_action_hides_the_bulk_actions_and_the_more_button()
    {
        CloudDataGridAction more = InMore("more", "More item");
        IRenderedComponent<CloudDataGridHeader> cut = RenderHeader(parameters =>
        {
            parameters.Add(header => header.Actions, [ElementAction(), BulkDelete(), more]);
            parameters.Add(header => header.SelectedRecords, [Id(1)]);
        });
        Assert.Single(BulkActions(cut));
        Assert.Single(cut.FindAll("button[title='More']"));

        cut.Find("button[title='Open panel']").Click();

        Assert.Empty(cut.FindAll(".cloudgridheader-bulkactions"));
        Assert.Empty(cut.FindAll("button[title='More']"));
    }

    [Fact]
    public void Opening_an_element_action_closes_an_open_more_menu()
    {
        CloudDataGridAction more = InMore("more", "More item");
        IRenderedComponent<CloudDataGridHeader> cut = RenderHeader(parameters => parameters.Add(header => header.Actions, [ElementAction(), more]));
        cut.Find("button[title='More']").Click();
        Assert.Single(cut.FindAll(".cloudgridheader-more-dropdown"));

        cut.Find("button[title='Open panel']").Click();

        Assert.Empty(cut.FindAll(".cloudgridheader-more-dropdown"));
    }

    [Fact]
    public void An_element_action_that_is_not_a_button_does_not_raise_OnActionClicked()
    {
        List<CloudDataGridActionEventArgs> events = [];
        IRenderedComponent<CloudDataGridHeader> cut = RenderHeader(parameters =>
        {
            parameters.Add(header => header.Actions, [ElementAction()]);
            parameters.Add(header => header.OnActionClicked, (CloudDataGridActionEventArgs args) => events.Add(args));
        });

        cut.Find("button[title='Open panel']").Click();

        Assert.Empty(events);
    }

    [Fact(Skip = "BUG: nothing invokes CloudDataGridAction.OnClicked. Its documentation (Models/CloudGridAction.cs, CloudGridActionType.cs) says a Button action 'fires OnClicked' when clicked, but CloudDataGridHeader/CloudDataGridBody only raise the grid-level OnActionClicked, so an action's own OnClicked callback is never called.")]
    public void Clicking_a_button_action_raises_the_actions_own_OnClicked_callback()
    {
        List<CloudDataGridActionEventArgs> own = [];
        CloudDataGridAction action = Button("add", text: "Add");
        action.OnClicked = EventCallback.Factory.Create<CloudDataGridActionEventArgs>(this, args => own.Add(args));
        IRenderedComponent<CloudDataGridHeader> cut = RenderHeader(parameters => parameters.Add(header => header.Actions, [action]));

        Assert.Single(DirectActions(cut)).Click();

        Assert.Single(own);
    }

    [Fact]
    public void The_extra_actions_fragment_is_rendered_in_the_toolbar()
    {
        IRenderedComponent<CloudDataGridHeader> cut = RenderHeader(parameters => parameters.Add(header => header.ExtraActions, Markup("<button class='mine'>extra</button>")));

        Assert.Equal("extra", Text(cut.Find(".right .mine")));
    }

    // ----- search ----------------------------------------------------------------------------------------------------------------

    private IRenderedComponent<CloudDataGridHeader> RenderSearchHeader(List<string?> searches, int debounce = 1)
    {
        return Render<CloudDataGridHeader>(parameters =>
        {
            parameters.Add(header => header.AllowRefresh, false);
            parameters.Add(header => header.AllowSearch, true);
            parameters.Add(header => header.SearchDebounceMilliseconds, debounce);
            parameters.Add(header => header.OnSearchChanged, (string? query) => searches.Add(query));
        });
    }

    private static void OpenSearch(IRenderedComponent<CloudDataGridHeader> cut) => cut.Find("button[title='Search']").Click();

    private static IElement SearchBox(IRenderedComponent<CloudDataGridHeader> cut) => cut.Find("input.cloudgridheader-search");

    [Fact]
    public void Opening_search_shows_a_focused_search_input_in_place_of_the_other_actions()
    {
        IRenderedComponent<CloudDataGridHeader> cut = RenderSearchHeader([]);

        OpenSearch(cut);

        IElement input = SearchBox(cut);
        Assert.Equal("search", input.GetAttribute("type"));
        Assert.Equal("Search...", input.GetAttribute("placeholder"));
        Assert.Empty(cut.FindAll("button[title='Search']"));
        JSInterop.VerifyFocusAsyncInvoke();
    }

    [Fact]
    public void Typing_in_search_raises_OnSearchChanged_with_the_text_after_the_debounce_delay()
    {
        List<string?> searches = [];
        IRenderedComponent<CloudDataGridHeader> cut = RenderSearchHeader(searches);
        OpenSearch(cut);

        SearchBox(cut).Input("alice");

        cut.WaitForAssertion(() => Assert.Equal(["alice"], searches));
    }

    [Fact]
    public void The_search_text_is_trimmed_before_it_is_raised()
    {
        List<string?> searches = [];
        IRenderedComponent<CloudDataGridHeader> cut = RenderSearchHeader(searches);
        OpenSearch(cut);

        SearchBox(cut).Input("  alice  ");

        cut.WaitForAssertion(() => Assert.Equal(["alice"], searches));
    }

    [Fact]
    public void Whitespace_only_search_text_is_raised_as_null()
    {
        List<string?> searches = [];
        IRenderedComponent<CloudDataGridHeader> cut = RenderSearchHeader(searches);
        OpenSearch(cut);

        SearchBox(cut).Input("   ");

        cut.WaitForAssertion(() => Assert.Equal([null], searches));
    }

    [Fact]
    public async Task Typing_again_within_the_debounce_delay_raises_only_the_last_text()
    {
        List<string?> searches = [];
        IRenderedComponent<CloudDataGridHeader> cut = RenderSearchHeader(searches, debounce: 400);
        OpenSearch(cut);
        IElement input = SearchBox(cut);

        input.Input("a");
        await Task.Delay(50);
        cut.Find("input.cloudgridheader-search").Input("ab");

        cut.WaitForAssertion(() => Assert.Equal(["ab"], searches), TimeSpan.FromSeconds(3));
        await Task.Delay(500);
        Assert.Equal(["ab"], searches);
    }

    [Fact]
    public void Nothing_is_raised_before_the_debounce_delay_has_passed()
    {
        List<string?> searches = [];
        IRenderedComponent<CloudDataGridHeader> cut = RenderSearchHeader(searches, debounce: 60_000);
        OpenSearch(cut);

        _ = Task.Run(() => SearchBox(cut).Input("alice"));
        Thread.Sleep(200);

        Assert.Empty(searches);
    }

    [Fact]
    public void Escape_with_text_clears_the_search_raises_null_and_keeps_it_open()
    {
        List<string?> searches = [];
        IRenderedComponent<CloudDataGridHeader> cut = RenderSearchHeader(searches);
        OpenSearch(cut);
        SearchBox(cut).Input("alice");
        cut.WaitForAssertion(() => Assert.Single(searches));

        SearchBox(cut).KeyDown(new KeyboardEventArgs { Key = "Escape" });

        cut.WaitForAssertion(() => Assert.Equal(["alice", null], searches));
        Assert.Equal(string.Empty, SearchBox(cut).GetAttribute("value") ?? string.Empty);
    }

    [Fact]
    public void Escape_in_an_empty_search_closes_it_without_raising_anything()
    {
        List<string?> searches = [];
        IRenderedComponent<CloudDataGridHeader> cut = RenderSearchHeader(searches);
        OpenSearch(cut);

        SearchBox(cut).KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Empty(cut.FindAll("input.cloudgridheader-search"));
        Assert.Single(cut.FindAll("button[title='Search']"));
        Assert.Empty(searches);
    }

    [Fact]
    public void Other_keys_do_not_close_or_clear_the_search()
    {
        List<string?> searches = [];
        IRenderedComponent<CloudDataGridHeader> cut = RenderSearchHeader(searches);
        OpenSearch(cut);

        SearchBox(cut).KeyDown(new KeyboardEventArgs { Key = "a" });
        SearchBox(cut).KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.Single(cut.FindAll("input.cloudgridheader-search"));
        Assert.Empty(searches);
    }

    [Fact]
    public void Leaving_an_empty_search_closes_it()
    {
        IRenderedComponent<CloudDataGridHeader> cut = RenderSearchHeader([]);
        OpenSearch(cut);

        SearchBox(cut).Blur();

        Assert.Empty(cut.FindAll("input.cloudgridheader-search"));
    }

    [Fact]
    public void Leaving_a_search_that_has_text_keeps_it_open()
    {
        List<string?> searches = [];
        IRenderedComponent<CloudDataGridHeader> cut = RenderSearchHeader(searches);
        OpenSearch(cut);
        SearchBox(cut).Input("alice");
        cut.WaitForAssertion(() => Assert.Single(searches));

        SearchBox(cut).Blur();

        Assert.Single(cut.FindAll("input.cloudgridheader-search"));
    }

    [Fact]
    public void Cancelling_a_search_with_text_raises_null_and_restores_the_actions()
    {
        List<string?> searches = [];
        IRenderedComponent<CloudDataGridHeader> cut = RenderSearchHeader(searches);
        OpenSearch(cut);
        SearchBox(cut).Input("alice");
        cut.WaitForAssertion(() => Assert.Single(searches));

        cut.Find("._cancel").Click();

        cut.WaitForAssertion(() => Assert.Equal(["alice", null], searches));
        Assert.Empty(cut.FindAll("input.cloudgridheader-search"));
        Assert.Single(cut.FindAll("button[title='Search']"));
    }

    [Fact]
    public void Cancelling_an_empty_search_raises_nothing()
    {
        List<string?> searches = [];
        IRenderedComponent<CloudDataGridHeader> cut = RenderSearchHeader(searches);
        OpenSearch(cut);

        cut.Find("._cancel").Click();

        Assert.Empty(searches);
        Assert.Single(cut.FindAll("button[title='Search']"));
    }

}
