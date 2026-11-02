using AngryMonkey.CloudComponents.Maps.Components;
using AngryMonkey.CloudComponents.Maps.Models;
using Bunit;

namespace CloudComponents.Tests.Maps.Component;

/// <summary>The public methods that forward to a controller function, and how the answer is turned into the C# records.</summary>
public sealed class AzureMapJsApiTests : AzureMapTestContext
{
    private IRenderedComponent<AzureMap> Cut => _cut ??= RenderMap();
    private IRenderedComponent<AzureMap>? _cut;

    // ── Camera ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task SetCenterAsync_sends_latitude_longitude_and_zoom()
    {
        await Cut.InvokeAsync(() => Cut.Instance.SetCenterAsync(33.9, 35.5, 11));

        Assert.Equal("[33.9,35.5,11]", Controller.Single("setCenter").ArgsJson);
    }

    [Fact]
    public async Task SetCenterAsync_without_a_zoom_sends_null_so_the_zoom_is_kept()
    {
        await Cut.InvokeAsync(() => Cut.Instance.SetCenterAsync(33.9, 35.5));

        Assert.Equal("[33.9,35.5,null]", Controller.Single("setCenter").ArgsJson);
    }

    [Fact]
    public async Task SetBoundsAsync_sends_south_west_north_east_and_a_default_padding_of_40()
    {
        await Cut.InvokeAsync(() => Cut.Instance.SetBoundsAsync(1, 2, 3, 4));

        Assert.Equal("[1,2,3,4,40]", Controller.Single("setBounds").ArgsJson);
    }

    [Fact]
    public async Task SetBoundsAsync_sends_the_padding_in_pixels()
    {
        await Cut.InvokeAsync(() => Cut.Instance.SetBoundsAsync(1, 2, 3, 4, 12));

        Assert.Equal("[1,2,3,4,12]", Controller.Single("setBounds").ArgsJson);
    }

    [Fact]
    public async Task SetBoundsAsync_with_a_MapPadding_sends_the_four_sides_as_an_object()
    {
        await Cut.InvokeAsync(() => Cut.Instance.SetBoundsAsync(1, 2, 3, 4, new MapPadding(10, 20, 30, 40)));

        Assert.Equal("""[1,2,3,4,{"top":10,"right":20,"bottom":30,"left":40}]""", Controller.Single("setBounds").ArgsJson);
    }

