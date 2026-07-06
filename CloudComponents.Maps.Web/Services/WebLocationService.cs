using AngryMonkey.CloudComponents.Maps.Services;
using Microsoft.JSInterop;

namespace AngryMonkey.CloudComponents.Maps.Web.Services;

/// <summary>
/// Browser implementation of <see cref="ILocationService"/> using
/// <c>navigator.geolocation</c> via JS module isolation.
/// </summary>
public sealed class WebLocationService : ILocationService, IAsyncDisposable
{
    private const string ModulePath = "./_content/AngryMonkey.CloudComponents.Maps/mapInterop.js";

    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;

    public WebLocationService(IJSRuntime js) => _js = js;

    public async Task<(double Lat, double Lng)?> GetCurrentLocationAsync()
    {
        try
        {
            var module = await GetModuleAsync();
            var coords = await module.InvokeAsync<double[]?>("getCurrentLocation");
            return coords is { Length: >= 2 } ? (coords[0], coords[1]) : null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<LocationPermissionState> GetPermissionStateAsync()
    {
        try
        {
            var module = await GetModuleAsync();
            var state = await module.InvokeAsync<string>("queryLocationPermission");
            return state switch
            {
                "granted" => LocationPermissionState.Granted,
                "denied" => LocationPermissionState.Denied,
                "unsupported" => LocationPermissionState.Unsupported,
                _ => LocationPermissionState.Prompt
            };
        }
        catch
        {
            return LocationPermissionState.Prompt;
        }
    }

    private async Task<IJSObjectReference> GetModuleAsync()
        => _module ??= await _js.InvokeAsync<IJSObjectReference>("import", ModulePath);

    public async ValueTask DisposeAsync()
    {
        if (_module is null) return;
        try { await _module.DisposeAsync(); }
        catch (JSDisconnectedException) { /* navigation */ }
    }
}
