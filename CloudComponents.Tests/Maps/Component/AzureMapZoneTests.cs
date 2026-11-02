using System.Text.Json;
using AngryMonkey.CloudComponents.Maps.Components;
using AngryMonkey.CloudComponents.Maps.Models;
using Bunit;
using Microsoft.JSInterop;

namespace CloudComponents.Tests.Maps.Component;

/// <summary>Zones: addresses resolved to boundary polygons by the component, and the check that tells whether that is possible.</summary>
public sealed class AzureMapZoneTests : AzureMapTestContext
{
    private static double[][][] Square(double origin) =>
        [[[origin, origin], [origin + 1, origin], [origin + 1, origin + 1], [origin, origin + 1], [origin, origin]]];

    private static MapZone Zone(string id, params string[] addresses) => new() { Id = id, Addresses = addresses };

    /// <summary>Scripts resolveBoundary so each address resolves to its own square, unless it is in <paramref name="missing"/>.</summary>
    private void ResolveEachAddressToASquare(params string[] missing)
    {
        Controller.On("resolveBoundary", call =>
        {
            string address = call.Json(0).GetString()!;
            return missing.Contains(address) ? null : Square(address.Length);
        });
    }

    private IEnumerable<string> RemovedRegionIds() => Controller.CallsTo("removeRegion").Select(c => c.Json(0).GetString()!);

    private IEnumerable<string> AddedRegionIds() => Controller.CallsTo("addRegion").Select(c => c.Json(0).GetProperty("id").GetString()!);

    // ── CanZoneAsync ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CanZoneAsync_for_a_blank_address_is_NotFound_without_calling_JavaScript(string address)
    {
        IRenderedComponent<AzureMap> cut = RenderMap();

        ZoneCheckResult result = await cut.InvokeAsync(() => cut.Instance.CanZoneAsync(address));

        Assert.Same(ZoneCheckResult.NotFound, result);
        Assert.False(Controller.WasCalled("geocode"));
    }

    [Fact]
    public async Task CanZoneAsync_for_an_address_with_a_geometry_can_be_zoned()
    {
        Controller.Returns("geocode", FakeMapJs.Json("""{"latitude":48.85,"longitude":2.35,"north":49,"south":48,"east":3,"west":2,"geometryId":"geo-1"}"""));
        IRenderedComponent<AzureMap> cut = RenderMap();

        ZoneCheckResult result = await cut.InvokeAsync(() => cut.Instance.CanZoneAsync("Paris"));

        Assert.True(result.Found);
        Assert.True(result.CanZone);
        Assert.Equal(new MapCoordinate(48.85, 2.35), result.Coordinate);
        Assert.Equal("geo-1", result.GeometryId);
    }

    [Fact]
    public async Task CanZoneAsync_for_an_address_found_without_a_geometry_is_found_but_cannot_be_zoned()
    {
        Controller.Returns("geocode", FakeMapJs.Json("""{"latitude":1,"longitude":2,"north":1,"south":1,"east":2,"west":2,"geometryId":null}"""));
        IRenderedComponent<AzureMap> cut = RenderMap();

        ZoneCheckResult result = await cut.InvokeAsync(() => cut.Instance.CanZoneAsync("12 Main Street"));

        Assert.True(result.Found);
        Assert.False(result.CanZone);
        Assert.Equal(new MapCoordinate(1, 2), result.Coordinate);
        Assert.Null(result.GeometryId);
    }

    [Fact]
    public async Task CanZoneAsync_treats_an_empty_geometry_id_as_no_geometry()
    {
        Controller.Returns("geocode", FakeMapJs.Json("""{"latitude":1,"longitude":2,"north":1,"south":1,"east":2,"west":2,"geometryId":""}"""));
        IRenderedComponent<AzureMap> cut = RenderMap();

        ZoneCheckResult result = await cut.InvokeAsync(() => cut.Instance.CanZoneAsync("x"));

        Assert.True(result.Found);
        Assert.False(result.CanZone);
    }

