namespace AngryMonkey.CloudComponents.Maps.Models;

/// <summary>
/// Represents a colored region overlay on the map defined by actual
/// administrative boundary coordinates (GeoJSON polygon rings).
/// </summary>
public sealed record MapRegion(
    double[][][] Coordinates,
    string FillColor = "rgba(0, 120, 212, 0.15)",
    string StrokeColor = "#0078d4",
    double StrokeWidth = 2)
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    /// <summary>Optional label rendered at the centroid of the region.</summary>
    public string? Label { get; init; }
}


