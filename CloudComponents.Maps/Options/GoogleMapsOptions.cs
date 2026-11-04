namespace AngryMonkey.CloudComponents.Maps.Options;

/// <summary>
/// Library-wide Google Maps configuration, normally registered through
/// <c>builder.Services.AddGoogleMaps(...)</c>.
/// </summary>
public sealed class GoogleMapsOptions
{
    /// <summary>Browser API key used to load the Google Maps JavaScript API.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Vector Map ID used by Advanced Markers and cloud-based map styling.</summary>
    public string MapId { get; set; } = string.Empty;
}
