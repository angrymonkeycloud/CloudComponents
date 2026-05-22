using CloudComponents.Map.Models;
using CloudComponents.Map.Options;
using CloudComponents.Map.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace CloudComponents.Map.Components;

/// <summary>
/// Azure Maps Blazor component. Encapsulates the Azure Maps Web SDK behind a
/// strongly-typed C# surface with full options, runtime setters, and events.
/// </summary>
public partial class AzureMap : ComponentBase, IAsyncDisposable
{
    private const string ModulePath = "./_content/CloudComponents.Map/mapInterop.js";

    private IJSObjectReference? _module;
    private IJSObjectReference? _controller;
    private DotNetObjectReference<AzureMap>? _selfRef;
    private bool _isLocating;
    private bool _isInitializing = true;
    private bool _showPermissionPrompt;
    private TaskCompletionSource<bool>? _permissionPromptTcs;
    private MapStyle _appliedStyle;
    private bool _appliedTrafficFlow;
    private bool _appliedTrafficIncidents;

    private readonly Dictionary<string, MapMarker> _markersById = new();

    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private IOptions<AzureMapsOptions>? GlobalOptions { get; set; }

    /// <summary>Optional. When registered, the built-in "locate me" button is enabled.</summary>
    [Inject] private ILocationService? Locator { get; set; }

    /// <summary>Unique DOM id for the map container.</summary>
    public string MapId { get; } = $"azmap-{Guid.NewGuid():N}";

    /// <summary>
    /// Subscription key. Optional — when omitted, the value is taken from
    /// <see cref="AzureMapsOptions"/> registered via <c>AddAzureMaps(...)</c>.
    /// </summary>
    [Parameter] public string? SubscriptionKey { get; set; }

    [Parameter] public string Height { get; set; } = "500px";
    [Parameter] public string Width { get; set; } = "100%";

    /// <summary>
    /// Initial camera (center + zoom). When <c>null</c>, the component asks the
    /// browser for the current location; if that fails, it falls back to
    /// <see cref="InitialMapView.World"/>.
    /// </summary>
    [Parameter] public InitialMapView? InitialView { get; set; }

    [Parameter] public MapStyle Style { get; set; } = MapStyle.Satellite;
    [Parameter] public string Language { get; set; } = "en-US";
    [Parameter] public string View { get; set; } = "Auto";

    [Parameter] public double Pitch { get; set; } = 0;
    [Parameter] public double Bearing { get; set; } = 0;
    [Parameter] public double? MinZoom { get; set; }
    [Parameter] public double? MaxZoom { get; set; }

    [Parameter] public bool ShowTrafficFlow { get; set; }
    [Parameter] public bool ShowTrafficIncidents { get; set; }

    [Parameter] public MapControlOption ZoomControl { get; set; } = new(true, MapControlPosition.TopRight);
    [Parameter] public MapControlOption CompassControl { get; set; } = new(false, MapControlPosition.TopRight);
    [Parameter] public MapControlOption PitchControl { get; set; } = new(false, MapControlPosition.TopRight);
    [Parameter] public MapControlOption StyleControl { get; set; } = new(false, MapControlPosition.TopLeft);
    [Parameter] public MapControlOption FullscreenControl { get; set; } = new(false, MapControlPosition.TopRight);
    [Parameter] public MapControlOption ScaleControl { get; set; } = new(false, MapControlPosition.BottomLeft);

    [Parameter] public bool Interactive { get; set; } = true;
    [Parameter] public bool DragPanInteraction { get; set; } = true;
    [Parameter] public bool DragRotateInteraction { get; set; } = true;
    [Parameter] public bool ScrollZoomInteraction { get; set; } = true;
    [Parameter] public bool DblClickZoomInteraction { get; set; } = false;
    [Parameter] public bool BoxZoomInteraction { get; set; } = true;
    [Parameter] public bool KeyboardInteraction { get; set; } = true;
    [Parameter] public bool TouchInteraction { get; set; } = true;

    [Parameter] public IReadOnlyList<MapMarker>? Markers { get; set; }

    /// <summary>How users add markers by interacting with the map. Default: <see cref="MarkerAddTrigger.DoubleClick"/>.</summary>
    [Parameter] public MarkerAddTrigger AddMarkerTrigger { get; set; } = MarkerAddTrigger.DoubleClick;

    /// <summary>If true, double-clicking a marker removes it.</summary>
    [Parameter] public bool AllowMarkerRemoval { get; set; } = true;

    [Parameter] public bool ShowCurrentLocationButton { get; set; } = true;
    [Parameter] public bool LocateOnOpen { get; set; } = false;

