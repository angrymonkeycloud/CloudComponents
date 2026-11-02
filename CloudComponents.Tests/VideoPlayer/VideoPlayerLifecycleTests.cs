using Bunit;
using Bunit.JSInterop;
using global::CloudComponents.VideoPlayer;
using Microsoft.JSInterop;
using Player = global::CloudComponents.VideoPlayer.VideoPlayer;

namespace CloudComponents.Tests.VideoPlayer;

public class VideoPlayerLifecycleTests : PlayerTestBase
{
    [Fact]
    public void Rendering_a_player_initialises_the_video_element_once()
    {
        RenderPlayer();

        Assert.Single(Calls("amcVideoPlayerInit"));
    }

    [Fact]
    public void The_video_element_gets_the_url_and_is_inline_playable()
    {
        IRenderedComponent<Player> cut = RenderPlayer(NewMetadata(m => m.VideoUrl = "https://media.example/a.mp4"));

        var video = cut.Find("video");

        Assert.Equal("https://media.example/a.mp4", video.GetAttribute("src"));
        Assert.True(video.HasAttribute("playsinline"));
    }

    [Fact]
    public void The_player_registers_the_six_browser_events_it_reacts_to()
    {
        RenderPlayer();

        string[] registered = [.. Calls("amcVideoPlayerRegisterCustomEventHandler").Select(call => (string)call.Arguments[1]!)];

        Assert.Equal(["timeupdate", "play", "playing", "pause", "waiting", "loadedmetadata"], registered);
    }

    [Fact]
    public void Each_event_asks_for_the_whole_state_when_the_host_gave_no_options()
    {
        RenderPlayer();

        foreach (JSRuntimeInvocation call in Calls("amcVideoPlayerRegisterCustomEventHandler"))
        {
            string[] payload = Assert.IsType<string[]>(call.Arguments[2]);

            Assert.Contains("currentTime", payload);
            Assert.Contains("paused", payload);
            Assert.Contains("volume", payload);
        }
    }

    [Fact]
    public void Host_event_options_select_which_state_each_event_carries()
    {
        Dictionary<VideoEvents, VideoStateOptions> options = new()
        {
            [VideoEvents.TimeUpdate] = new() { CurrentTime = true },
            [VideoEvents.Play] = new() { Paused = true, Volume = true },
        };

        RenderPlayer(configure: parameters => parameters.Add(p => p.VideoEventOptions, options));

        string[] ForEvent(string name) => (string[])Calls("amcVideoPlayerRegisterCustomEventHandler").Single(call => (string)call.Arguments[1]! == name).Arguments[2]!;

        Assert.Equal(["currentTime"], ForEvent("timeupdate"));
        Assert.Equal(["paused", "volume"], ForEvent("play"));
    }

    [Fact(Skip = "BUG: VideoPlayer.Lifecycle Implement() passes a default VideoStateOptions to TryGetValue's out parameter, so once the host supplies options for any event, every other event (timeupdate, loadedmetadata ...) is registered with an empty payload and CurrentTime never updates")]
    public void Events_the_host_gave_no_options_for_still_carry_the_state()
    {
        Dictionary<VideoEvents, VideoStateOptions> options = new() { [VideoEvents.Play] = new() { Paused = true } };

        RenderPlayer(configure: parameters => parameters.Add(p => p.VideoEventOptions, options));

        string[] timeUpdate = (string[])Calls("amcVideoPlayerRegisterCustomEventHandler").Single(call => (string)call.Arguments[1]! == "timeupdate").Arguments[2]!;

        Assert.Contains("currentTime", timeUpdate);
    }

    [Fact]
    public void A_successful_start_up_leaves_the_player_ready_and_fires_the_ready_and_state_callbacks()
    {
        List<VideoStates> states = [];
        int ready = 0;
        VideoPlayerMetadata metadata = NewMetadata();

        IRenderedComponent<Player> cut = RenderPlayer(metadata, parameters => parameters
            .Add(p => p.OnVideoReady, () => { Interlocked.Increment(ref ready); })
            .Add(p => p.OnVideoStateChanged, state => { lock (states) states.Add(state); }));

        Assert.Equal(VideoStates.Ready, metadata.VideoState);
        cut.WaitForAssertion(() => Assert.Equal(1, Volatile.Read(ref ready)));
        cut.WaitForAssertion(() => { lock (states) Assert.Contains(VideoStates.Ready, states); });
    }

