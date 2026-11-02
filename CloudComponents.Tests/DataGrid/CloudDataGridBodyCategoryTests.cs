using AngleSharp.Dom;
using AngryMonkey.CloudComponents.DataGrid.Components;
using AngryMonkey.CloudComponents.DataGrid.Models;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace CloudComponents.Tests.DataGrid;

/// <summary>Collapsible category headers built from the rows' category keys.</summary>
public class CloudDataGridBodyCategoryTests : DataGridTestBase
{
    private static CloudDataGridRow InCategory(int id, string name, string? key, string? label = null)
    {
        CloudDataGridRow row = Row(id, name);
        row.CategoryKey = key;
        row.CategoryLabel = label;
        return row;
    }

    private IRenderedComponent<CloudDataGridBody> RenderBody(
        IEnumerable<CloudDataGridRow> rows,
        Action<ComponentParameterCollectionBuilder<CloudDataGridBody>>? configure = null)
    {
        return Render<CloudDataGridBody>(parameters =>
        {
            parameters.Add(body => body.Columns, Columns("Name"));
            parameters.Add(body => body.Data, Result(rows));
            configure?.Invoke(parameters);
        });
    }

    private static IReadOnlyList<IElement> Categories(IRenderedComponent<CloudDataGridBody> cut) => cut.FindAll(".cloudgrid-category");

    /// <summary>The body's rows and category headers in display order, headers shown as "[label]".</summary>
    private static IReadOnlyList<string> Flow(IRenderedComponent<CloudDataGridBody> cut) =>
        [.. cut.FindAll(".cloudgrid-body > .cloudgrid-category, .cloudgrid-body > .cloudgrid-row").Select(element =>
            element.ClassList.Contains("cloudgrid-category")
                ? "[" + Text(element.QuerySelector("strong")!) + "]"
                : Text(element.QuerySelector(".cloudgrid-cell")!))];

    [Fact]
    public void Rows_without_a_category_key_get_no_headers()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody([InCategory(1, "a", null), InCategory(2, "b", "")]);