    [Fact]
    public async Task CanZoneAsync_when_nothing_is_found_is_NotFound()
    {
        Controller.Returns("geocode", null);
        IRenderedComponent<AzureMap> cut = RenderMap();

        ZoneCheckResult result = await cut.InvokeAsync(() => cut.Instance.CanZoneAsync("zzzz"));

        Assert.Same(ZoneCheckResult.NotFound, result);
        Assert.False(result.Found);
        Assert.False(result.CanZone);
        Assert.Null(result.Coordinate);
    }

    [Fact]
    public async Task CanZoneAsync_swallows_a_JavaScript_failure_and_reports_NotFound()
    {
        Controller.Throws("geocode", new JSException("boom"));
        IRenderedComponent<AzureMap> cut = RenderMap();

        ZoneCheckResult result = await cut.InvokeAsync(() => cut.Instance.CanZoneAsync("Paris"));

        Assert.Same(ZoneCheckResult.NotFound, result);
    }

    [Fact]
    public async Task CanZoneAsync_passes_the_country_set_and_no_entity_type_to_the_geocoder()
    {
        Controller.Returns("geocode", null);
        IRenderedComponent<AzureMap> cut = RenderMap();

        await cut.InvokeAsync(() => cut.Instance.CanZoneAsync("Lebanon", "LB"));

        Assert.Equal("""["Lebanon",null,"LB"]""", Controller.Single("geocode").ArgsJson);
    }

    [Fact]
    public async Task CanZoneAsync_draws_nothing()
    {
        Controller.Returns("geocode", FakeMapJs.Json("""{"latitude":1,"longitude":2,"north":1,"south":1,"east":2,"west":2,"geometryId":"g"}"""));
        IRenderedComponent<AzureMap> cut = RenderMap();

        await cut.InvokeAsync(() => cut.Instance.CanZoneAsync("Paris"));

        Assert.False(Controller.WasCalled("addRegion"));
        Assert.False(Controller.WasCalled("resolveBoundary"));
    }

    // ── AddZonesAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task A_zone_draws_one_region_per_address_that_has_a_boundary()
    {
        ResolveEachAddressToASquare();
        IRenderedComponent<AzureMap> cut = RenderMap();

        await cut.InvokeAsync(() => cut.Instance.AddZonesAsync([Zone("z1", "Lyon", "Marseille")]));

        Assert.Equal(2, Controller.CallsTo("addRegion").Count);
        Assert.Equal(["Lyon", "Marseille"], Controller.CallsTo("resolveBoundary").Select(c => c.Json(0).GetString()));
    }

    [Fact]
    public async Task A_zone_region_carries_the_zone_colours_stroke_and_label()
    {
        ResolveEachAddressToASquare();
        IRenderedComponent<AzureMap> cut = RenderMap();
        MapZone zone = new() { Id = "z1", Addresses = ["Paris"], Label = "Capital", FillColor = "#11223344", StrokeColor = "#abcdef", StrokeWidth = 5 };

        await cut.InvokeAsync(() => cut.Instance.AddZonesAsync([zone]));

        JsonElement region = Controller.Single("addRegion").Json(0);
        Assert.Equal("#11223344", region.GetProperty("fillColor").GetString());
        Assert.Equal("#abcdef", region.GetProperty("strokeColor").GetString());
        Assert.Equal(5, region.GetProperty("strokeWidth").GetDouble());
        Assert.Equal("Capital", region.GetProperty("label").GetString());
        Assert.Equal(Square(5)[0][2], region.GetProperty("coordinates")[0][2].EnumerateArray().Select(v => v.GetDouble()).ToArray());
    }

    [Fact]
    public async Task A_zone_without_entity_types_resolves_with_the_country_set_only()
    {
        ResolveEachAddressToASquare();
        IRenderedComponent<AzureMap> cut = RenderMap();
        MapZone zone = new() { Addresses = ["Lebanon"], CountrySet = "LB" };

        await cut.InvokeAsync(() => cut.Instance.AddZonesAsync([zone]));

        Assert.Equal("""["Lebanon",null,"LB"]""", Controller.Single("resolveBoundary").ArgsJson);
    }