    /// <summary>Title shown in the location-permission popup.</summary>
    [Parameter] public string LocationPromptTitle { get; set; } = "Use your location?";

    /// <summary>Message shown in the location-permission popup.</summary>
    [Parameter] public string LocationPromptMessage { get; set; } =
        "Allow this app to access your current location so the map can center on you.";

    [Parameter] public string LocationPromptConfirmText { get; set; } = "Allow";
    [Parameter] public string LocationPromptCancelText { get; set; } = "Cancel";

    [Parameter] public EventCallback OnMapReady { get; set; }
    [Parameter] public EventCallback<MapCoordinate> OnMapClick { get; set; }
    [Parameter] public EventCallback<MapMarker> OnMarkerAdded { get; set; }
    [Parameter] public EventCallback<MapMarker> OnMarkerClick { get; set; }
    [Parameter] public EventCallback<MapMarker> OnMarkerRemoved { get; set; }
    [Parameter] public EventCallback<MapCoordinate> OnCenterPinChanged { get; set; }
    [Parameter] public EventCallback<string> OnMapError { get; set; }

    public bool IsReady { get; private set; }
    public IReadOnlyDictionary<string, MapMarker> CurrentMarkers => _markersById;

    private string ResolvedKey =>
        !string.IsNullOrWhiteSpace(SubscriptionKey)
            ? SubscriptionKey!
            : GlobalOptions?.Value.SubscriptionKey is { Length: > 0 } k
                ? k
                : throw new InvalidOperationException(
                    "Azure Maps subscription key is not configured. Provide it via the SubscriptionKey parameter or AddAzureMaps(...) in DI.");

    // ── Lifecycle ────────────────────────────────────────────────────────

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        // Render the map immediately so the user is never staring at a blank
        // surface. If geolocation is needed, we handle it asynchronously and
        // recenter the camera when (and if) coordinates arrive.
        var startView = InitialView ?? InitialMapView.World;

        _selfRef = DotNetObjectReference.Create(this);
        _module = await JS.InvokeAsync<IJSObjectReference>("import", ModulePath);

        var options = BuildMapOptions(startView);
        _controller = await _module.InvokeAsync<IJSObjectReference>("createMap", _selfRef, options);
        _appliedStyle = Style;
        _appliedTrafficFlow = ShowTrafficFlow;
        _appliedTrafficIncidents = ShowTrafficIncidents;
        _isInitializing = false;
        StateHasChanged();

