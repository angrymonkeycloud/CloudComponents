using System.Text.Json;
using AngryMonkey.CloudComponents.Maps.Models;
using CloudComponents.Tests.Maps.Component;

namespace CloudComponents.Tests.Maps.Models;

public sealed class MapCoordinateAndViewTests
{
    // ── MapCoordinate ───────────────────────────────────────────────────────

    [Fact]
    public void A_coordinate_keeps_latitude_then_longitude()
    {
        MapCoordinate coordinate = new(33.5, 35.25);

        Assert.Equal(33.5, coordinate.Latitude);
        Assert.Equal(35.25, coordinate.Longitude);
    }

    [Fact]
    public void The_default_coordinate_is_the_origin()
    {
        MapCoordinate coordinate = default;

        Assert.Equal(new MapCoordinate(0, 0), coordinate);
    }

    [Fact]
    public void Coordinates_with_the_same_values_are_equal_by_method_and_operator_and_hash_alike()
    {
        MapCoordinate a = new(1.5, 2.5);
        MapCoordinate b = new(1.5, 2.5);

        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Swapping_latitude_and_longitude_gives_a_different_coordinate()
    {
        Assert.NotEqual(new MapCoordinate(1, 2), new MapCoordinate(2, 1));
    }

    [Fact]
    public void A_coordinate_can_be_deconstructed_in_order()
    {
        (double latitude, double longitude) = new MapCoordinate(7, 8);

        Assert.Equal((7.0, 8.0), (latitude, longitude));
    }

    [Fact]
    public void A_with_copy_changes_only_the_named_value_and_leaves_the_original()
    {
        MapCoordinate original = new(1, 2);

        MapCoordinate changed = original with { Longitude = 9 };

        Assert.Equal(new MapCoordinate(1, 9), changed);
        Assert.Equal(new MapCoordinate(1, 2), original);
    }

    [Fact]
    public void A_nullable_coordinate_can_tell_no_location_from_the_origin()
    {
        MapCoordinate? none = null;
        MapCoordinate? origin = new MapCoordinate(0, 0);

        Assert.Null(none);
        Assert.NotNull(origin);
    }

    [Fact]
    public void A_coordinate_serialises_as_latitude_and_longitude()
    {
        Assert.Equal("""{"latitude":1.5,"longitude":-2}""", JsonSerializer.Serialize(new MapCoordinate(1.5, -2), FakeMapJs.Wire));
    }

    [Theory]
    [InlineData("""{"latitude":1.5,"longitude":-2}""")]
    [InlineData("""{"Latitude":1.5,"Longitude":-2}""")]
    [InlineData("""{"longitude":-2,"latitude":1.5,"accuracy":10}""")]
    public void A_coordinate_reads_the_object_the_script_returns_whatever_the_casing_order_or_extras(string json)
    {
        MapCoordinate? coordinate = JsonSerializer.Deserialize<MapCoordinate>(json, FakeMapJs.Wire);

        Assert.Equal(new MapCoordinate(1.5, -2), coordinate);
    }

    [Fact]
    public void A_coordinate_missing_a_value_reads_it_as_zero()
    {
        MapCoordinate? coordinate = JsonSerializer.Deserialize<MapCoordinate>("""{"latitude":4}""", FakeMapJs.Wire);

        Assert.Equal(new MapCoordinate(4, 0), coordinate);
    }

    // ── InitialMapView ──────────────────────────────────────────────────────

    [Fact]
    public void An_initial_view_defaults_to_zoom_12()
    {
        InitialMapView view = new(33.9, 35.5);

        Assert.Equal((33.9, 35.5, 12.0), (view.Latitude, view.Longitude, view.Zoom));
    }

    [Fact]
    public void An_initial_view_keeps_the_zoom_it_is_given_including_fractions()
    {
        Assert.Equal(7.5, new InitialMapView(0, 0, 7.5).Zoom);
    }

    [Fact]
    public void The_world_view_is_the_equator_and_prime_meridian_at_zoom_2()
    {
        Assert.Equal((0.0, 0.0, 2.0), (InitialMapView.World.Latitude, InitialMapView.World.Longitude, InitialMapView.World.Zoom));
    }

    [Fact]
    public void The_world_view_is_one_shared_instance()
    {
        Assert.Same(InitialMapView.World, InitialMapView.World);
    }

    [Fact]
    public void Copying_the_world_view_does_not_change_the_shared_one()
    {
        InitialMapView closer = InitialMapView.World with { Zoom = 5 };

        Assert.Equal(5, closer.Zoom);
        Assert.Equal(2, InitialMapView.World.Zoom);
    }

    [Fact]
    public void Views_with_the_same_values_are_equal()
    {
        Assert.Equal(new InitialMapView(1, 2, 3), new InitialMapView(1, 2, 3));
        Assert.NotEqual(new InitialMapView(1, 2, 3), new InitialMapView(1, 2, 4));
        Assert.Equal(new InitialMapView(1, 2), new InitialMapView(1, 2, 12));
    }
}
