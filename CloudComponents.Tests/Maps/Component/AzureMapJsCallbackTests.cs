using AngryMonkey.CloudComponents.Maps.Components;
using AngryMonkey.CloudComponents.Maps.Models;
using AngryMonkey.CloudComponents.Maps.Options;
using Bunit;

namespace CloudComponents.Tests.Maps.Component;

/// <summary>
/// The calls JavaScript makes into the component (<c>invokeMethodAsync</c>) and the events they raise. Each test calls the
/// [JSInvokable] method by the name JavaScript uses, with arguments crossing as JSON.
/// </summary>
public sealed class AzureMapJsCallbackTests : AzureMapTestContext
{
    private readonly List<MapCoordinate> _clicks = [];
    private readonly List<MapCoordinate> _doubleClicks = [];
    private readonly List<MapCoordinate> _longPresses = [];
    private readonly List<MapCoordinate> _pinChanges = [];
    private readonly List<MapMarker> _added = [];
    private readonly List<MapMarker> _clicked = [];
    private readonly List<MapMarker> _removed = [];
    private readonly List<string> _errors = [];
    private int _ready;

    private IRenderedComponent<AzureMap> RenderWithHandlers(Action<ComponentParameterCollectionBuilder<AzureMap>>? configure = null) =>
        RenderMap(p =>
        {
            p.Add(m => m.OnMapClick, (MapCoordinate c) => _clicks.Add(c));
            p.Add(m => m.OnMapDoubleClick, (MapCoordinate c) => _doubleClicks.Add(c));
            p.Add(m => m.OnMapLongPress, (MapCoordinate c) => _longPresses.Add(c));
            p.Add(m => m.OnCenterPinChanged, (MapCoordinate c) => _pinChanges.Add(c));
            p.Add(m => m.OnMarkerAdded, (MapMarker m) => _added.Add(m));
            p.Add(m => m.OnMarkerClick, (MapMarker m) => _clicked.Add(m));
            p.Add(m => m.OnMarkerRemoved, (MapMarker m) => _removed.Add(m));
            p.Add(m => m.OnMapError, (string message) => _errors.Add(message));
            p.Add(m => m.OnMapReady, () => _ready++);
            configure?.Invoke(p);
        });

    [Fact]
    public void Every_callback_name_the_JavaScript_invokes_exists_as_a_JSInvokable_method()
    {
        string[] names =
        [
            "NotifyMapReadyAsync", "NotifyMapClickAsync", "NotifyMapDoubleClickAsync", "NotifyMapLongPressAsync", "NotifyMapAddMarkerAsync",
            "NotifyMarkerClickAsync", "NotifyMarkerRemovedAsync", "NotifyMapErrorAsync", "NotifyCenterPinChangedAsync", "NotifyStyleChangedAsync",
            "NotifyLocationLockRejectedAsync", "NotifyTimelinePointClickAsync"
        ];

        foreach (string name in names)
            Assert.True(FakeMapJs.Invokables.ContainsKey(name), $"AzureMap has no [JSInvokable] method '{name}'");

        Assert.Equal(names.Order(), FakeMapJs.Invokables.Keys.Order());
    }

    [Fact]
    public async Task A_map_click_raises_OnMapClick_with_the_coordinate()
    {
        RenderWithHandlers();

        await Js.InvokeFromJsAsync("NotifyMapClickAsync", 33.5, 35.25);

        Assert.Equal([new MapCoordinate(33.5, 35.25)], _clicks);
    }

    [Fact]
    public async Task A_map_click_keeps_latitude_and_longitude_in_that_order()
    {
        RenderWithHandlers();

        await Js.InvokeFromJsAsync("NotifyMapClickAsync", 10.0, 20.0);

        MapCoordinate click = Assert.Single(_clicks);
        Assert.Equal(10.0, click.Latitude);
        Assert.Equal(20.0, click.Longitude);
    }

    [Fact]
    public async Task A_double_click_raises_only_OnMapDoubleClick()
    {
        RenderWithHandlers();

        await Js.InvokeFromJsAsync("NotifyMapDoubleClickAsync", 1.5, 2.5);

        Assert.Equal([new MapCoordinate(1.5, 2.5)], _doubleClicks);
        Assert.Empty(_clicks);
    }

    [Fact]
    public async Task A_long_press_raises_only_OnMapLongPress()
    {
        RenderWithHandlers();

        await Js.InvokeFromJsAsync("NotifyMapLongPressAsync", -4.0, 7.0);

        Assert.Equal([new MapCoordinate(-4, 7)], _longPresses);
        Assert.Empty(_doubleClicks);
        Assert.Empty(_clicks);
    }

    [Fact]
    public async Task A_centre_pin_move_raises_OnCenterPinChanged()
    {
        RenderWithHandlers(p => p.Add(m => m.AddMarkerTrigger, MarkerAddTrigger.CenterPin));

        await Js.InvokeFromJsAsync("NotifyCenterPinChangedAsync", 12.25, 34.75);

        Assert.Equal([new MapCoordinate(12.25, 34.75)], _pinChanges);
    }