    [Fact]
    public async Task A_failing_initialisation_script_puts_only_this_player_in_the_error_state()
    {
        JSInterop.SetupVoid("amcVideoPlayerInit", _ => true).SetException(new JSException("hls asset missing"));
        int errors = 0;
        VideoPlayerMetadata metadata = NewMetadata();

        IRenderedComponent<Player> cut = RenderPlayer(metadata, parameters => parameters.Add(p => p.OnVideoError, () => { Interlocked.Increment(ref errors); }));

        Assert.Equal(VideoStates.Error, metadata.VideoState);
        cut.WaitForAssertion(() => Assert.Equal(1, errors));
        Assert.Equal("Error", cut.Find(".amc-videoplayer-error").TextContent.Trim());
        Assert.Empty(cut.FindAll(".amc-videoplayer-controls"));
        Assert.Empty(Calls("amcVideoPlayerRegisterCustomEventHandler"));
    }

    [Fact]
    public async Task A_player_in_the_error_state_refuses_to_play()
    {
        JSInterop.SetupVoid("amcVideoPlayerInit", _ => true).SetException(new JSException("boom"));
        IRenderedComponent<Player> cut = RenderPlayer();

        await cut.InvokeAsync(() => cut.Instance.PlayVideo());

        Assert.Empty(Calls("amcVideoPlayerPlay"));
    }

    [Fact]
    public void The_error_state_is_reported_through_the_state_changed_callback_too()
    {
        JSInterop.SetupVoid("amcVideoPlayerInit", _ => true).SetException(new JSException("boom"));
        List<VideoStates> states = [];

        IRenderedComponent<Player> cut = RenderPlayer(configure: parameters => parameters.Add(p => p.OnVideoStateChanged, state => { lock (states) states.Add(state); }));

        cut.WaitForAssertion(() => Assert.Contains(VideoStates.Error, states));
    }

    [Fact]
    public void A_player_without_a_url_does_not_try_to_play_even_with_autoplay()
    {
        RenderPlayer(NewMetadata(m => { m.VideoUrl = null; m.Autoplay = true; }));

        Assert.Empty(Calls("amcVideoPlayerPlay"));
        Assert.Empty(Calls("amcVideoPlayerGetVideoInfo"));
    }

    [Fact(Skip = "BUG: Init() sets VideoState NoVideo for a missing url and then unconditionally overwrites it with Ready, so a player without a video shows the full controls and the NoVideo state is never observable")]
    public void A_player_without_a_url_stays_in_the_no_video_state()
    {
        VideoPlayerMetadata metadata = NewMetadata(m => m.VideoUrl = null);

        RenderPlayer(metadata);

        Assert.Equal(VideoStates.NoVideo, metadata.VideoState);
    }

    [Fact]
    public void Autoplay_asks_the_video_element_to_play_when_the_player_starts()
    {
        VideoPlayerMetadata metadata = NewMetadata(m => m.Autoplay = true);

        RenderPlayer(metadata);

        Assert.NotEmpty(Calls("amcVideoPlayerPlay"));
        Assert.Equal(PlayingStates.Playing, metadata.PlayingState);
    }

    [Fact]
    public async Task Without_autoplay_loading_the_metadata_does_not_start_playback()
    {
        IRenderedComponent<Player> cut = RenderPlayer();

        await RaiseAsync(cut, "loadedmetadata");

        Assert.Empty(Calls("amcVideoPlayerPlay"));
    }

    [Theory]
    [InlineData("https://tv.example/live/stream.m3u8")]
    [InlineData("https://tv.example/live/STREAM.M3U8")]
    [InlineData("https://tv.example/live/stream.m3u8?token=abc")]
    public void A_playlist_url_is_treated_as_a_live_stream_and_hls_is_started(string url)
    {
        VideoPlayerMetadata metadata = NewMetadata(m => m.VideoUrl = url);

        RenderPlayer(metadata);

        Assert.True(metadata.IsLive);
        Assert.Single(Calls("amcVideoPlayerIsStreamingPlayableNatively"));

        object?[] initialise = Assert.Single(Calls("amcVideoPlayerInitializeStreamingUrl")).Arguments.ToArray();

        Assert.Equal(url, initialise[1]);
    }