    [Fact]
    public async Task SetBoundsAsync_rejects_a_null_padding_before_calling_JavaScript()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => Cut.Instance.SetBoundsAsync(1, 2, 3, 4, (MapPadding)null!));

        Assert.False(Controller.WasCalled("setBounds"));
    }

    [Fact]
    public async Task SetCameraOrientationAsync_sends_nulls_for_what_is_not_changed()
    {
        await Cut.InvokeAsync(() => Cut.Instance.SetCameraOrientationAsync(pitch: 30));
        await Cut.InvokeAsync(() => Cut.Instance.SetCameraOrientationAsync(bearing: 90));
        await Cut.InvokeAsync(() => Cut.Instance.SetCameraOrientationAsync(10, 20));

        Assert.Equal(["[30,null]", "[null,90]", "[10,20]"], Controller.CallsTo("setCameraOrientation").Select(c => c.ArgsJson));
    }

    [Fact]
    public async Task SetTrafficAsync_sends_flow_then_incidents()
    {
        await Cut.InvokeAsync(() => Cut.Instance.SetTrafficAsync(true, false));

        Assert.Equal("[true,false]", Controller.Single("setTraffic").ArgsJson);
    }

    [Fact]
    public async Task ShowCurrentLocationAsync_sends_the_point()
    {
        await Cut.InvokeAsync(() => Cut.Instance.ShowCurrentLocationAsync(33.1, 35.2));

        Assert.Equal("[33.1,35.2]", Controller.Single("showCurrentLocation").ArgsJson);
    }

    [Fact]
    public async Task Every_camera_method_fails_clearly_before_the_map_has_been_created()
    {
        Js.Module.Hold("createMap");
        IRenderedComponent<AzureMap> early = RenderMap();
        AzureMap map = early.Instance;

        await AssertNotInitializedAsync(() => map.SetCenterAsync(1, 2));
        await AssertNotInitializedAsync(() => map.SetBoundsAsync(1, 2, 3, 4));
        await AssertNotInitializedAsync(() => map.SetStyleAsync(AngryMonkey.CloudComponents.Maps.Options.MapStyle.Road));
        await AssertNotInitializedAsync(() => map.SetTrafficAsync(true, true));
        await AssertNotInitializedAsync(() => map.SetCameraOrientationAsync(1, 2));
        await AssertNotInitializedAsync(() => map.ShowCurrentLocationAsync(1, 2));
        await AssertNotInitializedAsync(() => map.GeocodeAsync("x"));
        await AssertNotInitializedAsync(() => map.ReverseGeocodeAsync(1, 2));
        await AssertNotInitializedAsync(() => map.GetPolygonAsync("g"));
        await AssertNotInitializedAsync(() => map.ResolveBoundaryAsync("x"));
        await AssertNotInitializedAsync(() => map.GetBrowserLocationAsync());
        await AssertNotInitializedAsync(() => map.SearchMunicipalitiesAsync("x", "LB"));
        await AssertNotInitializedAsync(() => map.SetRoutesAsync([]));
        await AssertNotInitializedAsync(() => map.SetTimelinesAsync([]));
    }

    private static async Task AssertNotInitializedAsync(Func<Task> call)
    {
        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(call);
        Assert.Equal("Maps has not been initialized yet.", error.Message);
    }

    // ── Geocode ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GeocodeAsync_sends_query_entity_type_and_country_set()
    {
        Controller.Returns("geocode", null);

        await Cut.InvokeAsync(() => Cut.Instance.GeocodeAsync("Jounieh", "Municipality", "LB"));

        Assert.Equal("""["Jounieh","Municipality","LB"]""", Controller.Single("geocode").ArgsJson);
    }

    [Fact]
    public async Task GeocodeAsync_sends_nulls_for_the_optional_filters()
    {
        Controller.Returns("geocode", null);

        await Cut.InvokeAsync(() => Cut.Instance.GeocodeAsync("Paris"));

        Assert.Equal("""["Paris",null,null]""", Controller.Single("geocode").ArgsJson);
    }

    [Fact]
    public async Task GeocodeAsync_maps_the_JavaScript_answer_to_a_GeocodeResult_and_ignores_extra_fields()
    {
        Controller.Returns("geocode", FakeMapJs.Json("""
            {"latitude":33.98,"longitude":35.62,"north":34.0,"south":33.9,"east":35.7,"west":35.5,
             "geometryId":"geo-1","entityType":"Municipality","names":["Jounieh","Lebanon"]}
            """));

        GeocodeResult? result = await Cut.InvokeAsync(() => Cut.Instance.GeocodeAsync("Jounieh"));

        Assert.Equal(new GeocodeResult(33.98, 35.62, 34.0, 33.9, 35.7, 35.5, "geo-1"), result);
    }

    [Fact]
    public async Task GeocodeAsync_keeps_the_viewport_sides_in_their_own_fields()
    {
        Controller.Returns("geocode", FakeMapJs.Json("""{"latitude":0,"longitude":0,"north":4,"south":1,"east":3,"west":2,"geometryId":null}"""));

        GeocodeResult result = (await Cut.InvokeAsync(() => Cut.Instance.GeocodeAsync("x")))!;

        Assert.Equal((4, 1, 3, 2), (result.North, result.South, result.East, result.West));
    }

    [Fact]
    public async Task GeocodeAsync_maps_a_missing_or_null_geometry_id_to_null()
    {
        Controller.Returns("geocode", FakeMapJs.Json("""{"latitude":1,"longitude":2,"north":1,"south":1,"east":2,"west":2}"""));

        GeocodeResult? result = await Cut.InvokeAsync(() => Cut.Instance.GeocodeAsync("street"));

        Assert.NotNull(result);
        Assert.Null(result.GeometryId);
    }

    [Fact]
    public async Task GeocodeAsync_returns_null_when_JavaScript_finds_nothing()
    {
        Controller.Returns("geocode", null);

        Assert.Null(await Cut.InvokeAsync(() => Cut.Instance.GeocodeAsync("nowhere")));
    }

    [Fact]
    public async Task GeocodeAsync_lets_a_JavaScript_failure_reach_the_caller()
    {
        Controller.Throws("geocode", new Microsoft.JSInterop.JSException("network down"));

        await Assert.ThrowsAsync<Microsoft.JSInterop.JSException>(() => Cut.Instance.GeocodeAsync("x"));
    }

    // ── Reverse geocode ─────────────────────────────────────────────────────

    [Fact]
    public async Task ReverseGeocodeAsync_sends_latitude_then_longitude()
    {
        Controller.Returns("reverseGeocode", null);

        await Cut.InvokeAsync(() => Cut.Instance.ReverseGeocodeAsync(33.5, 35.5));

        Assert.Equal("[33.5,35.5]", Controller.Single("reverseGeocode").ArgsJson);
    }

    [Fact]
    public async Task ReverseGeocodeAsync_maps_every_address_field()
    {
        Controller.Returns("reverseGeocode", FakeMapJs.Json("""
            {"countryCode":"LB","countrySubdivisionCode":"JL","countrySecondarySubdivision":"Keserwan","municipality":"Jounieh",
             "municipalitySubdivision":"Haret Sakher","postalCode":"1200","localName":"Jounieh","freeformAddress":"Main St, Jounieh"}
            """));

        ReverseGeocodeResult? result = await Cut.InvokeAsync(() => Cut.Instance.ReverseGeocodeAsync(1, 2));

        Assert.Equal(new ReverseGeocodeResult("LB", "JL", "Keserwan", "Jounieh", "Haret Sakher", "1200", "Jounieh", "Main St, Jounieh"), result);
    }

    [Fact]
    public async Task ReverseGeocodeAsync_maps_null_and_missing_fields_to_null()
    {
        Controller.Returns("reverseGeocode", FakeMapJs.Json("""{"countryCode":"LB","municipality":null}"""));

        ReverseGeocodeResult result = (await Cut.InvokeAsync(() => Cut.Instance.ReverseGeocodeAsync(1, 2)))!;

        Assert.Equal("LB", result.CountryCode);
        Assert.Null(result.Municipality);
        Assert.Null(result.PostalCode);
        Assert.Null(result.LocalName);
        Assert.Null(result.FreeformAddress);
    }

    [Fact]
    public async Task ReverseGeocodeAsync_returns_null_when_there_is_no_address()
    {
        Controller.Returns("reverseGeocode", null);

        Assert.Null(await Cut.InvokeAsync(() => Cut.Instance.ReverseGeocodeAsync(0, 0)));
    }

    // ── Municipality search ─────────────────────────────────────────────────

    [Fact]
    public async Task SearchMunicipalitiesAsync_sends_the_query_country_paging_and_typeahead_flag()
    {
        Controller.Returns("searchMunicipalities", new { totalResults = 0, results = Array.Empty<object>() });

        await Cut.InvokeAsync(() => Cut.Instance.SearchMunicipalitiesAsync("Jou", "LB", 25, 50, true));

        Assert.Equal("""["Jou","LB",25,50,true]""", Controller.Single("searchMunicipalities").ArgsJson);
    }

    [Fact]
    public async Task SearchMunicipalitiesAsync_defaults_to_a_page_of_100_from_the_start_without_typeahead()
    {
        Controller.Returns("searchMunicipalities", new { totalResults = 0, results = Array.Empty<object>() });

        await Cut.InvokeAsync(() => Cut.Instance.SearchMunicipalitiesAsync("Jou", "LB"));

        Assert.Equal("""["Jou","LB",100,0,false]""", Controller.Single("searchMunicipalities").ArgsJson);
    }

    [Fact]
    public async Task SearchMunicipalitiesAsync_maps_the_page_and_each_municipality()
    {
        Controller.Returns("searchMunicipalities", FakeMapJs.Json("""
            {"totalResults":230,"results":[
              {"countryCode":"LB","countrySubdivisionCode":"JL","countrySecondarySubdivision":"Keserwan","localName":"Jounieh",
               "municipality":"Jounieh","municipalitySubdivision":null,"freeformAddress":"Jounieh, Mount Lebanon"},
              {"countryCode":"LB"}]}
            """));

        MunicipalitySearchPage page = await Cut.InvokeAsync(() => Cut.Instance.SearchMunicipalitiesAsync("J", "LB"));

        Assert.Equal(230, page.TotalResults);
        Assert.Equal(2, page.Results.Count);
        Assert.Equal(new MunicipalitySearchResult("LB", "JL", "Keserwan", "Jounieh", "Jounieh", null, "Jounieh, Mount Lebanon"), page.Results[0]);
        Assert.Equal(new MunicipalitySearchResult("LB", null, null, null, null, null, null), page.Results[1]);
    }

    [Fact]
    public async Task SearchMunicipalitiesAsync_maps_an_empty_page()
    {
        Controller.Returns("searchMunicipalities", FakeMapJs.Json("""{"totalResults":0,"results":[]}"""));

        MunicipalitySearchPage page = await Cut.InvokeAsync(() => Cut.Instance.SearchMunicipalitiesAsync("zzz", "LB"));

        Assert.Equal(0, page.TotalResults);
        Assert.Empty(page.Results);
    }

    // ── Browser location ────────────────────────────────────────────────────

    [Fact]
    public async Task GetBrowserLocationAsync_maps_latitude_and_longitude_to_a_MapCoordinate()
    {
        Controller.Returns("getBrowserLocation", FakeMapJs.Json("""{"latitude":33.5,"longitude":35.5}"""));

        MapCoordinate? location = await Cut.InvokeAsync(() => Cut.Instance.GetBrowserLocationAsync());

        Assert.Equal(new MapCoordinate(33.5, 35.5), location);
    }

    [Fact]
    public async Task GetBrowserLocationAsync_returns_null_when_geolocation_is_unavailable()
    {
        Controller.Returns("getBrowserLocation", null);

        Assert.Null(await Cut.InvokeAsync(() => Cut.Instance.GetBrowserLocationAsync()));
    }

    // ── Polygons ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPolygonAsync_sends_the_geometry_id_and_returns_the_rings()
    {
        Controller.Returns("getPolygon", new[] { new[] { new[] { 35.0, 33.0 }, new[] { 36.0, 33.0 }, new[] { 36.0, 34.0 } } });

        double[][][]? rings = await Cut.InvokeAsync(() => Cut.Instance.GetPolygonAsync("geo-9"));

        Assert.Equal("""["geo-9"]""", Controller.Single("getPolygon").ArgsJson);
        Assert.NotNull(rings);
        Assert.Equal([35.0, 33.0], rings[0][0]);
        Assert.Equal(3, rings[0].Length);
    }

    [Fact]
    public async Task GetPolygonAsync_returns_null_when_there_is_no_polygon()
    {
        Controller.Returns("getPolygon", null);

        Assert.Null(await Cut.InvokeAsync(() => Cut.Instance.GetPolygonAsync("geo-9")));
    }

    [Fact]
    public async Task ResolveBoundaryAsync_with_one_entity_type_sends_it_as_a_string()
    {
        Controller.Returns("resolveBoundary", null);

        await Cut.InvokeAsync(() => Cut.Instance.ResolveBoundaryAsync("Jounieh", "Municipality", "LB"));

        Assert.Equal("""["Jounieh","Municipality","LB"]""", Controller.Single("resolveBoundary").ArgsJson);
    }

    [Fact]
    public async Task ResolveBoundaryAsync_without_filters_sends_nulls()
    {
        Controller.Returns("resolveBoundary", null);

        await Cut.InvokeAsync(() => Cut.Instance.ResolveBoundaryAsync("Jounieh"));

        Assert.Equal("""["Jounieh",null,null]""", Controller.Single("resolveBoundary").ArgsJson);
    }

    [Fact]
    public async Task ResolveBoundaryAsync_with_several_entity_types_sends_them_as_an_array_in_order()
    {
        Controller.Returns("resolveBoundary", null);

        await Cut.InvokeAsync(() => Cut.Instance.ResolveBoundaryAsync("Zouk", ["Municipality", "Neighbourhood"], "LB"));

        Assert.Equal("""["Zouk",["Municipality","Neighbourhood"],"LB"]""", Controller.Single("resolveBoundary").ArgsJson);
    }

    [Fact]
    public async Task ResolveBoundaryAsync_returns_the_polygon_rings_or_null()
    {
        Controller.Returns("resolveBoundary", new[] { new[] { new[] { 1.0, 2.0 } } });
        double[][][]? found = await Cut.InvokeAsync(() => Cut.Instance.ResolveBoundaryAsync("a"));

        Controller.Returns("resolveBoundary", null);
        double[][][]? missing = await Cut.InvokeAsync(() => Cut.Instance.ResolveBoundaryAsync("b"));

        Assert.Equal([1.0, 2.0], found![0][0]);
        Assert.Null(missing);
    }
}
