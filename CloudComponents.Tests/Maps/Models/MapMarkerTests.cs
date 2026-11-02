using System.Text.Json;
using AngryMonkey.CloudComponents.Maps.Models;
using CloudComponents.Tests.Maps.Component;

namespace CloudComponents.Tests.Maps.Models;

public sealed class MapMarkerTests
{
    [Fact]
    public void A_marker_has_the_documented_defaults()
    {
        MapMarker marker = new(33.9, 35.5);

        Assert.Equal(33.9, marker.Latitude);
        Assert.Equal(35.5, marker.Longitude);
        Assert.Null(marker.Label);
        Assert.Equal("#e81123", marker.Color);
        Assert.Null(marker.Title);
        Assert.Null(marker.City);
        Assert.Null(marker.District);
        Assert.Null(marker.Subdivision);
        Assert.Null(marker.Country);
        Assert.Null(marker.ImageUrl);
        Assert.Null(marker.Area);
        Assert.Null(marker.Price);
        Assert.Null(marker.DetailsUrl);
        Assert.Null(marker.DetailsLabel);
    }

    [Fact]
    public void The_generated_id_is_a_32_digit_hex_guid()
    {
        MapMarker marker = new(0, 0);

        Assert.Matches("^[0-9a-f]{32}$", marker.Id);
    }

    [Fact]
    public void The_generated_id_is_stable_for_the_life_of_the_instance()
    {
        MapMarker marker = new(0, 0);

        string first = marker.Id;

        Assert.Equal(first, marker.Id);
    }

    [Fact]
    public void Markers_get_different_generated_ids()
    {
        string[] ids = [.. Enumerable.Range(0, 200).Select(_ => new MapMarker(0, 0).Id)];

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void An_explicit_id_is_kept()
    {
        MapMarker marker = new(0, 0) { Id = "db-42" };

        Assert.Equal("db-42", marker.Id);
    }

    [Fact]
    public void A_copy_made_with_with_keeps_the_id_so_it_still_identifies_the_same_marker()
    {
        MapMarker marker = new(1, 2, "A");

        MapMarker moved = marker with { Latitude = 3 };

        Assert.Equal(marker.Id, moved.Id);
        Assert.Equal(3, moved.Latitude);
        Assert.Equal(1, marker.Latitude);
    }

    [Fact]
    public void A_copy_can_be_given_another_id()
    {
        MapMarker copy = new MapMarker(1, 2) with { Id = "other" };

        Assert.Equal("other", copy.Id);
    }

    [Fact]
    public void Two_markers_with_the_same_content_are_different_because_their_generated_ids_differ()
    {
        Assert.NotEqual(new MapMarker(1, 2, "A"), new MapMarker(1, 2, "A"));
    }

    [Fact]
    public void Two_markers_with_the_same_id_and_content_are_equal_and_hash_alike()
    {
        MapMarker a = new(1, 2, "A") { Id = "x", Price = "$1" };
        MapMarker b = new(1, 2, "A") { Id = "x", Price = "$1" };

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void A_difference_in_any_field_makes_markers_unequal()
    {
        MapMarker baseline = new(1, 2) { Id = "x" };

        Assert.NotEqual(baseline, baseline with { Title = "t" });
        Assert.NotEqual(baseline, baseline with { City = "c" });
        Assert.NotEqual(baseline, baseline with { District = "d" });
        Assert.NotEqual(baseline, baseline with { Subdivision = "s" });
        Assert.NotEqual(baseline, baseline with { Country = "c" });
        Assert.NotEqual(baseline, baseline with { ImageUrl = "i" });
        Assert.NotEqual(baseline, baseline with { Area = "a" });
        Assert.NotEqual(baseline, baseline with { Price = "p" });
        Assert.NotEqual(baseline, baseline with { DetailsUrl = "u" });
        Assert.NotEqual(baseline, baseline with { DetailsLabel = "l" });
        Assert.NotEqual(baseline, baseline with { Color = "#000" });
        Assert.NotEqual(baseline, baseline with { Label = "L" });
        Assert.NotEqual(baseline, baseline with { Latitude = 9 });
        Assert.NotEqual(baseline, baseline with { Longitude = 9 });
    }

    [Fact]
    public void The_marker_serialises_with_the_camelCase_names_the_script_reads()
    {
        MapMarker marker = new(1, 2, "L", "#fff") { Id = "x", Title = "T", ImageUrl = "i", DetailsUrl = "u", DetailsLabel = "dl" };

        JsonElement json = JsonSerializer.SerializeToElement(marker, FakeMapJs.Wire);

        Assert.Equal(
            ["latitude", "longitude", "label", "color", "id", "title", "city", "district", "subdivision", "country", "imageUrl", "area", "price", "detailsUrl", "detailsLabel"],
            json.EnumerateObject().Select(p => p.Name));
        Assert.Equal("x", json.GetProperty("id").GetString());
        Assert.Equal("dl", json.GetProperty("detailsLabel").GetString());
    }

    [Fact]
    public void The_marker_survives_a_round_trip_through_the_wire_format()
    {
        MapMarker marker = new(1.5, -2.5, "L", "#fff") { Id = "x", Title = "T", City = "C", Price = "$9" };

        MapMarker? back = JsonSerializer.Deserialize<MapMarker>(JsonSerializer.Serialize(marker, FakeMapJs.Wire), FakeMapJs.Wire);

        Assert.Equal(marker, back);
    }
}
