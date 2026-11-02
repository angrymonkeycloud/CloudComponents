using AngleSharp.Dom;
using AngryMonkey.CloudComponents.DataGrid.Components;
using AngryMonkey.CloudComponents.DataGrid.Models;
using Bunit;
using Microsoft.AspNetCore.Components.Web;

namespace CloudComponents.Tests.DataGrid;

/// <summary>Drag-to-reorder rows and drag-to-resize columns; both run on pointer events and a transparent shield, without JavaScript.</summary>
public class CloudDataGridBodyReorderResizeTests : DataGridTestBase
{
    // ----- reordering ------------------------------------------------------------------------------------------------------------

    private sealed record ReorderRig(
        IRenderedComponent<CloudDataGridBody> Cut,
        CloudDataGridDataResult Data,
        List<CloudDataGridRowReorder> Reorders,
        List<bool> DragStates);

    private ReorderRig RenderReorderable(int rows = 4, double rowHeight = 40, bool notes = false)
    {
        CloudDataGridDataResult data = Result(NumberedRows(rows));

        if (notes)
            data.Rows[0].Note = "note";

        List<CloudDataGridRowReorder> reorders = [];
        List<bool> dragStates = [];

        IRenderedComponent<CloudDataGridBody> cut = Render<CloudDataGridBody>(parameters =>
        {
            parameters.Add(body => body.Columns, Columns("Name"));
            parameters.Add(body => body.Data, data);
            parameters.Add(body => body.AllowReordering, true);
            parameters.Add(body => body.RowHeight, rowHeight);
            parameters.Add(body => body.OnRowsReordered, (CloudDataGridRowReorder reorder) => reorders.Add(reorder));
            parameters.Add(body => body.OnDraggingChanged, (bool dragging) => dragStates.Add(dragging));
        });

        return new ReorderRig(cut, data, reorders, dragStates);
    }

    private static void PressHandle(ReorderRig rig, int rowIndex, double clientY) =>
        rig.Cut.FindAll(".cloudgrid-row .cloudgrid-draghandle")[rowIndex].PointerDown(new PointerEventArgs { ClientY = clientY });

    private static void MoveTo(ReorderRig rig, double clientY) =>
        rig.Cut.Find(".cloudgrid-shield._rowdrag").PointerMove(new PointerEventArgs { ClientY = clientY });

    private static void Release(ReorderRig rig) => rig.Cut.Find(".cloudgrid-shield._rowdrag").PointerUp(new PointerEventArgs());

    [Fact]
    public void Without_reordering_enabled_rows_have_no_drag_handle()
    {
        IRenderedComponent<CloudDataGridBody> cut = Render<CloudDataGridBody>(parameters =>
        {
            parameters.Add(body => body.Columns, Columns("Name"));
            parameters.Add(body => body.Data, Result(NumberedRows(2)));
        });

        Assert.Empty(cut.FindAll(".cloudgrid-draghandle"));
    }

    [Fact]
    public void With_reordering_every_row_has_a_titled_drag_handle_with_an_icon()
    {
        ReorderRig rig = RenderReorderable(3);

        IReadOnlyList<IElement> handles = rig.Cut.FindAll(".cloudgrid-row .cloudgrid-draghandle");

        Assert.Equal(3, handles.Count);
        Assert.All(handles, handle =>
        {
            Assert.Equal("Drag to re-arrange", handle.GetAttribute("title"));
            Assert.NotNull(handle.QuerySelector("svg"));
        });
    }

    [Fact]
    public void Pressing_a_handle_starts_a_drag_with_a_shield_and_marks_the_row_dragging()
    {
        ReorderRig rig = RenderReorderable();
        Assert.Empty(rig.Cut.FindAll(".cloudgrid-shield"));

        PressHandle(rig, 1, 100);

        Assert.Equal([true], rig.DragStates);
        Assert.Single(rig.Cut.FindAll(".cloudgrid-shield._rowdrag"));
        Assert.Equal(["Row 2"], Texts(rig.Cut.FindAll(".cloudgrid-row._dragging .cloudgrid-cell")));
    }

