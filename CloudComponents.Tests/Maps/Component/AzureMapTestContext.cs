using AngryMonkey.CloudComponents.Maps.Components;
using AngryMonkey.CloudComponents.Maps.Services;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.JSInterop;

namespace CloudComponents.Tests.Maps.Component;

/// <summary>
/// Base class for tests of <see cref="AzureMap"/>: a bUnit context whose <c>IJSRuntime</c> is a <see cref="FakeMapJs"/>,
/// so no real JavaScript or network is involved and every call to the map is recorded.
/// </summary>
public abstract class AzureMapTestContext : BunitContext
{
    protected AzureMapTestContext()
    {
        Services.RemoveAll<IJSRuntime>();
        Services.AddSingleton<IJSRuntime>(Js);

        // AzureMap declares ILocationService as optional but a missing registration throws while it is created (see the skipped
        // test in AzureMapRenderTests), so every context registers one. It starts as Unsupported: it neither locates nor prompts.
        Services.AddSingleton<ILocationService>(Locator);
    }

    /// <summary>The registered location service; set its Permission and Location before rendering.</summary>
    protected FakeLocationService Locator { get; } = new() { Permission = LocationPermissionState.Unsupported };

    /// <summary>The scripted JavaScript side.</summary>
    protected FakeMapJs Js { get; } = new();

    /// <summary>The controller object <c>createMap</c> returns.</summary>
    protected FakeJsObject Controller => Js.Controller;

    /// <summary>
    /// Renders the map. The subscription key parameter is supplied unless <paramref name="withKey"/> is false, so a test only
    /// states the parameters it is about.
    /// </summary>
    protected IRenderedComponent<AzureMap> RenderMap(Action<ComponentParameterCollectionBuilder<AzureMap>>? configure = null, bool withKey = true)
    {
        IRenderedComponent<AzureMap> cut = Render<AzureMap>(parameters =>
        {
            if (withKey)
                parameters.Add(map => map.SubscriptionKey, "test-key");

            configure?.Invoke(parameters);
        });

        Js.Dispatch = work => cut.InvokeAsync(work);
        return cut;
    }

    /// <summary>Renders the map and raises <c>NotifyMapReadyAsync</c>, as the SDK's ready event does.</summary>
    protected async Task<IRenderedComponent<AzureMap>> RenderReadyMapAsync(Action<ComponentParameterCollectionBuilder<AzureMap>>? configure = null)
    {
        IRenderedComponent<AzureMap> cut = RenderMap(configure);
        await Js.InvokeFromJsAsync("NotifyMapReadyAsync");
        return cut;
    }

}

/// <summary>A scripted <see cref="ILocationService"/> that counts how often it was asked.</summary>
public sealed class FakeLocationService : ILocationService
{
    public LocationPermissionState Permission { get; set; } = LocationPermissionState.Granted;

    public (double Lat, double Lng)? Location { get; set; }

    public Exception? LocationFailure { get; set; }

    public int PermissionChecks { get; private set; }

    public int LocationRequests { get; private set; }

    /// <summary>When set, <see cref="GetCurrentLocationAsync"/> waits for it, to observe the "locating" state.</summary>
    public TaskCompletionSource<(double Lat, double Lng)?>? LocationGate { get; set; }

    public Task<LocationPermissionState> GetPermissionStateAsync()
    {
        PermissionChecks++;
        return Task.FromResult(Permission);
    }

    public Task<(double Lat, double Lng)?> GetCurrentLocationAsync()
    {
        LocationRequests++;

        if (LocationFailure is not null)
            throw LocationFailure;

        return LocationGate is { } gate ? gate.Task : Task.FromResult(Location);
    }
}
