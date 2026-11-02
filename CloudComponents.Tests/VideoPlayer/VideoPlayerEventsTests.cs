using Bunit;
using global::CloudComponents.VideoPlayer;
using Microsoft.AspNetCore.Components;
using Player = global::CloudComponents.VideoPlayer.VideoPlayer;

namespace CloudComponents.Tests.VideoPlayer;

/// <summary>What the player does with the media events the browser reports (the JSON payloads sent through the video's change event).</summary>
public class VideoPlayerEventsTests : PlayerTestBase
{
    private string TimeText(IRenderedComponent<Player> cut) => cut.Find(".amc-videoplayer-timeduration").TextContent.Trim();

    [Fact]
    public async Task Loaded_metadata_provides_the_duration_and_aspect_ratio()
    {
        VideoPlayerMetadata metadata = NewMetadata();
        IRenderedComponent<Player> cut = RenderPlayer(metadata);

        await LoadMetadataAsync(cut);

        Assert.Equal("0:00 / 2:05", TimeText(cut));
        Assert.Equal(VideoStates.Ready, metadata.VideoState);
        Assert.Equal(PlayingStates.NotPlaying, metadata.PlayingState);
    }

    [Theory]
    [InlineData(45, 0, "0 / 45")]
    [InlineData(45, 7, "7 / 45")]
    [InlineData(59.9, 5.5, "5 / 59")]
    [InlineData(60, 0, "0:00 / 1:00")]
    [InlineData(600, 65, "1:05 / 10:00")]
    [InlineData(3599, 3598, "59:58 / 59:59")]
    [InlineData(3600, 0, "0:00:00 / 1:00:00")]
    [InlineData(3725, 3661, "1:01:01 / 1:02:05")]
    [InlineData(36000, 0, "0:00:00 / 10:00:00")]
    [InlineData(90000, 0, "0:00:00:00 / 1:01:00:00")]
    public async Task Time_and_duration_use_the_shortest_clock_that_fits_the_whole_video(double duration, double current, string expected)
    {
        BrowserVideoInfo.Duration = duration;
        IRenderedComponent<Player> cut = RenderPlayer();
        await LoadMetadataAsync(cut);

        await RaiseAsync(cut, "timeupdate", new { currentTime = current });

        Assert.Equal(expected, TimeText(cut));
    }

    [Fact]
    public async Task Time_updates_move_the_displayed_position()
    {
        IRenderedComponent<Player> cut = RenderPlayer();
        await LoadMetadataAsync(cut);

        await RaiseAsync(cut, "timeupdate", new { currentTime = 30.7 });

        Assert.Equal("0:30 / 2:05", TimeText(cut));
    }

    [Fact]
    public async Task Time_updates_are_ignored_while_the_user_drags_the_progress_bar()
    {
        IRenderedComponent<Player> cut = RenderPlayer();
        await LoadMetadataAsync(cut);
        await cut.Find(".amc-videoplayer-progress").FireMouseDownAsync(new());

        await RaiseAsync(cut, "timeupdate", new { currentTime = 90 });

        Assert.Equal("0:00 / 2:05", TimeText(cut));
    }

    [Fact]
    public async Task Reaching_the_end_stops_the_video()
    {
        VideoPlayerMetadata metadata = NewMetadata();
        IRenderedComponent<Player> cut = RenderPlayer(metadata);
        await LoadMetadataAsync(cut);
        await RaiseAsync(cut, "playing");

        await RaiseAsync(cut, "timeupdate", new { currentTime = 125 });

        Assert.Single(Calls("amcVideoPlayerStop"));
        Assert.Equal(0, metadata.CurrentTime);
        Assert.Equal(PlayingStates.NotPlaying, metadata.PlayingState);
        Assert.Empty(Calls("amcVideoPlayerPlay"));
    }

