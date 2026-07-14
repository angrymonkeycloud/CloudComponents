namespace AngryMonkey.CloudComponents.Maps.Models;

/// <summary>
/// Result of <see cref="Components.AzureMap.CanZoneAsync"/> — describes whether an address
/// query can be resolved to a displayable boundary polygon on the map.
/// </summary>
public sealed record ZoneCheckResult
{
    /// <summary>
    /// <c>true</c> when the address was geocoded AND Azure Maps returned a geometry ID,
    /// meaning a real administrative boundary polygon can be drawn.
    /// </summary>
    public required bool CanZone { get; init; }

    /// <summary>
    /// <c>true</c> when the address was geocoded successfully (a position was found),
    /// even if no boundary polygon is available (<see cref="CanZone"/> is <c>false</c>).
    /// </summary>
    public required bool Found { get; init; }

    /// <summary>
    /// Center coordinate of the geocoded location, or <c>null</c> when the address
    /// could not be resolved at all.
    /// </summary>
    public MapCoordinate? Coordinate { get; init; }

    /// <summary>Geometry ID returned by Azure Maps, or <c>null</c> when not available.</summary>
    public string? GeometryId { get; init; }

    /// <summary>A result indicating the address could not be geocoded at all.</summary>
    public static readonly ZoneCheckResult NotFound = new() { CanZone = false, Found = false };
}
