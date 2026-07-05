using CloudComponents.Demo.Models;

namespace CloudComponents.Demo.Services;

public sealed class SampleTrackingService
{
    private static readonly Random _random = new();

    public TrackSession GetSampleTrace(string name = "Sample Route")
    {
        var session = new TrackSession(name)
        {
            Color = "#107c10",
            Description = "Sample route with multiple waypoints"
        };

        var waypoints = new[]
        {
            (37.7749, -122.4194, "San Francisco"),
            (37.5485, -120.8581, "Fresno"),
            (35.2828, -120.6625, "San Luis Obispo"),
            (34.8405, -120.2359, "Ventura"),
            (34.0522, -118.2437, "Los Angeles")
        };

        var baseTime = DateTime.Now.AddHours(-8);
        int pointsPerSegment = 10;

        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            var (lat1, lng1, _) = waypoints[i];
            var (lat2, lng2, _) = waypoints[i + 1];

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

    public List<TrackSession> GetMultipleSampleTraces() =>
    [
        GetSampleTrace("Route 1: SF to LA (I-5)"),
        GetSampleTrace("Route 2: SF to LA (Coastal)")
    ];

    public LiveTracker CreateLiveTracker(string name, double startLat, double startLng) =>
        new(name, startLat, startLng);

    public (double lat, double lng) SimulateNextPosition(LiveTracker tracker)
    {
        double newLat = Math.Max(-90, Math.Min(90, tracker.CurrentLat + (_random.NextDouble() - 0.5) * 0.01));
        double newLng = Math.Max(-180, Math.Min(180, tracker.CurrentLng + (_random.NextDouble() - 0.5) * 0.01));
        return (newLat, newLng);
    }

    private double CalculateDistance(List<TrackPoint> points)
    {
        if (points.Count < 2) return 0;
        double total = 0;
        for (int i = 0; i < points.Count - 1; i++)
            total += HaversineDistance(points[i].Latitude, points[i].Longitude,
                                       points[i + 1].Latitude, points[i + 1].Longitude);
        return Math.Round(total, 2);
    }

    private double HaversineDistance(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371;
        double dLat = ToRad(lat2 - lat1);
        double dLng = ToRad(lng2 - lng1);
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                   Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return R * 2 * Math.Asin(Math.Sqrt(a));
    }

    private static double ToRad(double deg) => deg * Math.PI / 180;
}
