using System.Text.Json;
using AngryMonkey.CloudComponents.Maps.Models;
using CloudComponents.Tests.Maps.Component;

namespace CloudComponents.Tests.Maps.Models;

public sealed class MapRouteTests
{
    [Fact]
    public void A_route_starts_blue_with_no_points_and_a_generated_id()
    {
        MapRoute route = new();

        Assert.Equal("#2563eb", route.Color);
        Assert.NotNull(route.Points);
        Assert.Empty(route.Points);
        Assert.Matches("^[0-9a-f]{32}$", route.Id);
    }

    [Fact]
    public void Routes_get_different_ids_and_each_keeps_its_own()
    {
        MapRoute a = new();
        MapRoute b = new();

        Assert.NotEqual(a.Id, b.Id);
        Assert.Equal(a.Id, a.Id);
    }

    [Fact]
    public void Setting_the_points_of_one_route_does_not_change_another()
    {
        MapRoute a = new();
        MapRoute b = new() { Points = [new MapCoordinate(1, 2)] };

        Assert.Empty(a.Points);
        Assert.Single(b.Points);
    }

    [Fact]
    public void A_copy_keeps_the_id()
    {
        MapRoute route = new() { Color = "#000" };

        Assert.Equal(route.Id, (route with { Color = "#fff" }).Id);
    }

    [Fact]
    public void Routes_with_the_same_id_colour_and_point_list_are_equal()
    {
        IReadOnlyList<MapCoordinate> points = [new MapCoordinate(1, 2)];

        Assert.Equal(new MapRoute { Id = "a", Points = points }, new MapRoute { Id = "a", Points = points });
    }

    [Fact]
    public void The_route_serialises_points_as_latitude_longitude_objects()
    {
        MapRoute route = new() { Id = "r", Color = "#123456", Points = [new MapCoordinate(1.5, 2.5), new MapCoordinate(3, 4)] };

        string json = JsonSerializer.Serialize(route, FakeMapJs.Wire);

        Assert.Equal("""{"id":"r","color":"#123456","points":[{"latitude":1.5,"longitude":2.5},{"latitude":3,"longitude":4}]}""", json);
    }
}
