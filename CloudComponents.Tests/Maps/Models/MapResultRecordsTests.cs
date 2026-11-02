using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using AngryMonkey.CloudComponents.Maps.Models;
using CloudComponents.Tests.Maps.Component;

namespace CloudComponents.Tests.Maps.Models;

/// <summary>The records JavaScript answers are turned into, read from JSON exactly as Blazor's JS runtime reads them.</summary>
public sealed class MapResultRecordsTests
{
    private static T Read<T>(string json) => JsonSerializer.Deserialize<T>(json, FakeMapJs.Wire)!;

    // ── GeocodeResult ───────────────────────────────────────────────────────

    [Fact]
    public void A_geocode_result_keeps_its_values_in_their_own_fields()
    {
        GeocodeResult result = new(1, 2, 3, 4, 5, 6, "g");

        Assert.Equal((1.0, 2.0, 3.0, 4.0, 5.0, 6.0, "g"), (result.Latitude, result.Longitude, result.North, result.South, result.East, result.West, result.GeometryId));
    }

    [Fact]
    public void A_geocode_result_reads_the_script_object_and_ignores_the_extra_fields()
    {
        GeocodeResult result = Read<GeocodeResult>("""{"latitude":1,"longitude":2,"north":4,"south":3,"east":6,"west":5,"geometryId":"g","entityType":"Municipality","names":["a"]}""");

        Assert.Equal(new GeocodeResult(1, 2, 4, 3, 6, 5, "g"), result);
    }

    [Fact]
    public void A_geocode_result_without_a_geometry_id_has_null()
    {
        GeocodeResult result = Read<GeocodeResult>("""{"latitude":1,"longitude":2,"north":1,"south":1,"east":2,"west":2,"geometryId":null}""");

        Assert.Null(result.GeometryId);
    }

    [Fact]
    public void Geocode_results_compare_by_value()
    {
        Assert.Equal(new GeocodeResult(1, 2, 3, 4, 5, 6, "g"), new GeocodeResult(1, 2, 3, 4, 5, 6, "g"));
        Assert.NotEqual(new GeocodeResult(1, 2, 3, 4, 5, 6, "g"), new GeocodeResult(1, 2, 3, 4, 5, 6, null));
    }

    // ── MapSearchResult ─────────────────────────────────────────────────────

    [Fact]
    public void A_search_result_has_no_description_by_default()
    {
        Assert.Null(new MapSearchResult("A", 1, 2, 3, 4, 5, 6, null).Description);
    }

    [Fact]
    public void A_search_result_reads_the_script_object_including_the_description()
    {
        MapSearchResult result = Read<MapSearchResult>("""{"address":"Jounieh","description":"Mount Lebanon","latitude":1,"longitude":2,"north":4,"south":3,"east":6,"west":5,"geometryId":"g"}""");

        Assert.Equal(new MapSearchResult("Jounieh", 1, 2, 4, 3, 6, 5, "g") { Description = "Mount Lebanon" }, result);
    }

    [Fact]
    public void A_search_result_with_a_null_description_reads_as_null()
    {
        MapSearchResult result = Read<MapSearchResult>("""{"address":"A","description":null,"latitude":1,"longitude":2,"north":1,"south":1,"east":2,"west":2,"geometryId":null}""");

        Assert.Null(result.Description);
        Assert.Null(result.GeometryId);
    }

    [Fact]
    public void The_description_is_part_of_search_result_equality()
    {
        MapSearchResult a = new("A", 1, 2, 3, 4, 5, 6, null) { Description = "x" };

        Assert.NotEqual(a, a with { Description = "y" });
        Assert.Equal(a, a with { });
    }

    // ── ReverseGeocodeResult ────────────────────────────────────────────────

    [Fact]
    public void A_reverse_geocode_result_defaults_local_name_and_freeform_address_to_null()
    {
        ReverseGeocodeResult result = new("LB", "JL", "Keserwan", "Jounieh", null, "1200");

        Assert.Null(result.LocalName);
        Assert.Null(result.FreeformAddress);
    }

