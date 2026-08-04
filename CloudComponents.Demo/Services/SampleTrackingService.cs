using AngryMonkey.CloudComponents.Maps.Models;
using CloudComponents.Demo.Models;

namespace CloudComponents.Demo.Services;

public sealed class SampleTrackingService
{
    private static readonly Random _random = new();

    // Pool of "places" for the daily-timeline generator. Each day picks a
    // deterministic subset so changing the date on the demo page produces a
    // visibly different journey. Coordinates are around San Francisco.
    private static readonly (double Lat, double Lng, string Name, string Detail)[] _places =
    [
        (37.7648, -122.4214, "Coffee Shop", "Quick espresso stop — 15 min"),
        (37.7897, -122.4009, "Office", "Worked on site — 3 h 20 min"),
        (37.7955, -122.3937, "Ferry Building", "Lunch at the market — 50 min"),
        (37.8024, -122.4058, "Client Visit", "Project review meeting — 1 h 10 min"),
        (37.8080, -122.4177, "Fisherman's Wharf", "Walked along the pier — 35 min"),
        (37.7694, -122.4862, "Golden Gate Park", "Afternoon run — 40 min"),
        (37.7680, -122.4290, "Gym", "Evening workout — 55 min"),
        (37.7793, -122.4193, "City Hall", "Paperwork errand — 25 min"),
        (37.7614, -122.4356, "Grocery Store", "Picked up groceries — 20 min")
    ];

    private const double HomeLat = 37.7599;
    private const double HomeLng = -122.4148;

    /// <summary>
    /// Deterministic full-day movement history for the given date: starts and ends
    /// at "Home", visits a date-dependent subset of places, with dense GPS points
    /// recorded along the way. Same date always yields the same timeline.
    /// </summary>
    public List<MapTimelinePoint> GetDailyTimeline(DateOnly date)
    {
        var rnd = new Random(date.DayNumber);

        // Pick 4–6 places for the day, keeping the pool order for a sane route.
        int stopCount = 4 + rnd.Next(3);
        var stops = Enumerable.Range(0, _places.Length)
            .OrderBy(_ => rnd.Next())
            .Take(stopCount)
            .OrderBy(i => i)
            .Select(i => _places[i])
            .ToList();

        var points = new List<MapTimelinePoint>();
        var time = date.ToDateTime(new TimeOnly(7, 30)).AddMinutes(rnd.Next(0, 45));

        (double Lat, double Lng) current = (HomeLat, HomeLng);
        points.Add(new MapTimelinePoint(HomeLat, HomeLng, time)
        {
            Label = "Home",
            Description = "Left home"
        });

        foreach (var stop in stops)
        {
            // Travel: a GPS fix roughly every 1–2 minutes along the way.
            int samples = 8 + rnd.Next(8);
            for (int j = 1; j < samples; j++)
            {
                double t = (double)j / samples;
                double lat = current.Lat + (stop.Lat - current.Lat) * t + (rnd.NextDouble() - 0.5) * 0.0015;
                double lng = current.Lng + (stop.Lng - current.Lng) * t + (rnd.NextDouble() - 0.5) * 0.0015;
                time = time.AddMinutes(1 + rnd.NextDouble());
                points.Add(new MapTimelinePoint(lat, lng, time));
            }

            time = time.AddMinutes(2);
            points.Add(new MapTimelinePoint(stop.Lat, stop.Lng, time)
            {
                Label = stop.Name,
                Description = stop.Detail
            });

            // Dwell time at the place before moving on.
            time = time.AddMinutes(20 + rnd.Next(80));
            current = (stop.Lat, stop.Lng);
        }

        // Head back home.
        int homeSamples = 8 + rnd.Next(8);
        for (int j = 1; j < homeSamples; j++)
        {
            double t = (double)j / homeSamples;
            double lat = current.Lat + (HomeLat - current.Lat) * t + (rnd.NextDouble() - 0.5) * 0.0015;
            double lng = current.Lng + (HomeLng - current.Lng) * t + (rnd.NextDouble() - 0.5) * 0.0015;
            time = time.AddMinutes(1 + rnd.NextDouble());
            points.Add(new MapTimelinePoint(lat, lng, time));
        }
        time = time.AddMinutes(2);
        points.Add(new MapTimelinePoint(HomeLat, HomeLng, time)
        {
            Label = "Home",
            Description = "Back home"
        });

        return points;
    }

    /// <summary>Total distance in km along a sequence of timeline points.</summary>
    public double CalculateTimelineDistanceKm(IReadOnlyList<MapTimelinePoint> points)
    {
        double total = 0;
        for (int i = 0; i < points.Count - 1; i++)
            total += HaversineDistance(points[i].Latitude, points[i].Longitude,
                                       points[i + 1].Latitude, points[i + 1].Longitude);
        return Math.Round(total, 2);
    }

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
