using System.Globalization;
using AngleSharp.Dom;
using AngryMonkey.CloudComponents.DataGrid.Components;
using AngryMonkey.CloudComponents.DataGrid.Models;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace CloudComponents.Tests.DataGrid;

/// <summary>The whole grid: data loading and the request it sends, paging, sorting, searching, refreshing, selection and wiring.</summary>
public class CloudDataGridTests : DataGridTestBase
{
    private IRenderedComponent<CloudDataGrid> RenderGrid(
        GridRig rig,
        Action<ComponentParameterCollectionBuilder<CloudDataGrid>>? configure = null,
        List<CloudDataGridColumn>? columns = null)
    {
        IRenderedComponent<CloudDataGrid> cut = Render<CloudDataGrid>(parameters =>
        {
            parameters.Add(grid => grid.DataProvider, rig.Provider);
            parameters.Add(grid => grid.Columns, columns ?? Columns("Name", "Value"));
            configure?.Invoke(parameters);
        });

        return cut;
    }

    private static void WaitForRows(IRenderedComponent<CloudDataGrid> cut, int count) =>
        cut.WaitForAssertion(() => Assert.Equal(count, cut.FindAll(".cloudgrid-row").Count));

    private static string Range(IRenderedComponent<CloudDataGrid> cut) => Text(cut.Find(".cloudgridfooter-info > span"));

    private static string PageLabel(IRenderedComponent<CloudDataGrid> cut) => Text(cut.Find(".cloudgridfooter-pager > span"));

    private static IElement NextButton(IRenderedComponent<CloudDataGrid> cut) => cut.Find("button[title='Next page']");

    private static IElement PreviousButton(IRenderedComponent<CloudDataGrid> cut) => cut.Find("button[title='Previous page']");

    private static IReadOnlyList<string> SelectedRowNames(IRenderedComponent<CloudDataGrid> cut) =>
        [.. cut.FindAll(".cloudgrid-row._selected").Select(row => Text(row.QuerySelector(".cloudgrid-cell")!))];

    private static CloudDataGridDataResult Page(int page, int pageSize, int total, int rowCount, int firstId) =>
        new()
        {
            Rows = [.. Enumerable.Range(firstId, rowCount).Select(number => new CloudDataGridRow { Id = new Guid($"{number:D8}-0000-0000-0000-000000000000"), Cells = [$"Row {number}", number] })],
            Page = page,
            PageSize = pageSize,
            Total = total
        };

    // ----- first load ---------------------------------------------------------------------------------------------------------

    [Fact]
    public void The_grid_asks_the_provider_for_the_first_page_with_the_default_page_size()
    {
        GridRig rig = new();

        RenderGrid(rig);

        CloudDataGridDataRequest request = Assert.Single(rig.Requests);
        Assert.Equal(1, request.Page);
        Assert.Equal(30, request.PageSize);
        Assert.Null(request.Search);
        Assert.Null(request.Sort);
        Assert.False(request.IsAppend);
        Assert.Equal(0, request.Total);
    }

    [Fact]
    public void Rows_per_page_becomes_the_page_size_of_the_request()
    {
        GridRig rig = new();

        RenderGrid(rig, p => p.Add(grid => grid.RowsPerPage, 12));

        Assert.Equal(12, rig.LastRequest.PageSize);
    }

    [Fact]
    public void The_provider_rows_are_rendered_under_the_column_headings()
    {
        GridRig rig = new();

        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig);

