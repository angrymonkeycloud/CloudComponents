using AngleSharp.Dom;
using AngryMonkey.CloudComponents;
using Bunit;
using GridViewComponent = AngryMonkey.CloudComponents.GridView;

namespace CloudComponents.Tests.Core.GridView;

/// <summary>The GridView component: loading placeholders, columns and rows, row selection, the footer range and paging requests.</summary>
public class GridViewTests : BunitContext
{
    private static GridViewColumns ColumnsOf(params string[] names) =>
        new() { Columns = names.Select(name => new GridViewColumn { Name = name }).ToArray() };

    private static GridViewRows RowsOf(params string[][] rows) =>
        new()
        {
            Rows = rows.Select(values => new GridViewRow
            {
                Cells = values.Select(value => new GridViewCell { Value = value }).ToArray()
            }).ToArray()
        };

    private IRenderedComponent<GridViewComponent> RenderGrid(
        GridViewColumns? columns = null,
        GridViewRows? rows = null,
        int pageSize = 10,
        int rowsCount = 0,
        List<GridViewRequestDataEventArgs>? requests = null)
    {
        return Render<GridViewComponent>(parameters =>
        {
            parameters.Add(grid => grid.Columns, columns!);
            parameters.Add(grid => grid.Rows, rows!);
            parameters.Add(grid => grid.PageSize, pageSize);
            parameters.Add(grid => grid.RowsCount, rowsCount);

            if (requests != null)
                parameters.Add(grid => grid.OnRequestData, (GridViewRequestDataEventArgs args) => requests.Add(args));
        });
    }

    private static string Text(IElement element) => element.TextContent.Trim();

    private static string Range(IRenderedComponent<GridViewComponent> cut) => Text(cut.Find(".pagination-pages"));

    // ----- placeholders -----------------------------------------------------------------------------------------------------------

    [Fact]
    public void Without_columns_only_the_columns_loading_placeholder_is_shown()
    {
        IRenderedComponent<GridViewComponent> cut = RenderGrid(columns: null);

        Assert.Equal("Loading... Columns", Text(cut.Find(".amc-datagrid-nodata")));
        Assert.Empty(cut.FindAll(".amc-datagrid-table"));
        Assert.Empty(cut.FindAll(".amc-datagrid-footer"));
    }

    [Fact]
    public void With_columns_but_no_rows_the_rows_loading_placeholder_is_shown_under_the_headers()
    {
        IRenderedComponent<GridViewComponent> cut = RenderGrid(ColumnsOf("Name"), rows: null);

        Assert.Equal("Loading... Rows", Text(cut.Find(".amc-datagrid-norows")));
        Assert.Empty(cut.FindAll(".amc-datagrid-nodata"));
        Assert.Empty(cut.FindAll(".amc-datagrid-row"));
    }

    [Fact]
    public void While_rows_are_still_loading_the_footer_shows_no_range_or_pager()
    {
        IRenderedComponent<GridViewComponent> cut = RenderGrid(ColumnsOf("Name"), rows: null, rowsCount: 5);

        Assert.Empty(cut.FindAll(".amc-datagrid-footer-pages"));
        Assert.Empty(cut.FindAll(".amc-datagrid-footer-pagination"));
    }

    [Fact]
    public void Supplying_the_columns_later_replaces_the_columns_placeholder_with_the_table()
    {
        IRenderedComponent<GridViewComponent> cut = RenderGrid(columns: null);

        cut.Render(parameters => parameters.Add(grid => grid.Columns, ColumnsOf("Name")));

        Assert.Empty(cut.FindAll(".amc-datagrid-nodata"));
        Assert.Single(cut.FindAll(".amc-datagrid-table"));
    }

    // ----- columns and rows ---------------------------------------------------------------------------------------------------------

    [Fact]
    public void The_header_has_a_leading_blank_cell_followed_by_one_cell_per_column_in_order()
    {
        IRenderedComponent<GridViewComponent> cut = RenderGrid(ColumnsOf("Name", "Age", "City"), RowsOf());

        IReadOnlyList<IElement> cells = cut.FindAll(".amc-datagrid-columns .amc-datagrid-column");

        Assert.Equal(4, cells.Count);
        Assert.Equal(" ", cells[0].TextContent);
        Assert.Equal(["Name", "Age", "City"], cells.Skip(1).Select(Text));
    }

