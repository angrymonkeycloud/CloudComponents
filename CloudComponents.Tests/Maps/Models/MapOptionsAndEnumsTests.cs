using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using AngryMonkey.CloudComponents.Maps.Models;
using AngryMonkey.CloudComponents.Maps.Options;
using AngryMonkey.CloudComponents.Maps.Services;
using CloudComponents.Tests.Maps.Component;

namespace CloudComponents.Tests.Maps.Models;

public sealed class MapOptionsAndEnumsTests
{
    private static string EnumMemberValue<TEnum>(TEnum value) where TEnum : struct, Enum =>
        typeof(TEnum).GetField(value.ToString())!.GetCustomAttribute<EnumMemberAttribute>()!.Value!;

    // ── MapControlOption / MapPadding ───────────────────────────────────────

    [Fact]
    public void A_control_option_defaults_to_enabled_in_the_top_right()
    {
        MapControlOption option = new();

        Assert.True(option.Enabled);
        Assert.Equal(MapControlPosition.TopRight, option.Position);
    }

    [Fact]
    public void Control_options_compare_by_value_which_is_what_makes_an_unchanged_control_not_resent()
    {
        Assert.Equal(new MapControlOption(true, MapControlPosition.BottomLeft), new MapControlOption(true, MapControlPosition.BottomLeft));
        Assert.NotEqual(new MapControlOption(true, MapControlPosition.BottomLeft), new MapControlOption(false, MapControlPosition.BottomLeft));
        Assert.NotEqual(new MapControlOption(true, MapControlPosition.BottomLeft), new MapControlOption(true, MapControlPosition.TopLeft));
    }

    [Fact]
    public void Padding_keeps_top_right_bottom_left_in_that_order()
    {
        MapPadding padding = new(1, 2, 3, 4);

        Assert.Equal((1.0, 2.0, 3.0, 4.0), (padding.Top, padding.Right, padding.Bottom, padding.Left));
    }

    [Fact]
    public void Padding_serialises_with_the_four_side_names()
    {
        Assert.Equal("""{"top":1,"right":2,"bottom":3,"left":4}""", JsonSerializer.Serialize(new MapPadding(1, 2, 3, 4), FakeMapJs.Wire));
    }

    [Fact]
    public void Paddings_with_equal_sides_are_equal()
    {
        Assert.Equal(new MapPadding(1, 2, 3, 4), new MapPadding(1, 2, 3, 4));
        Assert.NotEqual(new MapPadding(1, 2, 3, 4), new MapPadding(4, 3, 2, 1));
    }

    // ── Enums ───────────────────────────────────────────────────────────────

    [Fact]
    public void Every_style_has_a_distinct_lowercase_snake_case_Azure_identifier()
    {
        string[] identifiers = [.. Enum.GetValues<MapStyle>().Select(EnumMemberValue)];

        Assert.Equal(Enum.GetValues<MapStyle>().Length, identifiers.Distinct().Count());
        Assert.All(identifiers, id => Assert.Matches("^[a-z]+(_[a-z]+)*$", id));
    }

    [Fact]
    public void The_style_identifiers_are_the_ones_the_Azure_Maps_SDK_names()
    {
        Assert.Equal(
            ["road", "grayscale_light", "grayscale_dark", "night", "road_shaded_relief", "satellite", "satellite_road_labels", "high_contrast_dark", "high_contrast_light"],
            Enum.GetValues<MapStyle>().Select(EnumMemberValue));
    }

    [Fact]
    public void Road_is_the_first_style_so_it_is_what_an_unrecognised_value_falls_back_to()
    {
        Assert.Equal(MapStyle.Road, default(MapStyle));
    }

    [Fact]
    public void Every_control_position_has_a_distinct_dashed_identifier()
    {
        string[] identifiers = [.. Enum.GetValues<MapControlPosition>().Select(EnumMemberValue)];

        Assert.Equal(["top-left", "top-right", "bottom-left", "bottom-right"], identifiers);
    }

    [Fact]
    public void Marker_adding_is_off_by_default()
    {
        Assert.Equal(MarkerAddTrigger.Disabled, default(MarkerAddTrigger));
    }

    [Fact]
    public void The_marker_triggers_are_the_four_documented_ones()
    {
        Assert.Equal([MarkerAddTrigger.Disabled, MarkerAddTrigger.SingleClick, MarkerAddTrigger.DoubleClick, MarkerAddTrigger.CenterPin], Enum.GetValues<MarkerAddTrigger>());
    }

    [Fact]
    public void The_permission_states_are_the_four_documented_ones()
    {
        Assert.Equal(
            [LocationPermissionState.Unsupported, LocationPermissionState.Prompt, LocationPermissionState.Granted, LocationPermissionState.Denied],
            Enum.GetValues<LocationPermissionState>());
    }

    // ── AzureMapsOptions ────────────────────────────────────────────────────

    [Fact]
    public void The_options_start_with_an_empty_key_not_null()
    {
        Assert.Equal(string.Empty, new AzureMapsOptions().SubscriptionKey);
    }

    [Fact]
    public void The_options_key_can_be_set()
    {
        Assert.Equal("abc", new AzureMapsOptions { SubscriptionKey = "abc" }.SubscriptionKey);
    }
}
