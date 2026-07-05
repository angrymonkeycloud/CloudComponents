using Microsoft.JSInterop;

namespace CloudComponents.Demo.Services;

public sealed record SavedLocation(double Latitude, double Longitude, string? Label = null);

public class SavedLocationService
{
    private readonly IJSRuntime _jsRuntime;
    private SavedLocation? _current;
    private bool _isInitialized;

    public event Action? OnChanged;

    public SavedLocationService(IJSRuntime jsRuntime) { _jsRuntime = jsRuntime; }

    public async Task<SavedLocation?> InitializeAsync()
    {
        if (_isInitialized) return _current;

        try
        {
            var module = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./savedLocationStorage.js");
            _current = await module.InvokeAsync<SavedLocation?>("getSavedLocation");
        }
        catch { _current = null; }

        _isInitialized = true;
        return _current;
    }

    public SavedLocation? Current => _current;

    public async Task<bool> SaveAsync(SavedLocation location)
    {
        _current = location;
        bool persisted = false;

        try
        {
            var module = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./savedLocationStorage.js");
            await module.InvokeVoidAsync("setSavedLocation", location);
            persisted = true;
        }
        catch { }

        OnChanged?.Invoke();
        return persisted;
    }

    public async Task ClearAsync()
    {
        _current = null;

        try
        {
            var module = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./savedLocationStorage.js");
            await module.InvokeVoidAsync("clearSavedLocation");
        }
        catch { }

        OnChanged?.Invoke();
    }
}
