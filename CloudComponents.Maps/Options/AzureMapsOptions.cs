namespace CloudComponents.Maps.Options;

/// <summary>
/// Library-wide Azure Maps configuration, normally registered through
/// <c>builder.Services.AddAzureMaps(...)</c>.
/// </summary>
public sealed class AzureMapsOptions
{
    /// <summary>Azure Maps subscription (shared) key.</summary>
    public string SubscriptionKey { get; set; } = string.Empty;
}