    [Fact]
    public void Dragging_down_by_two_rows_moves_the_row_two_places_and_reports_it_on_release()
    {
        ReorderRig rig = RenderReorderable(rows: 4, rowHeight: 40);
        PressHandle(rig, 0, 100);

        MoveTo(rig, 180);
        Assert.Equal(["Row 2", "Row 3", "Row 1", "Row 4"], FirstCells(rig.Cut));
        Release(rig);

        CloudDataGridRowReorder reorder = Assert.Single(rig.Reorders);
        Assert.Equal(Id(1), reorder.RecordId);
        Assert.Equal(0, reorder.OldIndex);
        Assert.Equal(2, reorder.NewIndex);
        Assert.Equal([Id(2), Id(3), Id(1), Id(4)], reorder.OrderedRecordIds);
        Assert.Equal([true, false], rig.DragStates);
        Assert.Empty(rig.Cut.FindAll(".cloudgrid-shield"));
        Assert.Empty(rig.Cut.FindAll("._dragging"));
    }

    [Fact]
    public void Dragging_up_moves_the_row_towards_the_top()
    {
        ReorderRig rig = RenderReorderable(rows: 4, rowHeight: 40);
        PressHandle(rig, 3, 300);

        MoveTo(rig, 220);
        Release(rig);

        CloudDataGridRowReorder reorder = Assert.Single(rig.Reorders);
        Assert.Equal(3, reorder.OldIndex);
        Assert.Equal(1, reorder.NewIndex);
        Assert.Equal([Id(1), Id(4), Id(2), Id(3)], reorder.OrderedRecordIds);
    }

    [Fact]
    public void The_underlying_rows_are_reordered_in_place()
    {
        ReorderRig rig = RenderReorderable(rows: 3, rowHeight: 40);
        PressHandle(rig, 0, 0);

        MoveTo(rig, 40);
        Release(rig);

        Assert.Equal([Id(2), Id(1), Id(3)], rig.Data.Rows.Select(row => row.Id));
    }

    [Theory]
    [InlineData(19, 0)]
    [InlineData(21, 1)]
    [InlineData(59, 1)]
    [InlineData(61, 2)]
    public void The_pointer_distance_is_rounded_to_the_nearest_row(double distance, int expectedNewIndex)
    {
        ReorderRig rig = RenderReorderable(rows: 4, rowHeight: 40);
        PressHandle(rig, 0, 100);

        MoveTo(rig, 100 + distance);
        Release(rig);

        if (expectedNewIndex == 0)
            Assert.Empty(rig.Reorders);
        else
            Assert.Equal(expectedNewIndex, Assert.Single(rig.Reorders).NewIndex);
    }

    [Fact]
    public void Dragging_past_either_end_stops_at_the_first_and_last_row()
    {
        ReorderRig down = RenderReorderable(rows: 3, rowHeight: 40);
        PressHandle(down, 0, 100);
        MoveTo(down, 5000);
        Release(down);

        ReorderRig up = RenderReorderable(rows: 3, rowHeight: 40);
        PressHandle(up, 2, 100);
        MoveTo(up, -5000);
        Release(up);

        Assert.Equal(2, Assert.Single(down.Reorders).NewIndex);
        Assert.Equal(0, Assert.Single(up.Reorders).NewIndex);
    }

    [Fact]
    public void Dragging_there_and_back_reports_no_reorder()
    {
        ReorderRig rig = RenderReorderable(rows: 4, rowHeight: 40);
        PressHandle(rig, 1, 100);

        MoveTo(rig, 180);
        MoveTo(rig, 100);
        Release(rig);

        Assert.Empty(rig.Reorders);
        Assert.Equal(["Row 1", "Row 2", "Row 3", "Row 4"], FirstCells(rig.Cut));
        Assert.Equal([true, false], rig.DragStates);
    }

