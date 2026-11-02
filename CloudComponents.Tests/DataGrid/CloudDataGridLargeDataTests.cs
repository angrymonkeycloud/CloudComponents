using System.Diagnostics;
using AngryMonkey.CloudComponents.DataGrid.Components;
using AngryMonkey.CloudComponents.DataGrid.Models;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace CloudComponents.Tests.DataGrid;

/// <summary>Behaviour with thousands of rows: correctness first, with generous time limits so only a pathological slowdown fails.</summary>
public class CloudDataGridLargeDataTests : DataGridTestBase
{
    private static readonly TimeSpan Generous = TimeSpan.FromSeconds(30);

    private static List<CloudDataGridRow> ManyRows(int count) =>
        [.. Enumerable.Range(1, count).Select(number => Row(number, $"Row {number}", number))];

    [Fact]
    public void A_big_page_with_local_sorting_is_ordered_correctly_in_both_directions()
    {
        const int count = 3000;
        // Shuffle deterministically so the sort has real work to do.
        List<CloudDataGridRow> rows = [.. ManyRows(count).OrderBy(row => (int)row.Cells[1]! * 7919 % 10007)];
        IRenderedComponent<CloudDataGridBody> cut = Render<CloudDataGridBody>(parameters =>
        {
            parameters.Add(body => body.Columns, Columns("Name", "Value"));
            parameters.Add(body => body.Data, Result(rows));
        });
        Stopwatch watch = Stopwatch.StartNew();

        cut.FindAll(".cloudgrid-headcell")[1].Click();
        IReadOnlyList<string> ascending = Texts(cut.FindAll(".cloudgrid-row").Select(row => row.QuerySelectorAll(".cloudgrid-cell")[1]));
        cut.FindAll(".cloudgrid-headcell")[1].Click();
        IReadOnlyList<string> descending = Texts(cut.FindAll(".cloudgrid-row").Select(row => row.QuerySelectorAll(".cloudgrid-cell")[1]));

        Assert.Equal(Enumerable.Range(1, count).Select(number => number.ToString()), ascending);
        Assert.Equal(Enumerable.Range(1, count).Reverse().Select(number => number.ToString()), descending);
        Assert.True(watch.Elapsed < Generous, $"Sorting {count} rows took {watch.Elapsed}.");
    }

    [Fact]
    public void Select_all_over_thousands_of_rows_selects_each_row_exactly_once()
    {
        const int count = 5000;
        List<List<Guid>> changes = [];
        IRenderedComponent<CloudDataGridBody> cut = Render<CloudDataGridBody>(parameters =>
        {
            parameters.Add(body => body.Columns, Columns("Name", "Value"));
            parameters.Add(body => body.Data, Result(ManyRows(count)));
            parameters.Add(body => body.AllowSelection, true);
            parameters.Add(body => body.SelectedRecordsChanged, (List<Guid> updated) => changes.Add(updated));
        });

        cut.Find(".cloudgrid-head input[type=checkbox]").Change(true);

        List<Guid> selected = Assert.Single(changes);
        Assert.Equal(count, selected.Count);
        Assert.Equal(count, selected.Distinct().Count());
    }

    [Fact]
    public void Select_all_with_everything_already_selected_and_then_cleared_leaves_nothing()
    {
        const int count = 2000;
        List<Guid> everything = [.. ManyRows(count).Select(row => row.Id)];
        List<List<Guid>> changes = [];
        IRenderedComponent<CloudDataGridBody> cut = Render<CloudDataGridBody>(parameters =>
        {
            parameters.Add(body => body.Columns, Columns("Name", "Value"));
            parameters.Add(body => body.Data, Result(ManyRows(count)));
            parameters.Add(body => body.AllowSelection, true);
            parameters.Add(body => body.SelectedRecords, everything);
            parameters.Add(body => body.SelectedRecordsChanged, (List<Guid> updated) => changes.Add(updated));
        });

        Assert.True(cut.Find(".cloudgrid-head input[type=checkbox]").HasAttribute("checked"));
        cut.Find(".cloudgrid-head input[type=checkbox]").Change(false);

        Assert.Empty(Assert.Single(changes));
    }

