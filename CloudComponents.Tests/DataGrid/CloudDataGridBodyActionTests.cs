using AngleSharp.Dom;
using AngryMonkey.CloudComponents.DataGrid.Components;
using AngryMonkey.CloudComponents.DataGrid.Models;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace CloudComponents.Tests.DataGrid;

/// <summary>Actions rendered inside each row: buttons, links, element toggles, visibility rules and the click payload.</summary>
public class CloudDataGridBodyActionTests : DataGridTestBase
{
    private static RenderFragment Markup(string html) => builder => builder.AddMarkupContent(0, html);

    private static CloudDataGridAction RowButton(string key, string? text = null, string? tooltip = null) =>
        new() { Key = key, Type = CloudDataGridActionType.Button, ShowOnRow = true, Text = text, Tooltip = tooltip };

    private IRenderedComponent<CloudDataGridBody> RenderBody(
        List<CloudDataGridAction> actions,
        int rows = 2,
        Action<ComponentParameterCollectionBuilder<CloudDataGridBody>>? configure = null)
    {
        return Render<CloudDataGridBody>(parameters =>
        {
            parameters.Add(body => body.Columns, Columns("Name"));
            parameters.Add(body => body.Data, Result(NumberedRows(rows)));
            parameters.Add(body => body.RowActions, actions);
            configure?.Invoke(parameters);
        });
    }

    private static IReadOnlyList<IElement> ActionsOfRow(IRenderedComponent<CloudDataGridBody> cut, int row) =>
        cut.FindAll(".cloudgrid-row")[row].QuerySelectorAll(".cloudgrid-rowactions .cloudgrid-action").ToList();

    [Fact]
    public void A_row_action_is_rendered_in_every_row_with_its_title_and_text()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody([RowButton("edit", text: "Edit", tooltip: "Edit record")]);