    [Fact]
    public void Releasing_without_moving_ends_the_drag_without_a_reorder()
    {
        ReorderRig rig = RenderReorderable();
        PressHandle(rig, 2, 100);

        Release(rig);

        Assert.Empty(rig.Reorders);
        Assert.Equal([true, false], rig.DragStates);
    }

    [Fact]
    public void Leaving_the_shield_or_cancelling_the_pointer_also_ends_the_drag()
    {
        ReorderRig left = RenderReorderable();
        PressHandle(left, 0, 100);
        left.Cut.Find(".cloudgrid-shield._rowdrag").PointerLeave(new PointerEventArgs());

        ReorderRig cancelled = RenderReorderable();
        PressHandle(cancelled, 0, 100);
        cancelled.Cut.Find(".cloudgrid-shield._rowdrag").PointerCancel(new PointerEventArgs());

        Assert.Equal([true, false], left.DragStates);
        Assert.Equal([true, false], cancelled.DragStates);
        Assert.Empty(left.Cut.FindAll(".cloudgrid-shield"));
        Assert.Empty(cancelled.Cut.FindAll(".cloudgrid-shield"));
    }

    [Fact]
    public void A_row_note_makes_each_row_taller_for_the_drag_distance()
    {
        ReorderRig rig = RenderReorderable(rows: 4, rowHeight: 40, notes: true); // 40 + 24 note = 64 per row
        PressHandle(rig, 0, 100);

        MoveTo(rig, 100 + 63);
        Release(rig);

        Assert.Equal(1, Assert.Single(rig.Reorders).NewIndex);
    }

    [Fact]
    public void The_default_row_height_is_43_pixels_for_the_drag_distance()
    {
        CloudDataGridDataResult data = Result(NumberedRows(3));
        List<CloudDataGridRowReorder> reorders = [];
        IRenderedComponent<CloudDataGridBody> cut = Render<CloudDataGridBody>(parameters =>
        {
            parameters.Add(body => body.Columns, Columns("Name"));
            parameters.Add(body => body.Data, data);
            parameters.Add(body => body.AllowReordering, true);
            parameters.Add(body => body.OnRowsReordered, (CloudDataGridRowReorder reorder) => reorders.Add(reorder));
        });
        cut.FindAll(".cloudgrid-row .cloudgrid-draghandle")[1].PointerDown(new PointerEventArgs { ClientY = 0 });

        cut.Find(".cloudgrid-shield._rowdrag").PointerMove(new PointerEventArgs { ClientY = 43 });
        cut.Find(".cloudgrid-shield._rowdrag").PointerUp(new PointerEventArgs());

        Assert.Equal(2, Assert.Single(reorders).NewIndex);
    }

    [Fact]
    public void Reordering_a_selected_row_keeps_it_selected_and_wherever_it_lands()
    {
        CloudDataGridDataResult data = Result(NumberedRows(3));
        IRenderedComponent<CloudDataGridBody> cut = Render<CloudDataGridBody>(parameters =>
        {
            parameters.Add(body => body.Columns, Columns("Name"));
            parameters.Add(body => body.Data, data);
            parameters.Add(body => body.AllowReordering, true);
            parameters.Add(body => body.AllowSelection, true);
            parameters.Add(body => body.SelectedRecords, [Id(1)]);
            parameters.Add(body => body.RowHeight, 40d);
        });
        cut.FindAll(".cloudgrid-draghandle")[1].PointerDown(new PointerEventArgs { ClientY = 0 });

        cut.Find(".cloudgrid-shield._rowdrag").PointerMove(new PointerEventArgs { ClientY = 80 });
        cut.Find(".cloudgrid-shield._rowdrag").PointerUp(new PointerEventArgs());

        Assert.Equal(["Row 1"], Texts(cut.FindAll(".cloudgrid-row._selected .cloudgrid-cell")));
        Assert.Equal(["Row 2", "Row 3", "Row 1"], FirstCells(cut));
    }

