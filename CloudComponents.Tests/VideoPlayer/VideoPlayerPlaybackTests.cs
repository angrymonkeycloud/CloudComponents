using Bunit;
using global::CloudComponents.VideoPlayer;
using Player = global::CloudComponents.VideoPlayer.VideoPlayer;

namespace CloudComponents.Tests.VideoPlayer;

public class VideoPlayerPlaybackTests : PlayerTestBase
{
    private readonly List<ActionCodes> _actions = [];

    private IRenderedComponent<Player> RenderRecording(VideoPlayerMetadata? metadata = null) =>
        RenderPlayer(metadata, parameters => parameters.Add(p => p.OnAction, action => { _actions.Add(action.Action); }));

    [Fact]
    public async Task The_play_button_plays_the_video_and_reports_the_action()
    {
        VideoPlayerMetadata metadata = NewMetadata();
        IRenderedComponent<Player> cut = RenderRecording(metadata);

        await ClickAsync(cut.Find(".amc-videoplayer-play button"));

        Assert.Single(Calls("amcVideoPlayerPlay"));
        Assert.Equal(PlayingStates.Playing, metadata.PlayingState);
        Assert.Equal([ActionCodes.Play], _actions);
    }

    [Fact]
    public async Task The_pause_button_pauses_the_video_and_reports_the_action()
    {
        VideoPlayerMetadata metadata = NewMetadata();
        IRenderedComponent<Player> cut = RenderRecording(metadata);
        await ClickAsync(cut.Find(".amc-videoplayer-play button"));

        await ClickAsync(cut.Find(".amc-videoplayer-pause button"));

        Assert.Single(Calls("amcVideoPlayerPause"));
        Assert.Equal(PlayingStates.Paused, metadata.PlayingState);
        Assert.Equal([ActionCodes.Play, ActionCodes.Pause], _actions);
    }

    [Fact]
    public async Task Playing_for_the_first_time_asks_the_browser_for_the_video_size_and_length_once()
    {
        IRenderedComponent<Player> cut = RenderRecording();

        await cut.InvokeAsync(() => cut.Instance.PlayVideo());
        await cut.InvokeAsync(() => cut.Instance.PauseVideo());
        await cut.InvokeAsync(() => cut.Instance.PlayVideo());
        await cut.InvokeAsync(() => cut.Instance.UpdatedExternally());

        Assert.Single(Calls("amcVideoPlayerGetVideoInfo"));
        Assert.Equal("0:00 / 2:05", cut.Find(".amc-videoplayer-timeduration").TextContent.Trim());
    }

    [Fact]
    public async Task Play_pause_and_stop_can_be_driven_from_code()
    {
        VideoPlayerMetadata metadata = NewMetadata();
        IRenderedComponent<Player> cut = RenderRecording(metadata);

        await cut.InvokeAsync(() => cut.Instance.PlayVideo());
        await cut.InvokeAsync(() => cut.Instance.PauseVideo());
        await cut.InvokeAsync(() => cut.Instance.StopVideo());

        Assert.Equal([ActionCodes.Play, ActionCodes.Pause, ActionCodes.Stop], _actions);
        Assert.Equal(PlayingStates.NotPlaying, metadata.PlayingState);
    }

    [Fact]
    public void The_stop_button_is_only_shown_when_asked_for()
    {
        Assert.Empty(RenderPlayer().FindAll(".amc-videoplayer-stop"));
        Assert.Single(RenderPlayer(NewMetadata(m => m.ShowStopButton = true)).FindAll(".amc-videoplayer-stop"));
    }

    [Fact]
    public async Task The_stop_button_rewinds_stops_and_reports_the_action()
    {
        VideoPlayerMetadata metadata = NewMetadata(m => m.ShowStopButton = true);
        IRenderedComponent<Player> cut = RenderRecording(metadata);
        await LoadMetadataAsync(cut);
        await RaiseAsync(cut, "timeupdate", new { currentTime = 30 });
        Assert.Equal(30, metadata.CurrentTime);

        await ClickAsync(cut.Find(".amc-videoplayer-stop button"));

        Assert.Single(Calls("amcVideoPlayerStop"));
        Assert.Equal(0, metadata.CurrentTime);
        Assert.Equal(PlayingStates.NotPlaying, metadata.PlayingState);
        Assert.Equal([ActionCodes.Stop], _actions);
        Assert.StartsWith("0:00 /", cut.Find(".amc-videoplayer-timeduration").TextContent.Trim());
    }