    [Fact]
    public void Each_row_renders_its_cells_in_order_with_the_formatted_value()
    {
        IRenderedComponent<GridViewComponent> cut = RenderGrid(
            ColumnsOf("Name", "Age"),
            RowsOf(["Ada", "36"], ["Linus", "54"]));

        IReadOnlyList<IElement> rows = cut.FindAll(".amc-datagrid-row");

        Assert.Equal(2, rows.Count);
        Assert.Equal(["Ada", "36"], rows[0].QuerySelectorAll(".amc-datagrid-cell").Select(Text));
        Assert.Equal(["Linus", "54"], rows[1].QuerySelectorAll(".amc-datagrid-cell").Select(Text));
    }

    [Fact]
    public void Every_row_has_a_select_handle()
    {
        IRenderedComponent<GridViewComponent> cut = RenderGrid(ColumnsOf("Name"), RowsOf(["a"], ["b"], ["c"]));

        Assert.Equal(3, cut.FindAll(".amc-datagrid-select").Count);
    }

    [Fact]
    public void An_empty_row_collection_renders_the_rows_container_without_rows_or_the_loading_text()
    {
        IRenderedComponent<GridViewComponent> cut = RenderGrid(ColumnsOf("Name"), RowsOf());

        Assert.Single(cut.FindAll(".amc-datagrid-rows"));
        Assert.Empty(cut.FindAll(".amc-datagrid-row"));
        Assert.Empty(cut.FindAll(".amc-datagrid-norows"));
    }

    [Fact]
    public void Cell_text_is_html_encoded()
    {
        IRenderedComponent<GridViewComponent> cut = RenderGrid(ColumnsOf("Name"), RowsOf(["<b>bold</b>"]));

        IElement cell = cut.Find(".amc-datagrid-cell");

        Assert.Equal("<b>bold</b>", cell.TextContent);
        Assert.Empty(cell.QuerySelectorAll("b"));
    }

    [Fact]
    public void A_database_null_value_renders_as_an_empty_cell()
    {
        GridViewRows rows = new()
        {
            Rows = [new GridViewRow { Cells = [new GridViewCell { Value = DBNull.Value }] }]
        };

        IRenderedComponent<GridViewComponent> cut = RenderGrid(ColumnsOf("Name"), rows);

        Assert.Equal(string.Empty, Text(cut.Find(".amc-datagrid-cell")));
    }

    [Fact]
    public void The_grid_never_calls_into_javascript()
    {
        RenderGrid(ColumnsOf("Name"), RowsOf(["a"]), rowsCount: 1);

        Assert.Empty(JSInterop.Invocations);
    }

    [Fact]
    public void A_page_of_several_thousand_rows_renders_every_row_with_its_cells()
    {
        GridViewRows rows = new() { Rows = [.. Enumerable.Range(1, 3000).Select(number => new GridViewRow { Cells = [new GridViewCell { Value = number }, new GridViewCell { Value = $"n{number}" }] })] };

        IRenderedComponent<GridViewComponent> cut = RenderGrid(ColumnsOf("Number", "Text"), rows, pageSize: 3000, rowsCount: 3000);

        Assert.Equal(3000, cut.FindAll(".amc-datagrid-row").Count);
        Assert.Equal("1 - 3000 out of 3000", Range(cut));
        IElement last = cut.FindAll(".amc-datagrid-row")[^1];
        Assert.Equal(["3000", "n3000"], last.QuerySelectorAll(".amc-datagrid-cell").Select(Text));
    }

    // ----- selection ----------------------------------------------------------------------------------------------------------------

    [Fact]
    public void Rows_start_unselected()
    {
        IRenderedComponent<GridViewComponent> cut = RenderGrid(ColumnsOf("Name"), RowsOf(["a"], ["b"]));

        Assert.Empty(cut.FindAll(".amc-datagrid-row._selected"));
    }

    [Fact]
    public void Clicking_a_select_handle_marks_only_that_row_selected()
    {
        IRenderedComponent<GridViewComponent> cut = RenderGrid(ColumnsOf("Name"), RowsOf(["a"], ["b"], ["c"]));

        cut.FindAll(".amc-datagrid-select")[1].Click();

        IReadOnlyList<IElement> rows = cut.FindAll(".amc-datagrid-row");
        Assert.DoesNotContain("_selected", rows[0].ClassList);
        Assert.Contains("_selected", rows[1].ClassList);
        Assert.DoesNotContain("_selected", rows[2].ClassList);
    }