    // ----- resizing -------------------------------------------------------------------------------------------------------------------

    private sealed record ResizeRig(
        IRenderedComponent<CloudDataGridBody> Cut,
        List<Dictionary<string, double>> Widths,
        List<bool> ResizeStates,
        List<CloudDataGridSort> Sorts);

    private ResizeRig RenderResizable(List<CloudDataGridColumn>? columns = null, Dictionary<string, double>? saved = null)
    {
        List<Dictionary<string, double>> widths = [];
        List<bool> states = [];
        List<CloudDataGridSort> sorts = [];

        IRenderedComponent<CloudDataGridBody> cut = Render<CloudDataGridBody>(parameters =>
        {
            parameters.Add(body => body.Columns, columns ?? Columns("A", "B"));
            parameters.Add(body => body.Data, Result([Row(1, "x", "y")]));
            parameters.Add(body => body.SavedColumnWidths, saved);
            parameters.Add(body => body.ColumnWidthsChanged, (Dictionary<string, double> changed) => widths.Add(changed));
            parameters.Add(body => body.OnResizingChanged, (bool resizing) => states.Add(resizing));
            parameters.Add(body => body.OnSortChanged, (CloudDataGridSort sort) => sorts.Add(sort));
        });

        return new ResizeRig(cut, widths, states, sorts);
    }

    private static void Grab(ResizeRig rig, int column, double clientX) =>
        rig.Cut.FindAll(".cloudgrid-resizer")[column].PointerDown(new PointerEventArgs { ClientX = clientX });

    private static void Drag(ResizeRig rig, double clientX) =>
        rig.Cut.Find(".cloudgrid-shield").PointerMove(new PointerEventArgs { ClientX = clientX });

    private static void Drop(ResizeRig rig) => rig.Cut.Find(".cloudgrid-shield").PointerUp(new PointerEventArgs());

    private static string HeadWidthStyle(IRenderedComponent<CloudDataGridBody> cut, int column) => cut.FindAll(".cloudgrid-headcell")[column].GetAttribute("style")!;

    [Fact]
    public void Grabbing_a_resize_handle_starts_resizing_and_shows_the_shield()
    {
        ResizeRig rig = RenderResizable();
        Assert.Empty(rig.Cut.FindAll(".cloudgrid-shield"));

        Grab(rig, 0, 500);

        Assert.Equal([true], rig.ResizeStates);
        Assert.Single(rig.Cut.FindAll(".cloudgrid-shield"));
        Assert.Empty(rig.Cut.FindAll(".cloudgrid-shield._rowdrag"));
    }

    [Fact]
    public void Dragging_the_handle_changes_the_column_width_by_the_pointer_distance()
    {
        ResizeRig rig = RenderResizable();
        Grab(rig, 0, 500);

        Drag(rig, 560);

        Assert.Contains("width: 160px", HeadWidthStyle(rig.Cut, 0));
        Assert.Contains("width: 100px", HeadWidthStyle(rig.Cut, 1));
        Assert.Contains("width: 160px", rig.Cut.FindAll(".cloudgrid-row .cloudgrid-cell")[0].GetAttribute("style"));
    }

    [Fact]
    public void Dragging_left_makes_the_column_narrower_but_never_below_its_minimum_width()
    {
        List<CloudDataGridColumn> columns = Columns("A", "B");
        columns[0].MinWidth = 70;
        ResizeRig rig = RenderResizable(columns);
        Grab(rig, 0, 500);

        Drag(rig, 480);
        Assert.Contains("width: 80px", HeadWidthStyle(rig.Cut, 0));

        Drag(rig, 100);
        Assert.Contains("width: 70px", HeadWidthStyle(rig.Cut, 0));
    }

    [Fact]
    public void A_column_cannot_be_dragged_wider_than_3000_pixels()
    {
        ResizeRig rig = RenderResizable();
        Grab(rig, 0, 0);

        Drag(rig, 100_000);

        Assert.Contains("width: 3000px", HeadWidthStyle(rig.Cut, 0));
    }