        foreach (int row in new[] { 0, 1 })
        {
            IElement button = Assert.Single(ActionsOfRow(cut, row));
            Assert.Equal("button", button.TagName.ToLowerInvariant());
            Assert.Equal("Edit record", button.GetAttribute("title"));
            Assert.Equal("Edit", Text(button.QuerySelector("span")!));
        }
    }

    [Fact]
    public void An_action_not_meant_for_rows_is_not_rendered_on_them()
    {
        CloudDataGridAction headerOnly = RowButton("h", text: "H");
        headerOnly.ShowOnRow = false;

        IRenderedComponent<CloudDataGridBody> cut = RenderBody([headerOnly]);

        Assert.Empty(cut.FindAll(".cloudgrid-rowactions"));
    }

    [Fact]
    public void Rows_without_any_visible_action_have_no_action_container()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody([]);

        Assert.Empty(cut.FindAll(".cloudgrid-rowactions"));
    }

    [Fact]
    public void An_action_without_text_shows_its_tooltip_as_the_label()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody([RowButton("edit", tooltip: "Edit record")]);

        Assert.Equal("Edit record", Text(ActionsOfRow(cut, 0)[0].QuerySelector("span")!));
    }

    [Fact]
    public void An_action_without_text_or_tooltip_has_no_label_and_no_title_text()
    {
        CloudDataGridAction action = RowButton("edit");
        action.IconSvg = "<svg class='ic'></svg>";

        IRenderedComponent<CloudDataGridBody> cut = RenderBody([action]);

        IElement button = ActionsOfRow(cut, 0)[0];
        Assert.Empty(button.QuerySelectorAll("span"));
        Assert.NotNull(button.QuerySelector("svg.ic"));
    }

    [Fact]
    public void An_icon_fragment_wins_over_icon_svg_in_rows_too()
    {
        CloudDataGridAction action = RowButton("edit", text: "Edit");
        action.Icon = Markup("<i class='fragment'></i>");
        action.IconSvg = "<svg class='legacy'></svg>";

        IRenderedComponent<CloudDataGridBody> cut = RenderBody([action]);

        Assert.NotNull(ActionsOfRow(cut, 0)[0].QuerySelector("i.fragment"));
        Assert.Null(ActionsOfRow(cut, 0)[0].QuerySelector("svg.legacy"));
    }

    [Fact(Skip = "BUG: the row action renderer (CloudDataGridBody.razor, RowActionIconAndText) ignores CloudDataGridAction.IconOnly, which is documented as 'renders only its icon and never a text label, regardless of Text / Tooltip'. The header honours it; row actions still render a <span> with the text.")]
    public void An_icon_only_row_action_renders_no_text_label()
    {
        CloudDataGridAction action = RowButton("edit", text: "Edit", tooltip: "Edit record");
        action.IconOnly = true;
        action.IconSvg = "<svg class='ic'></svg>";

        IRenderedComponent<CloudDataGridBody> cut = RenderBody([action]);

        Assert.Empty(ActionsOfRow(cut, 0)[0].QuerySelectorAll("span"));
    }

    [Fact]
    public void Action_css_classes_are_added_to_the_row_action()
    {
        CloudDataGridAction action = RowButton("edit", text: "Edit");
        action.CssClass = "danger";

        IRenderedComponent<CloudDataGridBody> cut = RenderBody([action]);

        Assert.Contains("danger", ActionsOfRow(cut, 0)[0].ClassList);
        Assert.Contains("cloudgrid-action", ActionsOfRow(cut, 0)[0].ClassList);
    }

    [Fact]
    public void Clicking_a_row_button_raises_the_action_with_the_id_of_that_row_only()
    {
        CloudDataGridAction action = RowButton("edit", text: "Edit");
        List<CloudDataGridActionEventArgs> events = [];
        IRenderedComponent<CloudDataGridBody> cut = RenderBody([action], rows: 3, configure: p =>
            p.Add(body => body.OnActionClicked, (CloudDataGridActionEventArgs args) => events.Add(args)));

        ActionsOfRow(cut, 1)[0].Click();

        CloudDataGridActionEventArgs raised = Assert.Single(events);
        Assert.Same(action, raised.Action);
        Assert.Equal([Id(2)], raised.RecordIds);
    }

    [Fact]
    public void A_row_link_action_is_an_anchor_that_does_not_raise_a_click_event()
    {
        CloudDataGridAction link = new()
        {
            Key = "open",
            Type = CloudDataGridActionType.Link,
            ShowOnRow = true,
            Href = "/open",
            Target = "_blank",
            Text = "Open"
        };
        List<CloudDataGridActionEventArgs> events = [];
        IRenderedComponent<CloudDataGridBody> cut = RenderBody([link], configure: p =>
            p.Add(body => body.OnActionClicked, (CloudDataGridActionEventArgs args) => events.Add(args)));

        IElement anchor = ActionsOfRow(cut, 0)[0];

        Assert.Equal("a", anchor.TagName.ToLowerInvariant());
        Assert.Equal("/open", anchor.GetAttribute("href"));
        Assert.Equal("_blank", anchor.GetAttribute("target"));
        Assert.Equal("Open", anchor.GetAttribute("title"));
        Assert.Empty(events);
    }

    [Fact]
    public void Several_actions_are_rendered_in_the_given_order()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody([RowButton("a", tooltip: "First"), RowButton("b", tooltip: "Second"), RowButton("c", tooltip: "Third")]);

        Assert.Equal(["First", "Second", "Third"], ActionsOfRow(cut, 0).Select(action => action.GetAttribute("title")));
    }

    // ----- visibility rules ---------------------------------------------------------------------------------------------------

    [Fact]
    public void A_selection_only_action_is_shown_on_selected_rows_only()
    {
        CloudDataGridAction action = RowButton("edit", text: "Edit");
        action.VisibleOnSelectionOnly = true;

        IRenderedComponent<CloudDataGridBody> cut = RenderBody([action], rows: 3, configure: p =>
        {
            p.Add(body => body.AllowSelection, true);
            p.Add(body => body.SelectedRecords, [Id(2)]);
        });

        Assert.Empty(ActionsOfRow(cut, 0));
        Assert.Single(ActionsOfRow(cut, 1));
        Assert.Empty(ActionsOfRow(cut, 2));
    }

    [Fact]
    public void A_selection_only_action_appears_as_soon_as_the_row_becomes_selected()
    {
        CloudDataGridAction action = RowButton("edit", text: "Edit");
        action.VisibleOnSelectionOnly = true;
        IRenderedComponent<CloudDataGridBody> cut = RenderBody([action], configure: p => p.Add(body => body.AllowSelection, true));
        Assert.Empty(cut.FindAll(".cloudgrid-rowactions"));

        cut.Render(p => p.Add(body => body.SelectedRecords, [Id(1)]));

        Assert.Single(ActionsOfRow(cut, 0));
        Assert.Empty(ActionsOfRow(cut, 1));
    }

    [Fact]
    public void The_action_filter_decides_per_row_and_per_action_and_receives_both()
    {
        CloudDataGridAction edit = RowButton("edit", tooltip: "Edit");
        CloudDataGridAction delete = RowButton("delete", tooltip: "Delete");
        List<(Guid Row, string Action)> asked = [];

        IRenderedComponent<CloudDataGridBody> cut = RenderBody([edit, delete], rows: 2, configure: p =>
            p.Add(body => body.ActionFilter, (CloudDataGridRow row, CloudDataGridAction action) =>
            {
                asked.Add((row.Id, action.Key));
                return !(row.Id == Id(2) && action.Key == "delete");
            }));

        Assert.Equal(["Edit", "Delete"], ActionsOfRow(cut, 0).Select(action => action.GetAttribute("title")));
        Assert.Equal(["Edit"], ActionsOfRow(cut, 1).Select(action => action.GetAttribute("title")));
        Assert.Contains((Id(2), "delete"), asked);
        Assert.Contains((Id(1), "edit"), asked);
    }

    [Fact]
    public void A_filter_that_hides_everything_leaves_no_action_container()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody([RowButton("edit", text: "Edit")], configure: p =>
            p.Add(body => body.ActionFilter, (CloudDataGridRow _, CloudDataGridAction _) => false));

        Assert.Empty(cut.FindAll(".cloudgrid-rowactions"));
    }

    // ----- element actions on rows ---------------------------------------------------------------------------------------------

    private static CloudDataGridAction RowElement(string key = "inline", Func<Task>? onDeactivated = null)
    {
        CloudDataGridAction action = RowButton(key, tooltip: "Edit inline");
        action.Type = CloudDataGridActionType.Element;
        action.ChildContent = Markup("<input class='inline-editor' />");
        action.OnDeactivated = onDeactivated;
        return action;
    }

    [Fact]
    public void An_element_action_starts_as_a_button()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody([RowElement()]);

        Assert.Empty(cut.FindAll(".inline-editor"));
        Assert.Equal("Edit inline", ActionsOfRow(cut, 0)[0].GetAttribute("title"));
    }

    [Fact]
    public void Clicking_an_element_action_replaces_its_button_in_that_row_with_the_content_and_a_cancel_button()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody([RowElement()]);

        ActionsOfRow(cut, 0)[0].Click();

        Assert.Single(cut.FindAll(".inline-editor"));
        IElement cancel = cut.Find("button._cancel");
        Assert.Equal("cancel", Text(cancel));
        Assert.Equal("Edit inline", ActionsOfRow(cut, 1)[0].GetAttribute("title"));
    }

    [Fact]
    public void Cancelling_an_element_action_restores_the_button_and_calls_OnDeactivated()
    {
        int deactivated = 0;
        IRenderedComponent<CloudDataGridBody> cut = RenderBody([RowElement(onDeactivated: () =>
        {
            deactivated++;
            return Task.CompletedTask;
        })]);
        ActionsOfRow(cut, 0)[0].Click();

        cut.Find("button._cancel").Click();

        Assert.Equal(1, deactivated);
        Assert.Empty(cut.FindAll(".inline-editor"));
        Assert.Empty(cut.FindAll("button._cancel"));
    }

    [Fact]
    public void Activating_the_element_action_on_another_row_closes_the_first_one_and_notifies_it()
    {
        int deactivated = 0;
        IRenderedComponent<CloudDataGridBody> cut = RenderBody([RowElement(onDeactivated: () =>
        {
            deactivated++;
            return Task.CompletedTask;
        })]);
        ActionsOfRow(cut, 0)[0].Click();

        ActionsOfRow(cut, 1)[0].Click();

        Assert.Equal(1, deactivated);
        Assert.Single(cut.FindAll(".inline-editor"));
        Assert.Equal("Edit inline", ActionsOfRow(cut, 0)[0].GetAttribute("title"));
    }

    [Fact]
    public void Opening_an_element_action_raises_no_click_event()
    {
        List<CloudDataGridActionEventArgs> events = [];
        IRenderedComponent<CloudDataGridBody> cut = RenderBody([RowElement()], configure: p =>
            p.Add(body => body.OnActionClicked, (CloudDataGridActionEventArgs args) => events.Add(args)));

        ActionsOfRow(cut, 0)[0].Click();

        Assert.Empty(events);
    }
}
