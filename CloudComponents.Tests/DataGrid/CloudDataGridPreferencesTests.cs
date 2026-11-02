using System.Text.Json;
using AngleSharp.Dom;
using AngryMonkey.CloudComponents.DataGrid.Components;
using AngryMonkey.CloudComponents.DataGrid.Models;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace CloudComponents.Tests.DataGrid;

/// <summary>Remembering the sort and column widths per PreferenceKey in the browser's localStorage, and surviving storage failures.</summary>
public class CloudDataGridPreferencesTests : DataGridTestBase
{
    private const string GetItem = "localStorage.getItem";
    private const string SetItem = "localStorage.setItem";

    private IRenderedComponent<CloudDataGrid> RenderGrid(
        GridRig rig,
        string? preferenceKey = "orders",
        List<CloudDataGridColumn>? columns = null,
        Action<ComponentParameterCollectionBuilder<CloudDataGrid>>? configure = null)
    {
        IRenderedComponent<CloudDataGrid> cut = Render<CloudDataGrid>(parameters =>
        {
            parameters.Add(grid => grid.DataProvider, rig.Provider);
            parameters.Add(grid => grid.Columns, columns ?? Columns("Name", "Value"));
            parameters.Add(grid => grid.PreferenceKey, preferenceKey);
            configure?.Invoke(parameters);
        });

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".cloudgrid-row")));
        return cut;
    }

    private void Stored(string key, string? json) =>
        JSInterop.Setup<string?>(GetItem, key).SetResult(json);

    private static string Prefs(string? sortKey, int direction, params (string Key, double Width)[] widths) =>
        JsonSerializer.Serialize(new
        {
            SortKey = sortKey,
            Direction = direction,
            Widths = widths.ToDictionary(item => item.Key, item => item.Width)
        });

    private static string HeadStyle(IRenderedComponent<CloudDataGrid> cut, int column) => cut.FindAll(".cloudgrid-headcell")[column].GetAttribute("style")!;

    // ----- no key ------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void Without_a_preference_key_the_browser_storage_is_never_touched()
    {
        GridRig rig = new();
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig, preferenceKey: null);

        cut.FindAll(".cloudgrid-headcell")[0].Click();
        cut.WaitForAssertion(() => Assert.Equal(2, rig.Requests.Count));

        Assert.DoesNotContain(JSInterop.Invocations, call => call.Identifier.StartsWith("localStorage", StringComparison.Ordinal));
    }

    [Fact]
    public void An_empty_preference_key_counts_as_no_key()
    {
        GridRig rig = new();

        RenderGrid(rig, preferenceKey: "");

        Assert.DoesNotContain(JSInterop.Invocations, call => call.Identifier.StartsWith("localStorage", StringComparison.Ordinal));
    }

    // ----- restoring -----------------------------------------------------------------------------------------------------------------

    [Fact]
    public void The_preferences_are_read_from_a_storage_key_made_from_the_preference_key()
    {
        GridRig rig = new();

        RenderGrid(rig, "orders");

        JSRuntimeInvocation read = Assert.Single(JSInterop.Invocations[GetItem]);
        Assert.Equal("cloudgrid.view.v1:orders", read.Arguments[0]);
    }

    [Fact]
    public void Different_preference_keys_use_different_storage_entries()
    {
        RenderGrid(new GridRig(), "orders");
        RenderGrid(new GridRig(), "customers");

        Assert.Equal(
            ["cloudgrid.view.v1:orders", "cloudgrid.view.v1:customers"],
            JSInterop.Invocations[GetItem].Select(call => (string)call.Arguments[0]!));
    }

    [Fact]
    public void A_saved_sort_is_applied_to_the_very_first_request_and_shown_on_the_heading()
    {
        Stored("cloudgrid.view.v1:orders", Prefs("Value", direction: 1));
        GridRig rig = new();

        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig);

        CloudDataGridDataRequest first = rig.Requests[0];
        Assert.Equal("Value", first.Sort!.Key);
        Assert.Equal(CloudDataGridSortDirection.Descending, first.Sort.Direction);
        Assert.Equal(1, first.Sort.ColumnIndex);
        Assert.Contains("_sorted-descending", cut.FindAll(".cloudgrid-headcell")[1].ClassList);
        Assert.Single(rig.Requests);
    }

    [Fact]
    public void A_saved_sort_is_matched_by_column_key_when_the_column_has_one()
    {
        List<CloudDataGridColumn> columns = Columns("Name", "Value");
        columns[0].Key = "name_field";
        Stored("cloudgrid.view.v1:orders", Prefs("name_field", direction: 0));
        GridRig rig = new();

        RenderGrid(rig, columns: columns);

        Assert.Equal(0, rig.Requests[0].Sort!.ColumnIndex);
        Assert.Equal(CloudDataGridSortDirection.Ascending, rig.Requests[0].Sort!.Direction);
    }

    [Fact]
    public void A_saved_sort_for_an_unknown_column_is_ignored()
    {
        Stored("cloudgrid.view.v1:orders", Prefs("Gone", direction: 0));
        GridRig rig = new();

        RenderGrid(rig);

        Assert.Null(rig.Requests[0].Sort);
    }

    [Fact]
    public void A_saved_sort_for_a_column_that_is_no_longer_sortable_is_ignored()
    {
        List<CloudDataGridColumn> columns = Columns("Name", "Value");
        columns[1].Sortable = false;
        Stored("cloudgrid.view.v1:orders", Prefs("Value", direction: 0));
        GridRig rig = new();

        RenderGrid(rig, columns: columns);

        Assert.Null(rig.Requests[0].Sort);
    }

    [Fact]
    public void A_saved_sort_with_an_undefined_direction_is_ignored()
    {
        Stored("cloudgrid.view.v1:orders", Prefs("Name", direction: 7));
        GridRig rig = new();

        RenderGrid(rig);

        Assert.Null(rig.Requests[0].Sort);
    }

    [Fact]
    public void A_saved_sort_is_matched_against_the_sort_columns_when_those_are_given()
    {
        CloudDataGridColumn other = new() { Label = "Other" };
        List<CloudDataGridColumn> sortColumns = [new CloudDataGridColumn { Label = "X" }, other];
        Stored("cloudgrid.view.v1:orders", Prefs("Other", direction: 1));
        GridRig rig = new();

        RenderGrid(rig, configure: p => p.Add(grid => grid.SortColumns, sortColumns));

        CloudDataGridSort sort = rig.Requests[0].Sort!;
        Assert.Same(other, sort.Column);
        Assert.Equal(1, sort.ColumnIndex);
    }

    [Fact]
    public void With_sort_columns_a_saved_sort_for_a_display_column_that_is_not_among_them_is_ignored()
    {
        List<CloudDataGridColumn> sortColumns = [new CloudDataGridColumn { Label = "X" }];
        Stored("cloudgrid.view.v1:orders", Prefs("Name", direction: 0));
        GridRig rig = new();

        RenderGrid(rig, configure: p => p.Add(grid => grid.SortColumns, sortColumns));

        Assert.Null(rig.Requests[0].Sort);
    }

    [Fact]
    public void Saved_column_widths_are_applied_when_they_are_within_40_and_3000_pixels()
    {
        List<CloudDataGridColumn> columns = Columns("A", "B", "C", "D", "E");
        foreach (CloudDataGridColumn column in columns)
            column.MinWidth = 10;
        Stored("cloudgrid.view.v1:orders", Prefs(null, 0, ("A", 39), ("B", 40), ("C", 250), ("D", 3000), ("E", 3001)));

        IRenderedComponent<CloudDataGrid> cut = RenderGrid(new GridRig { Respond = _ => new CloudDataGridDataResult { Rows = [Row(1, "a", "b", "c", "d", "e")], Total = 1, PageSize = 30 } }, columns: columns);

        Assert.Contains("width: 100px", HeadStyle(cut, 0));
        Assert.Contains("width: 40px", HeadStyle(cut, 1));
        Assert.Contains("width: 250px", HeadStyle(cut, 2));
        Assert.Contains("width: 3000px", HeadStyle(cut, 3));
        Assert.Contains("width: 100px", HeadStyle(cut, 4));
    }

    [Fact]
    public void A_saved_width_is_never_narrower_than_the_column_minimum()
    {
        Stored("cloudgrid.view.v1:orders", Prefs(null, 0, ("Name", 45)));
        GridRig rig = new();

        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig);

        Assert.Contains("width: 60px", HeadStyle(cut, 0));
    }

    [Fact]
    public void Preferences_that_cannot_be_parsed_are_ignored_and_the_grid_still_loads()
    {
        Stored("cloudgrid.view.v1:orders", "{ this is not json");
        GridRig rig = new();

        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig);

        Assert.Null(rig.Requests[0].Sort);
        Assert.Contains("width: 100px", HeadStyle(cut, 0));
    }

    [Fact]
    public void A_stored_json_null_is_ignored()
    {
        Stored("cloudgrid.view.v1:orders", "null");
        GridRig rig = new();

        RenderGrid(rig);

        Assert.Null(rig.Requests[0].Sort);
    }

    [Fact]
    public void A_storage_read_that_throws_does_not_stop_the_grid_from_loading()
    {
        JSInterop.Setup<string?>(GetItem, _ => true).SetException(new JSException("storage is disabled"));
        GridRig rig = new();

        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig);

        Assert.Single(rig.Requests);
        Assert.Equal(3, cut.FindAll(".cloudgrid-row").Count);
    }

    // ----- saving -------------------------------------------------------------------------------------------------------------------

    private static JsonElement SavedPreferences(JSRuntimeInvocation call) => JsonDocument.Parse((string)call.Arguments[1]!).RootElement;

    [Fact]
    public void Sorting_saves_the_sort_under_the_storage_key()
    {
        GridRig rig = new();
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig);

        cut.FindAll(".cloudgrid-headcell")[1].Click();

        cut.WaitForAssertion(() => Assert.NotEmpty(JSInterop.Invocations[SetItem]));
        JSRuntimeInvocation save = JSInterop.Invocations[SetItem][^1];
        Assert.Equal("cloudgrid.view.v1:orders", save.Arguments[0]);
        JsonElement saved = SavedPreferences(save);
        Assert.Equal("Value", saved.GetProperty("SortKey").GetString());
        Assert.Equal((int)CloudDataGridSortDirection.Ascending, saved.GetProperty("Direction").GetInt32());
    }

    [Fact]
    public void Sorting_descending_saves_the_direction()
    {
        GridRig rig = new();
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig);

        cut.FindAll(".cloudgrid-headcell")[1].Click();
        cut.FindAll(".cloudgrid-headcell")[1].Click();

        cut.WaitForAssertion(() => Assert.Equal(2, JSInterop.Invocations[SetItem].Count));
        Assert.Equal((int)CloudDataGridSortDirection.Descending, SavedPreferences(JSInterop.Invocations[SetItem][^1]).GetProperty("Direction").GetInt32());
    }

    [Fact]
    public void Finishing_a_column_resize_saves_the_widths()
    {
        GridRig rig = new();
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig);

        cut.FindAll(".cloudgrid-resizer")[0].PointerDown(new PointerEventArgs { ClientX = 0 });
        cut.Find(".cloudgrid-shield").PointerMove(new PointerEventArgs { ClientX = 80 });
        cut.Find(".cloudgrid-shield").PointerUp(new PointerEventArgs());

        cut.WaitForAssertion(() => Assert.NotEmpty(JSInterop.Invocations[SetItem]));
        JsonElement saved = SavedPreferences(JSInterop.Invocations[SetItem][^1]);
        Assert.Equal(180, saved.GetProperty("Widths").GetProperty("Name").GetDouble());
        Assert.Contains("width: 180px", HeadStyle(cut, 0));
    }

    [Fact]
    public void Resizing_after_sorting_saves_both_the_sort_and_the_widths()
    {
        GridRig rig = new();
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig);
        cut.FindAll(".cloudgrid-headcell")[1].Click();
        cut.WaitForAssertion(() => Assert.Equal(2, rig.Requests.Count));

        cut.FindAll(".cloudgrid-resizer")[0].PointerDown(new PointerEventArgs { ClientX = 0 });
        cut.Find(".cloudgrid-shield").PointerMove(new PointerEventArgs { ClientX = 20 });
        cut.Find(".cloudgrid-shield").PointerUp(new PointerEventArgs());

        cut.WaitForAssertion(() => Assert.Equal(2, JSInterop.Invocations[SetItem].Count));
        JsonElement saved = SavedPreferences(JSInterop.Invocations[SetItem][^1]);
        Assert.Equal("Value", saved.GetProperty("SortKey").GetString());
        Assert.Equal(120, saved.GetProperty("Widths").GetProperty("Name").GetDouble());
    }

    [Fact]
    public void A_storage_write_that_throws_does_not_stop_sorting()
    {
        JSInterop.SetupVoid(SetItem, _ => true).SetException(new JSException("quota exceeded"));
        GridRig rig = new();
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(rig);

        cut.FindAll(".cloudgrid-headcell")[1].Click();

        cut.WaitForAssertion(() => Assert.Equal(2, rig.Requests.Count));
        Assert.Equal("Value", rig.LastRequest.Sort!.Key);
        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll(".cloudgrid-row").Count));
    }

    [Fact]
    public void Saved_preferences_round_trip_into_a_new_grid()
    {
        GridRig first = new();
        IRenderedComponent<CloudDataGrid> cut = RenderGrid(first);
        cut.FindAll(".cloudgrid-headcell")[1].Click();
        cut.WaitForAssertion(() => Assert.Equal(2, first.Requests.Count));
        cut.FindAll(".cloudgrid-headcell")[1].Click();
        cut.WaitForAssertion(() => Assert.Equal(2, JSInterop.Invocations[SetItem].Count));
        string json = (string)JSInterop.Invocations[SetItem][^1].Arguments[1]!;
        Stored("cloudgrid.view.v1:orders", json);

        GridRig second = new();
        RenderGrid(second);

        Assert.Equal("Value", second.Requests[0].Sort!.Key);
        Assert.Equal(CloudDataGridSortDirection.Descending, second.Requests[0].Sort!.Direction);
    }
}
