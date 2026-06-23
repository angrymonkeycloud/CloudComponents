using Microsoft.JSInterop;

namespace CloudComponents.Maps.Demo.Services;

/// <summary>
/// Manages Azure Maps subscription key storage and retrieval using browser SessionStorage.
/// SessionStorage is cleared when the browser tab/window is closed.
/// </summary>
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

    /// <summary>
    /// Initialize the service by loading the key from SessionStorage if it exists.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        try
        {
            var module = await _jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./mapsKeyStorage.js");
            _currentKey = await module.InvokeAsync<string?>("getAzureMapsKey");
        }
        catch
        {
            _currentKey = null;
        }

        _isInitialized = true;
    }

    /// <summary>
    /// Check if a subscription key is configured.
    /// </summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_currentKey);

    /// <summary>
    /// Get the current subscription key.
    /// </summary>
    public string GetSubscriptionKey() => _currentKey ?? string.Empty;

    /// <summary>
    /// Set and save the subscription key to SessionStorage.
    /// </summary>
    public async Task SetSubscriptionKeyAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Subscription key cannot be empty.", nameof(key));

        _currentKey = key;

        try
        {
            var module = await _jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./mapsKeyStorage.js");
            await module.InvokeVoidAsync("setAzureMapsKey", key);
        }
        catch
        {
            // If JS fails, keep the in-memory key but don't store it
        }

        OnConfigurationChanged?.Invoke();
    }

    /// <summary>
    /// Clear the stored subscription key from SessionStorage and memory.
    /// </summary>
    public async Task ClearSubscriptionKeyAsync()
    {
        _currentKey = null;

        try
        {
            var module = await _jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./mapsKeyStorage.js");
            await module.InvokeVoidAsync("clearAzureMapsKey");
        }
        catch
        {
            // Continue even if JS fails
        }

        OnConfigurationChanged?.Invoke();
    }
}