    [Fact]
    public void Thousands_of_rows_in_many_categories_are_grouped_and_counted()
    {
        const int count = 6000;
        List<CloudDataGridRow> rows = ManyRows(count);
        foreach (CloudDataGridRow row in rows)
            row.CategoryKey = $"group-{((int)row.Cells[1]! % 50):D2}";

        IRenderedComponent<CloudDataGridBody> cut = Render<CloudDataGridBody>(parameters =>
        {
            parameters.Add(body => body.Columns, Columns("Name", "Value"));
            parameters.Add(body => body.Data, Result(rows));
        });

        Assert.Equal(50, cut.FindAll(".cloudgrid-category").Count);
        Assert.Equal(count, cut.FindAll(".cloudgrid-row").Count);
        Assert.All(cut.FindAll(".cloudgrid-categorycount"), badge => Assert.Equal("120", Text(badge)));
    }

    [Fact]
    public void Collapsing_one_category_of_thousands_hides_only_its_rows()
    {
        const int count = 4000;
        List<CloudDataGridRow> rows = ManyRows(count);
        foreach (CloudDataGridRow row in rows)
            row.CategoryKey = (int)row.Cells[1]! % 2 == 0 ? "even" : "odd";
        IRenderedComponent<CloudDataGridBody> cut = Render<CloudDataGridBody>(parameters =>
        {
            parameters.Add(body => body.Columns, Columns("Name", "Value"));
            parameters.Add(body => body.Data, Result(rows));
        });

        cut.FindAll(".cloudgrid-category button")[0].Click();

        Assert.Equal(count / 2, cut.FindAll(".cloudgrid-row").Count);
    }

