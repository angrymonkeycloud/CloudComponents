using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using AngryMonkey.CloudComponents.Maps.Components;
using AngryMonkey.CloudComponents.Maps.Extensions;
using AngryMonkey.CloudComponents.Maps.Models;
using AngryMonkey.CloudComponents.Maps.Options;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace CloudComponents.Tests.Maps.Component;

/// <summary>
/// What the component sends JavaScript in <c>createMap(dotNetRef, options)</c>. These property names and string values are the
/// contract any map provider's JavaScript has to read.
/// </summary>
public sealed class AzureMapCreateOptionsTests : AzureMapTestContext
{
    private JsonElement Options => Js.CreateMapOptions;

    [Fact]
    public void The_module_is_imported_from_the_package_static_assets_path()
    {
        RenderMap();

        Assert.Equal(["./_content/AngryMonkey.CloudComponents.Maps/mapInterop.js"], Js.Imports);
    }

    [Fact]
    public void createMap_is_called_once_on_the_module_with_a_reference_to_the_component_and_an_options_object()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();

        JsCall call = Js.Module.Single("createMap");

        Assert.Equal(2, call.Args.Length);
        Assert.Same(cut.Instance, Assert.IsType<DotNetObjectReference<AzureMap>>(call.Args[0]).Value);
        Assert.Equal(JsonValueKind.Object, call.Json(1).ValueKind);
    }

    [Fact]
    public void createMap_is_the_only_call_made_on_the_module_when_the_map_is_created()
    {
        RenderMap();

        Assert.Equal(["createMap"], Js.Module.Methods);
    }

    [Fact]
    public void Defaults_are_sent_for_a_map_with_no_parameters_but_the_key()
    {
        RenderMap();

        Assert.Equal("test-key", Options.GetProperty("subscriptionKey").GetString());
        Assert.Equal("satellite", Options.GetProperty("style").GetString());
        Assert.Equal("en-US", Options.GetProperty("language").GetString());
        Assert.Equal("Auto", Options.GetProperty("view").GetString());
        Assert.Equal(0, Options.GetProperty("pitch").GetDouble());
        Assert.Equal(0, Options.GetProperty("bearing").GetDouble());
        Assert.Equal(JsonValueKind.Null, Options.GetProperty("minZoom").ValueKind);
        Assert.Equal(JsonValueKind.Null, Options.GetProperty("maxZoom").ValueKind);
        Assert.False(Options.GetProperty("showTrafficFlow").GetBoolean());
        Assert.False(Options.GetProperty("showTrafficIncidents").GetBoolean());
        Assert.False(Options.GetProperty("allowMarkerRemoval").GetBoolean());
        Assert.True(Options.GetProperty("showMarkerPopups").GetBoolean());
        Assert.False(Options.GetProperty("showMarkerTitlesOnZoom").GetBoolean());
        Assert.Equal(15, Options.GetProperty("markerTitleMinZoom").GetDouble());
        Assert.Equal("disabled", Options.GetProperty("addMarkerTrigger").GetString());
        Assert.True(Options.GetProperty("interactive").GetBoolean());
    }

    [Fact]
    public void Without_an_InitialView_the_camera_starts_at_the_world_view()
    {
        RenderMap();

        Assert.Equal(InitialMapView.World.Latitude, Options.GetProperty("latitude").GetDouble());
        Assert.Equal(InitialMapView.World.Longitude, Options.GetProperty("longitude").GetDouble());
        Assert.Equal(InitialMapView.World.Zoom, Options.GetProperty("zoom").GetDouble());
    }

    [Fact]
    public void An_InitialView_sets_the_centre_and_zoom()
    {
        RenderMap(p => p.Add(m => m.InitialView, new InitialMapView(33.89, 35.5, 9)));

        Assert.Equal(33.89, Options.GetProperty("latitude").GetDouble());
        Assert.Equal(35.5, Options.GetProperty("longitude").GetDouble());
        Assert.Equal(9, Options.GetProperty("zoom").GetDouble());
    }

    [Fact]
    public void An_InitialView_without_a_zoom_uses_the_record_default_of_12()
    {
        RenderMap(p => p.Add(m => m.InitialView, new InitialMapView(1, 2)));

        Assert.Equal(12, Options.GetProperty("zoom").GetDouble());
    }

    [Theory]
    [InlineData(MapStyle.Road, "road")]
    [InlineData(MapStyle.GrayscaleLight, "grayscale_light")]
    [InlineData(MapStyle.GrayscaleDark, "grayscale_dark")]
    [InlineData(MapStyle.Night, "night")]
    [InlineData(MapStyle.RoadShadedRelief, "road_shaded_relief")]
    [InlineData(MapStyle.Satellite, "satellite")]
    [InlineData(MapStyle.SatelliteRoadLabels, "satellite_road_labels")]
    [InlineData(MapStyle.HighContrastDark, "high_contrast_dark")]
    [InlineData(MapStyle.HighContrastLight, "high_contrast_light")]
    public void Every_style_is_sent_as_its_Azure_style_identifier(MapStyle style, string identifier)
    {
        RenderMap(p => p.Add(m => m.Style, style));

        Assert.Equal(identifier, Options.GetProperty("style").GetString());
    }

    [Fact]
    public void Every_style_is_sent_as_the_EnumMember_value_declared_on_the_enum()
    {
        foreach (MapStyle style in Enum.GetValues<MapStyle>())
        {
            string declared = typeof(MapStyle).GetField(style.ToString())!.GetCustomAttribute<EnumMemberAttribute>()!.Value!;
            Js.Module.ClearCalls();

            Render<AzureMap>(p => p.Add(m => m.SubscriptionKey, "k").Add(m => m.Style, style));

            Assert.Equal(declared, Js.CreateMapOptions.GetProperty("style").GetString());
        }
    }

    [Fact]
    public void Every_control_position_is_sent_as_the_EnumMember_value_declared_on_the_enum()
    {
        foreach (MapControlPosition position in Enum.GetValues<MapControlPosition>())
        {
            string declared = typeof(MapControlPosition).GetField(position.ToString())!.GetCustomAttribute<EnumMemberAttribute>()!.Value!;
            Js.Module.ClearCalls();

            Render<AzureMap>(p => p.Add(m => m.SubscriptionKey, "k").Add(m => m.ZoomControl, new MapControlOption(true, position)));

            Assert.Equal(declared, Js.CreateMapOptions.GetProperty("controls").GetProperty("zoom").GetProperty("position").GetString());
        }
    }

    [Fact]
    public void Language_view_pitch_bearing_and_zoom_limits_are_sent()
    {
        RenderMap(p => p
            .Add(m => m.Language, "fr-FR")
            .Add(m => m.View, "IN")
            .Add(m => m.Pitch, 45)
            .Add(m => m.Bearing, 90)
            .Add(m => m.MinZoom, 3)
            .Add(m => m.MaxZoom, 17.5));

        Assert.Equal("fr-FR", Options.GetProperty("language").GetString());
        Assert.Equal("IN", Options.GetProperty("view").GetString());
        Assert.Equal(45, Options.GetProperty("pitch").GetDouble());
        Assert.Equal(90, Options.GetProperty("bearing").GetDouble());
        Assert.Equal(3, Options.GetProperty("minZoom").GetDouble());
        Assert.Equal(17.5, Options.GetProperty("maxZoom").GetDouble());
    }

    [Fact]
    public void Traffic_flags_are_sent()
    {
        RenderMap(p => p.Add(m => m.ShowTrafficFlow, true).Add(m => m.ShowTrafficIncidents, true));

        Assert.True(Options.GetProperty("showTrafficFlow").GetBoolean());
        Assert.True(Options.GetProperty("showTrafficIncidents").GetBoolean());
    }

    [Fact]
    public void Traffic_flow_and_incidents_are_independent()
    {
        RenderMap(p => p.Add(m => m.ShowTrafficIncidents, true));

        Assert.False(Options.GetProperty("showTrafficFlow").GetBoolean());
        Assert.True(Options.GetProperty("showTrafficIncidents").GetBoolean());
    }

    [Fact]
    public void Marker_behaviour_flags_are_sent()
    {
        RenderMap(p => p
            .Add(m => m.AllowMarkerRemoval, true)
            .Add(m => m.ShowMarkerPopups, false)
            .Add(m => m.ShowMarkerTitlesOnZoom, true)
            .Add(m => m.MarkerTitleMinZoom, 11));

        Assert.True(Options.GetProperty("allowMarkerRemoval").GetBoolean());
        Assert.False(Options.GetProperty("showMarkerPopups").GetBoolean());
        Assert.True(Options.GetProperty("showMarkerTitlesOnZoom").GetBoolean());
        Assert.Equal(11, Options.GetProperty("markerTitleMinZoom").GetDouble());
    }

    [Theory]
    [InlineData(MarkerAddTrigger.Disabled, "disabled")]
    [InlineData(MarkerAddTrigger.SingleClick, "single")]
    [InlineData(MarkerAddTrigger.DoubleClick, "double")]
    [InlineData(MarkerAddTrigger.CenterPin, "center")]
    public void The_add_marker_trigger_is_sent_as_a_short_word(MarkerAddTrigger trigger, string word)
    {
        RenderMap(p => p.Add(m => m.AddMarkerTrigger, trigger));

        Assert.Equal(word, Options.GetProperty("addMarkerTrigger").GetString());
    }

    [Fact]
    public void Interactive_false_is_sent()
    {
        RenderMap(p => p.Add(m => m.Interactive, false));

        Assert.False(Options.GetProperty("interactive").GetBoolean());
    }

    [Fact]
    public void Interaction_defaults_allow_everything_except_scroll_zoom()
    {
        RenderMap();

        JsonElement interactions = Options.GetProperty("interactions");

        Assert.True(interactions.GetProperty("dragPan").GetBoolean());
        Assert.True(interactions.GetProperty("dragRotate").GetBoolean());
        Assert.False(interactions.GetProperty("scrollZoom").GetBoolean());
        Assert.True(interactions.GetProperty("dblClickZoom").GetBoolean());
        Assert.True(interactions.GetProperty("boxZoom").GetBoolean());
        Assert.True(interactions.GetProperty("keyboard").GetBoolean());
        Assert.True(interactions.GetProperty("touch").GetBoolean());
    }

    [Theory]
    [InlineData("dragPan")]
    [InlineData("dragRotate")]
    [InlineData("scrollZoom")]
    [InlineData("dblClickZoom")]
    [InlineData("boxZoom")]
    [InlineData("keyboard")]
    [InlineData("touch")]
    public void Each_interaction_flag_is_wired_to_its_own_parameter(string flag)
    {
        // Flip one flag away from its default and check that only that one changed.
        bool[] defaults = [true, true, false, true, true, true, true];
        string[] names = ["dragPan", "dragRotate", "scrollZoom", "dblClickZoom", "boxZoom", "keyboard", "touch"];
        int index = Array.IndexOf(names, flag);

        RenderMap(p =>
        {
            bool value = !defaults[index];
            switch (flag)
            {
                case "dragPan": p.Add(m => m.DragPanInteraction, value); break;
                case "dragRotate": p.Add(m => m.DragRotateInteraction, value); break;
                case "scrollZoom": p.Add(m => m.ScrollZoomInteraction, value); break;
                case "dblClickZoom": p.Add(m => m.DblClickZoomInteraction, value); break;
                case "boxZoom": p.Add(m => m.BoxZoomInteraction, value); break;
                case "keyboard": p.Add(m => m.KeyboardInteraction, value); break;
                case "touch": p.Add(m => m.TouchInteraction, value); break;
            }
        });

        JsonElement interactions = Options.GetProperty("interactions");
        for (int i = 0; i < names.Length; i++)
            Assert.Equal(i == index ? !defaults[i] : defaults[i], interactions.GetProperty(names[i]).GetBoolean());
    }

    [Fact]
    public void Control_defaults_show_only_the_zoom_control()
    {
        RenderMap();

        JsonElement controls = Options.GetProperty("controls");

        AssertControl(controls, "zoom", true, "top-right");
        AssertControl(controls, "compass", false, "top-right");
        AssertControl(controls, "pitch", false, "top-right");
        AssertControl(controls, "style", false, "top-left");
        AssertControl(controls, "fullscreen", false, "top-right");
        AssertControl(controls, "scale", false, "bottom-left");
    }

    [Fact]
    public void Each_control_is_wired_to_its_own_parameter_with_its_position()
    {
        RenderMap(p => p
            .Add(m => m.ZoomControl, new MapControlOption(false, MapControlPosition.BottomRight))
            .Add(m => m.CompassControl, new MapControlOption(true, MapControlPosition.TopLeft))
            .Add(m => m.PitchControl, new MapControlOption(true, MapControlPosition.BottomLeft))
            .Add(m => m.StyleControl, new MapControlOption(true, MapControlPosition.BottomRight))
            .Add(m => m.FullscreenControl, new MapControlOption(true, MapControlPosition.TopLeft))
            .Add(m => m.ScaleControl, new MapControlOption(true, MapControlPosition.TopRight)));

        JsonElement controls = Options.GetProperty("controls");

        AssertControl(controls, "zoom", false, "bottom-right");
        AssertControl(controls, "compass", true, "top-left");
        AssertControl(controls, "pitch", true, "bottom-left");
        AssertControl(controls, "style", true, "bottom-right");
        AssertControl(controls, "fullscreen", true, "top-left");
        AssertControl(controls, "scale", true, "top-right");
    }

    private static void AssertControl(JsonElement controls, string name, bool enabled, string position)
    {
        JsonElement control = controls.GetProperty(name);
        Assert.Equal(enabled, control.GetProperty("enabled").GetBoolean());
        Assert.Equal(position, control.GetProperty("position").GetString());
    }

    [Fact]
    public void The_options_object_has_exactly_the_properties_the_JavaScript_contract_names()
    {
        RenderMap();

        string[] names = [.. Options.EnumerateObject().Select(p => p.Name).Order()];

        string[] expected =
        [
            "addMarkerTrigger", "allowMarkerRemoval", "bearing", "controls", "elementId", "interactions", "interactive", "language",
            "latitude", "longitude", "markerTitleMinZoom", "maxZoom", "minZoom", "pitch", "showMarkerPopups", "showMarkerTitlesOnZoom",
            "showTrafficFlow", "showTrafficIncidents", "style", "subscriptionKey", "view", "zoom"
        ];

        Assert.Equal(expected, names);
    }

    // ── Subscription key ────────────────────────────────────────────────────

    [Fact]
    public void The_SubscriptionKey_parameter_wins_over_the_key_registered_in_DI()
    {
        Services.AddAzureMaps("from-di");

        Render<AzureMap>(p => p.Add(m => m.SubscriptionKey, "from-parameter"));

        Assert.Equal("from-parameter", Options.GetProperty("subscriptionKey").GetString());
    }

    [Fact]
    public void Without_a_parameter_the_key_registered_with_AddAzureMaps_is_used()
    {
        Services.AddAzureMaps("from-di");

        Render<AzureMap>();

        Assert.Equal("from-di", Options.GetProperty("subscriptionKey").GetString());
    }

    [Fact]
    public void A_whitespace_parameter_falls_back_to_the_DI_key()
    {
        Services.AddAzureMaps("from-di");

        Render<AzureMap>(p => p.Add(m => m.SubscriptionKey, "   "));

        Assert.Equal("from-di", Options.GetProperty("subscriptionKey").GetString());
    }

    [Fact]
    public void Without_any_key_the_component_throws_the_documented_InvalidOperationException()
    {
        Exception thrown = Assert.ThrowsAny<Exception>(() => Render<AzureMap>());

        Assert.Equal(
            "Azure Maps subscription key is not configured. Provide it via the SubscriptionKey parameter or AddAzureMaps(...) in DI.",
            Unwrap(thrown).Message);
    }

    [Fact]
    public void An_empty_key_registered_in_DI_counts_as_missing()
    {
        Services.AddAzureMaps(options => options.SubscriptionKey = "");

        Exception exception = Assert.ThrowsAny<Exception>(() => Render<AzureMap>());

        Assert.Contains("subscription key is not configured", Unwrap(exception).Message);
    }

    [Fact]
    public void No_map_is_created_when_the_key_is_missing()
    {
        Assert.ThrowsAny<Exception>(() => Render<AzureMap>());

        Assert.False(Js.Module.WasCalled("createMap"));
    }

    private static InvalidOperationException Unwrap(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
            if (current is InvalidOperationException invalid && invalid.Message.Contains("subscription key"))
                return invalid;

        throw new Xunit.Sdk.XunitException("Expected an InvalidOperationException about the subscription key but got: " + exception);
    }
}