    [Fact]
    public async Task A_map_error_raises_OnMapError_with_the_message()
    {
        RenderWithHandlers();

        await Js.InvokeFromJsAsync("NotifyMapErrorAsync", "Azure Maps: 401 Unauthorized");

        Assert.Equal(["Azure Maps: 401 Unauthorized"], _errors);
    }

    [Fact]
    public async Task Callbacks_without_a_handler_are_harmless()
    {
        RenderMap();

        await Js.InvokeFromJsAsync("NotifyMapClickAsync", 1.0, 2.0);
        await Js.InvokeFromJsAsync("NotifyMapDoubleClickAsync", 1.0, 2.0);
        await Js.InvokeFromJsAsync("NotifyMapLongPressAsync", 1.0, 2.0);
        await Js.InvokeFromJsAsync("NotifyCenterPinChangedAsync", 1.0, 2.0);
        await Js.InvokeFromJsAsync("NotifyMapErrorAsync", "x");
        await Js.InvokeFromJsAsync("NotifyMapReadyAsync");
    }

    // ── Ready ───────────────────────────────────────────────────────────────

    [Fact]
    public void The_map_is_not_ready_until_JavaScript_says_so()
    {
        IRenderedComponent<AzureMap> cut = RenderWithHandlers();

        Assert.False(cut.Instance.IsReady);
        Assert.Equal(0, _ready);
    }

    [Fact]
    public async Task Ready_sets_IsReady_and_raises_OnMapReady_once()
    {
        IRenderedComponent<AzureMap> cut = RenderWithHandlers();

        await Js.InvokeFromJsAsync("NotifyMapReadyAsync");

        Assert.True(cut.Instance.IsReady);
        Assert.Equal(1, _ready);
    }

    [Fact]
    public async Task Ready_with_no_markers_regions_or_zones_sends_the_controller_nothing()
    {
        RenderWithHandlers();

        await Js.InvokeFromJsAsync("NotifyMapReadyAsync");

        Assert.Empty(Controller.Calls);
    }

    [Fact]
    public async Task Ready_adds_the_Markers_parameter_to_the_map_in_order()
    {
        MapMarker first = new(1, 2, "one") { Id = "a" };
        MapMarker second = new(3, 4, "two") { Id = "b" };
        IRenderedComponent<AzureMap> cut = RenderWithHandlers(p => p.Add(m => m.Markers, new[] { first, second }));
        Assert.False(Controller.WasCalled("addMarker"));

        await Js.InvokeFromJsAsync("NotifyMapReadyAsync");

        Assert.Equal(["a", "b"], Controller.CallsTo("addMarker").Select(c => c.Json(0).GetProperty("id").GetString()));
        Assert.Equal(["a", "b"], cut.Instance.CurrentMarkers.Keys);
    }

    [Fact]
    public async Task Ready_adds_the_Regions_parameter_to_the_map()
    {
        MapRegion region = new([[[0, 0], [0, 1], [1, 1]]]) { Id = "r1", Label = "R" };
        RenderWithHandlers(p => p.Add(m => m.Regions, new[] { region }));

        await Js.InvokeFromJsAsync("NotifyMapReadyAsync");

        Assert.Equal("r1", Controller.Single("addRegion").Json(0).GetProperty("id").GetString());
    }

    [Fact]
    public async Task Ready_with_LocateOnOpen_asks_the_location_service_and_centres_the_map()
    {
        Locator.Location = (33.0, 35.0);
        Locator.Permission = AngryMonkey.CloudComponents.Maps.Services.LocationPermissionState.Granted;
        RenderWithHandlers(p => p.Add(m => m.InitialView, new InitialMapView(0, 0)).Add(m => m.LocateOnOpen, true));

        await Js.InvokeFromJsAsync("NotifyMapReadyAsync");

        Assert.Equal("[33,35]", Controller.Single("showCurrentLocation").ArgsJson);
    }

    [Fact]
    public async Task Ready_without_LocateOnOpen_does_not_ask_for_a_location()
    {
        Locator.Location = (33.0, 35.0);
        RenderWithHandlers(p => p.Add(m => m.InitialView, new InitialMapView(0, 0)));

        await Js.InvokeFromJsAsync("NotifyMapReadyAsync");

        Assert.Equal(0, Locator.LocationRequests);
        Assert.False(Controller.WasCalled("showCurrentLocation"));
    }

    // ── Marker rules ────────────────────────────────────────────────────────

    [Theory]
    [InlineData(MarkerAddTrigger.SingleClick)]
    [InlineData(MarkerAddTrigger.DoubleClick)]
    [InlineData(MarkerAddTrigger.CenterPin)]
    public async Task JavaScript_asking_to_add_a_marker_adds_it_when_a_trigger_is_enabled(MarkerAddTrigger trigger)
    {
        IRenderedComponent<AzureMap> cut = RenderWithHandlers(p => p.Add(m => m.AddMarkerTrigger, trigger));

        await Js.InvokeFromJsAsync("NotifyMapAddMarkerAsync", 33.5, 35.5);

        MapMarker marker = Assert.Single(_added);
        Assert.Equal(33.5, marker.Latitude);
        Assert.Equal(35.5, marker.Longitude);
        Assert.Equal(marker, Assert.Single(cut.Instance.CurrentMarkers).Value);
        Assert.Equal(marker.Id, Controller.Single("addMarker").Json(0).GetProperty("id").GetString());
    }

