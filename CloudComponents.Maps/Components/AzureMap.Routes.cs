using AngryMonkey.CloudComponents.Maps.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AngryMonkey.CloudComponents.Maps.Components;

public partial class AzureMap
{
    private IReadOnlyList<MapRoute>? _appliedRoutes;

    /// <summary>Route geometry to draw. Assign a new list to update or clear the routes.</summary>
    [Parameter] public IReadOnlyList<MapRoute>? Routes { get; set; }

    public async Task SetRoutesAsync(IEnumerable<MapRoute> routes)
    {
        IJSObjectReference controller = EnsureController();
        List<MapRoute> routeList = [.. routes];
        await controller.InvokeVoidAsync("setRoutes", routeList);
    }

    public async Task ClearRoutesAsync()
    {
        if (_controller is null)
            return;

        await _controller.InvokeVoidAsync("clearRoutes");
    }

    private async Task SyncRoutesAsync()
    {
        if (_controller is null || !IsReady || ReferenceEquals(_appliedRoutes, Routes))
            return;

        await _controller.InvokeVoidAsync("setRoutes", Routes ?? []);
        _appliedRoutes = Routes;
    }
}
