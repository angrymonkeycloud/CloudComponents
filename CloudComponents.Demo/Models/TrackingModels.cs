using AngryMonkey.Cloud.Geography;
using CloudComponents.Maps.Models;

namespace CloudComponents.Demo.Models;

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
    public string? CountrySecondarySubdivision { get; set; }
    public string? CountryTertiarySubdivision { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime ResolvedAt { get; set; }

    public AddressInfo() { ResolvedAt = DateTime.Now; }

    public string FormattedAddress
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(Street)) parts.Add(Street);
            if (!string.IsNullOrEmpty(City)) parts.Add(City);
            if (!string.IsNullOrEmpty(Subdivision)) parts.Add(Subdivision);
            if (!string.IsNullOrEmpty(CountryRegion)) parts.Add(CountryRegion);
            return string.Join(", ", parts.Where(p => !string.IsNullOrEmpty(p)));
        }
    }
}

public sealed class SubdivisionEntry
{
    public string Label { get; init; } = string.Empty;
    public string AzureName { get; init; } = string.Empty;
    public string? AzureCode { get; init; }
    public global::AngryMonkey.Cloud.Geography.Subdivision? GeoMatch { get; init; }
    public List<global::AngryMonkey.Cloud.Geography.Subdivision>? Children { get; init; }

    // Display fields used by Regions page
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
}
