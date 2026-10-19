using AngryMonkey.CloudComponents.Maps.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AngryMonkey.CloudComponents.Maps.Components;

/// <summary>
/// Zone-overlay surface of <see cref="AzureMap"/>.
/// Zones are high-level wrappers around one or more address queries that are resolved to
/// real administrative boundary polygons automatically by the component. Consumers declare
/// what to show; CloudMaps handles geocoding → polygon fetch → rendering.
/// </summary>
public partial class AzureMap
{
    // Tracks zone id → the MapRegion ids it produced so we can remove selectively.
    private readonly Dictionary<string, List<string>> _zoneRegionMap = [];

    /// <summary>
    /// Declarative zone overlays. Each <see cref="MapZone"/> describes one or more
    /// addresses that the component resolves to boundary polygons on your behalf.
    /// Set once at render time; for dynamic changes use <see cref="AddZonesAsync"/>,
    /// <see cref="RemoveZoneAsync"/>, and <see cref="ClearZonesAsync"/> at runtime.
    /// </summary>
    [Parameter] public IReadOnlyList<MapZone>? Zones { get; set; }

    // ── Public imperative API ────────────────────────────────────────────────

    /// <summary>
    /// Checks whether an address can be resolved to a boundary polygon on the map
    /// without drawing anything. Returns a <see cref="ZoneCheckResult"/> describing
    /// whether the address was found (<see cref="ZoneCheckResult.Found"/>) and whether
    /// a polygon boundary is available (<see cref="ZoneCheckResult.CanZone"/>).
    /// </summary>
    /// <param name="address">Free-text address or place name to check.</param>
    /// <param name="countrySet">
    /// Optional ISO 3166-1 alpha-2 country code(s) to narrow results (e.g. <c>"US"</c>,
    /// <c>"LB"</c>, <c>"US,CA"</c>). Mirrors <see cref="MapZone.CountrySet"/>.
    /// </param>
    public async Task<ZoneCheckResult> CanZoneAsync(string address, string? countrySet = null)
    {
        if (string.IsNullOrWhiteSpace(address))
            return ZoneCheckResult.NotFound;

        try
        {
            var geocode = await GeocodeAsync(address, countrySet: countrySet);

            if (geocode is null)
                return ZoneCheckResult.NotFound;

            return new ZoneCheckResult
            {
                Found = true,
                CanZone = geocode.GeometryId is { Length: > 0 },
                Coordinate = new MapCoordinate(geocode.Latitude, geocode.Longitude),
                GeometryId = geocode.GeometryId
            };
        }
        catch
        {
            return ZoneCheckResult.NotFound;
        }
    }

    /// <summary>
    /// Resolves each zone's addresses to boundary polygons and renders them on the map.
    /// Zones whose addresses yield no geometry Id are silently skipped.
    /// </summary>
    public async Task AddZonesAsync(IEnumerable<MapZone> zones)
    {
        if (MergeZoneBoundariesAbove is not int threshold)
        {
            foreach (var zone in zones)
                await ResolveAndRenderZoneAsync(zone);
            return;
        }

        List<(MapZone Zone, double[][][] Polygon)> boundaries = [];
        foreach (var zone in zones)
            foreach (var polygon in await ResolveZoneBoundariesAsync(zone))
                boundaries.Add((zone, polygon));

        if (boundaries.Count <= threshold)
        {
            foreach (var (zone, polygon) in boundaries)
                await RenderZoneBoundaryAsync(zone, polygon);
            return;
        }

        string mergedId = await AddMergedRegionAsync(
            boundaries.Select(boundary => boundary.Polygon),
            MergedBoundaryStrokeColor,
            boundaries[0].Zone.StrokeWidth);

        foreach (var zoneId in boundaries.Select(boundary => boundary.Zone.Id).Distinct())
            _zoneRegionMap[zoneId] = [mergedId];
    }

    /// <summary>
    /// When set, a batch of zones drawing more than this many boundaries is shown as one merged outline without fill
    /// instead of one coloured shape per area. Null (the default) always draws every boundary.
    /// </summary>
    [Parameter] public int? MergeZoneBoundariesAbove { get; set; }

    /// <summary>Outline color of merged zone boundaries. See <see cref="MergeZoneBoundariesAbove"/>.</summary>
    [Parameter] public string MergedBoundaryStrokeColor { get; set; } = "#0078d4";

    /// <summary>
    /// Removes the overlay(s) previously rendered for a zone identified by <paramref name="zoneId"/>.
    /// </summary>
    public async Task RemoveZoneAsync(string zoneId)
    {
        if (!_zoneRegionMap.TryGetValue(zoneId, out var regionIds))
            return;

        _zoneRegionMap.Remove(zoneId);

        if (_controller is not null)
        {
            foreach (var regionId in regionIds)
                await _controller.InvokeVoidAsync("removeRegion", regionId);
        }

        _regionLegendEntries.RemoveAll(e =>
            regionIds.Any(id => e.Label != null &&
                                _zoneRegionMap.Values.SelectMany(v => v).All(rid => rid != id)));

        StateHasChanged();
    }

    /// <summary>
    /// Removes all zone overlays from the map without touching manually-added
    /// <see cref="Regions"/> or regions added via <see cref="AddRegionsAsync"/>.
    /// </summary>
    public async Task ClearZonesAsync()
    {
        var allRegionIds = _zoneRegionMap.Values.SelectMany(v => v).ToList();
        _zoneRegionMap.Clear();

        if (_controller is not null)
        {
            foreach (var regionId in allRegionIds)
                await _controller.InvokeVoidAsync("removeRegion", regionId);
        }

        StateHasChanged();
    }

    // ── Internal ─────────────────────────────────────────────────────────────

    /// <summary>Loads the declarative <see cref="Zones"/> parameter after the map is ready.</summary>
    internal async Task SyncZonesAsync()
    {
        if (Zones is not { Count: > 0 })
            return;

        await AddZonesAsync(Zones);
    }

    private async Task ResolveAndRenderZoneAsync(MapZone zone)
    {
        foreach (var polygon in await ResolveZoneBoundariesAsync(zone))
            await RenderZoneBoundaryAsync(zone, polygon);
    }

    /// <summary>The boundary polygon of each of the zone's addresses that has one.</summary>
    private async Task<List<double[][][]>> ResolveZoneBoundariesAsync(MapZone zone)
    {
        List<double[][][]> polygons = [];

        if (_controller is null || zone.Addresses is not { Count: > 0 })
            return polygons;

        foreach (var address in zone.Addresses)
        {
            if (string.IsNullOrWhiteSpace(address))
                continue;

            try
            {
                // Geocode the address and fetch its administrative boundary polygon.
                var polygon = zone.EntityTypes is { Count: > 0 } levels
                    ? await ResolveBoundaryAsync(address, levels, zone.CountrySet)
                    : await ResolveBoundaryAsync(address, countrySet: zone.CountrySet);

                if (polygon is { Length: > 0 })
                    polygons.Add(polygon);
            }
            catch
            {
                // Best-effort: one address failing must not block the rest.
            }
        }

        return polygons;
    }

    private async Task RenderZoneBoundaryAsync(MapZone zone, double[][][] polygon)
    {
        var region = new MapRegion(
            polygon,
            FillColor: zone.FillColor,
            StrokeColor: zone.StrokeColor,
            StrokeWidth: zone.StrokeWidth)
        {
            Label = zone.Label
        };

        await AddRegionsAsync([region]);

        if (!_zoneRegionMap.TryGetValue(zone.Id, out var regionIds))
            _zoneRegionMap[zone.Id] = regionIds = [];
        regionIds.Add(region.Id);
    }
}

