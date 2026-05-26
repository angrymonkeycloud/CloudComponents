namespace CloudComponents.Maps.Models;

/// <summary>
/// Represents a marker on the map. <see cref="Id"/> is auto-generated unless
/// explicitly provided, so existing markers loaded from a data source can keep
/// their stable identity.
/// </summary>
public sealed record MapMarker(
    double Latitude,
    double Longitude,
    string? Label = null,
    string Color = "#e81123")
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
}