    [Fact]
    public void A_plain_file_is_not_a_live_stream_and_never_touches_hls()
    {
        VideoPlayerMetadata metadata = NewMetadata();

        RenderPlayer(metadata);

        Assert.False(metadata.IsLive);
        Assert.Empty(Calls("amcVideoPlayerIsStreamingPlayableNatively"));
        Assert.Empty(Calls("amcVideoPlayerInitializeStreamingUrl"));
    }

    [Fact]
    public void A_browser_that_plays_hls_natively_does_not_load_the_hls_library()
    {
        JSInterop.Setup<bool>("amcVideoPlayerIsStreamingPlayableNatively", _ => true).SetResult(true);
        VideoPlayerMetadata metadata = NewMetadata(m => m.VideoUrl = "https://tv.example/live.m3u8");

        RenderPlayer(metadata);

        Assert.True(metadata.LivePlaysNatively);
        Assert.Empty(Calls("amcVideoPlayerInitializeStreamingUrl"));
    }

    [Fact]
    public void A_live_stream_that_cannot_be_started_ends_in_the_error_state()
    {
        JSInterop.Setup<string>("amcVideoPlayerInitializeStreamingUrl", _ => true).SetException(new JSException("manifest failed"));
        VideoPlayerMetadata metadata = NewMetadata(m => m.VideoUrl = "https://tv.example/live.m3u8");

        IRenderedComponent<Player> cut = RenderPlayer(metadata);

        cut.WaitForAssertion(() => Assert.Equal(VideoStates.Error, metadata.VideoState));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".amc-videoplayer-error")));
    }

    [Fact]
    public void A_live_stream_hides_the_seek_bar_the_duration_and_the_loop_option()
    {
        IRenderedComponent<Player> cut = RenderPlayer(NewMetadata(m => m.VideoUrl = "https://tv.example/live.m3u8"));

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".amc-videoplayer-progress")));
        Assert.Empty(cut.FindAll(".amc-videoplayer-timeduration"));

        cut.Find(".amc-videoplayer-more button").Click();

        Assert.Empty(cut.FindAll(".amc-videoplayer-menu-loop"));
        Assert.NotEmpty(cut.FindAll(".amc-videoplayer-menu-playbackspeed"));
    }

    [Fact]
    public void Changing_the_url_reloads_the_video_and_disposes_the_old_stream()
    {
        VideoPlayerMetadata metadata = NewMetadata();
        IRenderedComponent<Player> cut = RenderPlayer(metadata);
        int disposeCallsBefore = Calls("amcVideoPlayerDisposeStreaming").Count();

        metadata.VideoUrl = "https://media.example/other.mp4";
        cut.Render(parameters => parameters.Add(p => p.Metadata, metadata));

        cut.WaitForAssertion(() => Assert.Equal("https://media.example/other.mp4", cut.Find("video").GetAttribute("src")));
        Assert.Equal(VideoStates.Loading, metadata.VideoState);
        Assert.True(Calls("amcVideoPlayerDisposeStreaming").Count() > disposeCallsBefore);
        Assert.False(metadata.IsLive);
    }

    [Fact]
    public void Changing_from_a_file_to_a_playlist_marks_the_player_live()
    {
        VideoPlayerMetadata metadata = NewMetadata();
        IRenderedComponent<Player> cut = RenderPlayer(metadata);

        metadata.VideoUrl = "https://tv.example/live.m3u8";
        cut.Render(parameters => parameters.Add(p => p.Metadata, metadata));

        cut.WaitForAssertion(() => Assert.True(metadata.IsLive));
    }

    [Fact]
    public async Task Changing_the_url_while_playing_stops_the_old_video_first()
    {
        VideoPlayerMetadata metadata = NewMetadata();
        IRenderedComponent<Player> cut = RenderPlayer(metadata);
        await RaiseAsync(cut, "playing");
        Assert.True(metadata.IsPlayingState);

        metadata.VideoUrl = "https://media.example/next.mp4";
        cut.Render(parameters => parameters.Add(p => p.Metadata, metadata));

        cut.WaitForAssertion(() => Assert.Single(Calls("amcVideoPlayerStop")));
        Assert.Equal(PlayingStates.NotPlaying, metadata.PlayingState);
    }

    [Fact]
    public void A_poster_is_shown_on_the_video_element()
    {
        IRenderedComponent<Player> cut = RenderPlayer(configure: parameters => parameters.Add(p => p.Poster, "https://media.example/poster.jpg"));

        Assert.Equal("https://media.example/poster.jpg", cut.Find("video").GetAttribute("poster"));
    }

    [Fact]
    public void Clearing_the_poster_removes_it_from_the_element_through_the_script()
    {
        IRenderedComponent<Player> cut = RenderPlayer(configure: parameters => parameters.Add(p => p.Poster, "https://media.example/poster.jpg"));

        cut.Render(parameters => parameters.Add(p => p.Poster, string.Empty));

        cut.WaitForAssertion(() => Assert.Single(Calls("amcVideoPlayerRemovePoster")));
    }

    [Fact]
    public void Keeping_the_same_poster_does_not_call_the_script()
    {
        IRenderedComponent<Player> cut = RenderPlayer(configure: parameters => parameters.Add(p => p.Poster, "p.jpg"));

        cut.Render(parameters => parameters.Add(p => p.Poster, "p.jpg"));

        Assert.Empty(Calls("amcVideoPlayerRemovePoster"));
    }

    [Fact]
    public async Task Reserving_the_aspect_ratio_sizes_the_player_from_the_video_once_it_is_known()
    {
        IRenderedComponent<Player> cut = RenderPlayer(NewMetadata(m => m.ReserveAspectRatio = true));

        Assert.Empty(Calls("amcVideoPlayerAddReserveAspectRatioListener"));

        await LoadMetadataAsync(cut);

        cut.WaitForAssertion(() => Assert.Single(Calls("amcVideoPlayerAddReserveAspectRatioListener")));
        Assert.Equal([1920d, 1080d], Calls("amcVideoPlayerAddReserveAspectRatioListener").Single().Arguments.Skip(1).Cast<double>());
    }

    [Fact]
    public async Task Not_reserving_the_aspect_ratio_never_adds_a_resize_listener()
    {
        IRenderedComponent<Player> cut = RenderPlayer();

        await LoadMetadataAsync(cut);

        Assert.Empty(Calls("amcVideoPlayerAddReserveAspectRatioListener"));
    }

    [Fact]
    public void A_muted_metadata_mutes_the_video_element()
    {
        VideoPlayerMetadata metadata = NewMetadata();
        IRenderedComponent<Player> cut = RenderPlayer(metadata);

        SetInternal(metadata, "IsMuted", true);
        cut.Render(parameters => parameters.Add(p => p.Metadata, metadata));

        cut.WaitForAssertion(() => Assert.Equal(true, Calls("amcVideoPlayerMuteVolume").Single().Arguments[1]));
    }

    [Fact(Skip = "BUG: the README says VideoPlayerMetadata.Volume is the initial volume, but nothing ever sends it to the video element, so a host asking for 0.3 gets a bar showing 0.3 and sound at 100%")]
    public void The_initial_volume_from_the_metadata_is_applied_to_the_video()
    {
        RenderPlayer(NewMetadata(m => m.Volume = 0.3));

        Assert.Contains(Calls("amcVideoPlayerChangeVolume"), call => Equals(call.Arguments[1], 0.3));
    }

    [Fact(Skip = "BUG: VideoPlayer has no Dispose, so the window resize listener added for ReserveAspectRatio and the hls.js instance of a live stream are never released when the player is removed")]
    public async Task Removing_the_player_releases_the_resize_listener_and_the_stream()
    {
        IRenderedComponent<Player> cut = RenderPlayer(NewMetadata(m => m.ReserveAspectRatio = true));
        await LoadMetadataAsync(cut);
        cut.WaitForAssertion(() => Assert.Single(Calls("amcVideoPlayerAddReserveAspectRatioListener")));

        await DisposeComponentsAsync();

        Assert.NotEmpty(Calls("amcVideoPlayerRemoveReserveAspectRatioListener"));
        Assert.NotEmpty(Calls("amcVideoPlayerDisposeStreaming"));
    }
}
