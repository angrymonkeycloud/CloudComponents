using Microsoft.JSInterop;

namespace CloudComponents.Demo.Services;

public class AzureMapsConfigurationService
{
    private readonly IJSRuntime _jsRuntime;
    private string? _currentKey;
    private bool _isInitialized;

    public event Action? OnConfigurationChanged;

    public AzureMapsConfigurationService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            var module = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./mapsKeyStorage.js");
            _currentKey = await module.InvokeAsync<string?>("getAzureMapsKey");
        }
        catch
        {
            _currentKey = null;
        }

        _isInitialized = true;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_currentKey);

    public string GetSubscriptionKey() => _currentKey ?? string.Empty;

    public async Task SetSubscriptionKeyAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Subscription key cannot be empty.", nameof(key));

        _currentKey = key;

        try
        {
            var module = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./mapsKeyStorage.js");
            await module.InvokeVoidAsync("setAzureMapsKey", key);
        }
        catch { }

        OnConfigurationChanged?.Invoke();
    }

    public async Task ClearSubscriptionKeyAsync()
    {
        _currentKey = null;

        try
        {
            var module = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./mapsKeyStorage.js");
            await module.InvokeVoidAsync("clearAzureMapsKey");
        }
        catch { }

        OnConfigurationChanged?.Invoke();
    }
}
