using AngleSharp.Dom;
using AngryMonkey.CloudComponents.DataGrid.Components;
using AngryMonkey.CloudComponents.DataGrid.Models;
using Bunit;

namespace CloudComponents.Tests.DataGrid;

/// <summary>Row checkboxes and select-all in the grid body.</summary>
public class CloudDataGridBodySelectionTests : DataGridTestBase
{
    private IRenderedComponent<CloudDataGridBody> RenderBody(
        CloudDataGridDataResult? data,
        List<Guid>? selected = null,
        List<List<Guid>>? changes = null,
        bool allowSelection = true)
    {
        return Render<CloudDataGridBody>(parameters =>
        {
            parameters.Add(body => body.Columns, Columns("Name"));
            parameters.Add(body => body.Data, data);
            parameters.Add(body => body.AllowSelection, allowSelection);
            parameters.Add(body => body.SelectedRecords, selected ?? []);

            if (changes != null)
                parameters.Add(body => body.SelectedRecordsChanged, (List<Guid> updated) => changes.Add(updated));
        });
    }

    private static IElement SelectAll(IRenderedComponent<CloudDataGridBody> cut) => cut.Find(".cloudgrid-head input[type=checkbox]");

    private static IReadOnlyList<IElement> RowBoxes(IRenderedComponent<CloudDataGridBody> cut) => cut.FindAll(".cloudgrid-row input[type=checkbox]");

    [Fact]
    public void Without_selection_enabled_there_are_no_checkboxes()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Result(NumberedRows(2)), allowSelection: false);

        Assert.Empty(cut.FindAll("input[type=checkbox]"));
    }

    [Fact]
    public void With_selection_enabled_there_is_a_select_all_box_and_a_box_per_row()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Result(NumberedRows(3)));

        Assert.Equal("Select all", SelectAll(cut).GetAttribute("title"));
        Assert.Equal(3, RowBoxes(cut).Count);
    }

    [Fact]
    public void Selected_rows_have_checked_boxes_and_the_selected_class()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Result(NumberedRows(3)), selected: [Id(2)]);

        IReadOnlyList<IElement> rows = cut.FindAll(".cloudgrid-row");
        Assert.False(RowBoxes(cut)[0].HasAttribute("checked"));
        Assert.True(RowBoxes(cut)[1].HasAttribute("checked"));
        Assert.DoesNotContain("_selected", rows[0].ClassList);
        Assert.Contains("_selected", rows[1].ClassList);
    }

    [Fact]
    public void Ids_that_are_not_on_the_page_do_not_select_anything_visible()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Result(NumberedRows(2)), selected: [Id(99)]);

        Assert.Empty(cut.FindAll(".cloudgrid-row._selected"));
        Assert.False(SelectAll(cut).HasAttribute("checked"));
    }

    [Fact]
    public void Ticking_a_row_raises_the_selection_with_that_id_added()
    {
        List<List<Guid>> changes = [];
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Result(NumberedRows(3)), selected: [Id(1)], changes: changes);

        RowBoxes(cut)[2].Change(true);

        Assert.Equal([Id(1), Id(3)], Assert.Single(changes));
    }

    [Fact]
    public void Unticking_a_row_raises_the_selection_with_that_id_removed()
    {
        List<List<Guid>> changes = [];
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Result(NumberedRows(3)), selected: [Id(1), Id(3)], changes: changes);

        RowBoxes(cut)[0].Change(false);

        Assert.Equal([Id(3)], Assert.Single(changes));
    }

    [Fact]
    public void Toggling_a_row_does_not_modify_the_list_that_was_passed_in()
    {
        List<Guid> original = [Id(1)];
        List<List<Guid>> changes = [];
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Result(NumberedRows(3)), selected: original, changes: changes);

        RowBoxes(cut)[1].Change(true);

        Assert.Equal([Id(1)], original);
        Assert.NotSame(original, changes[0]);
    }

    [Fact]
    public void Select_all_adds_every_row_of_the_page_and_keeps_other_selected_ids()
    {
        List<List<Guid>> changes = [];
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Result(NumberedRows(3)), selected: [Id(99), Id(2)], changes: changes);

        SelectAll(cut).Change(true);

        List<Guid> updated = Assert.Single(changes);
        Assert.Equal(4, updated.Count);
        Assert.Equal(updated.Count, updated.Distinct().Count());
        Assert.Contains(Id(99), updated);
        Assert.All([Id(1), Id(2), Id(3)], id => Assert.Contains(id, updated));
    }

    [Fact]
    public void Clearing_select_all_removes_only_the_rows_of_the_page()
    {
        List<List<Guid>> changes = [];
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Result(NumberedRows(3)), selected: [Id(99), Id(1), Id(2), Id(3)], changes: changes);

        SelectAll(cut).Change(false);

        Assert.Equal([Id(99)], Assert.Single(changes));
    }

    [Fact]
    public void Select_all_is_checked_only_when_every_row_of_the_page_is_selected()
    {
        IRenderedComponent<CloudDataGridBody> all = RenderBody(Result(NumberedRows(2)), selected: [Id(1), Id(2)]);
        IRenderedComponent<CloudDataGridBody> some = RenderBody(Result(NumberedRows(2)), selected: [Id(1)]);

        Assert.True(SelectAll(all).HasAttribute("checked"));
        Assert.False(SelectAll(some).HasAttribute("checked"));
    }

    [Fact]
    public void Select_all_is_not_checked_when_there_are_no_rows()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Result([]));

        Assert.False(SelectAll(cut).HasAttribute("checked"));
    }

    [Fact]
    public void Select_all_without_data_raises_the_selection_unchanged()
    {
        List<List<Guid>> changes = [];
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(null, selected: [Id(5)], changes: changes);

        SelectAll(cut).Change(true);

        Assert.Equal([Id(5)], Assert.Single(changes));
    }

    [Fact]
    public void A_new_selection_from_the_parent_is_reflected_in_the_row_classes()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Result(NumberedRows(2)));

        cut.Render(p => p.Add(body => body.SelectedRecords, [Id(2)]));

        Assert.Equal(["Row 2"], Texts(cut.FindAll(".cloudgrid-row._selected .cloudgrid-cell")));
    }
}
