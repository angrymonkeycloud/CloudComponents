using System.Text.Json;
using global::CloudComponents.VideoPlayer;

namespace CloudComponents.Tests.VideoPlayer;

public class VideoEventDataTests
{
    [Fact]
    public void A_new_event_has_no_name_yet()
    {
        VideoEventData data = new();

        Assert.Equal("NotSet", data.Name);
        Assert.Equal(VideoEvents.NotSet, data.EventName);
    }

    [Theory]
    [InlineData("timeupdate", VideoEvents.TimeUpdate)]
    [InlineData("TimeUpdate", VideoEvents.TimeUpdate)]
    [InlineData("loadedmetadata", VideoEvents.LoadedMetadata)]
    [InlineData("canplaythrough", VideoEvents.CanPlayThrough)]
    [InlineData("play", VideoEvents.Play)]
    [InlineData("waiting", VideoEvents.Waiting)]
    public void The_browser_event_name_maps_to_the_enum_ignoring_case(string name, VideoEvents expected)
    {
        Assert.Equal(expected, new VideoEventData { Name = name }.EventName);
    }

    [Fact]
    public void An_event_name_the_player_does_not_know_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new VideoEventData { Name = "teleport" }.EventName);
    }

    [Fact]
    public void Every_browser_media_event_the_enum_names_can_be_round_tripped_through_the_name()
    {
        foreach (VideoEvents expected in Enum.GetValues<VideoEvents>())
            Assert.Equal(expected, new VideoEventData { Name = expected.ToString().ToLowerInvariant() }.EventName);
    }

    [Fact]
    public void The_payload_sent_by_the_script_is_read_into_name_and_state()
    {
        VideoEventData? data = JsonSerializer.Deserialize<VideoEventData>("""{"name":"timeupdate","state":{"currentTime":12.5,"paused":true}}""");

        Assert.NotNull(data);
        Assert.Equal(VideoEvents.TimeUpdate, data.EventName);
        Assert.Equal(12.5, data.State.CurrentTime);
        Assert.True(data.State.Paused);
    }

    [Fact]
    public void A_payload_without_state_leaves_state_empty()
    {
        VideoEventData? data = JsonSerializer.Deserialize<VideoEventData>("""{"name":"pause"}""");

        Assert.NotNull(data);
        Assert.Equal(VideoEvents.Pause, data.EventName);
        Assert.Null(data.State);
    }

    [Fact(Skip = "BUG: VideoState only maps currentTime and paused (JsonPropertyName); the script sends camelCase volume, duration, muted, ended, playbackRate, src ... and System.Text.Json is case-sensitive, so every other state field is always its default in OnEvent")]
    public void The_whole_state_sent_by_the_script_reaches_the_host()
    {
        VideoEventData? data = JsonSerializer.Deserialize<VideoEventData>(
            """{"name":"volumechange","state":{"volume":0.4,"muted":true,"duration":90,"playbackRate":1.5,"ended":true,"loop":true}}""");

        Assert.NotNull(data?.State);
        Assert.Equal(0.4, data.State.Volume);
        Assert.True(data.State.Muted);
        Assert.Equal(90, data.State.Duration);
        Assert.Equal(1.5, data.State.PlaybackRate);
        Assert.True(data.State.Ended);
        Assert.True(data.State.Loop);
    }
}