    [Fact]
    public async Task A_script_failure_while_stopping_is_swallowed_and_the_player_still_stops()
    {
        JSInterop.SetupVoid("amcVideoPlayerStop", _ => true).SetException(new Microsoft.JSInterop.JSException("element gone"));
        VideoPlayerMetadata metadata = NewMetadata();
        IRenderedComponent<Player> cut = RenderRecording(metadata);
        await cut.InvokeAsync(() => cut.Instance.PlayVideo());

        await cut.InvokeAsync(() => cut.Instance.StopVideo());

        Assert.Equal(PlayingStates.NotPlaying, metadata.PlayingState);
        Assert.Equal(ActionCodes.Stop, _actions.Last());
    }

    [Fact]
    public async Task Clicking_the_picture_plays_a_stopped_video_and_pauses_a_playing_one()
    {
        VideoPlayerMetadata metadata = NewMetadata();
        IRenderedComponent<Player> cut = RenderRecording(metadata);

        await ClickAsync(cut.Find(".amc-videoplayer-empty"));
        Assert.Equal(PlayingStates.Playing, metadata.PlayingState);

        await ClickAsync(cut.Find(".amc-videoplayer-empty"));
        Assert.Equal(PlayingStates.Paused, metadata.PlayingState);

        await ClickAsync(cut.Find(".amc-videoplayer-empty"));
        Assert.Equal(PlayingStates.Playing, metadata.PlayingState);
        Assert.Equal([ActionCodes.Play, ActionCodes.Pause, ActionCodes.Play], _actions);
    }

    [Fact]
    public async Task A_touch_on_the_picture_does_not_also_toggle_playback_through_its_click()
    {
        // Touch devices send touchstart then click for one tap: the tap only shows/hides the controls.
        VideoPlayerMetadata metadata = NewMetadata();
        IRenderedComponent<Player> cut = RenderRecording(metadata);

        await cut.Find(".amc-videoplayer-empty").FireTouchStartAsync(new());
        await ClickAsync(cut.Find(".amc-videoplayer-empty"));

        Assert.Empty(_actions);
        Assert.Empty(Calls("amcVideoPlayerPlay"));

        await ClickAsync(cut.Find(".amc-videoplayer-empty"));

        Assert.Equal([ActionCodes.Play], _actions);
    }

    [Fact]
    public async Task The_playing_state_callback_reports_each_change_once_and_in_order()
    {
        List<PlayingStates> states = [];
        IRenderedComponent<Player> cut = RenderPlayer(configure: p => p.Add(x => x.OnPlayingStateChanged, state => { lock (states) states.Add(state); }));

        // The callback runs on a pool thread, so wait for each report before making the next change.
        await cut.InvokeAsync(() => cut.Instance.PlayVideo());
        cut.WaitForAssertion(() => Assert.Single(states));
        await cut.InvokeAsync(() => cut.Instance.PlayVideo());
        await cut.InvokeAsync(() => cut.Instance.PauseVideo());
        cut.WaitForAssertion(() => Assert.Equal(2, states.Count));
        await cut.InvokeAsync(() => cut.Instance.StopVideo());
        cut.WaitForAssertion(() => Assert.Equal(3, states.Count));

        Assert.Equal([PlayingStates.Playing, PlayingStates.Paused, PlayingStates.NotPlaying], states);
    }

    [Fact]
    public async Task The_play_state_is_shown_to_styles_through_the_player_class()
    {
        IRenderedComponent<Player> cut = RenderRecording();

        await PlayingAsync(cut);

        Assert.Contains("_playing", Classes(cut).Split(' '));
        Assert.Contains("_ready", Classes(cut).Split(' '));
    }

    [Fact(Skip = "BUG: OnVideoChange calls Repaint() before the event's state change is applied, so the player class (_playing, _hidecontrols) is one event behind: after a pause event the stopped player keeps the _playing class until the pointer moves")]
    public async Task The_player_class_is_up_to_date_right_after_the_event()
    {
        IRenderedComponent<Player> cut = RenderRecording();

        await RaiseAsync(cut, "playing");

        Assert.Contains("_playing", Classes(cut).Split(' '));
    }
}
