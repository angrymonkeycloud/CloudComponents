# CloudComponents.Maps

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/UI-Blazor-5C2D91?logo=blazor)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![Azure Maps](https://img.shields.io/badge/Maps-Azure%20Maps-0078D4?logo=microsoftazure)](https://azure.microsoft.com/products/azure-maps)
[![JS Interop](https://img.shields.io/badge/Interop-JS%20Isolation-0EA5E9)](https://learn.microsoft.com/aspnet/core/blazor/javascript-interoperability/call-javascript-from-dotnet)

Blazor Azure Maps component for .NET 10 with typed C# APIs for map initialization, controls, markers, regions, geocoding, polygon boundaries, location consent flow, and runtime camera/style updates.

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
  - user-added marker triggers (`SingleClick`, `DoubleClick`, `CenterPin`, `Disabled`)
- Region overlays (polygon GeoJSON rings) with optional legend labels
- Geocoding and polygon retrieval (Azure Maps Search API)
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
| `ScrollZoomInteraction` | `bool` | `true` | Enables wheel/pinch zoom. |
| `DblClickZoomInteraction` | `bool` | `false` | Enables double-click zoom interaction. |
| `BoxZoomInteraction` | `bool` | `true` | Enables box zoom interaction. |
| `KeyboardInteraction` | `bool` | `true` | Enables keyboard map interaction. |
| `TouchInteraction` | `bool` | `true` | Enables touch interactions. |
| `Markers` | `IReadOnlyList<MapMarker>?` | `null` | Initial markers to add after map ready. |
| `Regions` | `IReadOnlyList<MapRegion>?` | `null` | Initial region overlays. |
| `AddMarkerTrigger` | `MarkerAddTrigger` | `DoubleClick` | User marker-add interaction mode. |
| `AllowMarkerRemoval` | `bool` | `true` | Allows marker removal by double-clicking marker. |
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
```

Component state helpers:
- `IsReady`
- `CurrentMarkers`

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

---

## Usage patterns

### Marker interaction

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

---

## License

See repository licensing terms.
