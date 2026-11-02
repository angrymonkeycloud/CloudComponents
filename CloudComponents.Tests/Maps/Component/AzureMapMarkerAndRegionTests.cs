using System.Text.Json;
using AngryMonkey.CloudComponents.Maps.Components;
using AngryMonkey.CloudComponents.Maps.Models;
using Bunit;

namespace CloudComponents.Tests.Maps.Component;

/// <summary>The imperative marker and region API of the map and what it sends JavaScript.</summary>
public sealed class AzureMapMarkerAndRegionTests : AzureMapTestContext
{
    // ── Markers ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddMarkerAsync_sends_every_marker_field_under_its_camelCase_name()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();
        MapMarker marker = new(33.9, 35.5, "Label", "#00ff00")
        {
            Id = "m1",
            Title = "T",
            City = "Jounieh",
            District = "Keserwan",
            Subdivision = "Mount Lebanon",
            Country = "Lebanon",
            ImageUrl = "https://img/x.png",
            Area = "120 sqm",
            Price = "$150,000",
            DetailsUrl = "/p/1",
            DetailsLabel = "Open"
        };

        await cut.InvokeAsync(() => cut.Instance.AddMarkerAsync(marker));

        JsonElement sent = Controller.Single("addMarker").Json(0);
        Assert.Equal(
            """{"latitude":33.9,"longitude":35.5,"label":"Label","color":"#00ff00","id":"m1","title":"T","city":"Jounieh","district":"Keserwan","subdivision":"Mount Lebanon","country":"Lebanon","imageUrl":"https://img/x.png","area":"120 sqm","price":"$150,000","detailsUrl":"/p/1","detailsLabel":"Open"}""",
            sent.GetRawText());
    }

    [Fact]
    public async Task AddMarkerAsync_sends_the_default_colour_and_nulls_for_a_bare_marker()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();

        await cut.InvokeAsync(() => cut.Instance.AddMarkerAsync(new MapMarker(1, 2) { Id = "m" }));

        JsonElement sent = Controller.Single("addMarker").Json(0);
        Assert.Equal("#e81123", sent.GetProperty("color").GetString());
        Assert.Equal(JsonValueKind.Null, sent.GetProperty("label").ValueKind);
        Assert.Equal(JsonValueKind.Null, sent.GetProperty("title").ValueKind);
    }

    [Fact]
    public async Task AddMarkerAsync_registers_the_marker_in_CurrentMarkers_by_id()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();
        MapMarker marker = new(1, 2) { Id = "m1" };

        await cut.InvokeAsync(() => cut.Instance.AddMarkerAsync(marker));

        Assert.Same(marker, cut.Instance.CurrentMarkers["m1"]);
    }

    [Fact]
    public async Task Adding_a_second_marker_with_the_same_id_replaces_the_first_and_is_sent_again()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();
        MapMarker first = new(1, 2) { Id = "same" };
        MapMarker second = new(3, 4) { Id = "same" };

        await cut.InvokeAsync(() => cut.Instance.AddMarkerAsync(first));
        await cut.InvokeAsync(() => cut.Instance.AddMarkerAsync(second));

        Assert.Same(second, Assert.Single(cut.Instance.CurrentMarkers).Value);
        Assert.Equal(2, Controller.CallsTo("addMarker").Count);
    }

    [Fact]
    public async Task AddMarkersAsync_adds_each_marker_in_order()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();

        await cut.InvokeAsync(() => cut.Instance.AddMarkersAsync([new MapMarker(1, 1) { Id = "a" }, new MapMarker(2, 2) { Id = "b" }, new MapMarker(3, 3) { Id = "c" }]));

        Assert.Equal(["a", "b", "c"], Controller.CallsTo("addMarker").Select(c => c.Json(0).GetProperty("id").GetString()));
        Assert.Equal(3, cut.Instance.CurrentMarkers.Count);
    }

    [Fact]
    public async Task RemoveMarkerAsync_removes_a_known_marker_from_the_map_and_from_CurrentMarkers()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();
        await cut.InvokeAsync(() => cut.Instance.AddMarkerAsync(new MapMarker(1, 1) { Id = "a" }));

        await cut.InvokeAsync(() => cut.Instance.RemoveMarkerAsync("a"));

        Assert.Equal("""["a"]""", Controller.Single("removeMarker").ArgsJson);
        Assert.Empty(cut.Instance.CurrentMarkers);
    }

    [Fact]
    public async Task RemoveMarkerAsync_for_an_unknown_id_does_not_call_JavaScript()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();

        await cut.InvokeAsync(() => cut.Instance.RemoveMarkerAsync("ghost"));

        Assert.False(Controller.WasCalled("removeMarker"));
    }

    [Fact]
    public async Task RemoveMarkerAsync_does_not_raise_OnMarkerRemoved()
    {
        List<MapMarker> removed = [];
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.OnMarkerRemoved, (MapMarker m) => removed.Add(m)));
        await cut.InvokeAsync(() => cut.Instance.AddMarkerAsync(new MapMarker(1, 1) { Id = "a" }));

        await cut.InvokeAsync(() => cut.Instance.RemoveMarkerAsync("a"));

        Assert.Empty(removed);
    }

    [Fact]
    public async Task ClearMarkersAsync_empties_CurrentMarkers_and_clears_the_map()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();
        await cut.InvokeAsync(() => cut.Instance.AddMarkersAsync([new MapMarker(1, 1), new MapMarker(2, 2)]));

        await cut.InvokeAsync(() => cut.Instance.ClearMarkersAsync());

        Assert.Equal("[]", Controller.Single("clearMarkers").ArgsJson);
        Assert.Empty(cut.Instance.CurrentMarkers);
    }

    [Fact]
    public async Task A_marker_cleared_from_the_map_can_no_longer_be_clicked()
    {
        List<MapMarker> clicked = [];
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.OnMarkerClick, (MapMarker m) => clicked.Add(m)));
        await cut.InvokeAsync(() => cut.Instance.AddMarkerAsync(new MapMarker(1, 1) { Id = "a" }));
        await cut.InvokeAsync(() => cut.Instance.ClearMarkersAsync());

        await Js.InvokeFromJsAsync("NotifyMarkerClickAsync", "a");

        Assert.Empty(clicked);
    }

    [Fact]
    public async Task Marker_calls_before_the_map_exists_fail_clearly_or_do_nothing()
    {
        JsGate gate = Js.Module.Hold("createMap");
        IRenderedComponent<AzureMap> cut = RenderMap();

        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Instance.AddMarkerAsync(new MapMarker(1, 1)));
        await cut.Instance.RemoveMarkerAsync("a");
        await cut.Instance.ClearMarkersAsync();
        await cut.Instance.ClearRegionsAsync();
        await cut.Instance.ClearRoutesAsync();

        Assert.Equal("Maps has not been initialized yet.", error.Message);
        Assert.Empty(Controller.Calls);
        gate.Release(Controller);
    }

    // ── Regions ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddRegionsAsync_sends_the_region_with_its_colours_ring_coordinates_and_id()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();
        MapRegion region = new([[[35.0, 33.0], [36.0, 33.0], [36.0, 34.0], [35.0, 33.0]]], "red", "blue", 3) { Id = "r1", Label = "Zone" };

        await cut.InvokeAsync(() => cut.Instance.AddRegionsAsync([region]));

        Assert.Equal(
            """{"coordinates":[[[35,33],[36,33],[36,34],[35,33]]],"fillColor":"red","strokeColor":"blue","strokeWidth":3,"id":"r1","label":"Zone"}""",
            Controller.Single("addRegion").Json(0).GetRawText());
    }

    [Fact]
    public async Task AddRegionsAsync_with_two_regions_sends_two_addRegion_calls_in_order()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();

        await cut.InvokeAsync(() => cut.Instance.AddRegionsAsync([new MapRegion([[[0, 0]]]) { Id = "a" }, new MapRegion([[[1, 1]]]) { Id = "b" }]));

        Assert.Equal(["a", "b"], Controller.CallsTo("addRegion").Select(c => c.Json(0).GetProperty("id").GetString()));
    }

    [Fact]
    public async Task A_labelled_region_adds_a_legend_row_with_its_fill_and_stroke()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();

        await cut.InvokeAsync(() => cut.Instance.AddRegionsAsync([new MapRegion([[[0, 0]]], "#111", "#222") { Label = "North" }]));

        string style = cut.Find(".map-legend-swatch").GetAttribute("style")!;
        Assert.Contains("background: #111", style);
        Assert.Contains("border-color: #222", style);
    }

    [Fact]
    public async Task ClearRegionsAsync_clears_the_map_and_the_legend()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();
        await cut.InvokeAsync(() => cut.Instance.AddRegionsAsync([new MapRegion([[[0, 0]]]) { Label = "North" }]));

        await cut.InvokeAsync(() => cut.Instance.ClearRegionsAsync());

        Assert.Equal("[]", Controller.Single("clearRegions").ArgsJson);
        Assert.Empty(cut.FindAll(".map-legend"));
    }

    [Fact]
    public async Task AddMergedRegionAsync_sends_the_polygons_with_an_outline_and_returns_the_generated_id()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();
        double[][][] one = [[[0, 0], [0, 1], [1, 1]]];
        double[][][] two = [[[5, 5], [5, 6], [6, 6]]];

        string id = await cut.InvokeAsync(() => cut.Instance.AddMergedRegionAsync([one, two]));

        JsonElement sent = Controller.Single("addMergedRegion").Json(0);
        Assert.Equal(id, sent.GetProperty("id").GetString());
        Assert.Equal(32, id.Length);
        Assert.Equal(2, sent.GetProperty("polygons").GetArrayLength());
        Assert.Equal("#0078d4", sent.GetProperty("strokeColor").GetString());
        Assert.Equal(2, sent.GetProperty("strokeWidth").GetDouble());
        Assert.Equal(JsonValueKind.Null, sent.GetProperty("label").ValueKind);
    }

    [Fact]
    public async Task AddMergedRegionAsync_uses_the_id_label_and_stroke_it_is_given_and_adds_a_transparent_legend_swatch()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();

        string id = await cut.InvokeAsync(() => cut.Instance.AddMergedRegionAsync([[[[0.0, 0.0]]]], "#ff0000", 4, "Service area", "merged-1"));

        JsonElement sent = Controller.Single("addMergedRegion").Json(0);
        Assert.Equal("merged-1", id);
        Assert.Equal("merged-1", sent.GetProperty("id").GetString());
        Assert.Equal("#ff0000", sent.GetProperty("strokeColor").GetString());
        Assert.Equal(4, sent.GetProperty("strokeWidth").GetDouble());
        Assert.Equal("Service area", sent.GetProperty("label").GetString());
        string swatch = cut.Find(".map-legend-swatch").GetAttribute("style")!;
        Assert.Contains("background: transparent", swatch);
        Assert.Contains("border-color: #ff0000", swatch);
    }

    [Fact]
    public async Task AddMergedRegionAsync_without_a_label_adds_no_legend_row()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();

        await cut.InvokeAsync(() => cut.Instance.AddMergedRegionAsync([[[[0.0, 0.0]]]]));

        Assert.Empty(cut.FindAll(".map-legend"));
    }

    [Fact]
    public async Task Merged_regions_without_an_explicit_id_get_different_ids()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();

        string first = await cut.InvokeAsync(() => cut.Instance.AddMergedRegionAsync([[[[0.0, 0.0]]]]));
        string second = await cut.InvokeAsync(() => cut.Instance.AddMergedRegionAsync([[[[0.0, 0.0]]]]));

        Assert.NotEqual(first, second);
    }
}
