# CloudComponents.Maps

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/UI-Blazor-5C2D91?logo=blazor)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![Azure Maps](https://img.shields.io/badge/Maps-Azure%20Maps-0078D4?logo=microsoftazure)](https://azure.microsoft.com/products/azure-maps)
[![JS Interop](https://img.shields.io/badge/Interop-JS%20Isolation-0EA5E9)](https://learn.microsoft.com/aspnet/core/blazor/javascript-interoperability/call-javascript-from-dotnet)

Blazor Azure Maps component for .NET 10 with typed C# APIs for map initialization, controls, markers, regions, geocoding, polygon boundaries, place search, pin-my-location, geographic location lock, location consent flow, and runtime camera/style updates.

> Main component: `AzureMap` (`CloudComponents.Maps.Components.AzureMap`)

---

## Table of contents

- [Features](#features)
- [Requirements](#requirements)
- [Installation](#installation)
- [Setup](#setup)
  - [1) Register Azure Maps key](#1-register-azure-maps-key)
  - [2) Import namespaces](#2-import-namespaces)
- [Quick start](#quick-start)
- [Component API (`AzureMap`)](#component-api-azuremap)
- [Models and options](#models-and-options)
- [Public runtime methods](#public-runtime-methods)
- [Events](#events)
- [Usage patterns](#usage-patterns)
  - [Marker interaction](#marker-interaction)
  - [Center-pin location picker](#center-pin-location-picker)
  - [Regions and legend overlays](#regions-and-legend-overlays)
  - [Geocode + polygon boundary flow](#geocode--polygon-boundary-flow)
  - [Built-in place search](#built-in-place-search)
  - [Pin my location](#pin-my-location)
  - [Set and save a location](#set-and-save-a-location)
  - [Location lock (restrict to countries/subdivisions)](#location-lock-restrict-to-countriessubdivisions)
- [Controls and interactions](#controls-and-interactions)
- [Styling](#styling)
- [Troubleshooting](#troubleshooting)

---

## Features

- Azure Maps rendered through a Blazor component (`AzureMap`)
- JS isolation module (`mapInterop.js`) loaded on demand
- Rich camera setup:
  - center/zoom (`InitialMapView`)
  - pitch, bearing
  - min/max zoom
- Control configuration with per-control enable/position options
- Interaction toggles (pan/rotate/scroll/touch/keyboard/etc.)
- Marker system:
  - add/remove/clear programmatically
  - click callbacks
  - user-added marker triggers (`SingleClick`, `DoubleClick`, `CenterPin`, `Disabled`) with opt-in defaults (`Disabled` + `AllowMarkerRemoval=false`)
- Region overlays (polygon GeoJSON rings) with optional legend labels
- Geocoding and polygon retrieval (Azure Maps Search API)
- Built-in place search box with debounced fuzzy search, keyboard navigation, and result selection
- Pin-my-location: one call/button requests device geolocation (with permission consent) and recenters the map — same API on web (browser geolocation) and .NET MAUI (native `Geolocation`), each with its own `ILocationService` implementation
- Location lock: restrict marker/center-pin/search selection to one or more countries and/or subdivisions, with a rendered boundary overlay and rejection feedback
- Traffic overlays (flow/incidents)
- Built-in location consent prompt + optional locate-on-open flow
- Runtime API for style, center, bounds, and orientation updates

---

## Requirements

- .NET 10 Blazor app
- Valid Azure Maps subscription key
- Browser with JavaScript enabled

---

## Installation

Add a project reference to `CloudComponents.Maps` or package equivalent used by your solution.

Dependencies include:
- `Microsoft.AspNetCore.Components.Web`
- `AngryMonkey.Cloud.Geography`

---

## Setup

### 1) Register Azure Maps key

Use DI once at startup:

```csharp
using CloudComponents.Maps.Extensions;

builder.Services.AddAzureMaps("<your-azure-maps-subscription-key>");
```

Or with options:

```csharp
builder.Services.AddAzureMaps(options =>
{
	options.SubscriptionKey = "<your-azure-maps-subscription-key>";
});
```

You can still override per component using the `SubscriptionKey` parameter.

### 2) Import namespaces

In component/page:

```razor
@using CloudComponents.Maps.Components
@using CloudComponents.Maps.Models
@using CloudComponents.Maps.Options
```

---

## Quick start

```razor
@using CloudComponents.Maps.Components
@using CloudComponents.Maps.Models
@using CloudComponents.Maps.Options

<AzureMap Height="500px"
		  Width="100%"
		  InitialView="_start"
		  Style="MapStyle.Road"
		  AddMarkerTrigger="MarkerAddTrigger.DoubleClick"
		  ShowCurrentLocationButton="true"
		  OnMapReady="HandleReady"
		  OnMarkerAdded="HandleMarkerAdded"
		  OnMapError="HandleError"
		  @ref="_map" />

@code {
	private AzureMap? _map;
	private readonly InitialMapView _start = new(37.7749, -122.4194, 12);

	private Task HandleReady() => Task.CompletedTask;

	private Task HandleMarkerAdded(MapMarker marker)
	{
		// Persist marker or update state.
		return Task.CompletedTask;
	}

	private Task HandleError(string message)
	{
		Console.WriteLine(message);
		return Task.CompletedTask;
	}
}
```

---

## Component API (`AzureMap`)

| Parameter | Type | Default | Description |
|---|---|---|---|
| `SubscriptionKey` | `string?` | `null` | Azure Maps key override (falls back to DI options). |
| `Height` | `string` | `500px` | Host container height. |
| `Width` | `string` | `100%` | Host container width. |
| `InitialView` | `InitialMapView?` | `null` | Initial camera view. If null, component may auto-locate then fallback to world view. |
| `Style` | `MapStyle` | `Satellite` | Base map style. |
| `Language` | `string` | `en-US` | Map language code. |
| `View` | `string` | `Auto` | Azure Maps view mode. |
| `Pitch` | `double` | `0` | Camera pitch angle. |
| `Bearing` | `double` | `0` | Camera bearing angle. |
| `MinZoom` | `double?` | `null` | Minimum zoom level. |
| `MaxZoom` | `double?` | `null` | Maximum zoom level. |
| `ShowTrafficFlow` | `bool` | `false` | Enables traffic flow layer. |
| `ShowTrafficIncidents` | `bool` | `false` | Enables traffic incidents. |
| `ZoomControl` | `MapControlOption` | `Enabled=true, TopRight` | Zoom control config. |
| `CompassControl` | `MapControlOption` | `Enabled=false, TopRight` | Compass control config. |
| `PitchControl` | `MapControlOption` | `Enabled=false, TopRight` | Pitch control config. |
| `StyleControl` | `MapControlOption` | `Enabled=false, TopLeft` | Style selector config. |
| `FullscreenControl` | `MapControlOption` | `Enabled=false, TopRight` | Fullscreen control config. |
| `ScaleControl` | `MapControlOption` | `Enabled=false, BottomLeft` | Scale control config. |
| `Interactive` | `bool` | `true` | Master interaction toggle. |
| `DragPanInteraction` | `bool` | `true` | Enables pan drag interaction. |
| `DragRotateInteraction` | `bool` | `true` | Enables rotate interaction. |
| `ScrollZoomInteraction` | `bool` | `false` | Enables wheel/pinch zoom. Off by default so embedded maps do not hijack page scrolling. |
| `DblClickZoomInteraction` | `bool` | `true` | Enables double-click zoom interaction. |
| `BoxZoomInteraction` | `bool` | `true` | Enables box zoom interaction. |
| `KeyboardInteraction` | `bool` | `true` | Enables keyboard map interaction. |
| `TouchInteraction` | `bool` | `true` | Enables touch interactions. |
| `Markers` | `IReadOnlyList<MapMarker>?` | `null` | Initial markers to add after map ready. |
| `Regions` | `IReadOnlyList<MapRegion>?` | `null` | Initial region overlays. |
| `AddMarkerTrigger` | `MarkerAddTrigger` | `Disabled` | User marker-add interaction mode (opt-in). |
| `AllowMarkerRemoval` | `bool` | `false` | Allows marker removal by double-clicking marker (opt-in). |
| `ShowCurrentLocationButton` | `bool` | `true` | Shows locate-me floating button when location service is available. |
| `LocateOnOpen` | `bool` | `false` | Attempts locate flow automatically once map is ready. |
| `LocationPromptTitle` | `string` | `Use your location?` | Consent popup title. |
| `LocationPromptMessage` | `string` | `Allow this app...` | Consent popup body text. |
| `LocationPromptConfirmText` | `string` | `Allow` | Consent popup confirm text. |
| `LocationPromptCancelText` | `string` | `Cancel` | Consent popup cancel text. |
| `OnMapReady` | `EventCallback` | — | Raised when map and JS controller are ready. |
| `OnMapClick` | `EventCallback<MapCoordinate>` | — | Raised on map click. |
| `OnMarkerAdded` | `EventCallback<MapMarker>` | — | Raised after a marker is added by map interaction. |
| `OnMarkerClick` | `EventCallback<MapMarker>` | — | Raised on marker click. |
| `OnMarkerRemoved` | `EventCallback<MapMarker>` | — | Raised after marker removal. |
| `OnCenterPinChanged` | `EventCallback<MapCoordinate>` | — | Raised in center-pin mode when center coordinate changes. |
| `OnMapError` | `EventCallback<string>` | — | Raised on SDK/runtime errors. |
| `OnMyLocationFound` | `EventCallback<MapCoordinate>` | — | Raised after "pin my location" successfully recenters the map. |
| `ShowSearchBox` | `bool` | `false` | Shows the built-in place search box (top-left). Opt-in by default. |
| `SearchPlaceholder` | `string` | `Search for a place…` | Placeholder text for the search input. |
| `SearchDebounceMs` | `int` | `350` | Debounce delay before a typed query triggers a search request. |
| `SearchResultLimit` | `int` | `5` | Maximum number of suggestions shown in the results dropdown. |
| `SearchResultZoom` | `int` | `14` | Zoom level used when a selected result has no viewport (a single point). |
| `OnSearchResultSelected` | `EventCallback<MapSearchResult>` | — | Raised after the camera moves to a user-selected search result. |
| `LocationLock` | `LocationLockOptions?` | `null` | Restricts marker-add/center-pin/search-selection interactions to configured countries/subdivisions. |
| `OnLocationLockRejected` | `EventCallback<MapCoordinate>` | — | Raised when the user attempts to select a point outside the locked area. |

---

## Models and options

### `InitialMapView`

```csharp
public sealed record InitialMapView(double Latitude, double Longitude, double Zoom = 12)
```

Includes `InitialMapView.World` fallback (`0,0,2`).

### `MapCoordinate`

```csharp
public readonly record struct MapCoordinate(double Latitude, double Longitude)
```

### `MapMarker`

```csharp
public sealed record MapMarker(double Latitude, double Longitude, string? Label = null, string Color = "#e81123")
```

Additional optional metadata is supported:
- `Id`
- `Title`, `City`, `District`, `Subdivision`, `Country`
- `ImageUrl`, `Area`, `Price`
- `DetailsUrl`, `DetailsLabel`

### `MapRegion`

```csharp
public sealed record MapRegion(double[][][] Coordinates, string FillColor = "rgba(0, 120, 212, 0.15)", string StrokeColor = "#0078d4", double StrokeWidth = 2)
```

- `Id`
- optional `Label` (rendered in legend/UI flow)

### `GeocodeResult`

Contains center and viewport bounds plus optional geometry ID:
- `Latitude`, `Longitude`
- `North`, `South`, `East`, `West`
- `GeometryId`

### `MapSearchResult`

```csharp
public sealed record MapSearchResult(
	string Address, double Latitude, double Longitude,
	double North, double South, double East, double West, string? GeometryId)
```

- `Description` — secondary line (locality/country), when available.

### `LocationLockOptions` / `LocationLockArea`

```csharp
public sealed record LocationLockArea(string CountryCode, IReadOnlyList<string>? SubdivisionCodes = null);

public sealed record LocationLockOptions(IReadOnlyList<LocationLockArea> Areas)
{
	public LocationLockOptions(params string[] countryCodes) : this(...) { }

	public bool ShowBoundary { get; init; } = true;
	public string BoundaryFillColor { get; init; } = "rgba(16, 124, 16, 0.10)";
	public string BoundaryStrokeColor { get; init; } = "#107c10";
	public bool ZoomToBoundary { get; init; } = true;
	public string RestrictionMessage { get; init; } = "This location is outside the allowed area.";
}
```

`CountryCode` is ISO 3166-1 alpha-2 (e.g. `"US"`); `SubdivisionCodes` narrows to specific states/provinces (e.g. `"US-CA"`). When `SubdivisionCodes` is omitted, the entire country is allowed. Boundaries are resolved once (via `AngryMonkey.Cloud.Geography` + Azure Maps geocode/polygon lookup) and enforced client-side for fast, repeated checks.

### Enums / option records

- `MapStyle`
  - `Road`, `GrayscaleLight`, `GrayscaleDark`, `Night`, `RoadShadedRelief`, `Satellite`, `SatelliteRoadLabels`, `HighContrastDark`, `HighContrastLight`
- `MapControlPosition`
  - `TopLeft`, `TopRight`, `BottomLeft`, `BottomRight`
- `MapControlOption`
  - `record MapControlOption(bool Enabled = true, MapControlPosition Position = MapControlPosition.TopRight)`
- `MarkerAddTrigger`
  - `Disabled`, `SingleClick`, `DoubleClick`, `CenterPin`

---

## Public runtime methods

All methods are available on `@ref` once map is ready.

```csharp
public Task AddMarkerAsync(MapMarker marker)
public Task AddMarkersAsync(IEnumerable<MapMarker> markers)
public Task RemoveMarkerAsync(string id)
public Task ClearMarkersAsync()

public Task AddRegionsAsync(IEnumerable<MapRegion> regions)
public Task ClearRegionsAsync()

public Task<GeocodeResult?> GeocodeAsync(string query)
public Task<double[][][]?> GetPolygonAsync(string geometryId)

public Task SetCenterAsync(double latitude, double longitude, int? zoom = null)
public Task SetStyleAsync(MapStyle style)
public Task SetTrafficAsync(bool showFlow, bool showIncidents)
public Task SetCameraOrientationAsync(double? pitch = null, double? bearing = null)
public Task SetBoundsAsync(double south, double west, double north, double east, int paddingPx = 40)
public Task ShowCurrentLocationAsync(double latitude, double longitude)

// Pin my location — requests device geolocation (prompting for permission if needed)
// and recenters the map. Honors LocationLock. Returns true when recentered.
public Task<bool> PinMyLocationAsync()

// Built-in search box helpers
public void ClearSearch()

// Location lock helper — validate a coordinate against the active LocationLock
public Task<bool> IsPointAllowedByLocationLockAsync(double latitude, double longitude)
```

Component state helpers:
- `IsReady`
- `CurrentMarkers`
- `SearchResults` — current search suggestions, in relevance order
- `IsLocationLockActive` — true once at least one boundary for the current `LocationLock` has resolved
- `IsResolvingLocationLock` — true while location-lock boundaries are being resolved

---

## Events

| Event | Payload | Typical use |
|---|---|---|
| `OnMapReady` | none | Trigger initial marker/region load or camera fit. |
| `OnMapClick` | `MapCoordinate` | Capture clicked position. |
| `OnMarkerAdded` | `MapMarker` | Persist marker created by user interaction. |
| `OnMarkerClick` | `MapMarker` | Show side panel/details for selected marker. |
| `OnMarkerRemoved` | `MapMarker` | Remove from app state/store. |
| `OnCenterPinChanged` | `MapCoordinate` | Address picker / reverse geocode workflow. |
| `OnMapError` | `string` | Logging, telemetry, and user feedback. |
| `OnMyLocationFound` | `MapCoordinate` | React to a successful "pin my location" (e.g. update a form field). |
| `OnSearchResultSelected` | `MapSearchResult` | React to the user picking a search suggestion. |
| `OnLocationLockRejected` | `MapCoordinate` | Show custom feedback when a selection falls outside the locked area. |

---

## Usage patterns

### Marker interaction

Marker interactions are opt-in. Set `AddMarkerTrigger` explicitly (default is `Disabled`) and enable `AllowMarkerRemoval` only when needed.

```razor
<AzureMap AddMarkerTrigger="MarkerAddTrigger.DoubleClick"
		  AllowMarkerRemoval="true"
		  OnMarkerAdded="HandleAdded"
		  OnMarkerRemoved="HandleRemoved" />
```

### Center-pin location picker

```razor
<AzureMap AddMarkerTrigger="MarkerAddTrigger.CenterPin"
		  OnCenterPinChanged="HandleCenterPinChanged" />
```

Use `OnCenterPinChanged` + your reverse geocode call to create location picker UX.

### Regions and legend overlays

```csharp
var region = new MapRegion(coordinates)
{
	Label = "Region A"
};

await _map.AddRegionsAsync(new[] { region });
```

### Geocode + polygon boundary flow

```csharp
var geocode = await _map.GeocodeAsync("Berlin, Germany");
if (geocode?.GeometryId is { Length: > 0 } geometryId)
{
	var polygon = await _map.GetPolygonAsync(geometryId);
	if (polygon is not null)
	{
		await _map.AddRegionsAsync(new[]
		{
			new MapRegion(polygon) { Label = "Berlin" }
		});
	}
}
```

### Built-in place search

```razor
<AzureMap ShowSearchBox="true"
		  SearchPlaceholder="Search for a city, address, or landmark…"
		  SearchResultLimit="6"
		  OnSearchResultSelected="HandleSearchResultSelected" />

@code {
	private Task HandleSearchResultSelected(MapSearchResult result)
	{
		// Camera has already flown to the result; use it to update app state.
		return Task.CompletedTask;
	}
}
```

The search box debounces keystrokes (`SearchDebounceMs`), queries the Azure Maps fuzzy search API, and shows a keyboard-navigable dropdown (arrow keys + Enter/Escape). Selecting a result flies the camera to its viewport (or center + `SearchResultZoom` when no viewport is available). When `LocationLock` is active, a result outside the locked area raises `OnLocationLockRejected` instead of moving the camera. Call `_map.ClearSearch()` to reset the box programmatically.

### Pin my location

```razor
<AzureMap ShowCurrentLocationButton="true"
		  OnMyLocationFound="HandleMyLocationFound"
		  @ref="_map" />

<button @onclick="() => _map!.PinMyLocationAsync()">Use my location</button>

@code {
	private AzureMap? _map;

	private Task HandleMyLocationFound(MapCoordinate coordinate)
	{
		// Recenter finished — persist the coordinate, update a form, etc.
		return Task.CompletedTask;
	}
}
```

"Pin my location" is available two ways: the built-in floating locate button (`ShowCurrentLocationButton`) or calling `PinMyLocationAsync()` directly (e.g. from your own button). Both call through the registered `ILocationService`, which prompts for permission via the component's consent popup when needed, then recenters the map and raises `OnMyLocationFound`. Register the implementation that matches your host:

- **Blazor WebAssembly / Server (browser)** — `CloudComponents.Maps.Web`'s `WebLocationService`, using `navigator.geolocation` through JS isolation.
- **.NET MAUI Blazor Hybrid** — `CloudComponents.Maps.Mobile`'s `MauiLocationService`, using the native `Geolocation`/`Permissions` MAUI Essentials APIs.

```csharp
// Web / WebAssembly
builder.Services.AddScoped<ILocationService, WebLocationService>();

// .NET MAUI
builder.Services.AddSingleton<ILocationService, MauiLocationService>();
```

If no `ILocationService` is registered, the locate button is hidden and `PinMyLocationAsync()` returns `false`.

### Set and save a location

This pattern lets users click to pick a coordinate, persist it, and restore it later.

```razor
<AzureMap AddMarkerTrigger="MarkerAddTrigger.Disabled"
		  ShowSearchBox="false"
		  ShowCurrentLocationButton="true"
		  OnMapClick="HandleMapClick"
		  @ref="_map" />

<button @onclick="SaveSelectedAsync">Save selected location</button>
<button @onclick="RestoreSavedAsync">Restore saved location</button>

@code {
	private AzureMap? _map;
	private MapCoordinate? _selected;

	private Task HandleMapClick(MapCoordinate point)
	{
		_selected = point;
		return Task.CompletedTask;
	}

	private async Task SaveSelectedAsync()
	{
		if (!_selected.HasValue) return;
		// Persist to your store (LocalStorage, API, database, etc.)
	}

	private async Task RestoreSavedAsync()
	{
		// Load previously saved coordinate, then:
		// await _map!.SetCenterAsync(savedLat, savedLng, 15);
		// await _map.AddMarkerAsync(new MapMarker(savedLat, savedLng, "Saved"));
	}
}
```

### Location lock (restrict to countries/subdivisions)

```razor
<AzureMap LocationLock="@_lock"
		  AddMarkerTrigger="MarkerAddTrigger.SingleClick"
		  OnLocationLockRejected="HandleLockRejected" />

@code {
	// Whole countries:
	private readonly LocationLockOptions _lock = new("US", "CA");

	// Or specific subdivisions within a country:
	// private readonly LocationLockOptions _lock = new(
	//     new[] { new LocationLockArea("US", new[] { "US-CA", "US-NY" }) });

	private Task HandleLockRejected(MapCoordinate coordinate)
	{
		// A built-in toast already shows LocationLockOptions.RestrictionMessage;
		// use this event for additional telemetry/UX if needed.
		return Task.CompletedTask;
	}
}
```

When `LocationLock` is set, the component resolves each configured country/subdivision to a boundary polygon (via `AngryMonkey.Cloud.Geography` for country/subdivision lookup plus Azure Maps geocode/polygon APIs), then enforces it client-side for marker-add clicks, center-pin dragging, "pin my location", and search-result selection. The client-side validator uses a cached lock bounding-box fast-path before polygon checks, and large boundary rings are simplified to keep repeated checks responsive. Set `ShowBoundary = false` to hide the overlay, or tune `BoundaryFillColor`/`BoundaryStrokeColor`/`ZoomToBoundary`/`RestrictionMessage` as needed. Assign a new `LocationLockOptions` instance (or `null` to clear) to change the lock at runtime — the component re-resolves automatically.

---

## Controls and interactions

Enable controls by supplying `MapControlOption` values:

```razor
<AzureMap ZoomControl="@(new MapControlOption(true, MapControlPosition.TopRight))"
		  StyleControl="@(new MapControlOption(true, MapControlPosition.TopLeft))"
		  FullscreenControl="@(new MapControlOption(true))" />
```

Interaction toggles can be switched independently:

- `DragPanInteraction`
- `DragRotateInteraction`
- `ScrollZoomInteraction`
- `DblClickZoomInteraction`
- `BoxZoomInteraction`
- `KeyboardInteraction`
- `TouchInteraction`

---

## Styling

Primary styles are defined in:
- `Components/AzureMap.razor.less` (source)
- compiled scoped CSS (`AzureMap.razor.css`)

Key CSS hooks:
- `.map-host`
- `.map-canvas`
- `.map-pin`
- `.map-legend`, `.map-legend-item`, `.map-legend-swatch`, `.map-legend-label`
- `.map-permission-*`
- `.map-search`, `.map-search-box`, `.map-search-input`, `.map-search-results`, `.map-search-result`
- `.map-lock-toast`

Floating locate button uses `.btn` within map host.

---

## Troubleshooting

### “Azure Maps subscription key is not configured”

Provide one of:
- `SubscriptionKey` parameter on component
- `builder.Services.AddAzureMaps(...)` startup registration

### “Map renders but no controls appear”

Ensure each control has `Enabled = true` in its `MapControlOption`.

### “Markers do not add on click”

Check `AddMarkerTrigger` is not `Disabled`, and verify map is interactive.

### “Style switch appears to do nothing”

Use valid `MapStyle` enum values and verify base map tiles are permitted by your Azure Maps SKU.

### “Location button not visible”

`ShowCurrentLocationButton` must be true and an `ILocationService` implementation must be available.

### “Pin my location does nothing on mobile”

Ensure `CloudComponents.Maps.Mobile`'s `MauiLocationService` is registered as `ILocationService` and the app has requested the `LocationWhenInUse` permission in its platform manifest (`AndroidManifest.xml` / `Info.plist`).

### “Search box shows no results”

Verify the Azure Maps subscription key is valid and has Search API access; check `OnMapError` for the underlying failure message.

### “Search feels slow/freezes when LocationLock is enabled”

Upgrade to the latest package version. Recent updates added a cached lock bounding-box fast-path and polygon ring simplification in the JS interop layer to avoid expensive per-keystroke point-in-polygon checks on very large boundaries.

### “Location lock rejects everything / boundary never appears”

Confirm the `CountryCode`/`SubdivisionCodes` in `LocationLockOptions` are valid ISO codes recognized by `AngryMonkey.Cloud.Geography`, and that `IsResolvingLocationLock` has completed (boundary resolution is asynchronous) before interacting with the map.

---

## License

See repository licensing terms.
