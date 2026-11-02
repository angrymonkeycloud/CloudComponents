using AngryMonkey.CloudComponents.Maps.Components;
using AngryMonkey.CloudComponents.Maps.Models;
using Bunit;
using Microsoft.JSInterop;

namespace CloudComponents.Tests.Maps.Component;

/// <summary>Start-up failures, disposal, and how a half-created map behaves.</summary>
public sealed class AzureMapLifecycleTests : AzureMapTestContext
{
    // ── Disposal ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Disposing_tells_the_controller_to_dispose_then_releases_the_controller_and_the_module()
    {
        RenderMap();

        await DisposeComponentsAsync();

        Assert.Equal(["dispose"], Controller.Methods);
        Assert.Equal(1, Controller.DisposeCount);
        Assert.Equal(1, Js.Module.DisposeCount);
    }

    [Fact]
    public async Task Disposing_releases_the_reference_the_JavaScript_holds_to_the_component()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();
        DotNetObjectReference<AzureMap> reference = (DotNetObjectReference<AzureMap>)Js.Module.Single("createMap").Args[0]!;
        Assert.Same(cut.Instance, reference.Value);

        await DisposeComponentsAsync();

        Assert.Throws<ObjectDisposedException>(() => reference.Value);
    }

    [Fact]
    public async Task Nothing_is_sent_to_JavaScript_after_disposal()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();

        await DisposeComponentsAsync();

        Assert.Empty(Controller.CallsAfterDisposal);
        Assert.Empty(Js.Module.CallsAfterDisposal);
    }

    [Fact]
    public async Task A_disconnected_circuit_during_disposal_is_tolerated_and_the_component_reference_is_still_released()
    {
        IRenderedComponent<AzureMap> cut = RenderMap();
        DotNetObjectReference<AzureMap> reference = (DotNetObjectReference<AzureMap>)Js.Module.Single("createMap").Args[0]!;
        Controller.Throws("dispose", new JSDisconnectedException("circuit gone"));

        await DisposeComponentsAsync();

        Assert.Throws<ObjectDisposedException>(() => reference.Value);
    }

    [Fact]
    public async Task A_failing_controller_release_during_disposal_is_tolerated_and_the_component_reference_is_still_released()
    {
        RenderMap();
        DotNetObjectReference<AzureMap> reference = (DotNetObjectReference<AzureMap>)Js.Module.Single("createMap").Args[0]!;
        Controller.DisposeFailure = new JSDisconnectedException("circuit gone");

        await DisposeComponentsAsync();

        Assert.Throws<ObjectDisposedException>(() => reference.Value);
    }

    [Fact]
    public async Task A_failing_module_release_during_disposal_is_tolerated()
    {
        RenderMap();
        Js.Module.DisposeFailure = new JSDisconnectedException("circuit gone");

        await DisposeComponentsAsync();

        Assert.Equal(1, Js.Module.DisposeCount);
    }

    [Fact]
    public async Task Disposing_a_map_whose_controller_never_arrived_releases_only_what_exists()
    {
        Js.Module.Hold("createMap");
        IRenderedComponent<AzureMap> cut = RenderMap();
        DotNetObjectReference<AzureMap> reference = (DotNetObjectReference<AzureMap>)Js.Module.Single("createMap").Args[0]!;

        await DisposeComponentsAsync();

        Assert.False(Controller.WasCalled("dispose"));
        Assert.Equal(0, Controller.DisposeCount);
        Assert.Throws<ObjectDisposedException>(() => reference.Value);
    }

    [Fact]
    public async Task Disposing_stops_a_location_lock_that_is_still_resolving()
    {
        JsGate gate = Controller.Hold("resolveBoundary");
        RenderMap(p => p.Add(m => m.LocationLock, new LocationLockOptions("LB")));

        await DisposeComponentsAsync();
        gate.Release(new[] { new[] { new[] { 0.0, 0.0 } } });
        await Task.Delay(100);

        Assert.False(Controller.WasCalled("setLocationLock"));
    }

    // ── Start-up failures ───────────────────────────────────────────────────

    [Fact]
    public void A_missing_module_import_is_reported_to_the_caller_and_no_map_is_created()
    {
        Js.ImportFailure = new JSException("Failed to fetch dynamically imported module");

        Exception error = Assert.ThrowsAny<Exception>(() => RenderMap());

        Assert.Contains("Failed to fetch dynamically imported module", FullMessage(error));
        Assert.False(Js.Module.WasCalled("createMap"));
    }

    [Fact(Skip = "BUG: AzureMap.razor.cs OnAfterRenderAsync does not catch a failing createMap (for example the Azure Maps SDK script blocked or the key refused while loading), so OnMapError is never raised and the exception escapes the render; OnMapError is documented as the way a host learns about map errors")]
    public void A_createMap_failure_is_reported_through_OnMapError_and_does_not_escape_the_render()
    {
        Js.Module.Throws("createMap", new JSException("Azure Maps SDK failed to load"));
        List<string> errors = [];

        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.OnMapError, (string e) => errors.Add(e)));

        Assert.Contains(errors, e => e.Contains("Azure Maps SDK failed to load"));
        Assert.False(cut.Instance.IsReady);
    }

    [Fact]
    public async Task A_map_error_reported_by_JavaScript_does_not_break_the_map()
    {
        List<string> errors = [];
        IRenderedComponent<AzureMap> cut = RenderMap(p => p.Add(m => m.OnMapError, (string e) => errors.Add(e)));

        await Js.InvokeFromJsAsync("NotifyMapErrorAsync", "tile request failed");
        await Js.InvokeFromJsAsync("NotifyMapReadyAsync");

        Assert.Equal(["tile request failed"], errors);
        Assert.True(cut.Instance.IsReady);
    }

    private static string FullMessage(Exception exception)
    {
        List<string> messages = [];
        for (Exception? current = exception; current is not null; current = current.InnerException)
            messages.Add(current.Message);

        return string.Join(" | ", messages);
    }
}