    [Fact]
    public void Infinite_scroll_renders_only_a_window_of_a_very_large_row_set()
    {
        const int count = 20_000;
        IRenderedComponent<CloudDataGridBody> cut = Render<CloudDataGridBody>(parameters =>
        {
            parameters.Add(body => body.Columns, Columns("Name", "Value"));
            parameters.Add(body => body.Data, Result(ManyRows(count)));
            parameters.Add(body => body.PagingMode, CloudDataGridPagingMode.InfiniteScroll);
        });

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".cloudgrid-row")));

        Assert.True(cut.FindAll(".cloudgrid-row").Count < count / 10, $"{cut.FindAll(".cloudgrid-row").Count} of {count} rows were put in the DOM.");
        Assert.Equal("Row 1", FirstCells(cut)[0]);
    }

    [Fact]
    public void Exporting_a_big_page_writes_one_line_per_row_plus_the_header()
    {
        const int count = 5000;
        const string modulePath = "./_content/AngryMonkey.CloudComponents.DataGrid/cloudgrid.js";
        BunitJSModuleInterop module = JSInterop.SetupModule(modulePath);
        module.SetupVoid("downloadTextFile", _ => true);
        GridRig rig = new()
        {
            Respond = _ => new CloudDataGridDataResult { Rows = ManyRows(count), Page = 1, PageSize = count, Total = count }
        };
        IRenderedComponent<CloudDataGrid> cut = Render<CloudDataGrid>(parameters =>
        {
            parameters.Add(grid => grid.DataProvider, rig.Provider);
            parameters.Add(grid => grid.Columns, Columns("Name", "Value"));
            parameters.Add(grid => grid.Header, new CloudDataGridHeaderOptions { AllowRefresh = false });
        });
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".cloudgrid-row")));

        cut.Find("button[title='More']").Click();
        cut.Find(".cloudgridheader-more-item[title='Export']").Click();
        cut.FindAll(".cloudgrid-export-btn").Single(button => Text(button) == "Current page").Click();

        string csv = (string)Assert.Single(module.Invocations["downloadTextFile"]).Arguments[1]!;
        string[] lines = csv.Split(Environment.NewLine);
        Assert.Equal(count + 1, lines.Length);
        Assert.Equal("\"Name\",\"Value\"", lines[0]);
        Assert.Equal($"\"Row {count}\",\"{count}\"", lines[^1]);
    }

    [Fact]
    public void A_grid_with_many_columns_renders_every_cell()
    {
        const int columnCount = 200;
        List<CloudDataGridColumn> columns = [.. Enumerable.Range(1, columnCount).Select(number => new CloudDataGridColumn { Label = $"C{number}" })];
        CloudDataGridRow row = Row(1);
        row.Cells = [.. Enumerable.Range(1, columnCount).Select(number => (object?)number)];

        IRenderedComponent<CloudDataGridBody> cut = Render<CloudDataGridBody>(parameters =>
        {
            parameters.Add(body => body.Columns, columns);
            parameters.Add(body => body.Data, Result([row]));
        });

        Assert.Equal(columnCount, cut.FindAll(".cloudgrid-headcell").Count);
        Assert.Equal(columnCount, cut.FindAll(".cloudgrid-row .cloudgrid-cell").Count);
        Assert.Equal("200", Text(cut.FindAll(".cloudgrid-row .cloudgrid-cell")[^1]));
    }

    // ----- grid level paging in the accumulating modes -----------------------------------------------------------------------

    [Fact]
    public void Infinite_scroll_through_the_grid_appends_pages_until_the_total_is_reached()
    {
        GridRig rig = new();
        rig.Respond = request =>
        {
            int first = ((request.Page - 1) * 20) + 1;
            int take = Math.Min(20, 50 - first + 1);
            return new CloudDataGridDataResult
            {
                Rows = [.. Enumerable.Range(first, take).Select(number => Row(number, $"Row {number}", number))],
                Page = request.Page,
                PageSize = 20,
                Total = 50
            };
        };

        IRenderedComponent<CloudDataGrid> cut = Render<CloudDataGrid>(parameters =>
        {
            parameters.Add(grid => grid.DataProvider, rig.Provider);
            parameters.Add(grid => grid.Columns, Columns("Name", "Value"));
            parameters.Add(grid => grid.PagingMode, CloudDataGridPagingMode.InfiniteScroll);
            parameters.Add(grid => grid.RowsPerPage, 20);
        });

        cut.WaitForAssertion(() => Assert.Equal(50, FirstCells(cut).Count), TimeSpan.FromSeconds(10));
        Assert.Equal(["Row 1", "Row 2"], FirstCells(cut).Take(2));
        Assert.Equal("Row 50", FirstCells(cut)[^1]);
        Assert.All(rig.Requests.Skip(1), request => Assert.True(request.IsAppend));
        Assert.Equal([1, 2, 3], rig.Requests.Select(request => request.Page));
    }

    [Fact]
    public void Load_more_text_reaches_the_button_through_the_grid()
    {
        GridRig rig = new()
        {
            Respond = _ => new CloudDataGridDataResult { Rows = [Row(1, "a", 1)], Page = 1, PageSize = 1, Total = 5 }
        };

        IRenderedComponent<CloudDataGrid> cut = Render<CloudDataGrid>(parameters =>
        {
            parameters.Add(grid => grid.DataProvider, rig.Provider);
            parameters.Add(grid => grid.Columns, Columns("Name", "Value"));
            parameters.Add(grid => grid.PagingMode, CloudDataGridPagingMode.LoadMore);
            parameters.Add(grid => grid.LoadMoreText, "more please");
        });

        cut.WaitForAssertion(() => Assert.Equal("more please", Text(cut.Find("button.cloudgrid-loadmore"))));
    }

    [Fact]
    public void Extra_header_actions_are_rendered_through_the_grid()
    {
        GridRig rig = new();
        RenderFragment extra = builder => builder.AddMarkupContent(0, "<button class='save-order'>Save order</button>");

        IRenderedComponent<CloudDataGrid> cut = Render<CloudDataGrid>(parameters =>
        {
            parameters.Add(grid => grid.DataProvider, rig.Provider);
            parameters.Add(grid => grid.Columns, Columns("Name", "Value"));
            parameters.Add(grid => grid.Header, new CloudDataGridHeaderOptions { ExtraActions = extra, AllowExport = false });
        });

        Assert.Equal("Save order", Text(cut.Find(".cloudgridheader .save-order")));
    }
}