    [Fact]
    public void Clicking_a_selected_rows_handle_again_deselects_it()
    {
        IRenderedComponent<GridViewComponent> cut = RenderGrid(ColumnsOf("Name"), RowsOf(["a"]));

        cut.Find(".amc-datagrid-select").Click();
        cut.Find(".amc-datagrid-select").Click();

        Assert.Empty(cut.FindAll(".amc-datagrid-row._selected"));
    }

    [Fact]
    public void Selecting_several_rows_keeps_all_of_them_selected()
    {
        IRenderedComponent<GridViewComponent> cut = RenderGrid(ColumnsOf("Name"), RowsOf(["a"], ["b"], ["c"]));

        cut.FindAll(".amc-datagrid-select")[0].Click();
        cut.FindAll(".amc-datagrid-select")[2].Click();

        Assert.Equal(2, cut.FindAll(".amc-datagrid-row._selected").Count);
    }

    [Fact]
    public void Selecting_a_row_updates_the_selected_flag_on_the_model()
    {
        GridViewRows rows = RowsOf(["a"], ["b"]);
        IRenderedComponent<GridViewComponent> cut = RenderGrid(ColumnsOf("Name"), rows);

        cut.FindAll(".amc-datagrid-select")[0].Click();

        Assert.True(rows.Rows[0].Selected);
        Assert.False(rows.Rows[1].Selected);
    }

    [Fact]
    public void A_row_that_is_selected_before_rendering_is_shown_selected()
    {
        GridViewRows rows = RowsOf(["a"], ["b"]);
        rows.Rows[1].Selected = true;

        IRenderedComponent<GridViewComponent> cut = RenderGrid(ColumnsOf("Name"), rows);

        Assert.Equal("b", Text(cut.Find(".amc-datagrid-row._selected .amc-datagrid-cell")));
    }

    // ----- initial data request -------------------------------------------------------------------------------------------------

    [Fact]
    public void The_grid_asks_for_the_first_page_when_it_is_created()
    {
        List<GridViewRequestDataEventArgs> requests = [];

        RenderGrid(ColumnsOf("Name"), pageSize: 25, rowsCount: 100, requests: requests);

        GridViewRequestDataEventArgs request = Assert.Single(requests);
        Assert.Equal(1, request.PageNumber);
        Assert.Equal(25, request.PageSize);
    }

    [Fact]
    public void Re_rendering_with_new_parameters_does_not_ask_for_data_again()
    {
        List<GridViewRequestDataEventArgs> requests = [];
        IRenderedComponent<GridViewComponent> cut = RenderGrid(ColumnsOf("Name"), requests: requests);

        cut.Render(parameters => parameters.Add(grid => grid.Rows, RowsOf(["a"])));

        Assert.Single(requests);
    }

    // ----- footer range ---------------------------------------------------------------------------------------------------------

    [Fact]
    public void The_first_page_shows_the_range_starting_at_one()
    {
        IRenderedComponent<GridViewComponent> cut = RenderGrid(ColumnsOf("Name"), RowsOf(["a"]), pageSize: 10, rowsCount: 25);

        Assert.Equal("1 - 10 out of 25", Range(cut));
        Assert.Equal("Page 1", Text(cut.Find(".amc-datagrid-footer-pagination-page")));
    }

    [Fact]
    public void The_second_page_shows_the_range_after_the_first_page()
    {
        IRenderedComponent<GridViewComponent> cut = RenderGrid(ColumnsOf("Name"), RowsOf(["a"]), pageSize: 10, rowsCount: 25);

        cut.Find(".amc-datagrid-footer-pagination-navigatebutton").Click();

        Assert.Equal("11 - 20 out of 25", Range(cut));
        Assert.Equal("Page 2", Text(cut.Find(".amc-datagrid-footer-pagination-page")));
    }