    [Fact]
    public async Task JavaScript_asking_to_add_a_marker_is_ignored_when_adding_is_disabled()
    {
        IRenderedComponent<AzureMap> cut = RenderWithHandlers();

        await Js.InvokeFromJsAsync("NotifyMapAddMarkerAsync", 33.5, 35.5);

        Assert.Empty(_added);
        Assert.Empty(cut.Instance.CurrentMarkers);
        Assert.False(Controller.WasCalled("addMarker"));
    }

    [Fact]
    public async Task A_marker_added_by_the_user_gets_a_fresh_id_each_time()
    {
        RenderWithHandlers(p => p.Add(m => m.AddMarkerTrigger, MarkerAddTrigger.SingleClick));

        await Js.InvokeFromJsAsync("NotifyMapAddMarkerAsync", 1.0, 1.0);
        await Js.InvokeFromJsAsync("NotifyMapAddMarkerAsync", 1.0, 1.0);

        Assert.Equal(2, _added.Select(m => m.Id).Distinct().Count());
    }

    [Fact]
    public async Task A_marker_click_raises_OnMarkerClick_with_the_stored_marker()
    {
        IRenderedComponent<AzureMap> cut = RenderWithHandlers();
        MapMarker marker = new(5, 6, "Office") { Id = "m1" };
        await cut.InvokeAsync(() => cut.Instance.AddMarkerAsync(marker));

        await Js.InvokeFromJsAsync("NotifyMarkerClickAsync", "m1");

        Assert.Same(marker, Assert.Single(_clicked));
    }

    [Fact]
    public async Task A_click_on_an_unknown_marker_id_is_ignored()
    {
        RenderWithHandlers();

        await Js.InvokeFromJsAsync("NotifyMarkerClickAsync", "ghost");

        Assert.Empty(_clicked);
    }

    [Fact]
    public async Task The_marker_click_is_still_raised_when_the_built_in_popup_is_turned_off()
    {
        IRenderedComponent<AzureMap> cut = RenderWithHandlers(p => p.Add(m => m.ShowMarkerPopups, false));
        await cut.InvokeAsync(() => cut.Instance.AddMarkerAsync(new MapMarker(1, 1) { Id = "m1" }));

        await Js.InvokeFromJsAsync("NotifyMarkerClickAsync", "m1");

        Assert.Single(_clicked);
        Assert.False(Js.CreateMapOptions.GetProperty("showMarkerPopups").GetBoolean());
    }

    [Fact]
    public async Task A_marker_removed_by_the_user_is_forgotten_and_raises_OnMarkerRemoved()
    {
        IRenderedComponent<AzureMap> cut = RenderWithHandlers(p => p.Add(m => m.AllowMarkerRemoval, true));
        MapMarker marker = new(5, 6) { Id = "m1" };
        await cut.InvokeAsync(() => cut.Instance.AddMarkerAsync(marker));

        await Js.InvokeFromJsAsync("NotifyMarkerRemovedAsync", "m1");

        Assert.Same(marker, Assert.Single(_removed));
        Assert.Empty(cut.Instance.CurrentMarkers);
        Assert.True(Js.CreateMapOptions.GetProperty("allowMarkerRemoval").GetBoolean());
    }

    [Fact]
    public async Task A_removal_reported_by_JavaScript_does_not_send_removeMarker_back()
    {
        IRenderedComponent<AzureMap> cut = RenderWithHandlers();
        await cut.InvokeAsync(() => cut.Instance.AddMarkerAsync(new MapMarker(5, 6) { Id = "m1" }));
        Controller.ClearCalls();

        await Js.InvokeFromJsAsync("NotifyMarkerRemovedAsync", "m1");

        Assert.False(Controller.WasCalled("removeMarker"));
    }

    [Fact]
    public async Task A_removal_of_an_unknown_marker_id_raises_nothing()
    {
        RenderWithHandlers();

        await Js.InvokeFromJsAsync("NotifyMarkerRemovedAsync", "ghost");

        Assert.Empty(_removed);
    }

    [Fact]
    public async Task A_marker_removed_by_the_user_cannot_be_clicked_afterwards()
    {
        IRenderedComponent<AzureMap> cut = RenderWithHandlers();
        await cut.InvokeAsync(() => cut.Instance.AddMarkerAsync(new MapMarker(5, 6) { Id = "m1" }));
        await Js.InvokeFromJsAsync("NotifyMarkerRemovedAsync", "m1");

        await Js.InvokeFromJsAsync("NotifyMarkerClickAsync", "m1");

        Assert.Empty(_clicked);
    }
}
