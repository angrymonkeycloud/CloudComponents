namespace AngryMonkey.CloudComponents.Maps.Models;

/// <summary>
/// A single timestamped position within a <see cref="MapTimeline"/>.
/// Points with a <see cref="Label"/> are treated as significant places: they are never
/// clustered away, show their name on the map, and report how long the journey stayed
/// there. Unlabeled points are raw GPS fixes that stack into clusters when zoomed out.
/// </summary>
public sealed record MapTimelinePoint(
    double Latitude,
    double Longitude,
    DateTime Timestamp)
{
    /// <summary>Optional place name (e.g. "Office", "Coffee Shop"). Labeled points are always shown.</summary>
    public string? Label { get; init; }

    /// <summary>Optional extra detail surfaced in the point's popup (e.g. "Stayed 45 min").</summary>
    public string? Description { get; init; }
}

/// <summary>
/// A tracking/history timeline rendered on the map as a route line (with direction
/// arrows) plus its points. Detail adapts to the zoom level: the raw GPS fixes stack
/// into numbered clusters when zoomed out and break apart as the user zooms in or
/// clicks a cluster, while labeled places and the start/end badges stay visible at
/// every zoom. Clicking any point opens a details popup and raises
/// <c>OnTimelinePointClick</c>.
/// </summary>
public sealed record MapTimeline
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    /// <summary>Display name used in point popups (e.g. the tracked person/vehicle).</summary>
    public string? Name { get; init; }

    /// <summary>Line and point color.</summary>
    public string Color { get; init; } = "#0078d4";

    /// <summary>When <c>true</c> (default) the points are connected with a route line.</summary>
    public bool ShowLine { get; init; } = true;

    /// <summary>Chronologically ordered points.</summary>
    public IReadOnlyList<MapTimelinePoint> Points { get; init; } = [];
}

/// <summary>Payload for <c>AzureMap.OnTimelinePointClick</c>.</summary>
public sealed record MapTimelinePointClickEventArgs(
    MapTimeline Timeline,
    MapTimelinePoint Point,
    int PointIndex);
