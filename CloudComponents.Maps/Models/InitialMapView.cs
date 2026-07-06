namespace AngryMonkey.CloudComponents.Maps.Models;

/// <summary>
/// Initial camera setup for an <c>AzureMap</c>. When <c>null</c> is passed to
/// the component, the map will attempt to use the device's current location.
/// </summary>
/// <param name="Latitude">Initial center latitude.</param>
/// <param name="Longitude">Initial center longitude.</param>
/// <param name="Zoom">Initial zoom level (Azure Maps range is roughly 0�22).</param>
public sealed record InitialMapView(double Latitude, double Longitude, double Zoom = 12)
{
    /// <summary>A reasonable world-level fallback view.</summary>
    public static InitialMapView World { get; } = new(0, 0, 2);
}
