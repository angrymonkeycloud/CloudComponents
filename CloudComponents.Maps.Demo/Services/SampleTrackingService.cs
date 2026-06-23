namespace CloudComponents.Maps.Demo.Services;

using CloudComponents.Maps.Demo.Models;

/// <summary>
/// Provides sample GPS tracking data for demo purposes.
/// Simulates real-time position updates and historical traces.
/// </summary>
public sealed class SampleTrackingService
{
    private static readonly Random _random = new();

    /// <summary>
    /// Get a sample GPS trace (historical data) for replay/history demo.
    /// Simulates a route between multiple cities.
    /// </summary>
    public TrackSession GetSampleTrace(string name = "Sample Route")
    {
        var session = new TrackSession(name)
        {
            Color = "#107c10",
            Description = "Sample route with multiple waypoints"
        };

        // San Francisco to Los Angeles via San Luis Obispo
        var waypoints = new[]
        {
            (37.7749, -122.4194, "San Francisco"),      // Start
            (37.5485, -120.8581, "Fresno"),
            (35.2828, -120.6625, "San Luis Obispo"),
            (34.8405, -120.2359, "Ventura"),
            (34.0522, -118.2437, "Los Angeles")         // End
        };

        var baseTime = DateTime.Now.AddHours(-8);
        int pointsPerSegment = 10;

        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            var (lat1, lng1, label1) = waypoints[i];
            var (lat2, lng2, label2) = waypoints[i + 1];

            for (int j = 0; j <= pointsPerSegment; j++)
            {
                double t = (double)j / pointsPerSegment;
                double lat = lat1 + (lat2 - lat1) * t + (_random.NextDouble() - 0.5) * 0.01;
                double lng = lng1 + (lng2 - lng1) * t + (_random.NextDouble() - 0.5) * 0.01;
                var timestamp = baseTime.AddMinutes(i * pointsPerSegment * 5 + j * 5);

                session.Points.Add(new TrackPoint(lat, lng, timestamp, 15.0));
            }
        }

        if (session.Points.Count > 0)
        {
            session.StartTime = session.Points.First().Timestamp;
            session.EndTime = session.Points.Last().Timestamp;
            session.TotalDistanceKm = CalculateDistance(session.Points);
        }

        return session;
    }

    /// <summary>
    /// Get multiple sample traces for comparison/multi-track demo.
    /// </summary>
    public List<TrackSession> GetMultipleSampleTraces()
    {
        var traces = new List<TrackSession>
        {
            GetSampleTrace("Route 1: SF to LA (I-5)"),
            GetSampleTrace("Route 2: SF to LA (Coast Road)")
        };

        // Adjust the second route to be different
        if (traces.Count > 1)
        {
            traces[1].Color = "#c50f1f";
            foreach (var point in traces[1].Points)
            {
                // Shift slightly to show as a different route
                var newLat = point.Latitude + (_random.NextDouble() - 0.5) * 0.5;
                var newLng = point.Longitude + (_random.NextDouble() - 0.5) * 0.5;
                traces[1].Points[traces[1].Points.IndexOf(point)] = 
                    new TrackPoint(newLat, newLng, point.Timestamp, point.Accuracy, point.Label);
            }
        }

        return traces;
    }

    /// <summary>
    /// Create a live tracking simulator that generates pseudo-random position updates.
    /// </summary>
    public LiveTracker CreateLiveTracker(string name, double startLat, double startLng)
    {
        return new LiveTracker(name, startLat, startLng);
    }

    /// <summary>
    /// Simulate the next position update for a live tracker (random walk around current position).
    /// </summary>
    public (double lat, double lng) SimulateNextPosition(LiveTracker tracker)
    {
        // Random walk: move slightly in a random direction
        double latChange = (_random.NextDouble() - 0.5) * 0.01; // ~1 km max
        double lngChange = (_random.NextDouble() - 0.5) * 0.01;

        double newLat = Math.Max(-90, Math.Min(90, tracker.CurrentLat + latChange));
        double newLng = Math.Max(-180, Math.Min(180, tracker.CurrentLng + lngChange));

        return (newLat, newLng);
    }

    /// <summary>
    /// Calculate total distance traveled along a track using Haversine formula.
    /// </summary>
    private double CalculateDistance(List<TrackPoint> points)
    {
        if (points.Count < 2) return 0;

        double totalKm = 0;
        for (int i = 0; i < points.Count - 1; i++)
        {
            totalKm += HaversineDistance(
                points[i].Latitude, points[i].Longitude,
                points[i + 1].Latitude, points[i + 1].Longitude);
        }

        return Math.Round(totalKm, 2);
    }

    /// <summary>
    /// Calculate distance between two coordinates using Haversine formula (in km).
    /// </summary>
    private double HaversineDistance(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371; // Earth radius in km
        double dLat = ToRadians(lat2 - lat1);
        double dLng = ToRadians(lng2 - lng1);
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                   Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        double c = 2 * Math.Asin(Math.Sqrt(a));
        return R * c;
    }

    private double ToRadians(double degrees) => degrees * Math.PI / 180;

    /// <summary>
    /// Get a sample multi-track live tracking session.
    /// </summary>
    public TrackingSession GetSampleLiveTracking()
    {
        var session = new TrackingSession("Live Fleet Tracking");

        var trackers = new[]
        {
            ("Vehicle 1", 37.7749, -122.4194),
            ("Vehicle 2", 37.7849, -122.4100),
            ("Vehicle 3", 37.7649, -122.4300),
        };

        foreach (var (name, lat, lng) in trackers)
        {
            var tracker = CreateLiveTracker(name, lat, lng);
            var track = new TrackSession(name);
            track.Points.Add(new TrackPoint(lat, lng, DateTime.Now));
            session.Tracks.Add(track);
        }

        return session;
    }
}