        WaitForRows(cut, 3);
        Assert.Equal(["Name", "Value"], Texts(cut.FindAll(".cloudgrid-headcell")));
        Assert.Equal(["Row 1", "Row 2", "Row 3"], FirstCells(cut));
    }

    [Fact]
    public void The_provider_is_not_called_again_by_an_ordinary_re_render()
    {
        GridRig rig = new();
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig);
        WaitForRows(cut, 3);

        cut.Render(p => p.Add(grid => grid.CssClass, "extra"));

        Assert.Single(rig.Requests);
    }

    [Fact]
    public void While_the_first_page_loads_the_grid_is_busy_and_shows_the_loading_text()
    {
        TaskCompletionSource gate = new();
        GridRig rig = new() { Gate = gate.Task };

        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig, p => p.Add(grid => grid.LoadingText, "hold on"));

        Assert.Contains("_busy", cut.Find(".cloudgrid").ClassList);
        Assert.Equal("hold on", Text(cut.Find(".cloudgrid-status._loading")));
        Assert.Empty(cut.FindAll(".cloudgrid-row"));

        gate.SetResult();
        WaitForRows(cut, 3);
        Assert.DoesNotContain("_busy", cut.Find(".cloudgrid").ClassList);
        Assert.Empty(cut.FindAll(".cloudgrid-status"));
    }

    [Fact]
    public void An_empty_result_says_there_are_no_records()
    {
        GridRig rig = new() { Respond = _ => new CloudDataGridDataResult() };

        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig);

        cut.WaitForAssertion(() => Assert.Equal("No records.", Text(cut.Find(".cloudgrid-status._message"))));
    }

    [Fact]
    public void An_error_message_from_the_provider_is_shown_instead_of_the_rows()
    {
        GridRig rig = new() { Respond = _ => new CloudDataGridDataResult { ErrorMessage = "Quota exceeded", Rows = [Row(1, "hidden", 1)], Total = 1 } };

        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig);

        cut.WaitForAssertion(() => Assert.Equal("Quota exceeded", Text(cut.Find(".cloudgrid-status._message"))));
        Assert.Empty(cut.FindAll(".cloudgrid-row"));
    }

    [Fact]
    public void A_null_result_is_reported_as_a_failed_load()
    {
        GridRig rig = new() { Respond = _ => null };

        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig);

        cut.WaitForAssertion(() => Assert.Equal("Failed to load data.", Text(cut.Find(".cloudgrid-status._message"))));
    }

    [Fact]
    public void A_provider_that_throws_shows_the_exception_message_and_does_not_crash_the_grid()
    {
        GridRig rig = new() { Respond = _ => throw new InvalidOperationException("database is down") };

        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig);

        cut.WaitForAssertion(() => Assert.Equal("database is down", Text(cut.Find(".cloudgrid-status._message"))));
        Assert.DoesNotContain("_busy", cut.Find(".cloudgrid").ClassList);
    }

    [Fact]
    public void After_a_failed_load_the_refresh_button_loads_again()
    {
        GridRig rig = new() { Respond = _ => null };
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig, p => p.Add(grid => grid.Header, new CloudDataGridHeaderOptions { AllowExport = false }));
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".cloudgrid-status._message")));
        rig.Respond = _ => Page(1, 30, 1, 1, 1);

        cut.Find("button[title='Refresh']").Click();

        WaitForRows(cut, 1);
    }

    // ----- footer and paging -------------------------------------------------------------------------------------------------

    [Fact]
    public void The_footer_shows_the_range_the_page_and_the_total()
    {
        GridRig rig = new() { Respond = _ => Page(1, 30, 70, 30, 1) };

        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig);

        WaitForRows(cut, 30);
        Assert.Equal("1 - 30 out of 70", Range(cut));
        Assert.Equal("Page 1", PageLabel(cut));
    }

    [Fact]
    public void On_the_first_page_previous_is_disabled_and_next_is_enabled_when_more_rows_exist()
    {
        GridRig rig = new() { Respond = _ => Page(1, 30, 70, 30, 1) };

        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig);

        WaitForRows(cut, 30);
        Assert.True(PreviousButton(cut).HasAttribute("disabled"));
        Assert.False(NextButton(cut).HasAttribute("disabled"));
    }

    [Fact]
    public void Next_requests_the_following_page_with_the_known_total_and_replaces_the_rows()
    {
        GridRig rig = new();
        rig.Respond = request => request.Page == 1 ? Page(1, 30, 70, 30, 1) : Page(2, 30, 70, 30, 31);
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig);
        WaitForRows(cut, 30);

        NextButton(cut).Click();

        cut.WaitForAssertion(() => Assert.Equal("Row 31", FirstCells(cut)[0]));
        CloudDataGridDataRequest request = rig.LastRequest;
        Assert.Equal(2, request.Page);
        Assert.False(request.IsAppend);
        Assert.Equal(70, request.Total);
        Assert.Equal(30, cut.FindAll(".cloudgrid-row").Count);
        Assert.Equal("31 - 60 out of 70", Range(cut));
        Assert.Equal("Page 2", PageLabel(cut));
        Assert.False(PreviousButton(cut).HasAttribute("disabled"));
    }

    [Fact]
    public void Previous_goes_back_to_the_page_before()
    {
        GridRig rig = new();
        rig.Respond = request => request.Page == 1 ? Page(1, 30, 70, 30, 1) : Page(2, 30, 70, 30, 31);
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig);
        WaitForRows(cut, 30);
        NextButton(cut).Click();
        cut.WaitForAssertion(() => Assert.Equal("Page 2", PageLabel(cut)));

        PreviousButton(cut).Click();

        cut.WaitForAssertion(() => Assert.Equal("Page 1", PageLabel(cut)));
        Assert.Equal(1, rig.LastRequest.Page);
    }

    [Fact]
    public void On_the_last_page_next_is_disabled_and_the_range_ends_at_the_total()
    {
        GridRig rig = new();
        rig.Respond = request => request.Page switch
        {
            1 => Page(1, 30, 70, 30, 1),
            2 => Page(2, 30, 70, 30, 31),
            _ => Page(3, 30, 70, 10, 61)
        };
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig);
        WaitForRows(cut, 30);
        NextButton(cut).Click();
        cut.WaitForAssertion(() => Assert.Equal("Page 2", PageLabel(cut)));

        NextButton(cut).Click();

        cut.WaitForAssertion(() => Assert.Equal("61 - 70 out of 70", Range(cut)));
        Assert.True(NextButton(cut).HasAttribute("disabled"));
    }

    [Fact]
    public void Pressing_previous_on_the_first_page_does_not_call_the_provider()
    {
        GridRig rig = new() { Respond = _ => Page(1, 30, 70, 30, 1) };
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig);
        WaitForRows(cut, 30);

        PreviousButton(cut).Click();

        Assert.Single(rig.Requests);
    }

    [Fact]
    public void A_single_page_of_results_has_no_next_page()
    {
        GridRig rig = new();
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig);

        WaitForRows(cut, 3);

        Assert.True(NextButton(cut).HasAttribute("disabled"));
        Assert.True(PreviousButton(cut).HasAttribute("disabled"));
        Assert.Equal("1 - 3 out of 3", Range(cut));
    }

    [Fact]
    public void A_provider_that_forgets_the_page_size_gets_a_range_starting_at_one()
    {
        GridRig rig = new() { Respond = _ => new CloudDataGridDataResult { Rows = [Row(1, "a", 1)], Page = 2, PageSize = 0, Total = 5 } };

        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig);

        WaitForRows(cut, 1);
        Assert.Equal("1 - 1 out of 5", Range(cut));
    }

    // ----- load more ------------------------------------------------------------------------------------------------------------

    private void ConfigureLoadMore(ComponentParameterCollectionBuilder<CloudDataGrid> p) => p.Add(grid => grid.PagingMode, CloudDataGridPagingMode.LoadMore);

    [Fact]
    public void Load_more_shows_a_button_and_no_pager_and_a_range_over_the_loaded_rows()
    {
        GridRig rig = new() { Respond = _ => Page(1, 30, 40, 30, 1) };

        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig, ConfigureLoadMore);

        WaitForRows(cut, 30);
        Assert.Equal("load more", Text(cut.Find("button.cloudgrid-loadmore")));
        Assert.Contains("_hidden", cut.Find(".cloudgridfooter-pager").ClassList);
        Assert.Equal("1 - 30 out of 40", Range(cut));
    }

    [Fact]
    public void Clicking_load_more_requests_the_next_page_as_an_append_and_adds_the_rows()
    {
        GridRig rig = new();
        rig.Respond = request => request.IsAppend ? Page(2, 30, 40, 10, 31) : Page(1, 30, 40, 30, 1);
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig, ConfigureLoadMore);
        WaitForRows(cut, 30);

        cut.Find("button.cloudgrid-loadmore").Click();

        WaitForRows(cut, 40);
        CloudDataGridDataRequest request = rig.LastRequest;
        Assert.Equal(2, request.Page);
        Assert.True(request.IsAppend);
        Assert.Equal(40, request.Total);
        Assert.Equal("Row 1", FirstCells(cut)[0]);
        Assert.Equal("Row 40", FirstCells(cut)[39]);
        Assert.Empty(cut.FindAll("button.cloudgrid-loadmore"));
        Assert.Equal("1 - 40 out of 40", Range(cut));
    }

    [Fact]
    public void While_more_rows_load_the_existing_rows_stay_and_the_grid_is_not_busy()
    {
        GridRig rig = new() { Respond = _ => Page(1, 30, 40, 30, 1) };
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig, ConfigureLoadMore);
        WaitForRows(cut, 30);
        TaskCompletionSource gate = new();
        rig.Gate = gate.Task;
        rig.Respond = _ => Page(2, 30, 40, 10, 31);

        cut.Find("button.cloudgrid-loadmore").Click();

        Assert.Equal(30, cut.FindAll(".cloudgrid-row").Count);
        Assert.Empty(cut.FindAll(".cloudgrid-status"));
        Assert.DoesNotContain("_busy", cut.Find(".cloudgrid").ClassList);
        gate.SetResult();
        WaitForRows(cut, 40);
    }

    // ----- sorting --------------------------------------------------------------------------------------------------------------

    [Fact]
    public void Clicking_a_heading_sends_the_sort_to_the_provider_from_page_one()
    {
        GridRig rig = new();
        List<CloudDataGridColumn> columns = Columns("Name", "Value");
        columns[1].Key = "value_field";
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig, columns: columns);
        WaitForRows(cut, 3);

        cut.FindAll(".cloudgrid-headcell")[1].Click();

        cut.WaitForAssertion(() => Assert.Equal(2, rig.Requests.Count));
        CloudDataGridSort sort = rig.LastRequest.Sort!;
        Assert.Equal("value_field", sort.Key);
        Assert.Equal(1, sort.ColumnIndex);
        Assert.Equal(CloudDataGridSortDirection.Ascending, sort.Direction);
        Assert.Equal(1, rig.LastRequest.Page);
        Assert.False(rig.LastRequest.IsAppend);
    }

    [Fact]
    public void The_grid_does_not_sort_locally_but_leaves_the_order_to_the_provider()
    {
        GridRig rig = new() { Respond = _ => Page(1, 30, 3, 3, 1) };
        rig.Respond = _ =>
        {
            CloudDataGridDataResult result = Page(1, 30, 3, 3, 1);
            result.Rows.Reverse();
            return result;
        };
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig);
        WaitForRows(cut, 3);

        cut.FindAll(".cloudgrid-headcell")[0].Click();
        cut.WaitForAssertion(() => Assert.Equal(2, rig.Requests.Count));
        WaitForRows(cut, 3);

        Assert.Equal(["Row 3", "Row 2", "Row 1"], FirstCells(cut));
    }

    [Fact]
    public void The_sorted_heading_shows_the_direction_and_a_second_click_reverses_it()
    {
        GridRig rig = new();
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig);
        WaitForRows(cut, 3);

        cut.FindAll(".cloudgrid-headcell")[1].Click();
        cut.WaitForAssertion(() => Assert.Contains("_sorted-ascending", cut.FindAll(".cloudgrid-headcell")[1].ClassList));
        cut.FindAll(".cloudgrid-headcell")[1].Click();

        cut.WaitForAssertion(() => Assert.Contains("_sorted-descending", cut.FindAll(".cloudgrid-headcell")[1].ClassList));
        Assert.Equal(CloudDataGridSortDirection.Descending, rig.LastRequest.Sort!.Direction);
    }

    [Fact]
    public void A_column_that_is_not_sortable_does_not_trigger_a_request()
    {
        GridRig rig = new();
        List<CloudDataGridColumn> columns = Columns("Name", "Value");
        columns[0].Sortable = false;
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig, columns: columns);
        WaitForRows(cut, 3);

        cut.FindAll(".cloudgrid-headcell")[0].Click();

        Assert.Single(rig.Requests);
    }

    [Fact]
    public void Paging_keeps_the_active_sort()
    {
        GridRig rig = new() { Respond = request => Page(request.Page, 30, 70, 30, ((request.Page - 1) * 30) + 1) };
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig);
        WaitForRows(cut, 30);
        cut.FindAll(".cloudgrid-headcell")[0].Click();
        cut.WaitForAssertion(() => Assert.Equal(2, rig.Requests.Count));

        NextButton(cut).Click();

        cut.WaitForAssertion(() => Assert.Equal(3, rig.Requests.Count));
        Assert.Equal(2, rig.LastRequest.Page);
        Assert.Equal("Name", rig.LastRequest.Sort!.Key);
    }

    [Fact]
    public void Sorting_returns_to_page_one()
    {
        GridRig rig = new() { Respond = request => Page(request.Page, 30, 70, 30, ((request.Page - 1) * 30) + 1) };
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig);
        WaitForRows(cut, 30);
        NextButton(cut).Click();
        cut.WaitForAssertion(() => Assert.Equal("Page 2", PageLabel(cut)));

        cut.FindAll(".cloudgrid-headcell")[0].Click();

        cut.WaitForAssertion(() => Assert.Equal("Page 1", PageLabel(cut)));
        Assert.Equal(1, rig.LastRequest.Page);
    }

    // ----- search and refresh ---------------------------------------------------------------------------------------------------

    private static CloudDataGridHeaderOptions SearchableHeader() => new() { AllowSearch = true, SearchDebounceMilliseconds = 1, AllowExport = false };

    private static void Search(IRenderedComponent<CloudDataGrid> cut, string text)
    {
        if (cut.FindAll("input.cloudgridheader-search").Count == 0)
            cut.Find("button[title='Search']").Click();

        cut.Find("input.cloudgridheader-search").Input(text);
    }

    [Fact]
    public void Searching_sends_the_trimmed_query_from_page_one_without_append()
    {
        GridRig rig = new();
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig, p => p.Add(grid => grid.Header, SearchableHeader()));
        WaitForRows(cut, 3);

        Search(cut, "  alice ");

        cut.WaitForAssertion(() => Assert.Equal(2, rig.Requests.Count));
        Assert.Equal("alice", rig.LastRequest.Search);
        Assert.Equal(1, rig.LastRequest.Page);
        Assert.False(rig.LastRequest.IsAppend);
    }

    [Fact]
    public void While_a_search_runs_the_grid_shows_the_searching_text_not_the_loading_text()
    {
        GridRig rig = new();
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig, p =>
        {
            p.Add(grid => grid.Header, SearchableHeader());
            p.Add(grid => grid.SearchingText, "looking...");
        });
        WaitForRows(cut, 3);
        TaskCompletionSource gate = new();
        rig.Gate = gate.Task;

        Search(cut, "alice");

        cut.WaitForAssertion(() => Assert.Equal("looking...", Text(cut.Find(".cloudgrid-status._searching"))));
        Assert.Contains("_busy", cut.Find(".cloudgrid").ClassList);
        gate.SetResult();
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".cloudgrid-status")));
    }

    [Fact]
    public void A_search_without_results_names_the_query()
    {
        GridRig rig = new();
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig, p => p.Add(grid => grid.Header, SearchableHeader()));
        WaitForRows(cut, 3);
        rig.Respond = _ => new CloudDataGridDataResult();

        Search(cut, "zzz");

        cut.WaitForAssertion(() => Assert.Equal("No records match \"zzz\".", Text(cut.Find(".cloudgrid-status._message"))));
    }

    [Fact]
    public void Clearing_the_search_loads_everything_again_without_a_query()
    {
        GridRig rig = new();
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig, p => p.Add(grid => grid.Header, SearchableHeader()));
        WaitForRows(cut, 3);
        Search(cut, "alice");
        cut.WaitForAssertion(() => Assert.Equal("alice", rig.LastRequest.Search));

        cut.Find("input.cloudgridheader-search").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        cut.WaitForAssertion(() => Assert.Null(rig.LastRequest.Search));
        Assert.Equal(3, rig.Requests.Count);
    }

    [Fact]
    public void Searching_clears_the_sort_when_there_is_no_preference_key()
    {
        GridRig rig = new();
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig, p => p.Add(grid => grid.Header, SearchableHeader()));
        WaitForRows(cut, 3);
        cut.FindAll(".cloudgrid-headcell")[0].Click();
        cut.WaitForAssertion(() => Assert.NotNull(rig.LastRequest.Sort));

        Search(cut, "alice");

        cut.WaitForAssertion(() => Assert.Equal("alice", rig.LastRequest.Search));
        Assert.Null(rig.LastRequest.Sort);
        Assert.Empty(cut.FindAll("._sorted-ascending"));
    }

    [Fact]
    public void Searching_keeps_the_sort_when_a_preference_key_is_set()
    {
        GridRig rig = new();
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig, p =>
        {
            p.Add(grid => grid.Header, SearchableHeader());
            p.Add(grid => grid.PreferenceKey, "people");
        });
        WaitForRows(cut, 3);
        cut.FindAll(".cloudgrid-headcell")[0].Click();
        cut.WaitForAssertion(() => Assert.NotNull(rig.LastRequest.Sort));

        Search(cut, "alice");

        cut.WaitForAssertion(() => Assert.Equal("alice", rig.LastRequest.Search));
        Assert.Equal("Name", rig.LastRequest.Sort!.Key);
    }

    [Fact]
    public void Refresh_reloads_from_page_one_and_drops_the_sort_without_a_preference_key()
    {
        GridRig rig = new() { Respond = request => Page(request.Page, 30, 70, 30, ((request.Page - 1) * 30) + 1) };
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig, p => p.Add(grid => grid.Header, new CloudDataGridHeaderOptions { AllowExport = false }));
        WaitForRows(cut, 30);
        cut.FindAll(".cloudgrid-headcell")[0].Click();
        cut.WaitForAssertion(() => Assert.NotNull(rig.LastRequest.Sort));
        NextButton(cut).Click();
        cut.WaitForAssertion(() => Assert.Equal("Page 2", PageLabel(cut)));
        int before = rig.Requests.Count;

        cut.Find("button[title='Refresh']").Click();

        cut.WaitForAssertion(() => Assert.True(rig.Requests.Count > before));
        Assert.Null(rig.LastRequest.Search);
        Assert.Null(rig.LastRequest.Sort);
        Assert.Equal(1, rig.LastRequest.Page);
        Assert.Equal(0, rig.LastRequest.Total);
    }

    [Fact]
    public void Refresh_keeps_the_sort_when_a_preference_key_is_set()
    {
        GridRig rig = new();
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig, p =>
        {
            p.Add(grid => grid.Header, new CloudDataGridHeaderOptions { AllowExport = false });
            p.Add(grid => grid.PreferenceKey, "people");
        });
        WaitForRows(cut, 3);
        cut.FindAll(".cloudgrid-headcell")[0].Click();
        cut.WaitForAssertion(() => Assert.Equal(2, rig.Requests.Count));

        cut.Find("button[title='Refresh']").Click();

        cut.WaitForAssertion(() => Assert.Equal(3, rig.Requests.Count));
        Assert.Equal("Name", rig.LastRequest.Sort!.Key);
        Assert.Equal(1, rig.LastRequest.Page);
    }

    [Fact]
    public async Task ReloadAsync_starts_over_without_search_or_sort_and_shows_the_loading_state()
    {
        GridRig rig = new();
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig);
        WaitForRows(cut, 3);
        cut.FindAll(".cloudgrid-headcell")[0].Click();
        cut.WaitForAssertion(() => Assert.Equal(2, rig.Requests.Count));
        TaskCompletionSource gate = new();
        rig.Gate = gate.Task;

        Task reload = cut.InvokeAsync(() => cut.Instance.ReloadAsync());

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".cloudgrid-status._loading")));
        gate.SetResult();
        await reload;
        Assert.Null(rig.LastRequest.Sort);
        Assert.Null(rig.LastRequest.Search);
        Assert.Equal(1, rig.LastRequest.Page);
        Assert.Equal(0, rig.LastRequest.Total);
    }

    [Fact]
    public async Task RefreshAsync_reloads_page_one_but_keeps_the_sort()
    {
        GridRig rig = new() { Respond = request => Page(request.Page, 30, 70, 30, ((request.Page - 1) * 30) + 1) };
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig);
        WaitForRows(cut, 30);
        cut.FindAll(".cloudgrid-headcell")[0].Click();
        cut.WaitForAssertion(() => Assert.Equal(2, rig.Requests.Count));
        NextButton(cut).Click();
        cut.WaitForAssertion(() => Assert.Equal(3, rig.Requests.Count));

        await cut.InvokeAsync(() => cut.Instance.RefreshAsync());

        Assert.Equal(1, rig.LastRequest.Page);
        Assert.Equal("Name", rig.LastRequest.Sort!.Key);
        Assert.Equal(70, rig.LastRequest.Total);
    }

    // ----- selection --------------------------------------------------------------------------------------------------------------

    [Fact]
    public void Without_selection_enabled_there_is_no_checkbox_column_hidden_field_or_selected_count()
    {
        GridRig rig = new();
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig, p => p.Add(grid => grid.SelectedRecords, [Id(1)]));
        WaitForRows(cut, 3);

        Assert.Empty(cut.FindAll("input[type=checkbox]"));
        Assert.Empty(cut.FindAll("input#SelectedRecords"));
        Assert.Empty(cut.FindAll(".cloudgridfooter-selected"));
        Assert.DoesNotContain("_selectable", cut.Find(".cloudgrid").ClassList);
    }

    [Fact]
    public void With_selection_enabled_the_initial_selection_is_checked_in_the_rows_hidden_field_and_footer()
    {
        GridRig rig = new();
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig, p =>
        {
            p.Add(grid => grid.AllowSelection, true);
            p.Add(grid => grid.SelectedRecords, [Id(1), Id(3)]);
        });
        WaitForRows(cut, 3);

        Assert.Contains("_selectable", cut.Find(".cloudgrid").ClassList);
        Assert.Equal($"{Id(1)},{Id(3)}", cut.Find("input#SelectedRecords").GetAttribute("value"));
        Assert.Equal("2 selected", Text(cut.Find(".cloudgridfooter-selected")));
        Assert.Equal(["Row 1", "Row 3"], SelectedRowNames(cut));
    }

    [Fact]
    public void Ticking_a_row_raises_SelectedRecordsChanged_and_updates_the_footer_and_hidden_field()
    {
        List<List<Guid>> changes = [];
        GridRig rig = new();
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig, p =>
        {
            p.Add(grid => grid.AllowSelection, true);
            p.Add(grid => grid.SelectedRecordsChanged, (List<Guid> selected) => changes.Add(selected));
        });
        WaitForRows(cut, 3);

        cut.FindAll(".cloudgrid-row input[type=checkbox]")[1].Change(true);

        Assert.Equal([Id(2)], Assert.Single(changes));
        Assert.Equal("1 selected", Text(cut.Find(".cloudgridfooter-selected")));
        Assert.Equal(Id(2).ToString(), cut.Find("input#SelectedRecords").GetAttribute("value"));
    }

    [Fact]
    public void The_select_all_box_selects_every_loaded_row()
    {
        List<List<Guid>> changes = [];
        GridRig rig = new();
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig, p =>
        {
            p.Add(grid => grid.AllowSelection, true);
            p.Add(grid => grid.SelectedRecordsChanged, (List<Guid> selected) => changes.Add(selected));
        });
        WaitForRows(cut, 3);

        cut.Find(".cloudgrid-head input[type=checkbox]").Change(true);

        Assert.Equal([Id(1), Id(2), Id(3)], Assert.Single(changes));
        Assert.Equal("3 selected", Text(cut.Find(".cloudgridfooter-selected")));
    }

    [Fact]
    public void A_user_selection_survives_a_re_render_that_passes_the_same_list_instance()
    {
        List<Guid> bound = [];
        GridRig rig = new();
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig, p =>
        {
            p.Add(grid => grid.AllowSelection, true);
            p.Add(grid => grid.SelectedRecords, bound);
        });
        WaitForRows(cut, 3);
        cut.FindAll(".cloudgrid-row input[type=checkbox]")[0].Change(true);

        cut.Render(p => p.Add(grid => grid.SelectedRecords, bound));

        Assert.Equal("1 selected", Text(cut.Find(".cloudgridfooter-selected")));
    }

    [Fact]
    public void Passing_a_new_list_instance_replaces_the_selection()
    {
        GridRig rig = new();
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig, p =>
        {
            p.Add(grid => grid.AllowSelection, true);
            p.Add(grid => grid.SelectedRecords, [Id(1)]);
        });
        WaitForRows(cut, 3);

        cut.Render(p => p.Add(grid => grid.SelectedRecords, [Id(2), Id(3)]));

        Assert.Equal("2 selected", Text(cut.Find(".cloudgridfooter-selected")));
        Assert.Equal(["Row 2", "Row 3"], SelectedRowNames(cut));
    }

    [Fact]
    public void Passing_null_clears_the_selection()
    {
        GridRig rig = new();
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig, p =>
        {
            p.Add(grid => grid.AllowSelection, true);
            p.Add(grid => grid.SelectedRecords, [Id(1)]);
        });
        WaitForRows(cut, 3);

        cut.Render(p => p.Add(grid => grid.SelectedRecords, null));

        Assert.Empty(cut.FindAll(".cloudgridfooter-selected"));
        Assert.Empty(cut.FindAll(".cloudgrid-row._selected"));
    }

    [Fact]
    public void The_selection_is_kept_when_paging_to_rows_that_are_not_selected()
    {
        GridRig rig = new() { Respond = request => Page(request.Page, 3, 6, 3, ((request.Page - 1) * 3) + 1) };
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig, p =>
        {
            p.Add(grid => grid.AllowSelection, true);
            p.Add(grid => grid.SelectedRecords, [Id(1)]);
        });
        WaitForRows(cut, 3);

        NextButton(cut).Click();
        cut.WaitForAssertion(() => Assert.Equal("Row 4", FirstCells(cut)[0]));

        Assert.Empty(cut.FindAll(".cloudgrid-row._selected"));
        Assert.Equal("1 selected", Text(cut.Find(".cloudgridfooter-selected")));
    }

    [Fact]
    public void Bulk_actions_in_the_header_follow_the_selection_and_carry_the_selected_ids()
    {
        List<CloudDataGridActionEventArgs> clicks = [];
        CloudDataGridAction delete = new() { Key = "delete", Text = "Delete", ShowOnBulkHeader = true, ShowOnHeader = false };
        GridRig rig = new();
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig, p =>
        {
            p.Add(grid => grid.AllowSelection, true);
            p.Add(grid => grid.Header, new CloudDataGridHeaderOptions
            {
                Actions = [delete],
                AllowExport = false,
                AllowRefresh = false,
                OnActionClicked = EventCallback.Factory.Create<CloudDataGridActionEventArgs>(this, args => clicks.Add(args))
            });
        });
        WaitForRows(cut, 3);
        Assert.Empty(cut.FindAll(".cloudgridheader-bulkactions"));

        cut.FindAll(".cloudgrid-row input[type=checkbox]")[0].Change(true);
        cut.FindAll(".cloudgrid-row input[type=checkbox]")[2].Change(true);
        cut.Find(".cloudgridheader-bulkactions button").Click();

        CloudDataGridActionEventArgs click = Assert.Single(clicks);
        Assert.Same(delete, click.Action);
        Assert.Equal([Id(1), Id(3)], click.RecordIds);
    }

    [Fact]
    public void Without_selection_enabled_bulk_actions_never_appear_even_if_ids_were_passed_in()
    {
        CloudDataGridAction delete = new() { Key = "delete", Text = "Delete", ShowOnBulkHeader = true, ShowOnHeader = false };
        GridRig rig = new();
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig, p =>
        {
            p.Add(grid => grid.SelectedRecords, [Id(1)]);
            p.Add(grid => grid.Header, new CloudDataGridHeaderOptions { Actions = [delete], AllowExport = false });
        });
        WaitForRows(cut, 3);

        Assert.Empty(cut.FindAll(".cloudgridheader-bulkactions"));
    }

    // ----- header ---------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void Without_header_options_there_is_no_toolbar()
    {
        GridRig rig = new();
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig);
        WaitForRows(cut, 3);

        Assert.Empty(cut.FindAll(".cloudgridheader"));
    }

    [Fact]
    public void With_header_options_the_toolbar_shows_the_label_and_the_built_in_buttons()
    {
        GridRig rig = new();
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig, p => p.Add(grid => grid.Header, new CloudDataGridHeaderOptions { Label = "People", AllowSearch = true }));
        WaitForRows(cut, 3);

        Assert.Equal("People", Text(cut.Find(".cloudgridheader h2")));
        Assert.Single(cut.FindAll("button[title='Refresh']"));
        Assert.Single(cut.FindAll("button[title='Search']"));
        Assert.Single(cut.FindAll("button[title='More']"));
    }

    [Fact]
    public void Refresh_and_export_can_be_switched_off_in_the_header_options()
    {
        GridRig rig = new();
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig, p => p.Add(grid => grid.Header, new CloudDataGridHeaderOptions { AllowRefresh = false, AllowExport = false }));
        WaitForRows(cut, 3);

        Assert.Empty(cut.FindAll("button[title='Refresh']"));
        Assert.Empty(cut.FindAll("button[title='More']"));
    }

    [Fact]
    public void A_header_button_action_raises_the_header_options_OnActionClicked()
    {
        List<CloudDataGridActionEventArgs> clicks = [];
        CloudDataGridAction add = new() { Key = "add", Text = "Add" };
        GridRig rig = new();
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig, p => p.Add(grid => grid.Header, new CloudDataGridHeaderOptions
        {
            Actions = [add],
            AllowExport = false,
            OnActionClicked = EventCallback.Factory.Create<CloudDataGridActionEventArgs>(this, args => clicks.Add(args))
        }));
        WaitForRows(cut, 3);

        cut.Find("button[title='Add']").Click();

        Assert.Same(add, Assert.Single(clicks).Action);
    }

    [Fact(Skip = "BUG: CloudDataGrid.OnActionClicked is documented as 'Raised when any button action is activated (row or header)' (and the README says 'row/header/bulk'), but header and bulk actions are only raised through Header.OnActionClicked; the grid-level callback fires for row actions only.")]
    public void A_header_button_action_also_raises_the_grid_level_OnActionClicked()
    {
        List<CloudDataGridActionEventArgs> clicks = [];
        CloudDataGridAction add = new() { Key = "add", Text = "Add" };
        GridRig rig = new();
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig, p =>
        {
            p.Add(grid => grid.Header, new CloudDataGridHeaderOptions { Actions = [add], AllowExport = false });
            p.Add(grid => grid.OnActionClicked, (CloudDataGridActionEventArgs args) => clicks.Add(args));
        });
        WaitForRows(cut, 3);

        cut.Find("button[title='Add']").Click();

        Assert.Same(add, Assert.Single(clicks).Action);
    }

    // ----- row actions ------------------------------------------------------------------------------------------------------------

    [Fact]
    public void A_row_action_raises_the_grid_level_OnActionClicked_with_the_row_id()
    {
        List<CloudDataGridActionEventArgs> clicks = [];
        CloudDataGridAction edit = new() { Key = "edit", Text = "Edit", ShowOnRow = true };
        GridRig rig = new();
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig, p =>
        {
            p.Add(grid => grid.RowActions, [edit]);
            p.Add(grid => grid.OnActionClicked, (CloudDataGridActionEventArgs args) => clicks.Add(args));
        });
        WaitForRows(cut, 3);

        cut.FindAll(".cloudgrid-rowactions button")[2].Click();

        CloudDataGridActionEventArgs click = Assert.Single(clicks);
        Assert.Same(edit, click.Action);
        Assert.Equal([Id(3)], click.RecordIds);
    }

    [Fact]
    public void Header_row_actions_and_explicit_row_actions_are_merged_and_the_header_one_wins_a_key_clash()
    {
        CloudDataGridAction fromHeader = new() { Key = "edit", Text = "Header edit", ShowOnRow = true };
        CloudDataGridAction duplicate = new() { Key = "edit", Text = "Explicit edit", ShowOnRow = true };
        CloudDataGridAction other = new() { Key = "view", Text = "View", ShowOnRow = true };
        GridRig rig = new();
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig, p =>
        {
            p.Add(grid => grid.Header, new CloudDataGridHeaderOptions { Actions = [fromHeader], AllowExport = false, AllowRefresh = false });
            p.Add(grid => grid.RowActions, [duplicate, other]);
        });
        WaitForRows(cut, 3);

        IReadOnlyList<IElement> firstRowActions = cut.FindAll(".cloudgrid-row")[0].QuerySelectorAll(".cloudgrid-rowactions button").ToList();

        Assert.Equal(["Header edit", "View"], Texts(firstRowActions));
    }

    [Fact]
    public void The_action_filter_is_applied_to_the_rows()
    {
        CloudDataGridAction edit = new() { Key = "edit", Text = "Edit", ShowOnRow = true };
        GridRig rig = new();
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig, p =>
        {
            p.Add(grid => grid.RowActions, [edit]);
            p.Add(grid => grid.ActionFilter, (CloudDataGridRow row, CloudDataGridAction action) => row.Id != Id(2));
        });
        WaitForRows(cut, 3);

        Assert.Equal(2, cut.FindAll(".cloudgrid-rowactions").Count);
    }

    // ----- reorder wiring and root adjectives -----------------------------------------------------------------------------------

    [Fact]
    public void Reordering_rows_is_reported_through_the_grid_and_flags_the_root_while_dragging()
    {
        List<CloudDataGridRowReorder> reorders = [];
        GridRig rig = new();
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig, p =>
        {
            p.Add(grid => grid.AllowReordering, true);
            p.Add(grid => grid.RowHeight, 40d);
            p.Add(grid => grid.OnRowsReordered, (CloudDataGridRowReorder reorder) => reorders.Add(reorder));
        });
        WaitForRows(cut, 3);
        Assert.Contains("_reorderable", cut.Find(".cloudgrid").ClassList);

        cut.FindAll(".cloudgrid-row .cloudgrid-draghandle")[0].PointerDown(new PointerEventArgs { ClientY = 0 });
        Assert.Contains("_rowdragging", cut.Find(".cloudgrid").ClassList);
        cut.Find(".cloudgrid-shield._rowdrag").PointerMove(new PointerEventArgs { ClientY = 40 });
        cut.Find(".cloudgrid-shield._rowdrag").PointerUp(new PointerEventArgs());

        CloudDataGridRowReorder reorder = Assert.Single(reorders);
        Assert.Equal(Id(1), reorder.RecordId);
        Assert.Equal([Id(2), Id(1), Id(3)], reorder.OrderedRecordIds);
        Assert.DoesNotContain("_rowdragging", cut.Find(".cloudgrid").ClassList);
    }

    [Fact]
    public void Resizing_a_column_flags_the_root_while_the_handle_is_held()
    {
        GridRig rig = new();
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig);
        WaitForRows(cut, 3);

        cut.FindAll(".cloudgrid-resizer")[0].PointerDown(new PointerEventArgs { ClientX = 0 });
        Assert.Contains("_resizing", cut.Find(".cloudgrid").ClassList);
        cut.Find(".cloudgrid-shield").PointerUp(new PointerEventArgs());

        Assert.DoesNotContain("_resizing", cut.Find(".cloudgrid").ClassList);
    }

    [Fact]
    public void The_root_is_full_height_by_default_and_not_in_the_other_height_modes()
    {
        GridRig rig = new();
        IRenderedComponent<CloudDataGrid> full = RenderGrid(rig);
        IRenderedComponent<CloudDataGrid> auto = RenderGrid(new GridRig(), p => p.Add(grid => grid.HeightMode, CloudDataGridHeightMode.Auto));
        IRenderedComponent<CloudDataGrid> rows = RenderGrid(new GridRig(), p => p.Add(grid => grid.HeightMode, CloudDataGridHeightMode.RowHeight));

        Assert.Contains("_full-height", full.Find(".cloudgrid").ClassList);
        Assert.DoesNotContain("_full-height", auto.Find(".cloudgrid").ClassList);
        Assert.DoesNotContain("_full-height", rows.Find(".cloudgrid").ClassList);
    }

    [Fact]
    public void A_css_class_and_unmatched_attributes_land_on_the_root_element()
    {
        GridRig rig = new();

        IRenderedComponent<CloudDataGrid> cut = Render<CloudDataGrid>(parameters =>
        {
            parameters.Add(grid => grid.DataProvider, rig.Provider);
            parameters.Add(grid => grid.Columns, Columns("A"));
            parameters.Add(grid => grid.CssClass, "mine other");
            parameters.AddUnmatched("data-testid", "people-grid");
            parameters.AddUnmatched("aria-label", "People");
        });

        IElement root = cut.Find(".cloudgrid");
        Assert.Contains("mine", root.ClassList);
        Assert.Contains("other", root.ClassList);
        Assert.Equal("people-grid", root.GetAttribute("data-testid"));
        Assert.Equal("People", root.GetAttribute("aria-label"));
    }

    [Fact]
    public void A_blank_css_class_adds_nothing()
    {
        GridRig rig = new();

        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig, p => p.Add(grid => grid.CssClass, "  "));

        Assert.DoesNotContain("  ", cut.Find(".cloudgrid").GetAttribute("class"));
    }

    [Fact]
    public void The_row_height_is_set_as_a_css_variable_in_pixels_using_the_invariant_culture()
    {
        CultureInfo original = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            GridRig rig = new();

            IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig, p => p.Add(grid => grid.RowHeight, 37.5));

            Assert.Contains("--cloudcomponents-grid-row-height: 37.5px", cut.Find(".cloudgrid").GetAttribute("style"));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Without_a_row_height_the_variable_is_not_set()
    {
        GridRig rig = new();

        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig);

        Assert.DoesNotContain("--cloudcomponents-grid-row-height", cut.Find(".cloudgrid").GetAttribute("style"));
    }

    [Fact]
    public void The_note_height_variable_is_zero_until_a_row_has_a_note()
    {
        GridRig rig = new();
        IRenderedComponent<CloudDataGrid> plain = RenderGrid(rig);
        WaitForRows(plain, 3);

        GridRig withNotes = new();
        withNotes.Respond = _ =>
        {
            CloudDataGridDataResult result = Page(1, 30, 3, 3, 1);
            result.Rows[1].Note = "careful";
            return result;
        };
        IRenderedComponent<CloudDataGrid> noted = RenderGrid(withNotes, p => p.Add(grid => grid.RowNoteHeight, 30));
        WaitForRows(noted, 3);

        Assert.Contains("--cloudcomponents-grid-note-height: 0px", plain.Find(".cloudgrid").GetAttribute("style"));
        Assert.Contains("--cloudcomponents-grid-note-height: 30px", noted.Find(".cloudgrid").GetAttribute("style"));
    }

    [Fact]
    public void Reserving_note_space_sets_the_note_height_even_without_notes_and_hiding_notes_zeroes_it()
    {
        GridRig reserve = new();
        IRenderedComponent<CloudDataGrid> reserved = RenderGrid(reserve, p => p.Add(grid => grid.ReserveRowNoteSpace, true));
        GridRig hide = new();
        hide.Respond = _ =>
        {
            CloudDataGridDataResult result = Page(1, 30, 3, 3, 1);
            result.Rows[0].Note = "hi";
            return result;
        };
        IRenderedComponent<CloudDataGrid> hidden = RenderGrid(hide, p => p.Add(grid => grid.ShowRowNotes, false));
        WaitForRows(hidden, 3);

        Assert.Contains("--cloudcomponents-grid-note-height: 24px", reserved.Find(".cloudgrid").GetAttribute("style"));
        Assert.Contains("--cloudcomponents-grid-note-height: 0px", hidden.Find(".cloudgrid").GetAttribute("style"));
    }

    // ----- body parameters pass through ------------------------------------------------------------------------------------------

    [Fact]
    public void Display_options_reach_the_body()
    {
        GridRig rig = new();
        rig.Respond = _ =>
        {
            CloudDataGridDataResult result = Page(1, 30, 1, 1, 1);
            result.Rows[0].Cells = [null, 1];
            result.Rows[0].Link = "/r/1";
            return result;
        };

        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig, p =>
        {
            p.Add(grid => grid.EmptyCellText, "n/a");
            p.Add(grid => grid.LinkTarget, "_self");
            p.Add(grid => grid.DisplayAllText, true);
        });

        WaitForRows(cut, 1);
        Assert.Equal("n/a", Text(cut.Find(".cloudgrid-cell")));
        Assert.Equal("_self", cut.Find(".cloudgrid-rowlink").GetAttribute("target"));
        Assert.Contains("_display-all-text", cut.Find(".cloudgrid-body").ClassList);
    }

    [Fact]
    public void Column_footer_rows_are_rendered_by_the_grid()
    {
        GridRig rig = new();

        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig, p => p.Add(grid => grid.ColumnFooterRows, [new CloudDataGridFooterRow { Cells = ["Total", 6] }]));

        WaitForRows(cut, 3);
        Assert.Equal(["Total", "6"], Texts(cut.FindAll(".cloudgrid-columnfooter .cloudgrid-cell")));
    }

    [Fact]
    public void Collapsed_categories_are_two_way_and_follow_the_bound_instance()
    {
        List<HashSet<string>> changes = [];
        GridRig rig = new();
        rig.Respond = _ =>
        {
            CloudDataGridDataResult result = Page(1, 30, 3, 3, 1);
            result.Rows[0].CategoryKey = "a";
            result.Rows[1].CategoryKey = "a";
            result.Rows[2].CategoryKey = "b";
            return result;
        };
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig, p => p.Add(grid => grid.CollapsedCategoriesChanged, (HashSet<string> set) => changes.Add(set)));
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(".cloudgrid-category").Count));
        Assert.Equal(3, cut.FindAll(".cloudgrid-row").Count);

        cut.FindAll(".cloudgrid-category button")[0].Click();

        Assert.Equal(["a"], Assert.Single(changes));
        Assert.Equal(1, cut.FindAll(".cloudgrid-row").Count);

        cut.Render(p => p.Add(grid => grid.CollapsedCategories, new HashSet<string> { "b" }));
        Assert.Equal(2, cut.FindAll(".cloudgrid-row").Count);
    }
}
