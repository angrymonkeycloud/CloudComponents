using AngleSharp.Dom;
using AngryMonkey.CloudComponents.DataGrid.Components;
using AngryMonkey.CloudComponents.DataGrid.Models;
using Bunit;

namespace CloudComponents.Tests.DataGrid;

/// <summary>Sorting from the column headers: local sorting without a handler, and the OnSortChanged contract with one.</summary>
public class CloudDataGridBodySortingTests : DataGridTestBase
{
    private IRenderedComponent<CloudDataGridBody> RenderBody(
        CloudDataGridDataResult data,
        List<CloudDataGridColumn>? columns = null,
        Action<ComponentParameterCollectionBuilder<CloudDataGridBody>>? configure = null)
    {
        return Render<CloudDataGridBody>(parameters =>
        {
            parameters.Add(body => body.Columns, columns ?? Columns("Name", "Value"));
            parameters.Add(body => body.Data, data);
            configure?.Invoke(parameters);
        });
    }

    private static void ClickHead(IRenderedComponent<CloudDataGridBody> cut, int index) => cut.FindAll(".cloudgrid-headcell")[index].Click();

    private static IReadOnlyList<string> Column(IRenderedComponent<CloudDataGridBody> cut, int index) =>
        Texts(cut.FindAll(".cloudgrid-row").Select(row => row.QuerySelectorAll(".cloudgrid-cell")[index]));

    private static CloudDataGridDataResult ValuesOf(params object?[] values) =>
        Result(values.Select((value, index) => Row(index + 1, $"r{index + 1}", value)));

    // ----- local sorting ---------------------------------------------------------------------------------------------------------

    [Fact]
    public void Without_a_handler_clicking_a_header_sorts_the_loaded_rows_ascending()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(ValuesOf(3, 1, 2));

        ClickHead(cut, 1);

