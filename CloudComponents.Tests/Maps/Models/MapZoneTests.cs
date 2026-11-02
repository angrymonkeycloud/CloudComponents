using System.Reflection;
using System.Runtime.CompilerServices;
using AngryMonkey.CloudComponents.Maps.Models;

namespace CloudComponents.Tests.Maps.Models;

public sealed class MapZoneTests
{
    [Fact]
    public void Addresses_is_a_required_member()
    {
        PropertyInfo addresses = typeof(MapZone).GetProperty(nameof(MapZone.Addresses))!;

        Assert.NotNull(addresses.GetCustomAttribute<RequiredMemberAttribute>());
    }

    [Fact]
    public void Addresses_is_the_only_required_member()
    {
        string[] required = [.. typeof(MapZone).GetProperties().Where(p => p.GetCustomAttribute<RequiredMemberAttribute>() is not null).Select(p => p.Name)];

        Assert.Equal([nameof(MapZone.Addresses)], required);
    }

    [Fact]
    public void A_zone_has_the_documented_defaults()
    {
        MapZone zone = new() { Addresses = ["Paris"] };

        Assert.Equal(["Paris"], zone.Addresses);
        Assert.Null(zone.EntityTypes);
        Assert.Null(zone.Label);
        Assert.Null(zone.CountrySet);
        Assert.Equal("rgba(0,120,212,0.15)", zone.FillColor);
        Assert.Equal("#0078d4", zone.StrokeColor);
        Assert.Equal(2, zone.StrokeWidth);
    }

    [Fact]
    public void The_generated_id_is_a_32_digit_hex_guid_stable_per_instance_and_different_between_instances()
    {
        MapZone a = new() { Addresses = [] };
        MapZone b = new() { Addresses = [] };

        Assert.Matches("^[0-9a-f]{32}$", a.Id);
        Assert.Equal(a.Id, a.Id);
        Assert.NotEqual(a.Id, b.Id);
    }

    [Fact]
    public void An_explicit_id_is_kept()
    {
        MapZone zone = new() { Addresses = [], Id = "zone-1" };

        Assert.Equal("zone-1", zone.Id);
    }

    [Fact]
    public void A_copy_keeps_the_id_and_the_address_list()
    {
        IReadOnlyList<string> addresses = ["Lyon", "Nice"];
        MapZone zone = new() { Addresses = addresses, Label = "South" };

        MapZone copy = zone with { Label = "North" };

        Assert.Equal(zone.Id, copy.Id);
        Assert.Same(addresses, copy.Addresses);
        Assert.Equal("North", copy.Label);
    }

    [Fact]
    public void Zones_with_the_same_id_and_the_same_list_instance_are_equal()
    {
        IReadOnlyList<string> addresses = ["Lyon"];

        Assert.Equal(new MapZone { Addresses = addresses, Id = "a" }, new MapZone { Addresses = addresses, Id = "a" });
    }

    [Fact]
    public void Zones_with_different_ids_are_not_equal()
    {
        IReadOnlyList<string> addresses = ["Lyon"];

        Assert.NotEqual(new MapZone { Addresses = addresses }, new MapZone { Addresses = addresses });
    }

    [Fact]
    public void Entity_types_and_country_set_are_kept_as_given()
    {
        MapZone zone = new() { Addresses = ["Zouk"], EntityTypes = ["Municipality", "Neighbourhood"], CountrySet = "LB,FR" };

        Assert.Equal(["Municipality", "Neighbourhood"], zone.EntityTypes);
        Assert.Equal("LB,FR", zone.CountrySet);
    }
}
