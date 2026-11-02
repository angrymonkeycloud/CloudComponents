using System.Text.Json;
using AngryMonkey.CloudComponents.Maps.Models;
using CloudComponents.Tests.Maps.Component;

namespace CloudComponents.Tests.Maps.Models;

public sealed class MapRegionTests
{
    private static readonly double[][][] Ring = [[[0, 0], [0, 1], [1, 1], [0, 0]]];

    [Fact]
    public void A_region_has_the_documented_default_style()
    {
        MapRegion region = new(Ring);

        Assert.Same(Ring, region.Coordinates);
        Assert.Equal("rgba(0, 120, 212, 0.15)", region.FillColor);
        Assert.Equal("#0078d4", region.StrokeColor);
        Assert.Equal(2, region.StrokeWidth);
        Assert.Null(region.Label);
    }

    [Fact]
    public void The_generated_id_is_a_32_digit_hex_guid_stable_per_instance_and_different_between_instances()
    {
        MapRegion a = new(Ring);
        MapRegion b = new(Ring);

        Assert.Matches("^[0-9a-f]{32}$", a.Id);
        Assert.Equal(a.Id, a.Id);
        Assert.NotEqual(a.Id, b.Id);
    }

    [Fact]
    public void An_explicit_id_and_label_are_kept()
    {
        MapRegion region = new(Ring) { Id = "north", Label = "North" };

        Assert.Equal("north", region.Id);
        Assert.Equal("North", region.Label);
    }

    [Fact]
    public void A_copy_keeps_the_id_and_label()
    {
        MapRegion region = new(Ring) { Label = "North" };

        MapRegion copy = region with { StrokeWidth = 5 };

        Assert.Equal(region.Id, copy.Id);
        Assert.Equal("North", copy.Label);
        Assert.Equal(5, copy.StrokeWidth);
    }

    [Fact]
    public void Regions_with_the_same_ring_instance_and_id_are_equal()
    {
        MapRegion a = new(Ring) { Id = "x" };
        MapRegion b = new(Ring) { Id = "x" };

        Assert.Equal(a, b);
    }

    [Fact]
    public void Regions_are_not_equal_when_their_ids_differ()
    {
        Assert.NotEqual(new MapRegion(Ring), new MapRegion(Ring));
    }

    [Fact]
    public void Equality_compares_the_coordinate_arrays_by_reference()
    {
        // Documents what the record gives: equal-looking ring data in a different array is a different region.
        MapRegion a = new([[[0, 0]]]) { Id = "x" };
        MapRegion b = new([[[0, 0]]]) { Id = "x" };

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void The_region_serialises_the_coordinates_as_nested_arrays_with_the_names_the_script_reads()
    {
        MapRegion region = new([[[35.0, 33.0], [36.0, 34.0]]], "red", "blue", 3) { Id = "r", Label = "L" };

        string json = JsonSerializer.Serialize(region, FakeMapJs.Wire);

        Assert.Equal("""{"coordinates":[[[35,33],[36,34]]],"fillColor":"red","strokeColor":"blue","strokeWidth":3,"id":"r","label":"L"}""", json);
    }
}
