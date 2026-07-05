using AngryMonkey.Cloud.Geography;
using CloudComponents.Maps.Models;

namespace CloudComponents.Maps.Demo.Models;

/// <summary>
/// Represents a single point in a GPS trace with timestamp and accuracy.
/// </summary>
public sealed record TrackPoint(
    double Latitude,
    double Longitude,
    DateTime Timestamp,
    double? Accuracy = null,
    string? Label = null)
{
    public MapCoordinate ToCoordinate() => new(Latitude, Longitude);

    public MapMarker ToMarker(string trackId) => new(Latitude, Longitude)
    {
        Label = Label ?? $"Track {trackId}",
        Title = Timestamp.ToString("HH:mm:ss"),
        Subdivision = $"Point at {Timestamp:HH:mm:ss}"
    };
}

/// <summary>
/// Represents a complete GPS trace session with multiple points and metadata.
/// </summary>
public sealed class TrackSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; }
    public List<TrackPoint> Points { get; set; } = new();
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double TotalDistanceKm { get; set; }
    public string? Description { get; set; }
    public string Color { get; set; } = "#0078d4";

    public TrackSession(string name = "Unnamed Track")
    {
        Name = name;
        StartTime = DateTime.Now;
        EndTime = DateTime.Now;
    }

    public TimeSpan Duration => EndTime - StartTime;
}

/// <summary>
/// Represents a live tracked object with current position and metadata.
/// </summary>
public sealed class LiveTracker
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; }
    public double CurrentLat { get; set; }
    public double CurrentLng { get; set; }
    public DateTime LastUpdate { get; set; }
    public List<TrackPoint> PositionHistory { get; set; } = new();
    public string Color { get; set; } = "#0078d4";
    public string? IconUrl { get; set; }

    public LiveTracker(string name, double initialLat, double initialLng)
    {
        Name = name;
        CurrentLat = initialLat;
        CurrentLng = initialLng;
        LastUpdate = DateTime.Now;
    }

    public MapMarker ToMarker() => new(CurrentLat, CurrentLng)
    {
        Label = Name,
        Title = $"{Name} - {LastUpdate:HH:mm:ss}",
        Color = Color
    };

    public void UpdatePosition(double lat, double lng)
    {
        PositionHistory.Add(new TrackPoint(CurrentLat, CurrentLng, LastUpdate));
        CurrentLat = lat;
        CurrentLng = lng;
        LastUpdate = DateTime.Now;
    }
}

/// <summary>
/// Container for multiple track sessions used in live/replay scenarios.
/// </summary>
public sealed class TrackingSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; }
    public List<TrackSession> Tracks { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? Notes { get; set; }

    public TrackingSession(string name = "Tracking Session")
    {
        Name = name;
    }

    public (double south, double west, double north, double east) GetBounds()
    {
        if (Tracks.Count == 0 || Tracks.All(t => t.Points.Count == 0))
            return (0, 0, 0, 0);

        var allPoints = Tracks.SelectMany(t => t.Points).ToList();
        double minLat = allPoints.Min(p => p.Latitude);
        double maxLat = allPoints.Max(p => p.Latitude);
        double minLng = allPoints.Min(p => p.Longitude);
        double maxLng = allPoints.Max(p => p.Longitude);

        return (minLat, minLng, maxLat, maxLng);
    }
}

/// <summary>
/// Represents a reverse geocode result with administrative subdivision data.
/// </summary>
public sealed class AddressInfo
{
    public string? Street { get; set; }
    public string? Neighborhood { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? County { get; set; }
    public string? District { get; set; }
    public string? CountryRegion { get; set; }
    public string? CountryCode { get; set; }
    public string? Subdivision { get; set; }
    public string? SubdivisionCode { get; set; }
    public string? CountryTertiarySubdivision { get; set; }
    public string? CountrySecondarySubdivision { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime ResolvedAt { get; set; }

    public AddressInfo()
    {
        ResolvedAt = DateTime.Now;
    }

    public string FormattedAddress
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(Street)) parts.Add(Street);
            if (!string.IsNullOrEmpty(City)) parts.Add(City);
            if (!string.IsNullOrEmpty(District)) parts.Add(District);
            if (!string.IsNullOrEmpty(Subdivision)) parts.Add(Subdivision);
            if (!string.IsNullOrEmpty(CountryRegion)) parts.Add(CountryRegion);
            return string.Join(", ", parts.Where(p => !string.IsNullOrEmpty(p)));
        }
    }
}

/// <summary>
/// Holds a single subdivision level from Azure Maps alongside the Cloud Geography match result.
/// </summary>
public sealed class SubdivisionEntry
{
    public string Label { get; init; } = string.Empty;      // e.g. "State", "County", "District"
    public string AzureName { get; init; } = string.Empty;  // raw name from Azure Maps
    public string? AzureCode { get; init; }                 // raw code from Azure Maps (may be null)
    public Subdivision? GeoMatch { get; init; }             // Cloud Geography match (null = no match)
    public List<Subdivision>? Children { get; init; }       // children of matched subdivision
}
