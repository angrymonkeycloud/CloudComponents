using System.Text.Json;
using AngryMonkey.CloudComponents.Maps.Models;
using CloudComponents.Tests.Maps.Component;

namespace CloudComponents.Tests.Maps.Models;

public sealed class MapTimelineTests
{
    private static readonly DateTime Noon = new(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void A_timeline_has_the_documented_defaults()
    {
        MapTimeline timeline = new();

        Assert.Null(timeline.Name);
        Assert.Equal("#0078d4", timeline.Color);
        Assert.True(timeline.ShowLine);
        Assert.NotNull(timeline.Points);
        Assert.Empty(timeline.Points);
    }

    [Fact]
    public void The_generated_id_is_a_32_digit_hex_guid_stable_per_instance_and_different_between_instances()
    {
        MapTimeline a = new();
        MapTimeline b = new();

        Assert.Matches("^[0-9a-f]{32}$", a.Id);
        Assert.Equal(a.Id, a.Id);
        Assert.NotEqual(a.Id, b.Id);
    }

    [Fact]
    public void A_copy_keeps_the_id()
    {
        MapTimeline timeline = new() { Name = "Courier" };

        Assert.Equal(timeline.Id, (timeline with { Name = "Other" }).Id);
    }

    [Fact]
    public void A_point_has_no_label_or_description_unless_given()
    {
        MapTimelinePoint point = new(1, 2, Noon);

        Assert.Equal((1.0, 2.0, Noon), (point.Latitude, point.Longitude, point.Timestamp));
        Assert.Null(point.Label);
        Assert.Null(point.Description);
    }

    [Fact]
    public void Points_with_the_same_content_are_equal_and_a_label_makes_them_different()
    {
        MapTimelinePoint plain = new(1, 2, Noon);

        Assert.Equal(plain, new MapTimelinePoint(1, 2, Noon));
        Assert.NotEqual(plain, plain with { Label = "Office" });
        Assert.NotEqual(plain, plain with { Description = "Stayed 45 min" });
        Assert.NotEqual(plain, plain with { Timestamp = Noon.AddMinutes(1) });
    }

    [Fact]
    public void The_click_args_carry_the_timeline_point_and_index()
    {
        MapTimelinePoint point = new(1, 2, Noon);
        MapTimeline timeline = new() { Points = [point] };

        MapTimelinePointClickEventArgs args = new(timeline, point, 0);

        Assert.Same(timeline, args.Timeline);
        Assert.Same(point, args.Point);
        Assert.Equal(0, args.PointIndex);
    }

    [Fact]
    public void A_point_serialises_its_timestamp_as_an_ISO_8601_string()
    {
        MapTimelinePoint point = new(1, 2, Noon) { Label = "A" };

        string json = JsonSerializer.Serialize(point, FakeMapJs.Wire);

        Assert.Equal("""{"latitude":1,"longitude":2,"timestamp":"2026-05-01T12:00:00Z","label":"A","description":null}""", json);
    }

    [Fact]
    public void A_local_timestamp_is_sent_without_a_zone_designator()
    {
        MapTimelinePoint point = new(1, 2, new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Unspecified));

        string json = JsonSerializer.Serialize(point, FakeMapJs.Wire);

        Assert.Contains("\"timestamp\":\"2026-05-01T12:00:00\"", json);
    }

    [Fact]
    public void A_timeline_serialises_with_the_names_the_script_reads()
    {
        MapTimeline timeline = new() { Id = "t", Name = "N", Color = "#fff", ShowLine = false, Points = [new MapTimelinePoint(1, 2, Noon)] };

        JsonElement json = JsonSerializer.SerializeToElement(timeline, FakeMapJs.Wire);

        Assert.Equal(["id", "name", "color", "showLine", "points"], json.EnumerateObject().Select(p => p.Name));
        Assert.False(json.GetProperty("showLine").GetBoolean());
        Assert.Equal(1, json.GetProperty("points").GetArrayLength());
    }
}
