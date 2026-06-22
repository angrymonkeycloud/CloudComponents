namespace CloudComponents.Maps.Models;

/// <summary>
/// Result of a geocode operation, including the center point, viewport,
/// and the geometry ID that can be used to fetch the actual administrative
/// boundary polygon from the Azure Maps Search Polygon API.
/// </summary>
public sealed record GeocodeResult(
    double Latitude,
    double Longitude,
    double North,
    double South,
    double East,
    double West,
    string? GeometryId);