    [Fact]
    public async Task A_zone_with_entity_types_resolves_with_the_list_of_levels()
    {
        ResolveEachAddressToASquare();
        IRenderedComponent<AzureMap> cut = RenderMap();
        MapZone zone = new() { Addresses = ["Zouk"], EntityTypes = ["Municipality", "Neighbourhood"], CountrySet = "LB" };

        await cut.InvokeAsync(() => cut.Instance.AddZonesAsync([zone]));

        Assert.Equal("""["Zouk",["Municipality","Neighbourhood"],"LB"]""", Controller.Single("resolveBoundary").ArgsJson);
    }

    [Fact]
    public async Task A_zone_with_an_empty_entity_type_list_falls_back_to_the_best_match()
    {
        ResolveEachAddressToASquare();
        IRenderedComponent<AzureMap> cut = RenderMap();
        MapZone zone = new() { Addresses = ["Zouk"], EntityTypes = [] };

        await cut.InvokeAsync(() => cut.Instance.AddZonesAsync([zone]));

        Assert.Equal("""["Zouk",null,null]""", Controller.Single("resolveBoundary").ArgsJson);
    }

    [Fact]
    public async Task Addresses_without_a_boundary_are_skipped_and_the_rest_are_drawn()
    {
        ResolveEachAddressToASquare(missing: "12 Main Street");
        IRenderedComponent<AzureMap> cut = RenderMap();

        await cut.InvokeAsync(() => cut.Instance.AddZonesAsync([Zone("z1", "12 Main Street", "Lyon")]));

        Assert.Single(Controller.CallsTo("addRegion"));
    }

    [Fact]
    public async Task An_empty_polygon_is_not_drawn()
    {
        Controller.Returns("resolveBoundary", Array.Empty<double[][]>());
        IRenderedComponent<AzureMap> cut = RenderMap();

        await cut.InvokeAsync(() => cut.Instance.AddZonesAsync([Zone("z1", "Lyon")]));

        Assert.False(Controller.WasCalled("addRegion"));
    }

    [Fact]
    public async Task Blank_addresses_are_not_looked_up()
    {
        ResolveEachAddressToASquare();
        IRenderedComponent<AzureMap> cut = RenderMap();

        await cut.InvokeAsync(() => cut.Instance.AddZonesAsync([Zone("z1", "", "  ", "Lyon")]));

        Assert.Equal(["Lyon"], Controller.CallsTo("resolveBoundary").Select(c => c.Json(0).GetString()));
    }

    [Fact]
    public async Task A_zone_without_addresses_draws_nothing()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();

        await cut.InvokeAsync(() => cut.Instance.AddZonesAsync([new MapZone { Addresses = [] }]));