    [Fact]
    public void Dropping_the_handle_reports_the_widths_by_column_key_and_ends_resizing()
    {
        List<CloudDataGridColumn> columns = Columns("A", "B");
        columns[1].Key = "b_field";
        ResizeRig rig = RenderResizable(columns);
        Grab(rig, 1, 500);
        Drag(rig, 550);

        Drop(rig);

        Dictionary<string, double> reported = Assert.Single(rig.Widths);
        Assert.Equal(150, reported["b_field"]);
        Assert.False(reported.ContainsKey("B"));
        Assert.Equal([true, false], rig.ResizeStates);
        Assert.Empty(rig.Cut.FindAll(".cloudgrid-shield"));
    }

    [Fact]
    public void Columns_without_a_key_are_reported_by_their_label()
    {
        ResizeRig rig = RenderResizable();
        Grab(rig, 0, 0);
        Drag(rig, 25);

        Drop(rig);

        Assert.Equal(125, Assert.Single(rig.Widths)["A"]);
    }

    [Fact]
    public void Leaving_the_shield_also_finishes_the_resize()
    {
        ResizeRig rig = RenderResizable();
        Grab(rig, 0, 0);
        Drag(rig, 40);

        rig.Cut.Find(".cloudgrid-shield").PointerLeave(new PointerEventArgs());

        Assert.Equal([true, false], rig.ResizeStates);
        Assert.Equal(140, Assert.Single(rig.Widths)["A"]);
    }

    [Fact]
    public void Resizing_one_column_leaves_the_widths_of_the_others_untouched_in_the_report()
    {
        ResizeRig rig = RenderResizable();
        Grab(rig, 0, 0);
        Drag(rig, 10);
        Drop(rig);
        Grab(rig, 1, 0);
        Drag(rig, 30);

        Drop(rig);

        Dictionary<string, double> last = rig.Widths[^1];
        Assert.Equal(110, last["A"]);
        Assert.Equal(130, last["B"]);
    }

    [Fact]
    public void A_second_drag_continues_from_the_width_the_first_one_left()
    {
        ResizeRig rig = RenderResizable();
        Grab(rig, 0, 0);
        Drag(rig, 50);
        Drop(rig);

        Grab(rig, 0, 1000);
        Drag(rig, 1020);

        Assert.Contains("width: 170px", HeadWidthStyle(rig.Cut, 0));
    }

    [Fact]
    public void Saved_widths_are_applied_to_their_columns()
    {
        ResizeRig rig = RenderResizable(saved: new Dictionary<string, double> { ["B"] = 333 });

        Assert.Contains("width: 100px", HeadWidthStyle(rig.Cut, 0));
        Assert.Contains("width: 333px", HeadWidthStyle(rig.Cut, 1));
    }

    [Fact]
    public void A_saved_width_below_the_column_minimum_is_raised_to_the_minimum()
    {
        List<CloudDataGridColumn> columns = Columns("A", "B");
        columns[0].MinWidth = 90;
        ResizeRig rig = RenderResizable(columns, new Dictionary<string, double> { ["A"] = 20 });

        Assert.Contains("width: 90px", HeadWidthStyle(rig.Cut, 0));
    }

    [Fact]
    public void A_pointer_press_on_the_handle_does_not_sort_the_column()
    {
        ResizeRig rig = RenderResizable();

        Grab(rig, 0, 0);
        Drop(rig);

        Assert.Empty(rig.Sorts);
    }

    [Fact]
    public void Clicking_a_header_while_a_resize_is_in_progress_does_not_sort()
    {
        ResizeRig rig = RenderResizable();
        Grab(rig, 0, 0);

        rig.Cut.FindAll(".cloudgrid-headcell")[1].Click();

        Assert.Empty(rig.Sorts);
    }
}
