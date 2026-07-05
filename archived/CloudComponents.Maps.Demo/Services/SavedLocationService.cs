using Microsoft.JSInterop;

namespace CloudComponents.Maps.Demo.Services;

/// <summary>A single named point saved by the user via the "Save Location" demo page.</summary>
public sealed record SavedLocation(double Latitude, double Longitude, string? Label = null);

/// <summary>
/// Demo persistence service for the "pick a location on the map and save it" pattern.
/// Backed by browser LocalStorage here for simplicity — in a real app, replace the
/// LocalStorage calls with an HTTP call to your own backend/API while keeping the
/// same async method shapes so <c>SaveLocationPage</c> doesn't need to change.
/// </summary>
public class SavedLocationService
{
    private readonly IJSRuntime _jsRuntime;
    private SavedLocation? _current;
    private bool _isInitialized;

    public event Action? OnChanged;

    public SavedLocationService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>Loads the previously saved location (if any) from storage.</summary>
    public async Task<SavedLocation?> InitializeAsync()
    {
        if (_isInitialized)
            return _current;

        try
        {
            var module = await _jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./savedLocationStorage.js");
            _current = await module.InvokeAsync<SavedLocation?>("getSavedLocation");
        }
        catch
        {
            _current = null;
        }

        _isInitialized = true;
        return _current;
    }

    public SavedLocation? Current => _current;

    /// <summary>Persists the given location, replacing any previously saved one. Returns <c>true</c> on success.</summary>
    public async Task<bool> SaveAsync(SavedLocation location)
    {
        _current = location;
        var persisted = false;

        try
        {
            var module = await _jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./savedLocationStorage.js");
            await module.InvokeVoidAsync("setSavedLocation", location);
            persisted = true;
        }
        catch
        {
            // Keep the in-memory value even if persistence fails, but let the
            // caller know so it can surface an accurate save-status message.
        }

        OnChanged?.Invoke();
        return persisted;
    }

    /// <summary>Clears the saved location from storage and memory.</summary>
    public async Task ClearAsync()
    {
        _current = null;

        try
        {
            var module = await _jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./savedLocationStorage.js");
            await module.InvokeVoidAsync("clearSavedLocation");
        }
        catch
        {
            // Continue even if JS fails.
        }

        OnChanged?.Invoke();
    }
}