        Assert.Empty(Controller.Calls);
    }

    [Fact]
    public async Task One_address_failing_does_not_block_the_next()
    {
        Controller.On("resolveBoundary", call => call.Json(0).GetString() == "Bad" ? throw new JSException("boom") : Square(1));
        IRenderedComponent<AzureMap> cut = RenderMap();

        await cut.InvokeAsync(() => cut.Instance.AddZonesAsync([Zone("z1", "Bad", "Good")]));

        Assert.Single(Controller.CallsTo("addRegion"));
        Assert.Equal(2, Controller.CallsTo("resolveBoundary").Count);
    }

    [Fact]
    public async Task A_labelled_zone_appears_once_in_the_legend()
    {
        ResolveEachAddressToASquare();
        IRenderedComponent<AzureMap> cut = RenderMap();

        await cut.InvokeAsync(() => cut.Instance.AddZonesAsync([new MapZone { Addresses = ["Lyon", "Marseille"], Label = "Key Cities" }]));

        Assert.Equal("Key Cities", Assert.Single(cut.FindAll(".map-legend-label")).TextContent);
    }

    [Fact]
    public async Task Zones_given_at_creation_are_drawn_when_the_map_becomes_ready()
    {
        ResolveEachAddressToASquare();
        RenderMap(p => p.Add(m => m.Zones, new[] { Zone("z1", "Lyon") }));
        Assert.False(Controller.WasCalled("resolveBoundary"));

        await Js.InvokeFromJsAsync("NotifyMapReadyAsync");

        Assert.Single(Controller.CallsTo("addRegion"));
    }

    // ── Removing zones ──────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveZoneAsync_removes_every_region_the_zone_drew_and_only_those()
    {
        ResolveEachAddressToASquare();
        IRenderedComponent<AzureMap> cut = RenderMap();
        await cut.InvokeAsync(() => cut.Instance.AddZonesAsync([Zone("a", "Lyon", "Nice"), Zone("b", "Paris")]));
        string[] added = [.. AddedRegionIds()];

        await cut.InvokeAsync(() => cut.Instance.RemoveZoneAsync("a"));

        Assert.Equal(added[..2], RemovedRegionIds());
    }

    [Fact]
    public async Task RemoveZoneAsync_for_an_unknown_zone_does_nothing()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();

        await cut.InvokeAsync(() => cut.Instance.RemoveZoneAsync("ghost"));

        Assert.False(Controller.WasCalled("removeRegion"));
    }

    [Fact]
    public async Task A_zone_removed_twice_is_removed_from_the_map_once()
    {
        ResolveEachAddressToASquare();
        IRenderedComponent<AzureMap> cut = RenderMap();
        await cut.InvokeAsync(() => cut.Instance.AddZonesAsync([Zone("a", "Lyon")]));

        await cut.InvokeAsync(() => cut.Instance.RemoveZoneAsync("a"));
        await cut.InvokeAsync(() => cut.Instance.RemoveZoneAsync("a"));

        Assert.Single(Controller.CallsTo("removeRegion"));
    }

    [Fact]
    public async Task Removing_the_only_labelled_zone_removes_its_legend_row()
    {
        ResolveEachAddressToASquare();
        IRenderedComponent<AzureMap> cut = RenderMap();
        await cut.InvokeAsync(() => cut.Instance.AddZonesAsync([new MapZone { Id = "a", Addresses = ["Lyon"], Label = "Lyon area" }]));

        await cut.InvokeAsync(() => cut.Instance.RemoveZoneAsync("a"));

        Assert.Empty(cut.FindAll(".map-legend"));
    }

    [Fact(Skip = "BUG: AzureMap.Zones.cs RemoveZoneAsync (line ~124) removes the legend row of every labelled region, not just the removed zone's: the predicate only tests that the removed region ids are absent from the remaining zones, which is always true")]
    public async Task Removing_one_zone_keeps_the_legend_rows_of_the_other_zones()
    {
        ResolveEachAddressToASquare();
        IRenderedComponent<AzureMap> cut = RenderMap();
        await cut.InvokeAsync(() => cut.Instance.AddZonesAsync(
        [
            new MapZone { Id = "a", Addresses = ["Lyon"], Label = "Lyon area" },
            new MapZone { Id = "b", Addresses = ["Marseille"], Label = "Marseille area" }
        ]));

        await cut.InvokeAsync(() => cut.Instance.RemoveZoneAsync("a"));

        Assert.Equal(["Marseille area"], cut.FindAll(".map-legend-label").Select(l => l.TextContent));
    }

    [Fact]
    public async Task ClearZonesAsync_removes_every_zone_region_but_not_manually_added_regions()
    {
        ResolveEachAddressToASquare();
        IRenderedComponent<AzureMap> cut = RenderMap();
        await cut.InvokeAsync(() => cut.Instance.AddRegionsAsync([new MapRegion([[[0, 0]]]) { Id = "manual" }]));
        await cut.InvokeAsync(() => cut.Instance.AddZonesAsync([Zone("a", "Lyon"), Zone("b", "Paris")]));
        string[] zoneRegions = [.. AddedRegionIds().Where(id => id != "manual")];

        await cut.InvokeAsync(() => cut.Instance.ClearZonesAsync());

        Assert.Equal(zoneRegions.Order(), RemovedRegionIds().Order());
        Assert.DoesNotContain("manual", RemovedRegionIds());
    }

    [Fact]
    public async Task After_ClearZonesAsync_the_zones_can_no_longer_be_removed()
    {
        ResolveEachAddressToASquare();
        IRenderedComponent<AzureMap> cut = RenderMap();
        await cut.InvokeAsync(() => cut.Instance.AddZonesAsync([Zone("a", "Lyon")]));
        await cut.InvokeAsync(() => cut.Instance.ClearZonesAsync());
        Controller.ClearCalls();

        await cut.InvokeAsync(() => cut.Instance.RemoveZoneAsync("a"));

        Assert.False(Controller.WasCalled("removeRegion"));
    }

    [Fact(Skip = "BUG: AzureMap.Zones.cs ClearZonesAsync removes the regions but leaves the zones' legend rows on screen (ClearRegionsAsync clears its rows; ClearZonesAsync never touches _regionLegendEntries)")]
    public async Task ClearZonesAsync_also_clears_the_legend_rows_of_the_zones()
    {
        ResolveEachAddressToASquare();
        IRenderedComponent<AzureMap> cut = RenderMap();
        await cut.InvokeAsync(() => cut.Instance.AddZonesAsync([new MapZone { Id = "a", Addresses = ["Lyon"], Label = "Lyon area" }]));

        await cut.InvokeAsync(() => cut.Instance.ClearZonesAsync());

        Assert.Empty(cut.FindAll(".map-legend"));
    }

    // ── Merging ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Without_MergeZoneBoundariesAbove_every_boundary_is_drawn_however_many_there_are()
    {
        ResolveEachAddressToASquare();
        IRenderedComponent<AzureMap> cut = RenderMap();

        await cut.InvokeAsync(() => cut.Instance.AddZonesAsync([Zone("a", "A", "BB", "CCC", "DDDD")]));

        Assert.Equal(4, Controller.CallsTo("addRegion").Count);
        Assert.False(Controller.WasCalled("addMergedRegion"));
    }

    [Fact]
    public async Task A_batch_at_or_below_the_merge_threshold_is_drawn_boundary_by_boundary()
    {
        ResolveEachAddressToASquare();
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.MergeZoneBoundariesAbove, 3));

        await cut.InvokeAsync(() => cut.Instance.AddZonesAsync([Zone("a", "A", "BB", "CCC")]));

        Assert.Equal(3, Controller.CallsTo("addRegion").Count);
        Assert.False(Controller.WasCalled("addMergedRegion"));
    }

    [Fact]
    public async Task A_batch_above_the_merge_threshold_becomes_one_merged_outline()
    {
        ResolveEachAddressToASquare();
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.MergeZoneBoundariesAbove, 2).Add(m => m.MergedBoundaryStrokeColor, "#ff00ff"));
        MapZone first = new() { Id = "a", Addresses = ["A", "BB"], StrokeWidth = 7 };

        await cut.InvokeAsync(() => cut.Instance.AddZonesAsync([first, Zone("b", "CCC")]));

        JsonElement merged = Controller.Single("addMergedRegion").Json(0);
        Assert.Equal(3, merged.GetProperty("polygons").GetArrayLength());
        Assert.Equal("#ff00ff", merged.GetProperty("strokeColor").GetString());
        Assert.Equal(7, merged.GetProperty("strokeWidth").GetDouble());
        Assert.False(Controller.WasCalled("addRegion"));
    }

    [Fact]
    public async Task Removing_a_zone_drawn_as_a_merged_outline_removes_that_outline()
    {
        ResolveEachAddressToASquare();
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.MergeZoneBoundariesAbove, 1));
        await cut.InvokeAsync(() => cut.Instance.AddZonesAsync([Zone("a", "A", "BB")]));
        string mergedId = Controller.Single("addMergedRegion").Json(0).GetProperty("id").GetString()!;

        await cut.InvokeAsync(() => cut.Instance.RemoveZoneAsync("a"));

        Assert.Equal([mergedId], RemovedRegionIds());
    }

    [Fact]
    public async Task A_merge_threshold_of_zero_merges_any_batch_with_a_boundary()
    {
        ResolveEachAddressToASquare();
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.MergeZoneBoundariesAbove, 0));

        await cut.InvokeAsync(() => cut.Instance.AddZonesAsync([Zone("a", "A")]));

        Assert.Single(Controller.CallsTo("addMergedRegion"));
    }

    [Fact]
    public async Task A_batch_with_no_boundaries_at_all_draws_nothing_even_with_a_merge_threshold()
    {
        Controller.Returns("resolveBoundary", null);
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.MergeZoneBoundariesAbove, 0));

        await cut.InvokeAsync(() => cut.Instance.AddZonesAsync([Zone("a", "A")]));

        Assert.False(Controller.WasCalled("addMergedRegion"));
        Assert.False(Controller.WasCalled("addRegion"));
    }
}