    [Fact]
    public void A_reverse_geocode_result_reads_every_field_and_missing_ones_as_null()
    {
        ReverseGeocodeResult full = Read<ReverseGeocodeResult>("""{"countryCode":"LB","countrySubdivisionCode":"JL","countrySecondarySubdivision":"Keserwan","municipality":"Jounieh","municipalitySubdivision":"Sakher","postalCode":"1200","localName":"Jounieh","freeformAddress":"Main St"}""");
        ReverseGeocodeResult sparse = Read<ReverseGeocodeResult>("""{"countryCode":"LB"}""");

        Assert.Equal(new ReverseGeocodeResult("LB", "JL", "Keserwan", "Jounieh", "Sakher", "1200", "Jounieh", "Main St"), full);
        Assert.Equal(new ReverseGeocodeResult("LB", null, null, null, null, null), sparse);
    }

    // ── Municipalities ──────────────────────────────────────────────────────

    [Fact]
    public void A_municipality_page_reads_the_total_and_each_result()
    {
        MunicipalitySearchPage page = Read<MunicipalitySearchPage>("""{"totalResults":3,"results":[{"countryCode":"LB","localName":"A"},{"municipality":"B"}]}""");

        Assert.Equal(3, page.TotalResults);
        Assert.Equal(new MunicipalitySearchResult("LB", null, null, "A", null, null, null), page.Results[0]);
        Assert.Equal(new MunicipalitySearchResult(null, null, null, null, "B", null, null), page.Results[1]);
    }

    [Fact]
    public void Municipality_results_compare_by_value()
    {
        Assert.Equal(
            new MunicipalitySearchResult("LB", "JL", "K", "L", "M", "S", "F"),
            new MunicipalitySearchResult("LB", "JL", "K", "L", "M", "S", "F"));
    }

    // ── ZoneCheckResult ─────────────────────────────────────────────────────

    [Fact]
    public void CanZone_and_Found_are_required_members()
    {
        string[] required = [.. typeof(ZoneCheckResult).GetProperties().Where(p => p.GetCustomAttribute<RequiredMemberAttribute>() is not null).Select(p => p.Name).Order()];

        Assert.Equal([nameof(ZoneCheckResult.CanZone), nameof(ZoneCheckResult.Found)], required);
    }

    [Fact]
    public void NotFound_is_a_not_found_result_without_coordinate_or_geometry()
    {
        Assert.False(ZoneCheckResult.NotFound.Found);
        Assert.False(ZoneCheckResult.NotFound.CanZone);
        Assert.Null(ZoneCheckResult.NotFound.Coordinate);
        Assert.Null(ZoneCheckResult.NotFound.GeometryId);
    }

    [Fact]
    public void NotFound_is_one_shared_instance()
    {
        Assert.Same(ZoneCheckResult.NotFound, ZoneCheckResult.NotFound);
    }

    [Fact]
    public void Changing_a_copy_of_NotFound_does_not_change_the_shared_one()
    {
        ZoneCheckResult found = ZoneCheckResult.NotFound with { Found = true, Coordinate = new MapCoordinate(1, 2) };

        Assert.True(found.Found);
        Assert.False(ZoneCheckResult.NotFound.Found);
        Assert.Null(ZoneCheckResult.NotFound.Coordinate);
    }

    [Fact]
    public void Zone_check_results_compare_by_value()
    {
        ZoneCheckResult a = new() { Found = true, CanZone = true, Coordinate = new MapCoordinate(1, 2), GeometryId = "g" };
        ZoneCheckResult b = new() { Found = true, CanZone = true, Coordinate = new MapCoordinate(1, 2), GeometryId = "g" };

        Assert.Equal(a, b);
        Assert.NotEqual(a, b with { CanZone = false });
        Assert.NotEqual(a, b with { GeometryId = null });
    }
}