        // Start the consent + locate flow only when the caller did not give
        // us an explicit initial view.
        if (InitialView is null && Locator is not null)
            _ = TryAutoLocateAsync();
    }

    /// <summary>
    /// Background flow: probe permission, show our consent popup if needed,
    /// then fetch the location and recenter the already-rendered map.
    /// Never blocks the initial render.
    /// </summary>
    private async Task TryAutoLocateAsync()
    {
        if (Locator is null) return;

        var permission = await Locator.GetPermissionStateAsync();
        if (permission is LocationPermissionState.Unsupported or LocationPermissionState.Denied)
            return;

        if (permission != LocationPermissionState.Granted)
        {
            var allow = await RequestLocationConsentAsync();
            if (!allow) return;
        }

        _isLocating = true;
        StateHasChanged();
        try
        {
            var loc = await Locator.GetCurrentLocationAsync();
            if (loc is { } p && _controller is not null)
                await _controller.InvokeVoidAsync("showCurrentLocation", p.Lat, p.Lng);
        }
        finally
        {
            _isLocating = false;
            StateHasChanged();
        }
    }

    private Task<bool> RequestLocationConsentAsync()
    {
        _permissionPromptTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _showPermissionPrompt = true;
        StateHasChanged();
        return _permissionPromptTcs.Task;
    }

    private void ConfirmLocationConsent()
    {
        _showPermissionPrompt = false;
        _permissionPromptTcs?.TrySetResult(true);
    }

    private void CancelLocationConsent()
    {
        _showPermissionPrompt = false;
        _permissionPromptTcs?.TrySetResult(false);
    }

    // Bridges PopupComp's two-way IsOpen binding to our cancel path so that
    // dismissing the popup via overlay click / back-button counts as "cancel".
    private void OnPermissionPopupOpenChanged(bool isOpen)
    {
        if (isOpen) return;
        if (_permissionPromptTcs is { Task.IsCompleted: false })
            _permissionPromptTcs.TrySetResult(false);
        _showPermissionPrompt = false;
    }

    protected override async Task OnParametersSetAsync()
    {
        if (_controller is null) return;

        // Always push the style through to JS. The SDK is idempotent, and a
        // local cache can drift when the user changes style via the in-map
        // StyleControl, causing later C#-driven changes to be no-ops.
        if (_appliedStyle != Style)
        {
            _appliedStyle = Style;
            await _controller.InvokeVoidAsync("setStyle", StyleToString(Style));
        }

        if (_appliedTrafficFlow != ShowTrafficFlow || _appliedTrafficIncidents != ShowTrafficIncidents)
        {
            _appliedTrafficFlow = ShowTrafficFlow;
            _appliedTrafficIncidents = ShowTrafficIncidents;
            await _controller.InvokeVoidAsync("setTraffic", ShowTrafficFlow, ShowTrafficIncidents);
        }
    }

    private object BuildMapOptions(InitialMapView view) => new
    {
        elementId = MapId,
        subscriptionKey = ResolvedKey,
        latitude = view.Latitude,
        longitude = view.Longitude,
        zoom = view.Zoom,
        style = StyleToString(Style),
        language = Language,
        view = View,
        pitch = Pitch,
        bearing = Bearing,
        minZoom = MinZoom,
        maxZoom = MaxZoom,
        showTrafficFlow = ShowTrafficFlow,
        showTrafficIncidents = ShowTrafficIncidents,
        allowMarkerRemoval = AllowMarkerRemoval,
        addMarkerTrigger = TriggerToString(AddMarkerTrigger),
        interactive = Interactive,
        interactions = new
        {
            dragPan = DragPanInteraction,
            dragRotate = DragRotateInteraction,
            scrollZoom = ScrollZoomInteraction,
            dblClickZoom = DblClickZoomInteraction,
            boxZoom = BoxZoomInteraction,
            keyboard = KeyboardInteraction,
            touch = TouchInteraction
        },
        controls = new
        {
            zoom = ControlToJs(ZoomControl),
            compass = ControlToJs(CompassControl),
            pitch = ControlToJs(PitchControl),
            style = ControlToJs(StyleControl),
            fullscreen = ControlToJs(FullscreenControl),
            scale = ControlToJs(ScaleControl)
        }
    };

    private static object ControlToJs(MapControlOption c) => new
    {
        enabled = c.Enabled,
        position = PositionToString(c.Position)
    };

    private static string TriggerToString(MarkerAddTrigger t) => t switch
    {
        MarkerAddTrigger.SingleClick => "single",
        MarkerAddTrigger.DoubleClick => "double",
        MarkerAddTrigger.CenterPin => "center",
        _ => "disabled"
    };

    private static string StyleToString(MapStyle s) => s switch
    {
        MapStyle.Road => "road",
        MapStyle.GrayscaleLight => "grayscale_light",
        MapStyle.GrayscaleDark => "grayscale_dark",
        MapStyle.Night => "night",
        MapStyle.RoadShadedRelief => "road_shaded_relief",
        MapStyle.Satellite => "satellite",
        MapStyle.SatelliteRoadLabels => "satellite_road_labels",
        MapStyle.HighContrastDark => "high_contrast_dark",
        MapStyle.HighContrastLight => "high_contrast_light",
        _ => "road"
    };

    private static string PositionToString(MapControlPosition p) => p switch
    {
        MapControlPosition.TopLeft => "top-left",
        MapControlPosition.TopRight => "top-right",
        MapControlPosition.BottomLeft => "bottom-left",
        MapControlPosition.BottomRight => "bottom-right",
        _ => "top-right"
    };

    // ?? Public API ??????????????????????????????????????????????????????????

    public async Task AddMarkerAsync(MapMarker marker)
    {
        var c = EnsureController();
        _markersById[marker.Id] = marker;
        await c.InvokeVoidAsync("addMarker", marker);
    }

    public async Task AddMarkersAsync(IEnumerable<MapMarker> markers)
    {
        var c = EnsureController();
        foreach (var m in markers)
        {
            _markersById[m.Id] = m;
            await c.InvokeVoidAsync("addMarker", m);
        }
    }

    public async Task RemoveMarkerAsync(string id)
    {
        if (_controller is null) return;
        if (_markersById.Remove(id))
            await _controller.InvokeVoidAsync("removeMarker", id);
    }

    public async Task ClearMarkersAsync()
    {
        if (_controller is null) return;
        _markersById.Clear();
        await _controller.InvokeVoidAsync("clearMarkers");
    }

    public async Task SetCenterAsync(double latitude, double longitude, int? zoom = null)
    {
        var c = EnsureController();
        await c.InvokeVoidAsync("setCenter", latitude, longitude, zoom);
    }

    public async Task SetStyleAsync(MapStyle style)
    {
        var c = EnsureController();
        await c.InvokeVoidAsync("setStyle", StyleToString(style));
    }

    public async Task SetTrafficAsync(bool showFlow, bool showIncidents)
    {
        var c = EnsureController();
        await c.InvokeVoidAsync("setTraffic", showFlow, showIncidents);
    }

    public async Task SetCameraOrientationAsync(double? pitch = null, double? bearing = null)
    {
        var c = EnsureController();
        await c.InvokeVoidAsync("setCameraOrientation", pitch, bearing);
    }

    public async Task SetBoundsAsync(double south, double west, double north, double east, int paddingPx = 40)
    {
        var c = EnsureController();
        await c.InvokeVoidAsync("setBounds", south, west, north, east, paddingPx);
    }

    public async Task ShowCurrentLocationAsync(double latitude, double longitude)
    {
        var c = EnsureController();
        await c.InvokeVoidAsync("showCurrentLocation", latitude, longitude);
    }

    private async Task LocateMeAsync()
    {
        if (Locator is null || _isLocating) return;

        _isLocating = true;
        try
        {
            var loc = await Locator.GetCurrentLocationAsync();
            if (loc is { } p)
                await ShowCurrentLocationAsync(p.Lat, p.Lng);
        }
        finally
        {
            _isLocating = false;
        }
    }

    // ?? JS callbacks ????????????????????????????????????????????????????????

    [JSInvokable]
    public async Task NotifyMapReadyAsync()
    {
        IsReady = true;
        await OnMapReady.InvokeAsync();

        if (Markers is { Count: > 0 })
            await AddMarkersAsync(Markers);

        if (LocateOnOpen)
            await LocateMeAsync();
    }

    [JSInvokable]
    public Task NotifyMapClickAsync(double latitude, double longitude)
        => OnMapClick.InvokeAsync(new MapCoordinate(latitude, longitude));

    [JSInvokable]
    public async Task NotifyMapAddMarkerAsync(double latitude, double longitude)
    {
        if (AddMarkerTrigger == MarkerAddTrigger.Disabled) return;
        var marker = new MapMarker(latitude, longitude);
        await AddMarkerAsync(marker);
        await OnMarkerAdded.InvokeAsync(marker);
    }

    [JSInvokable]
    public async Task NotifyMarkerClickAsync(string id)
    {
        if (_markersById.TryGetValue(id, out var marker))
            await OnMarkerClick.InvokeAsync(marker);
    }

    [JSInvokable]
    public async Task NotifyMarkerRemovedAsync(string id)
    {
        if (_markersById.Remove(id, out var marker))
            await OnMarkerRemoved.InvokeAsync(marker);
    }

    [JSInvokable]
    public Task NotifyMapErrorAsync(string message)
        => OnMapError.InvokeAsync(message);

    [JSInvokable]
    public Task NotifyCenterPinChangedAsync(double latitude, double longitude)
        => OnCenterPinChanged.InvokeAsync(new MapCoordinate(latitude, longitude));

    /// <summary>
    /// Invoked by JS when the SDK's style changes for any reason (including the
    /// user picking a value from the in-map StyleControl). Keeps the local
    /// cache in sync so a subsequent C#-driven style change is honored.
    /// </summary>
    [JSInvokable]
    public Task NotifyStyleChangedAsync(string style)
    {
        _appliedStyle = StyleFromString(style);
        return Task.CompletedTask;
    }

    private static MapStyle StyleFromString(string s) => s switch
    {
        "road" => MapStyle.Road,
        "grayscale_light" => MapStyle.GrayscaleLight,
        "grayscale_dark" => MapStyle.GrayscaleDark,
        "night" => MapStyle.Night,
        "road_shaded_relief" => MapStyle.RoadShadedRelief,
        "satellite" => MapStyle.Satellite,
        "satellite_road_labels" => MapStyle.SatelliteRoadLabels,
        "high_contrast_dark" => MapStyle.HighContrastDark,
        "high_contrast_light" => MapStyle.HighContrastLight,
        _ => MapStyle.Road
    };

    private IJSObjectReference EnsureController()
        => _controller ?? throw new InvalidOperationException("Map has not been initialized yet.");

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_controller is not null)
            {
                await _controller.InvokeVoidAsync("dispose");
                await _controller.DisposeAsync();
            }
            if (_module is not null)
                await _module.DisposeAsync();
        }
        catch (JSDisconnectedException) { /* navigation */ }
        catch (Exception) { /* best-effort */ }
        finally
        {
            _selfRef?.Dispose();
        }
    }
}