        Assert.Empty(Categories(cut));
        Assert.Equal(["a", "b"], Flow(cut));
    }

    [Fact]
    public void Rows_sharing_a_key_are_grouped_beneath_one_header_in_first_appearance_order()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody([InCategory(1, "a", "x"), InCategory(2, "b", "y"), InCategory(3, "c", "x")]);

        Assert.Equal(["[x]", "a", "c", "[y]", "b"], Flow(cut));
    }

    [Fact]
    public void Rows_without_a_key_stay_in_the_flow_among_the_groups()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody([InCategory(1, "plain", null), InCategory(2, "a", "x")]);

        Assert.Equal(["plain", "[x]", "a"], Flow(cut));
    }

    [Fact]
    public void The_header_shows_the_label_when_given_and_the_key_otherwise()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody([InCategory(1, "a", "dept-1", "Engineering"), InCategory(2, "b", "dept-2")]);

        Assert.Equal(["[Engineering]", "a", "[dept-2]", "b"], Flow(cut));
    }

    [Fact]
    public void The_first_non_blank_label_in_the_group_is_used()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody([InCategory(1, "a", "k", "  "), InCategory(2, "b", "k", "Second"), InCategory(3, "c", "k", "Third")]);

        Assert.Equal(["[Second]", "a", "b", "c"], Flow(cut));
    }

    [Fact]
    public void The_header_shows_how_many_rows_are_in_the_group()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody([InCategory(1, "a", "x"), InCategory(2, "b", "x"), InCategory(3, "c", "y")]);

        Assert.Equal(["2", "1"], Texts(cut.FindAll(".cloudgrid-categorycount")));
    }

    [Fact]
    public void Category_keys_are_case_sensitive()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody([InCategory(1, "a", "x"), InCategory(2, "b", "X")]);

        Assert.Equal(["[x]", "a", "[X]", "b"], Flow(cut));
    }

    [Fact]
    public void Headers_are_expanded_by_default_and_expose_that_to_assistive_technology()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody([InCategory(1, "a", "x")]);

        IElement header = Categories(cut).Single();
        Assert.Contains("_expanded", header.ClassList);
        Assert.Equal("true", header.QuerySelector("button")!.GetAttribute("aria-expanded"));
    }

    [Fact]
    public void A_collapsed_category_hides_its_rows_but_keeps_its_header_with_the_row_count()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody([InCategory(1, "a", "x"), InCategory(2, "b", "x"), InCategory(3, "c", "y")],
            p => p.Add(body => body.CollapsedCategories, new HashSet<string> { "x" }));

        Assert.Equal(["[x]", "[y]", "c"], Flow(cut));
        IElement header = Categories(cut)[0];
        Assert.Contains("_collapsed", header.ClassList);
        Assert.Equal("false", header.QuerySelector("button")!.GetAttribute("aria-expanded"));
        Assert.Equal("2", Text(header.QuerySelector(".cloudgrid-categorycount")!));
    }

    [Fact]
    public void Clicking_a_header_raises_the_collapsed_set_with_that_key_added_and_collapses_it()
    {
        List<HashSet<string>> changes = [];
        IRenderedComponent<CloudDataGridBody> cut = RenderBody([InCategory(1, "a", "x"), InCategory(2, "b", "y")],
            p => p.Add(body => body.CollapsedCategoriesChanged, (HashSet<string> set) => changes.Add(set)));

        cut.Find(".cloudgrid-category button").Click();

        Assert.Equal(["x"], Assert.Single(changes));
        Assert.Equal(["[x]", "[y]", "b"], Flow(cut));
    }

    [Fact]
    public void Clicking_a_collapsed_header_expands_it_again()
    {
        List<HashSet<string>> changes = [];
        IRenderedComponent<CloudDataGridBody> cut = RenderBody([InCategory(1, "a", "x")], p =>
        {
            p.Add(body => body.CollapsedCategories, new HashSet<string> { "x", "other" });
            p.Add(body => body.CollapsedCategoriesChanged, (HashSet<string> set) => changes.Add(set));
        });

        cut.Find(".cloudgrid-category button").Click();

        Assert.Equal(["other"], Assert.Single(changes));
        Assert.Equal(["[x]", "a"], Flow(cut));
    }

    [Fact]
    public void The_raised_set_is_a_copy_the_caller_may_keep()
    {
        List<HashSet<string>> changes = [];
        HashSet<string> collapsed = ["keep"];
        IRenderedComponent<CloudDataGridBody> cut = RenderBody([InCategory(1, "a", "x")], p =>
        {
            p.Add(body => body.CollapsedCategories, collapsed);
            p.Add(body => body.CollapsedCategoriesChanged, (HashSet<string> set) => changes.Add(set));
        });

        cut.Find(".cloudgrid-category button").Click();

        Assert.Equal(["keep"], collapsed);
        Assert.Equal(2, changes[0].Count);
    }

    [Fact]
    public void Without_collapsing_allowed_the_header_is_plain_content_without_a_button()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody([InCategory(1, "a", "x")], p => p.Add(body => body.AllowCategoryCollapse, false));

        IElement header = Categories(cut).Single();
        Assert.Empty(header.QuerySelectorAll("button"));
        Assert.Empty(header.QuerySelectorAll(".cloudgrid-categorychevron"));
        Assert.Equal("x", Text(header.QuerySelector("strong")!));
    }

    [Fact]
    public void Turning_categories_off_shows_every_row_in_a_flat_list_even_if_a_key_is_collapsed()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody([InCategory(1, "a", "x"), InCategory(2, "b", "y")], p =>
        {
            p.Add(body => body.EnableRowCategories, false);
            p.Add(body => body.CollapsedCategories, new HashSet<string> { "x" });
        });

        Assert.Empty(Categories(cut));
        Assert.Equal(["a", "b"], Flow(cut));
    }

    [Fact]
    public void Categories_can_be_switched_on_and_off_at_runtime()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody([InCategory(1, "a", "x")]);
        Assert.Single(Categories(cut));

        cut.Render(p => p.Add(body => body.EnableRowCategories, false));
        Assert.Empty(Categories(cut));

        cut.Render(p => p.Add(body => body.EnableRowCategories, true));
        Assert.Single(Categories(cut));
    }

    [Fact]
    public void A_header_template_replaces_the_default_content_and_receives_the_category_context()
    {
        List<CloudDataGridCategoryContext> contexts = [];
        RenderFragment<CloudDataGridCategoryContext> template = context =>
        {
            contexts.Add(context);
            return builder => builder.AddMarkupContent(0, $"<em class='custom'>{context.Label}:{context.VisibleRowCount}:{context.IsExpanded}</em>");
        };

        IRenderedComponent<CloudDataGridBody> cut = RenderBody([InCategory(1, "a", "x", "Group X"), InCategory(2, "b", "x")], p =>
        {
            p.Add(body => body.CategoryHeaderTemplate, template);
            p.Add(body => body.CollapsedCategories, new HashSet<string> { "x" });
        });

        Assert.Equal("Group X:2:False", Text(cut.Find(".cloudgrid-category em.custom")));
        Assert.Empty(cut.FindAll(".cloudgrid-category strong"));
        CloudDataGridCategoryContext context = contexts.First();
        Assert.Equal("x", context.Key);
        Assert.Equal("Group X", context.Label);
        Assert.Equal(2, context.VisibleRowCount);
        Assert.False(context.IsExpanded);
    }

    [Fact]
    public void A_header_template_is_also_used_when_collapsing_is_not_allowed()
    {
        RenderFragment<CloudDataGridCategoryContext> template = context => builder => builder.AddMarkupContent(0, "<em class='custom'>t</em>");

        IRenderedComponent<CloudDataGridBody> cut = RenderBody([InCategory(1, "a", "x")], p =>
        {
            p.Add(body => body.CategoryHeaderTemplate, template);
            p.Add(body => body.AllowCategoryCollapse, false);
        });

        Assert.Single(cut.FindAll(".cloudgrid-categorycontent em.custom"));
    }

    [Fact]
    public void Categories_are_formed_after_local_sorting_so_rows_keep_their_group_when_sorted()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody([InCategory(1, "b", "x"), InCategory(2, "a", "y"), InCategory(3, "a2", "x")]);

        cut.FindAll(".cloudgrid-headcell")[0].Click();

        Assert.Equal(["[y]", "a", "[x]", "a2", "b"], Flow(cut));
    }

    [Fact]
    public void Selection_checkboxes_still_work_for_rows_inside_a_category()
    {
        List<List<Guid>> changes = [];
        IRenderedComponent<CloudDataGridBody> cut = RenderBody([InCategory(1, "a", "x")], p =>
        {
            p.Add(body => body.AllowSelection, true);
            p.Add(body => body.SelectedRecordsChanged, (List<Guid> updated) => changes.Add(updated));
        });

        cut.Find(".cloudgrid-row input[type=checkbox]").Change(true);

        Assert.Equal([Id(1)], Assert.Single(changes));
    }
}