    [Fact(Skip = "BUG: GridView.razor.cs EndCount is StartCount + PageSize - 1 and is never clamped to RowsCount, so the last, partial page of 25 rows at size 10 reads '21 - 30 out of 25' instead of '21 - 25 out of 25'.")]
    public void The_range_on_a_partial_last_page_ends_at_the_total_row_count()
    {
        IRenderedComponent<GridViewComponent> cut = RenderGrid(ColumnsOf("Name"), RowsOf(["a"]), pageSize: 10, rowsCount: 25);

        cut.Find(".amc-datagrid-footer-pagination-navigatebutton").Click();
        cut.Find(".amc-datagrid-footer-pagination-navigatebutton:last-of-type").Click();

        Assert.Equal("21 - 25 out of 25", Range(cut));
    }

    // ----- paging buttons ---------------------------------------------------------------------------------------------------------

    [Fact]
    public void On_the_first_page_there_is_only_a_next_button()
    {
        IRenderedComponent<GridViewComponent> cut = RenderGrid(ColumnsOf("Name"), RowsOf(["a"]), pageSize: 10, rowsCount: 25);

        IReadOnlyList<IElement> buttons = cut.FindAll(".amc-datagrid-footer-pagination-navigatebutton");

        Assert.Single(buttons);
        Assert.Contains("13.3,48.1", buttons[0].InnerHtml);
    }

    [Fact]
    public void After_moving_forward_both_previous_and_next_buttons_are_shown()
    {
        IRenderedComponent<GridViewComponent> cut = RenderGrid(ColumnsOf("Name"), RowsOf(["a"]), pageSize: 10, rowsCount: 25);

        cut.Find(".amc-datagrid-footer-pagination-navigatebutton").Click();

        Assert.Equal(2, cut.FindAll(".amc-datagrid-footer-pagination-navigatebutton").Count);
    }

    [Fact]
    public void Clicking_next_requests_the_following_page_with_the_page_size()
    {
        List<GridViewRequestDataEventArgs> requests = [];
        IRenderedComponent<GridViewComponent> cut = RenderGrid(ColumnsOf("Name"), RowsOf(["a"]), pageSize: 10, rowsCount: 25, requests: requests);

        cut.Find(".amc-datagrid-footer-pagination-navigatebutton").Click();

        Assert.Equal(2, requests.Count);
        Assert.Equal(2, requests[1].PageNumber);
        Assert.Equal(10, requests[1].PageSize);
    }

    [Fact]
    public void Clicking_previous_requests_the_page_before_and_returns_to_the_first_page()
    {
        List<GridViewRequestDataEventArgs> requests = [];
        IRenderedComponent<GridViewComponent> cut = RenderGrid(ColumnsOf("Name"), RowsOf(["a"]), pageSize: 10, rowsCount: 25, requests: requests);
        cut.Find(".amc-datagrid-footer-pagination-navigatebutton").Click();

        cut.Find(".amc-datagrid-footer-pagination-navigatebutton").Click();

        Assert.Equal(1, requests[^1].PageNumber);
        Assert.Equal("Page 1", Text(cut.Find(".amc-datagrid-footer-pagination-page")));
        Assert.Single(cut.FindAll(".amc-datagrid-footer-pagination-navigatebutton"));
    }

    [Fact(Skip = "BUG: GridView.razor.cs IsNavigateForward hides the next button when CurrentPage == PageSize (page 2 of 3 at size 2) instead of when CurrentPage reaches TotalPages, so later pages cannot be reached.")]
    public void The_next_button_stays_available_until_the_last_page_even_when_the_page_number_equals_the_page_size()
    {
        IRenderedComponent<GridViewComponent> cut = RenderGrid(ColumnsOf("Name"), RowsOf(["a"]), pageSize: 2, rowsCount: 6);
        cut.Find(".amc-datagrid-footer-pagination-navigatebutton").Click();

        Assert.Equal("Page 2", Text(cut.Find(".amc-datagrid-footer-pagination-page")));
        Assert.Contains("13.3,48.1", cut.FindAll(".amc-datagrid-footer-pagination-navigatebutton")[^1].InnerHtml);
    }