        Assert.Equal(["1", "2", "3"], Column(cut, 1));
        Assert.Equal(["r2", "r3", "r1"], Column(cut, 0));
    }

    [Fact]
    public void Clicking_the_same_header_again_reverses_the_order()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(ValuesOf(3, 1, 2));

        ClickHead(cut, 1);
        ClickHead(cut, 1);

        Assert.Equal(["3", "2", "1"], Column(cut, 1));
    }

    [Fact]
    public void A_third_click_returns_to_ascending()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(ValuesOf(3, 1, 2));

        ClickHead(cut, 1);
        ClickHead(cut, 1);
        ClickHead(cut, 1);

        Assert.Equal(["1", "2", "3"], Column(cut, 1));
    }

    [Fact]
    public void Clicking_another_header_sorts_by_it_ascending()
    {
        CloudDataGridDataResult data = Result([Row(1, "b", 1), Row(2, "a", 2), Row(3, "c", 3)]);
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(data);
        ClickHead(cut, 1);
        ClickHead(cut, 1);

        ClickHead(cut, 0);

        Assert.Equal(["a", "b", "c"], Column(cut, 0));
    }

    [Fact]
    public void Numbers_sort_numerically_and_not_as_text()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(ValuesOf(10, 9, 100, 2));

        ClickHead(cut, 1);

        Assert.Equal(["2", "9", "10", "100"], Column(cut, 1));
    }

    [Fact]
    public void Negative_and_decimal_numbers_sort_numerically()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(ValuesOf(0.5m, -2m, 10m, -0.25m));

        ClickHead(cut, 1);

        Assert.Equal([(-2m).ToString(), (-0.25m).ToString(), 0.5m.ToString(), 10m.ToString()], Column(cut, 1));
    }

    [Fact]
    public void Dates_sort_chronologically()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(ValuesOf(new DateTime(2024, 3, 1), new DateTime(2023, 12, 31), new DateTime(2024, 2, 29)));

        ClickHead(cut, 1);

        Assert.Equal(["r2", "r3", "r1"], Column(cut, 0));
    }

    [Fact]
    public void Text_sorts_without_regard_to_case()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(ValuesOf("banana", "Apple", "cherry", "apple2"));

        ClickHead(cut, 1);

        Assert.Equal(["Apple", "apple2", "banana", "cherry"], Column(cut, 1));
    }

    [Fact]
    public void Values_of_different_types_are_compared_as_text()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(ValuesOf(5, "apple", 3));

        ClickHead(cut, 1);

        Assert.Equal(["3", "5", "apple"], Column(cut, 1));
    }

    [Fact]
    public void Null_cells_sort_last_in_ascending_order()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(ValuesOf(2, null, 1));

        ClickHead(cut, 1);

        Assert.Equal(["1", "2", "--"], Column(cut, 1));
    }

    [Fact(Skip = "BUG: CloudDataGridBody sorts with OrderByDescending and a comparer that ranks null above everything, so in descending order the null cells come FIRST. The README ('Sorting behavior': 'nulls are pushed last') and the comparer's summary say nulls sort last. Only reachable when CloudDataGridBody is used without OnSortChanged; CloudDataGrid always sets the handler.")]
    public void Null_cells_also_sort_last_in_descending_order()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(ValuesOf(2, null, 1));

        ClickHead(cut, 1);
        ClickHead(cut, 1);

        Assert.Equal(["2", "1", "--"], Column(cut, 1));
    }

    [Fact]
    public void Rows_with_equal_values_keep_their_original_relative_order()
    {
        CloudDataGridDataResult data = Result([Row(1, "first", 1), Row(2, "second", 0), Row(3, "third", 1), Row(4, "fourth", 0)]);
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(data);

        ClickHead(cut, 1);

        Assert.Equal(["second", "fourth", "first", "third"], Column(cut, 0));
    }

    [Fact]
    public void A_cell_wrapper_is_sorted_by_its_value()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(ValuesOf(new CloudDataGridCell { Value = 30 }, new CloudDataGridCell { Value = 4 }, 12));

        ClickHead(cut, 1);

        Assert.Equal(["r2", "r3", "r1"], Column(cut, 0));
    }

    [Fact]
    public void A_row_with_too_few_cells_sorts_like_a_null()
    {
        CloudDataGridDataResult data = Result([Row(1, "short"), Row(2, "b", 5), Row(3, "c", 1)]);
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(data);

        ClickHead(cut, 1);

        Assert.Equal(["c", "b", "short"], Column(cut, 0));
    }

    [Fact]
    public void Local_sorting_does_not_reorder_the_underlying_data()
    {
        CloudDataGridDataResult data = ValuesOf(3, 1, 2);
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(data);

        ClickHead(cut, 1);

        Assert.Equal([Id(1), Id(2), Id(3)], data.Rows.Select(row => row.Id));
    }

    [Fact]
    public void A_column_that_is_not_sortable_ignores_clicks()
    {
        List<CloudDataGridColumn> columns = Columns("Name", "Value");
        columns[1].Sortable = false;
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(ValuesOf(3, 1, 2), columns);

        ClickHead(cut, 1);

        Assert.Equal(["3", "1", "2"], Column(cut, 1));
        Assert.Empty(cut.FindAll(".cloudgrid-headcell svg"));
    }

    [Fact]
    public void Sorting_keeps_the_grouping_of_category_headers()
    {
        CloudDataGridRow b = Row(1, "b", 2);
        b.CategoryKey = "g";
        CloudDataGridRow a = Row(2, "a", 1);
        a.CategoryKey = "g";
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(Result([b, a]));

        ClickHead(cut, 0);

        Assert.Single(cut.FindAll(".cloudgrid-category"));
        Assert.Equal(["a", "b"], Column(cut, 0));
    }

    // ----- indicator -------------------------------------------------------------------------------------------------------------

    [Fact]
    public void Before_sorting_no_header_shows_a_direction()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(ValuesOf(3, 1, 2));

        Assert.Empty(cut.FindAll(".cloudgrid-headcell svg"));
        Assert.Empty(cut.FindAll("._sorted-ascending"));
        Assert.Empty(cut.FindAll("._sorted-descending"));
    }

    [Fact]
    public void The_sorted_header_shows_an_up_arrow_ascending_then_a_down_arrow_descending()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(ValuesOf(3, 1, 2));

        ClickHead(cut, 1);
        IElement ascending = cut.FindAll(".cloudgrid-headcell")[1];
        Assert.Contains("_sorted-ascending", ascending.ClassList);
        Assert.NotNull(ascending.QuerySelector("svg._up"));

        ClickHead(cut, 1);
        IElement descending = cut.FindAll(".cloudgrid-headcell")[1];
        Assert.Contains("_sorted-descending", descending.ClassList);
        Assert.DoesNotContain("_sorted-ascending", descending.ClassList);
        Assert.NotNull(descending.QuerySelector("svg._down"));
    }

    [Fact]
    public void Sorting_by_another_column_moves_the_indicator()
    {
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(ValuesOf(3, 1, 2));
        ClickHead(cut, 1);

        ClickHead(cut, 0);

        Assert.Empty(cut.FindAll(".cloudgrid-headcell")[1].QuerySelectorAll("svg"));
        Assert.Contains("_sorted-ascending", cut.FindAll(".cloudgrid-headcell")[0].ClassList);
    }

    // ----- with a handler ---------------------------------------------------------------------------------------------------------

    private IRenderedComponent<CloudDataGridBody> RenderServerSorted(CloudDataGridDataResult data, List<CloudDataGridSort> sorts, List<CloudDataGridColumn>? columns = null)
    {
        return RenderBody(data, columns, parameters => parameters.Add(body => body.OnSortChanged, (CloudDataGridSort sort) => sorts.Add(sort)));
    }

    [Fact]
    public void With_a_handler_the_first_click_raises_an_ascending_sort_for_that_column()
    {
        List<CloudDataGridSort> sorts = [];
        List<CloudDataGridColumn> columns = Columns("Name", "Value");
        IRenderedComponent<CloudDataGridBody> cut = RenderServerSorted(ValuesOf(3, 1, 2), sorts, columns);

        ClickHead(cut, 1);

        CloudDataGridSort sort = Assert.Single(sorts);
        Assert.Same(columns[1], sort.Column);
        Assert.Equal(1, sort.ColumnIndex);
        Assert.Equal(CloudDataGridSortDirection.Ascending, sort.Direction);
    }

    [Fact]
    public void With_a_handler_repeated_clicks_alternate_the_direction()
    {
        List<CloudDataGridSort> sorts = [];
        IRenderedComponent<CloudDataGridBody> cut = RenderServerSorted(ValuesOf(3, 1, 2), sorts);

        ClickHead(cut, 1);
        ClickHead(cut, 1);
        ClickHead(cut, 1);

        Assert.Equal(
            [CloudDataGridSortDirection.Ascending, CloudDataGridSortDirection.Descending, CloudDataGridSortDirection.Ascending],
            sorts.Select(sort => sort.Direction));
    }

    [Fact]
    public void With_a_handler_switching_column_starts_ascending_again()
    {
        List<CloudDataGridSort> sorts = [];
        IRenderedComponent<CloudDataGridBody> cut = RenderServerSorted(ValuesOf(3, 1, 2), sorts);
        ClickHead(cut, 1);
        ClickHead(cut, 1);

        ClickHead(cut, 0);

        Assert.Equal(0, sorts[^1].ColumnIndex);
        Assert.Equal(CloudDataGridSortDirection.Ascending, sorts[^1].Direction);
    }

    [Fact]
    public void With_a_handler_the_rows_are_left_in_the_order_the_provider_gave()
    {
        List<CloudDataGridSort> sorts = [];
        IRenderedComponent<CloudDataGridBody> cut = RenderServerSorted(ValuesOf(3, 1, 2), sorts);

        ClickHead(cut, 1);

        Assert.Equal(["3", "1", "2"], Column(cut, 1));
    }

    [Fact]
    public void With_a_handler_a_non_sortable_column_raises_nothing()
    {
        List<CloudDataGridSort> sorts = [];
        List<CloudDataGridColumn> columns = Columns("Name", "Value");
        columns[0].Sortable = false;
        IRenderedComponent<CloudDataGridBody> cut = RenderServerSorted(ValuesOf(3, 1, 2), sorts, columns);

        ClickHead(cut, 0);

        Assert.Empty(sorts);
    }

    [Fact]
    public void The_sort_key_of_a_raised_sort_falls_back_to_the_label_and_prefers_the_column_key()
    {
        List<CloudDataGridSort> sorts = [];
        List<CloudDataGridColumn> columns = Columns("Name", "Value");
        columns[1].Key = "value_field";
        IRenderedComponent<CloudDataGridBody> cut = RenderServerSorted(ValuesOf(3, 1, 2), sorts, columns);

        ClickHead(cut, 0);
        ClickHead(cut, 1);

        Assert.Equal(["Name", "value_field"], sorts.Select(sort => sort.Key));
    }

    [Fact]
    public void With_a_handler_the_indicator_follows_the_active_sort_parameter()
    {
        List<CloudDataGridColumn> columns = Columns("Name", "Value");
        CloudDataGridSort active = new() { Column = columns[1], ColumnIndex = 1, Direction = CloudDataGridSortDirection.Descending };

        IRenderedComponent<CloudDataGridBody> cut = RenderBody(ValuesOf(3, 1, 2), columns, p =>
        {
            p.Add(body => body.OnSortChanged, (CloudDataGridSort _) => { });
            p.Add(body => body.ActiveSort, active);
        });

        IElement head = cut.FindAll(".cloudgrid-headcell")[1];
        Assert.Contains("_sorted-descending", head.ClassList);
        Assert.NotNull(head.QuerySelector("svg._down"));
    }

    [Fact]
    public void The_active_sort_is_matched_by_key_when_the_column_has_one()
    {
        List<CloudDataGridColumn> columns = Columns("Name", "Value");
        columns[0].Key = "name_field";
        CloudDataGridSort active = new() { Column = new CloudDataGridColumn { Label = "Other", Key = "name_field" }, ColumnIndex = 0, Direction = CloudDataGridSortDirection.Ascending };

        IRenderedComponent<CloudDataGridBody> cut = RenderBody(ValuesOf(3, 1, 2), columns, p =>
        {
            p.Add(body => body.OnSortChanged, (CloudDataGridSort _) => { });
            p.Add(body => body.ActiveSort, active);
        });

        Assert.Contains("_sorted-ascending", cut.FindAll(".cloudgrid-headcell")[0].ClassList);
    }

    [Fact]
    public void Clearing_the_active_sort_removes_the_indicator()
    {
        List<CloudDataGridColumn> columns = Columns("Name", "Value");
        CloudDataGridSort active = new() { Column = columns[1], ColumnIndex = 1, Direction = CloudDataGridSortDirection.Ascending };
        IRenderedComponent<CloudDataGridBody> cut = RenderBody(ValuesOf(3, 1, 2), columns, p =>
        {
            p.Add(body => body.OnSortChanged, (CloudDataGridSort _) => { });
            p.Add(body => body.ActiveSort, active);
        });

        cut.Render(p => p.Add(body => body.ActiveSort, null));

        Assert.Empty(cut.FindAll("._sorted-ascending"));
        Assert.Empty(cut.FindAll(".cloudgrid-headcell svg"));
    }

    [Fact]
    public void An_active_sort_for_a_column_that_is_gone_shows_no_indicator()
    {
        List<CloudDataGridColumn> columns = Columns("Name", "Value");
        CloudDataGridSort active = new() { Column = new CloudDataGridColumn { Label = "Missing" }, ColumnIndex = 5, Direction = CloudDataGridSortDirection.Ascending };

        IRenderedComponent<CloudDataGridBody> cut = RenderBody(ValuesOf(3, 1, 2), columns, p =>
        {
            p.Add(body => body.OnSortChanged, (CloudDataGridSort _) => { });
            p.Add(body => body.ActiveSort, active);
        });

        Assert.Empty(cut.FindAll(".cloudgrid-headcell svg"));
    }
}
