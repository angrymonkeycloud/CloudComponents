using System.Text.Json;
using AngryMonkey.CloudComponents.Maps.Components;
using AngryMonkey.CloudComponents.Maps.Models;
using AngryMonkey.CloudComponents.Maps.Options;
using Bunit;

namespace CloudComponents.Tests.Maps.Component;

/// <summary>What the component tells an existing map when the host re-renders it with different parameters.</summary>
public sealed class AzureMapParameterChangeTests : AzureMapTestContext
{
    [Fact]
    public void Creating_the_map_sends_the_controller_nothing()
    {
        RenderMap();

        Assert.Empty(Controller.Calls);
    }

    [Fact]
    public void Re_rendering_with_the_same_parameters_sends_the_controller_nothing()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();

        cut.Render(p => p.Add(m => m.SubscriptionKey, "test-key"));
        cut.Render(p => p.Add(m => m.SubscriptionKey, "test-key"));

        Assert.Empty(Controller.Calls);
    }

    [Fact]
    public void Changing_only_the_size_does_not_touch_the_map()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();

        cut.Render(p => p.Add(m => m.Height, "900px"));

        Assert.Empty(Controller.Calls);
        Assert.Contains("height:900px", cut.Find("div.map-host").GetAttribute("style"));
    }

    [Fact]
    public void Changing_the_style_parameter_restyles_the_map()
    {
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.Style, MapStyle.Satellite));

        cut.Render(p => p.Add(m => m.Style, MapStyle.Night));

        JsCall call = Controller.Single("setStyle");
        Assert.Equal("""["night"]""", call.ArgsJson);
    }

    [Fact]
    public void Re_rendering_with_the_same_style_does_not_restyle_the_map()
    {
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.Style, MapStyle.Night));

        cut.Render(p => p.Add(m => m.Style, MapStyle.Night));

        Assert.False(Controller.WasCalled("setStyle"));
    }

    [Fact]
    public async Task A_style_picked_by_the_user_is_not_undone_when_the_host_re_renders_with_the_same_value()
    {
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.Style, MapStyle.Satellite));
        cut.Find(".map-style-switch button:first-child").Click();
        Controller.ClearCalls();

        await cut.InvokeAsync(() => cut.Render(p => p.Add(m => m.Style, MapStyle.Satellite)));
        cut.Render(p => p.Add(m => m.Height, "400px"));

        Assert.False(Controller.WasCalled("setStyle"));
        Assert.Equal("true", cut.Find(".map-style-switch button:first-child").GetAttribute("aria-pressed"));
    }

    [Fact]
    public void A_new_style_from_the_host_wins_over_the_users_earlier_choice()
    {
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.Style, MapStyle.Satellite));
        cut.Find(".map-style-switch button:first-child").Click();
        Controller.ClearCalls();

        cut.Render(p => p.Add(m => m.Style, MapStyle.Night));

        Assert.Equal("""["night"]""", Controller.Single("setStyle").ArgsJson);
    }

    [Fact]
    public void Changing_back_to_a_value_the_user_had_overridden_still_restyles()
    {
        // Host: Satellite -> user picks Road -> host sets Road: the host's change is a change even though the screen already shows Road.
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.Style, MapStyle.Satellite));
        cut.Find(".map-style-switch button:first-child").Click();
        Controller.ClearCalls();

        cut.Render(p => p.Add(m => m.Style, MapStyle.Road));

        Assert.Equal("""["road"]""", Controller.Single("setStyle").ArgsJson);
    }

    [Fact]
    public void Turning_traffic_on_sends_both_flags()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();

        cut.Render(p => p.Add(m => m.ShowTrafficFlow, true));

        Assert.Equal("[true,false]", Controller.Single("setTraffic").ArgsJson);
    }

    [Fact]
    public void Changing_only_the_incidents_flag_sends_both_flags_with_the_new_value()
    {
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.ShowTrafficFlow, true));

        cut.Render(p => p.Add(m => m.ShowTrafficFlow, true).Add(m => m.ShowTrafficIncidents, true));

        Assert.Equal("[true,true]", Controller.Single("setTraffic").ArgsJson);
    }

    [Fact]
    public void Unchanged_traffic_flags_are_not_resent()
    {
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.ShowTrafficFlow, true));

        cut.Render(p => p.Add(m => m.ShowTrafficFlow, true));

        Assert.False(Controller.WasCalled("setTraffic"));
    }

    [Fact]
    public void Traffic_turned_off_again_is_sent()
    {
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.ShowTrafficFlow, true).Add(m => m.ShowTrafficIncidents, true));

        cut.Render(p => p.Add(m => m.ShowTrafficFlow, false).Add(m => m.ShowTrafficIncidents, false));

        Assert.Equal("[false,false]", Controller.Single("setTraffic").ArgsJson);
    }

    [Fact]
    public void Changing_a_control_sends_all_six_controls_in_one_setControls_call()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();

        cut.Render(p => p.Add(m => m.CompassControl, new MapControlOption(true, MapControlPosition.BottomRight)));

        JsonElement controls = Controller.Single("setControls").Json(0);
        Assert.Equal(["compass", "fullscreen", "pitch", "scale", "style", "zoom"], controls.EnumerateObject().Select(c => c.Name).Order());
        Assert.True(controls.GetProperty("compass").GetProperty("enabled").GetBoolean());
        Assert.Equal("bottom-right", controls.GetProperty("compass").GetProperty("position").GetString());
        Assert.True(controls.GetProperty("zoom").GetProperty("enabled").GetBoolean());
    }

    [Fact]
    public void A_control_replaced_by_an_equal_value_is_not_resent()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();

        cut.Render(p => p.Add(m => m.ZoomControl, new MapControlOption(true, MapControlPosition.TopRight)));

        Assert.False(Controller.WasCalled("setControls"));
    }

    [Fact]
    public void Moving_a_control_to_another_corner_is_sent()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();

        cut.Render(p => p.Add(m => m.ZoomControl, new MapControlOption(true, MapControlPosition.BottomLeft)));

        Assert.Equal("bottom-left", Controller.Single("setControls").Json(0).GetProperty("zoom").GetProperty("position").GetString());
    }

    [Fact]
    public void Changing_an_interaction_flag_sends_all_seven_flags_in_one_setInteractions_call()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();

        cut.Render(p => p.Add(m => m.ScrollZoomInteraction, true).Add(m => m.TouchInteraction, false));

        JsonElement interactions = Controller.Single("setInteractions").Json(0);
        Assert.Equal(["boxZoom", "dblClickZoom", "dragPan", "dragRotate", "keyboard", "scrollZoom", "touch"], interactions.EnumerateObject().Select(c => c.Name).Order());
        Assert.True(interactions.GetProperty("scrollZoom").GetBoolean());
        Assert.False(interactions.GetProperty("touch").GetBoolean());
        Assert.True(interactions.GetProperty("dragPan").GetBoolean());
    }

    [Fact]
    public void Unchanged_interactions_are_not_resent()
    {
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.DragPanInteraction, false));

        cut.Render(p => p.Add(m => m.DragPanInteraction, false));

        Assert.False(Controller.WasCalled("setInteractions"));
    }

    [Fact]
    public void Changing_the_add_marker_trigger_is_sent_and_draws_the_centre_pin()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();
        Assert.Empty(cut.FindAll(".map-pin"));

        cut.Render(p => p.Add(m => m.AddMarkerTrigger, MarkerAddTrigger.CenterPin));

        Assert.Equal("""["center"]""", Controller.Single("setAddMarkerTrigger").ArgsJson);
        Assert.Single(cut.FindAll(".map-pin"));
    }

    [Fact]
    public void Switching_the_trigger_back_to_disabled_removes_the_centre_pin()
    {
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.AddMarkerTrigger, MarkerAddTrigger.CenterPin));

        cut.Render(p => p.Add(m => m.AddMarkerTrigger, MarkerAddTrigger.Disabled));

        Assert.Equal("""["disabled"]""", Controller.Single("setAddMarkerTrigger").ArgsJson);
        Assert.Empty(cut.FindAll(".map-pin"));
    }

    [Fact]
    public void An_unchanged_add_marker_trigger_is_not_resent()
    {
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.AddMarkerTrigger, MarkerAddTrigger.SingleClick));

        cut.Render(p => p.Add(m => m.AddMarkerTrigger, MarkerAddTrigger.SingleClick));

        Assert.False(Controller.WasCalled("setAddMarkerTrigger"));
    }

    [Fact]
    public async Task Markers_and_regions_only_seed_the_map_changing_them_later_does_not_touch_it()
    {
        IRenderedComponent<AzureMap> cut = await RenderReadyMapAsync(p => p.Add(m => m.Markers, new[] { new MapMarker(1, 1) { Id = "a" } }));
        Controller.ClearCalls();

        cut.Render(p => p
            .Add(m => m.Markers, new[] { new MapMarker(2, 2) { Id = "b" } })
            .Add(m => m.Regions, new[] { new MapRegion([[[0, 0]]]) }));

        Assert.Empty(Controller.Calls);
        Assert.Equal(["a"], cut.Instance.CurrentMarkers.Keys);
    }

    [Fact]
    public void The_pin_shows_the_loading_class_while_a_location_is_being_fetched()
    {
        Locator.Permission = AngryMonkey.CloudComponents.Maps.Services.LocationPermissionState.Granted;
        Locator.LocationGate = new TaskCompletionSource<(double Lat, double Lng)?>();

        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.AddMarkerTrigger, MarkerAddTrigger.CenterPin));

        cut.WaitForAssertion(() => Assert.Contains("_loading", cut.Find(".map-pin").ClassName));

        Locator.LocationGate.SetResult(null);
        cut.WaitForAssertion(() => Assert.DoesNotContain("_loading", cut.Find(".map-pin").ClassName));
    }
}
