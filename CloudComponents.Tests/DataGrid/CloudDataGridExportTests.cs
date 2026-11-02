using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngryMonkey.CloudComponents.DataGrid.Components;
using AngryMonkey.CloudComponents.DataGrid.Models;
using Bunit;

namespace CloudComponents.Tests.DataGrid;

/// <summary>The built-in CSV export: the More menu entry, the three scopes, the file contents and the JavaScript download call.</summary>
public class CloudDataGridExportTests : DataGridTestBase
{
    private const string ModulePath = "./_content/AngryMonkey.CloudComponents.DataGrid/cloudgrid.js";

    private BunitJSModuleInterop Module { get; }

    public CloudDataGridExportTests()
    {
        Module = JSInterop.SetupModule(ModulePath);
        Module.SetupVoid("downloadTextFile", _ => true);
    }

    private IRenderedComponent<CloudDataGrid> RenderGrid(
        GridRig rig,
        List<CloudDataGridColumn>? columns = null,
        Action<ComponentParameterCollectionBuilder<CloudDataGrid>>? configure = null,
        CloudDataGridHeaderOptions? header = null)
    {
        IRenderedComponent<CloudDataGrid> cut = Render<CloudDataGrid>(parameters =>
        {
            parameters.Add(grid => grid.DataProvider, rig.Provider);
            parameters.Add(grid => grid.Columns, columns ?? Columns("Name", "Age"));
            parameters.Add(grid => grid.Header, header ?? new CloudDataGridHeaderOptions { AllowRefresh = false });
            configure?.Invoke(parameters);
        });

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".cloudgrid-row")));
        return cut;
    }

    private static GridRig PeopleRig() => new()
    {
        Respond = _ => new CloudDataGridDataResult
        {
            Rows =
            [
                Row(1, "Ada", 36),
                Row(2, "Linus \"Tux\" Torvalds", 54),
                Row(3, "Grace", null)
            ],
            Page = 1,
            PageSize = 30,
            Total = 3
        }
    };

    private static void OpenExportPanel(IRenderedComponent<CloudDataGrid> cut)
    {
        cut.Find("button[title='More']").Click();
        cut.Find(".cloudgridheader-more-item[title='Export']").Click();
    }

    private static void ClickExport(IRenderedComponent<CloudDataGrid> cut, string buttonText)
    {
        OpenExportPanel(cut);
        cut.FindAll(".cloudgrid-export-btn").Single(button => Text(button).StartsWith(buttonText, StringComparison.Ordinal)).Click();
    }

    private (string FileName, string Csv, string ContentType) Downloaded()
    {
        IReadOnlyList<JSRuntimeInvocation> calls = Module.Invocations["downloadTextFile"];
        JSRuntimeInvocation call = Assert.Single(calls);

        return ((string)call.Arguments[0]!, (string)call.Arguments[1]!, (string)call.Arguments[2]!);
    }

    private static readonly string NewLine = Environment.NewLine;

    // ----- the menu entry and panel ---------------------------------------------------------------------------------------------

    [Fact]
    public void Export_is_offered_in_the_more_menu_by_default()
    {
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(PeopleRig());

        cut.Find("button[title='More']").Click();

        Assert.Equal(["Export"], Texts(cut.FindAll(".cloudgridheader-more-item")));
    }

    [Fact]
    public void Export_can_be_switched_off()
    {
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(PeopleRig(), header: new CloudDataGridHeaderOptions { AllowRefresh = false, AllowExport = false });

        Assert.Empty(cut.FindAll("button[title='More']"));
    }

    [Fact]
    public void Export_is_not_offered_without_header_options()
    {
        GridRig rig = PeopleRig();
        IRenderedComponent<CloudDataGrid> cut = Render<CloudDataGrid>(parameters =>
        {
            parameters.Add(grid => grid.DataProvider, rig.Provider);
            parameters.Add(grid => grid.Columns, Columns("Name", "Age"));
        });
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".cloudgrid-row")));

        Assert.Empty(cut.FindAll("button[title='More']"));
    }

    [Fact]
    public void The_export_entry_sits_next_to_the_consumers_own_more_actions_without_duplicating()
    {
        CloudDataGridAction mine = new() { Key = "archive", Text = "Archive", ShowInMore = true };
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(PeopleRig(), header: new CloudDataGridHeaderOptions { AllowRefresh = false, Actions = [mine] });
        cut.Render(_ => { });
        cut.Render(_ => { });

        cut.Find("button[title='More']").Click();

        Assert.Equal(["Archive", "Export"], Texts(cut.FindAll(".cloudgridheader-more-item")));
    }

    [Fact]
    public void Choosing_export_opens_a_panel_with_current_page_and_all_records()
    {
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(PeopleRig());

        OpenExportPanel(cut);

        Assert.Equal(["Current page", "All records"], Texts(cut.FindAll(".cloudgrid-export-panel .cloudgrid-export-btn")));
    }

    [Fact]
    public void The_panel_offers_the_selection_with_its_size_only_when_rows_are_selected()
    {
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(PeopleRig(), configure: p =>
        {
            p.Add(grid => grid.AllowSelection, true);
            p.Add(grid => grid.SelectedRecords, [Id(1), Id(3)]);
        });

        OpenExportPanel(cut);

        Assert.Equal(["Current page", "All records", "Selection (2)"], Texts(cut.FindAll(".cloudgrid-export-btn")));
    }

    // ----- current page ------------------------------------------------------------------------------------------------------------

    [Fact]
    public void Exporting_the_current_page_downloads_a_csv_with_the_header_and_every_row()
    {
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(PeopleRig());

        ClickExport(cut, "Current page");

        (string fileName, string csv, string contentType) = Downloaded();
        Assert.Equal("text/csv;charset=utf-8", contentType);
        Assert.Matches(new Regex(@"^cloudgrid-export-current-page-\d{8}-\d{6}\.csv$"), fileName);
        Assert.Equal(
            string.Join(NewLine, "\"Name\",\"Age\"", "\"Ada\",\"36\"", "\"Linus \"\"Tux\"\" Torvalds\",\"54\"", "\"Grace\",\"\""),
            csv);
    }

    [Fact]
    public void The_file_name_carries_the_time_of_the_export()
    {
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(PeopleRig());
        DateTime before = DateTime.Now;

        ClickExport(cut, "Current page");

        string stamp = Regex.Match(Downloaded().FileName, @"(\d{8}-\d{6})").Value;
        DateTime parsed = DateTime.ParseExact(stamp, "yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture);
        Assert.InRange(parsed, before.AddSeconds(-2), DateTime.Now.AddSeconds(2));
    }

    [Fact]
    public void Image_columns_are_left_out_of_the_header_and_the_rows()
    {
        List<CloudDataGridColumn> columns = Columns("Photo", "Name");
        columns[0].IsImage = true;
        GridRig rig = new()
        {
            Respond = _ => new CloudDataGridDataResult { Rows = [Row(1, "https://x/a.png", "Ada")], Total = 1, PageSize = 30 }
        };
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig, columns);

        ClickExport(cut, "Current page");

        Assert.Equal(string.Join(NewLine, "\"Name\"", "\"Ada\""), Downloaded().Csv);
    }

    [Fact]
    public void Cell_wrappers_are_exported_by_their_value()
    {
        GridRig rig = new()
        {
            Respond = _ => new CloudDataGridDataResult
            {
                Rows = [Row(1, new CloudDataGridCell { Value = "Ada", CssClass = "x" }, new CloudDataGridCell { Value = 36 })],
                Total = 1,
                PageSize = 30
            }
        };
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig);

        ClickExport(cut, "Current page");

        Assert.Equal(string.Join(NewLine, "\"Name\",\"Age\"", "\"Ada\",\"36\""), Downloaded().Csv);
    }

    [Fact]
    public void Values_a_spreadsheet_would_run_as_formulas_are_neutralised_in_the_file()
    {
        GridRig rig = new()
        {
            Respond = _ => new CloudDataGridDataResult
            {
                Rows = [Row(1, "=cmd|' /C calc'!A0", "+961 70 123 456")],
                Total = 1,
                PageSize = 30
            }
        };
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig);

        ClickExport(cut, "Current page");

        string[] lines = Downloaded().Csv.Split(NewLine);
        Assert.Equal("\"'=cmd|' /C calc'!A0\",\"+961 70 123 456\"", lines[1]);
    }

    [Fact]
    public void Column_labels_in_the_header_line_are_neutralised_too()
    {
        GridRig rig = PeopleRig();
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig, Columns("=Name", "Age"));

        ClickExport(cut, "Current page");

        Assert.StartsWith("\"'=Name\",\"Age\"" + NewLine, Downloaded().Csv);
    }

    [Fact]
    public void Exporting_the_current_page_does_not_call_the_provider_again()
    {
        GridRig rig = PeopleRig();
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig);

        ClickExport(cut, "Current page");

        Assert.Single(rig.Requests);
    }

    [Fact]
    public void After_an_export_the_panel_closes_and_the_normal_actions_return()
    {
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(PeopleRig(), header: new CloudDataGridHeaderOptions { AllowRefresh = true });

        ClickExport(cut, "Current page");

        Assert.Empty(cut.FindAll(".cloudgrid-export-panel"));
        Assert.Single(cut.FindAll("button[title='Refresh']"));
        Assert.Single(cut.FindAll("button[title='More']"));
    }

    [Fact]
    public void Cancelling_the_panel_downloads_nothing()
    {
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(PeopleRig());
        OpenExportPanel(cut);

        cut.Find("._cancel").Click();

        Assert.Empty(cut.FindAll(".cloudgrid-export-panel"));
        Assert.Empty(Module.Invocations["downloadTextFile"]);
    }

    // ----- all records -------------------------------------------------------------------------------------------------------------

    [Fact]
    public void Exporting_all_records_asks_the_provider_for_everything_in_one_page()
    {
        GridRig rig = new();
        rig.Respond = request => request.PageSize >= 3 && request.Page == 1
            ? new CloudDataGridDataResult { Rows = [Row(1, "A", 1), Row(2, "B", 2), Row(3, "C", 3)], Total = 3, PageSize = request.PageSize }
            : new CloudDataGridDataResult { Rows = [Row(1, "A", 1)], Total = 3, PageSize = 1 };
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig);

        ClickExport(cut, "All records");

        CloudDataGridDataRequest request = rig.LastRequest;
        Assert.Equal(2, rig.Requests.Count);
        Assert.Equal(1, request.Page);
        Assert.Equal(3, request.PageSize);
        Assert.False(request.IsAppend);
        Assert.Equal(3, request.Total);
        (string fileName, string csv, _) = Downloaded();
        Assert.StartsWith("cloudgrid-export-all-records-", fileName);
        Assert.Equal(4, csv.Split(NewLine).Length);
    }

    [Fact]
    public void Exporting_all_records_uses_the_current_sort()
    {
        GridRig rig = PeopleRig();
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig);
        cut.FindAll(".cloudgrid-headcell")[1].Click();
        cut.WaitForAssertion(() => Assert.Equal(2, rig.Requests.Count));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".cloudgrid-row")));

        ClickExport(cut, "All records");

        Assert.Equal("Age", rig.LastRequest.Sort!.Key);
        Assert.Equal(CloudDataGridSortDirection.Ascending, rig.LastRequest.Sort!.Direction);
    }

    [Fact]
    public void Exporting_all_records_asks_for_the_biggest_page_when_no_total_is_known()
    {
        GridRig rig = new() { Respond = _ => new CloudDataGridDataResult { Rows = [Row(1, "A", 1)], Total = 0, PageSize = 30 } };
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig);

        ClickExport(cut, "All records");

        Assert.Equal(int.MaxValue, rig.LastRequest.PageSize);
    }

    [Fact]
    public void A_provider_that_fails_during_export_all_ends_quietly_without_a_download()
    {
        GridRig rig = PeopleRig();
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig);
        rig.Respond = _ => throw new InvalidOperationException("boom");

        ClickExport(cut, "All records");

        Assert.Empty(Module.Invocations["downloadTextFile"]);
        Assert.NotEmpty(cut.FindAll(".cloudgrid-row"));
    }

    [Fact]
    public void A_null_answer_during_export_all_downloads_nothing()
    {
        GridRig rig = PeopleRig();
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig);
        rig.Respond = _ => null;

        ClickExport(cut, "All records");

        Assert.Empty(Module.Invocations["downloadTextFile"]);
    }

    [Fact]
    public void Exporting_all_records_does_not_disturb_the_rows_on_screen()
    {
        GridRig rig = PeopleRig();
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig);

        ClickExport(cut, "All records");

        Assert.Equal(["Ada", "Linus \"Tux\" Torvalds", "Grace"], FirstCells(cut));
    }

    // ----- selection ----------------------------------------------------------------------------------------------------------------

    [Fact]
    public void Exporting_the_selection_includes_only_the_selected_rows_in_display_order()
    {
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(PeopleRig(), configure: p =>
        {
            p.Add(grid => grid.AllowSelection, true);
            p.Add(grid => grid.SelectedRecords, [Id(3), Id(1)]);
        });

        ClickExport(cut, "Selection");

        (string fileName, string csv, _) = Downloaded();
        Assert.StartsWith("cloudgrid-export-selection-", fileName);
        Assert.Equal(string.Join(NewLine, "\"Name\",\"Age\"", "\"Ada\",\"36\"", "\"Grace\",\"\""), csv);
    }

    [Fact]
    public void Selected_ids_that_are_not_on_the_current_page_are_not_exported()
    {
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(PeopleRig(), configure: p =>
        {
            p.Add(grid => grid.AllowSelection, true);
            p.Add(grid => grid.SelectedRecords, [Id(2), Id(99)]);
        });

        ClickExport(cut, "Selection");

        Assert.Equal(2, Downloaded().Csv.Split(NewLine).Length);
    }

    // ----- disposal ---------------------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Disposing_a_grid_that_never_exported_does_not_touch_javascript()
    {
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(PeopleRig());
        int before = JSInterop.Invocations.Count;

        await cut.Instance.DisposeAsync();

        Assert.Equal(before, JSInterop.Invocations.Count);
    }

    [Fact]
    public async Task Disposing_after_an_export_releases_the_module_without_error()
    {
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(PeopleRig());
        ClickExport(cut, "Current page");

        await cut.Instance.DisposeAsync();
    }

    [Fact]
    public void The_module_is_imported_once_and_reused_for_later_exports()
    {
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(PeopleRig());

        ClickExport(cut, "Current page");
        ClickExport(cut, "Current page");

        Assert.Equal(2, Module.Invocations["downloadTextFile"].Count);
        Assert.Single(JSInterop.Invocations.Where(call => call.Identifier == "import"));
        Assert.Equal(ModulePath, JSInterop.Invocations.Single(call => call.Identifier == "import").Arguments[0]);
    }
}