    [Fact]
    public async Task Reaching_the_end_of_a_looping_video_starts_it_again()
    {
        IRenderedComponent<Player> cut = RenderPlayer(NewMetadata(m => m.Loop = true));
        await LoadMetadataAsync(cut);

        await RaiseAsync(cut, "timeupdate", new { currentTime = 125 });

        Assert.Single(Calls("amcVideoPlayerStop"));
        Assert.Single(Calls("amcVideoPlayerPlay"));
    }

    [Fact]
    public async Task Waiting_shows_the_buffering_spinner_and_playing_removes_it()
    {
        VideoPlayerMetadata metadata = NewMetadata();
        IRenderedComponent<Player> cut = RenderPlayer(metadata);
        await LoadMetadataAsync(cut);
        Assert.Empty(cut.Find(".amc-videoplayer-status").Children);

        await RaiseAsync(cut, "waiting");

        Assert.Equal(PlayingStates.Buffering, metadata.PlayingState);
        Assert.Single(cut.Find(".amc-videoplayer-status").QuerySelectorAll("svg"));

        await RaiseAsync(cut, "playing");

        Assert.Equal(PlayingStates.Playing, metadata.PlayingState);
        Assert.Empty(cut.Find(".amc-videoplayer-status").Children);
    }

    [Fact]
    public async Task The_play_event_marks_the_player_as_playing()
    {
        VideoPlayerMetadata metadata = NewMetadata();
        IRenderedComponent<Player> cut = RenderPlayer(metadata);

        await RaiseAsync(cut, "play");

        Assert.Equal(PlayingStates.Playing, metadata.PlayingState);
    }

    [Fact]
    public async Task A_pause_at_the_very_start_counts_as_not_playing_and_later_as_paused()
    {
        VideoPlayerMetadata metadata = NewMetadata();
        IRenderedComponent<Player> cut = RenderPlayer(metadata);
        await RaiseAsync(cut, "playing");

        await RaiseAsync(cut, "pause");
        Assert.Equal(PlayingStates.NotPlaying, metadata.PlayingState);

        await RaiseAsync(cut, "playing");
        await RaiseAsync(cut, "timeupdate", new { currentTime = 10 });
        await RaiseAsync(cut, "pause");

        Assert.Equal(PlayingStates.Paused, metadata.PlayingState);
    }

    [Fact]
    public async Task The_host_receives_every_event_with_its_name_and_state()
    {
        List<VideoEventData> events = [];
        IRenderedComponent<Player> cut = RenderPlayer(configure: p => p.Add(x => x.OnEvent, data => { events.Add(data); }));
        await LoadMetadataAsync(cut);

        await RaiseAsync(cut, "timeupdate", new { currentTime = 12.5, paused = false });
        await RaiseAsync(cut, "pause", new { currentTime = 12.5, paused = true });

        Assert.Equal([VideoEvents.LoadedMetadata, VideoEvents.TimeUpdate, VideoEvents.Pause], events.Select(e => e.EventName));
        Assert.Equal(12.5, events[1].State.CurrentTime);
        Assert.True(events[2].State.Paused);
    }

    [Theory]
    [InlineData("not json at all")]
    [InlineData("{")]
    [InlineData("")]
    [InlineData("null")]
    public async Task A_malformed_event_payload_is_ignored(string payload)
    {
        List<VideoEventData> events = [];
        VideoPlayerMetadata metadata = NewMetadata();
        IRenderedComponent<Player> cut = RenderPlayer(metadata, p => p.Add(x => x.OnEvent, data => { events.Add(data); }));

        await cut.Find("video").FireChangeAsync(new ChangeEventArgs { Value = payload });

        Assert.Empty(events);
        Assert.Equal(VideoStates.Ready, metadata.VideoState);
    }

    [Fact]
    public async Task Events_are_ignored_when_the_player_has_no_video()
    {
        List<VideoEventData> events = [];
        IRenderedComponent<Player> cut = RenderPlayer(NewMetadata(m => m.VideoUrl = null), p => p.Add(x => x.OnEvent, data => { events.Add(data); }));

        await RaiseAsync(cut, "playing");

        Assert.Empty(events);
    }
}