    [Fact(Skip = "BUG: GridView.razor.cs IsNavigateForward only compares CurrentPage with PageSize, so the next button is still offered on the last page (page 3 of 3 at size 10).")]
    public void The_next_button_is_hidden_on_the_last_page()
    {
        IRenderedComponent<GridViewComponent> cut = RenderGrid(ColumnsOf("Name"), RowsOf(["a"]), pageSize: 10, rowsCount: 25);
        cut.Find(".amc-datagrid-footer-pagination-navigatebutton").Click();
        cut.Find(".amc-datagrid-footer-pagination-navigatebutton:last-of-type").Click();

        Assert.Equal("Page 3", Text(cut.Find(".amc-datagrid-footer-pagination-page")));
        Assert.Single(cut.FindAll(".amc-datagrid-footer-pagination-navigatebutton"));
    }

    [Fact(Skip = "BUG: GridView.razor.cs TotalPages divides RowsCount by PageSize, which is 0 by default, so clicking next with the default PageSize throws DivideByZeroException.")]
    public void Clicking_next_with_the_default_page_size_does_not_crash()
    {
        IRenderedComponent<GridViewComponent> cut = Render<GridViewComponent>(parameters =>
        {
            parameters.Add(grid => grid.Columns, ColumnsOf("Name"));
            parameters.Add(grid => grid.Rows, RowsOf(["a"]));
            parameters.Add(grid => grid.RowsCount, 5);
        });

        cut.Find(".amc-datagrid-footer-pagination-navigatebutton").Click();
    }

    // ----- public navigation methods --------------------------------------------------------------------------------------------

    [Fact]
    public async Task NavigateToPage_next_moves_to_the_next_page_and_raises_the_request()
    {
        List<GridViewRequestDataEventArgs> requests = [];
        IRenderedComponent<GridViewComponent> cut = RenderGrid(ColumnsOf("Name"), RowsOf(["a"]), pageSize: 10, rowsCount: 25, requests: requests);

        await cut.InvokeAsync(() => cut.Instance.NavigateToPage("next"));

        Assert.Equal(2, requests[^1].PageNumber);
    }

    [Fact]
    public async Task NavigateToPage_next_on_the_last_page_stays_on_the_last_page()
    {
        List<GridViewRequestDataEventArgs> requests = [];
        IRenderedComponent<GridViewComponent> cut = RenderGrid(ColumnsOf("Name"), RowsOf(["a"]), pageSize: 10, rowsCount: 25, requests: requests);
        await cut.InvokeAsync(() => cut.Instance.NavigateToPage("next"));
        await cut.InvokeAsync(() => cut.Instance.NavigateToPage("next"));

        await cut.InvokeAsync(() => cut.Instance.NavigateToPage("next"));

        Assert.Equal(3, requests[^1].PageNumber);
    }

    [Fact]
    public async Task NavigateToPage_previous_on_the_first_page_stays_on_the_first_page()
    {
        List<GridViewRequestDataEventArgs> requests = [];
        IRenderedComponent<GridViewComponent> cut = RenderGrid(ColumnsOf("Name"), RowsOf(["a"]), pageSize: 10, rowsCount: 25, requests: requests);

        await cut.InvokeAsync(() => cut.Instance.NavigateToPage("previous"));

        Assert.Equal(1, requests[^1].PageNumber);
    }

    [Fact]
    public async Task NavigateToPage_with_an_unknown_direction_re_requests_the_current_page()
    {
        List<GridViewRequestDataEventArgs> requests = [];
        IRenderedComponent<GridViewComponent> cut = RenderGrid(ColumnsOf("Name"), RowsOf(["a"]), pageSize: 10, rowsCount: 25, requests: requests);
        await cut.InvokeAsync(() => cut.Instance.NavigateToPage("next"));

        await cut.InvokeAsync(() => cut.Instance.NavigateToPage("sideways"));

        Assert.Equal(2, requests[^1].PageNumber);
    }

    [Fact]
    public async Task updateList_requests_the_given_page_and_makes_it_current()
    {
        List<GridViewRequestDataEventArgs> requests = [];
        IRenderedComponent<GridViewComponent> cut = RenderGrid(ColumnsOf("Name"), RowsOf(["a"]), pageSize: 10, rowsCount: 100, requests: requests);

        await cut.InvokeAsync(() => cut.Instance.updateList(7));
        cut.Render(parameters => parameters.Add(grid => grid.Rows, RowsOf(["b"])));

        Assert.Equal(7, requests[^1].PageNumber);
        Assert.Equal("Page 7", Text(cut.Find(".amc-datagrid-footer-pagination-page")));
        Assert.Equal("61 - 70 out of 100", Range(cut));
    }
}
