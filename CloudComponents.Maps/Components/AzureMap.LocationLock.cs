using AngryMonkey.Cloud;
using CloudComponents.Maps.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CloudComponents.Maps.Components;

/// <summary>
/// Restricts interactive location selection (marker add, center-pin drag, search-result
/// selection) to the countries/subdivisions configured via <see cref="AzureMap.LocationLock"/>.
/// Boundaries are resolved once (geocode + polygon lookup) and enforced client-side.
/// </summary>
public partial class AzureMap
{
    private LocationLockOptions? _appliedLocationLock;
    private List<double[][][]>? _lockPolygons;
    private bool _isResolvingLocationLock;
    private bool _showLockRestrictionToast;
    private CancellationTokenSource? _lockToastCts;
    private CancellationTokenSource? _lockResolveCts;

    /// <summary>
    /// When set, restricts marker-add / center-pin / search-selection interactions to the
    /// configured countries and/or subdivisions. <c>null</c> (default) means no restriction.
    /// </summary>
    [Parameter] public LocationLockOptions? LocationLock { get; set; }

    /// <summary>Raised whenever the user attempts to select a point outside the locked area.</summary>
    [Parameter] public EventCallback<MapCoordinate> OnLocationLockRejected { get; set; }

    /// <summary>True once at least one boundary for the current <see cref="LocationLock"/> has resolved.</summary>
    public bool IsLocationLockActive => _lockPolygons is { Count: > 0 };

    /// <summary>True while location-lock boundaries are being resolved (geocode + polygon lookup).</summary>
    public bool IsResolvingLocationLock => _isResolvingLocationLock;

    private string LockRestrictionMessage => LocationLock?.RestrictionMessage ?? "This location is outside the allowed area.";

    /// <summary>
    /// Applies <see cref="LocationLock"/> to the map when it changed since the last sync.
    /// Safe to call repeatedly (e.g. from <c>OnParametersSetAsync</c> and once the map becomes ready).
    /// </summary>
    private async Task SyncLocationLockAsync()
    {
        if (_controller is null || ReferenceEquals(_appliedLocationLock, LocationLock))
            return;

        _appliedLocationLock = LocationLock;

        _lockResolveCts?.Cancel();
        _lockResolveCts?.Dispose();
        _lockResolveCts = null;

        if (LocationLock is null || LocationLock.Areas.Count == 0)
        {
            _lockPolygons = null;
            await _controller.InvokeVoidAsync("clearLocationLock");
            return;
        }

        var cts = new CancellationTokenSource();
        _lockResolveCts = cts;

        _isResolvingLocationLock = true;
        StateHasChanged();

        try
        {
            var polygons = await ResolveLocationLockPolygonsAsync(LocationLock, cts.Token);
            if (cts.IsCancellationRequested || _controller is null)
                return;

            _lockPolygons = polygons;

            await _controller.InvokeVoidAsync("setLocationLock", polygons, new
            {
                showBoundary = LocationLock.ShowBoundary,
                fillColor = LocationLock.BoundaryFillColor,
                strokeColor = LocationLock.BoundaryStrokeColor,
                zoomToBoundary = LocationLock.ZoomToBoundary
            });
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer LocationLock assignment.
        }
        catch (Exception ex)
        {
            await OnMapError.InvokeAsync($"Failed to resolve location lock: {ex.Message}");
        }
        finally
        {
            _isResolvingLocationLock = false;
            StateHasChanged();
        }
    }

    private async Task<List<double[][][]>> ResolveLocationLockPolygonsAsync(
        LocationLockOptions options, CancellationToken cancellationToken)
    {
        var geoClient = new CloudGeographyClient();
        var polygons = new List<double[][][]>();

        foreach (var area in options.Areas)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var country = geoClient.Countries.Get(area.CountryCode);
            if (country is null)
                continue;

            if (area.SubdivisionCodes is not { Count: > 0 })
            {
                // entityType=Country ensures a query like "Lebanon" resolves to the country
                // itself, never a same-named city/POI elsewhere (e.g. Lebanon, PA/OH/NH in the US).
                var polygon = await ResolveBoundaryPolygonAsync(country.Name, cancellationToken, entityType: "Country");
                if (polygon is not null)
                    polygons.Add(polygon);
                continue;
            }

            foreach (var subdivisionCode in area.SubdivisionCodes)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var subdivision = geoClient.Subdivisions.Get(country.Code, subdivisionCode);
                var name = subdivision?.Name ?? subdivisionCode;
                // entityType=CountrySubdivision + countrySet pins the match to the right
                // administrative level inside the right country (avoids cross-country name clashes).
                var polygon = await ResolveBoundaryPolygonAsync($"{name}, {country.Name}", cancellationToken,
                    entityType: "CountrySubdivision", countrySet: country.Code);
                if (polygon is not null)
                    polygons.Add(polygon);
            }
        }

        return polygons;
    }

    private async Task<double[][][]?> ResolveBoundaryPolygonAsync(
        string query, CancellationToken cancellationToken, string? entityType = null, string? countrySet = null)
    {
        var geocode = await GeocodeAsync(query, entityType, countrySet);
        cancellationToken.ThrowIfCancellationRequested();

        if (geocode?.GeometryId is not { Length: > 0 } geometryId)
            return null;

        return await GetPolygonAsync(geometryId);
    }

    /// <summary>
    /// Checks a coordinate against the currently-applied <see cref="LocationLock"/>.
    /// Always returns <c>true</c> when no lock is active.
    /// </summary>
    public async Task<bool> IsPointAllowedByLocationLockAsync(double latitude, double longitude)
    {
        if (!IsLocationLockActive || _controller is null)
            return true;

        return await _controller.InvokeAsync<bool>("isPointAllowed", latitude, longitude);
    }

    [JSInvokable]
    public Task NotifyLocationLockRejectedAsync(double latitude, double longitude)
        => RaiseLocationLockRejectedAsync(latitude, longitude);

    private async Task RaiseLocationLockRejectedAsync(double latitude, double longitude)
    {
        await OnLocationLockRejected.InvokeAsync(new MapCoordinate(latitude, longitude));
        await ShowLockRestrictionToastAsync();
    }

    private async Task ShowLockRestrictionToastAsync()
    {
        _lockToastCts?.Cancel();
        _lockToastCts?.Dispose();
        var cts = new CancellationTokenSource();
        _lockToastCts = cts;

        _showLockRestrictionToast = true;
        StateHasChanged();

        try
        {
            await Task.Delay(2600, cts.Token);
            _showLockRestrictionToast = false;
            StateHasChanged();
        }
        catch (TaskCanceledException)
        {
            // Superseded by a newer rejection; the latest call owns hiding the toast.
        }
    }

    private void CancelLocationLockWork()
    {
        _lockResolveCts?.Cancel();
        _lockResolveCts?.Dispose();
        _lockResolveCts = null;

        _lockToastCts?.Cancel();
        _lockToastCts?.Dispose();
        _lockToastCts = null;
    }
}
