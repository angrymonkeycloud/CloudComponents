using System.Globalization;
using Bunit;
using global::CloudComponents.VideoPlayer;
using Player = global::CloudComponents.VideoPlayer.VideoPlayer;

namespace CloudComponents.Tests.VideoPlayer;

/// <summary>
/// Casting. The cast component keeps a static reference to the most recently created instance (the target of the
/// JSInvokable callback), so these tests share one collection and never run beside another test that casts.
/// </summary>
[Collection("VideoPlayerCast")]
public class VideoPlayerCastTests : PlayerTestBase
{
    private readonly List<ActionCodes> _actions = [];

    private async Task<(IRenderedComponent<Player> Player, IRenderedComponent<VideoPlayerCast> Cast, VideoPlayerMetadata Metadata)> StartCastingAsync(VideoPlayerMetadata? metadata = null)
    {
        metadata ??= NewMetadata();
        IRenderedComponent<Player> player = RenderPlayer(metadata, p => p.Add(x => x.OnAction, action => { _actions.Add(action.Action); }));

        await ClickAsync(player.Find(".amc-videoplayer-castbutton button"));

        player.WaitForAssertion(() => Assert.NotEmpty(Calls("amcVideoPlayerStartCasting")));
        return (player, player.FindComponent<VideoPlayerCast>(), metadata);
    }

    private static string Text(IRenderedComponent<Player> player, string selector) => player.Find(selector).TextContent.Trim();

    [Fact]
    public async Task Pressing_cast_pauses_the_local_video_and_shows_the_cast_view()
    {
        var (player, _, metadata) = await StartCastingAsync();

        Assert.True(metadata.IsCasting);
        Assert.Single(Calls("amcVideoPlayerPause"));
        Assert.Equal([ActionCodes.Pause, ActionCodes.Cast], _actions);
        Assert.Single(player.FindAll(".amc-videoplayer-cast"));
        Assert.Contains("_casting", Classes(player).Split(' '));
        Assert.Equal("Clip", Text(player, ".amc-videoplayer-cast-title"));
        Assert.Equal("Casting", Text(player, ".amc-videoplayer-cast-status"));
    }

    [Fact]
    public async Task Starting_a_cast_initialises_the_cast_script_then_sends_the_video_url()
    {
        await StartCastingAsync();

        Assert.Single(Calls("amcVideoPlayerCastInit"));
        object?[] arguments = Assert.Single(Calls("amcVideoPlayerStartCasting")).Arguments.ToArray();
        Assert.Equal("https://media.example/clip.mp4", arguments[0]);
    }

    [Fact(Skip = "BUG: VideoPlayerCast.StartCast hard-codes \" | Coverbox TV\" onto the title sent to the Chromecast, so every host's receiver shows another company's brand")]
    public async Task The_cast_title_is_the_video_title()
    {
        await StartCastingAsync();

        Assert.Equal("Clip", Assert.Single(Calls("amcVideoPlayerStartCasting")).Arguments[1]);
    }

    [Fact(Skip = "BUG: VideoPlayerCast.razor uses <CloudComponents.Icons.Cast/Loading/PlayButton/PauseButton/Close />, a namespace that does not exist (the icons are AngryMonkey.CloudComponents.Icons or CloudIcons.Icons), so Razor renders them as unknown empty elements and the whole cast view has no icons")]
    public async Task The_cast_view_buttons_show_icons()
    {
        var (player, _, _) = await StartCastingAsync();

        Assert.All(
            player.FindAll(".amc-videoplayer-cast-icon, .amc-videoplayer-cast .amc-videoplayer-play button, .amc-videoplayer-cast .amc-videoplayer-pause button, .amc-videoplayer-cast .amc-videoplayer-castbutton button"),
            element => Assert.NotNull(element.QuerySelector("svg")));
    }

    [Fact]
    public async Task Casting_from_fullscreen_leaves_fullscreen_first()
    {
        IRenderedComponent<Player> player = RenderPlayer(configure: p => p.Add(x => x.OnAction, action => { _actions.Add(action.Action); }));
        await player.Find(".amc-videoplayer").FireTriggerEventAsync("onfullscreenchange", EventArgs.Empty);

        await ClickAsync(player.Find(".amc-videoplayer-castbutton button"));

        Assert.Single(Calls("amcVideoPlayerExitFullScreen"));
        Assert.Equal([ActionCodes.FullScreen, ActionCodes.Pause, ActionCodes.Cast], _actions);
    }

    [Fact]
    public async Task The_receiver_connecting_shows_the_device_name_and_marks_casting()
    {
        var (player, cast, metadata) = await StartCastingAsync();
        Assert.Single(player.Find(".amc-videoplayer-cast .amc-videoplayer-status").Children);

        await player.InvokeAsync(() => cast.Instance.HandleCastJsEvent("connect", "Living Room TV"));

        Assert.Equal("Living Room TV", Text(player, ".amc-videoplayer-cast-status"));
        Assert.Empty(player.Find(".amc-videoplayer-cast .amc-videoplayer-status").Children);
        Assert.True(metadata.IsCasting);
    }

    [Theory]
    [InlineData("playing", PlayingStates.Playing)]
    [InlineData("PLAYING", PlayingStates.Playing)]
    [InlineData("pause", PlayingStates.Paused)]
    [InlineData("buffering", PlayingStates.Buffering)]
    public async Task Receiver_events_update_the_playing_state(string eventName, PlayingStates expected)
    {
        var (player, cast, metadata) = await StartCastingAsync();

        await player.InvokeAsync(() => cast.Instance.HandleCastJsEvent(eventName, null));

        Assert.Equal(expected, metadata.PlayingState);
    }

    [Fact]
    public async Task Buffering_on_the_receiver_shows_the_spinner_even_when_connected()
    {
        var (player, cast, _) = await StartCastingAsync();
        await player.InvokeAsync(() => cast.Instance.HandleCastJsEvent("connect", "TV"));

        await player.InvokeAsync(() => cast.Instance.HandleCastJsEvent("buffering", null));

        Assert.Single(player.Find(".amc-videoplayer-cast .amc-videoplayer-status").Children);
    }

    [Fact]
    public async Task An_event_the_player_does_not_know_changes_nothing()
    {
        var (player, cast, metadata) = await StartCastingAsync();
        PlayingStates before = metadata.PlayingState;

        await player.InvokeAsync(() => cast.Instance.HandleCastJsEvent("volumechange", 0.5));

        Assert.Equal(before, metadata.PlayingState);
        Assert.Equal("Casting", Text(player, ".amc-videoplayer-cast-status"));
    }

    [Fact]
    public async Task The_receiver_position_updates_the_current_time()
    {
        var (player, cast, metadata) = await StartCastingAsync();
        await LoadMetadataAsync(player);

        using (new CultureScope(CultureInfo.InvariantCulture))
            await player.InvokeAsync(() => cast.Instance.HandleCastJsEvent("timeupdate", "72.5"));

        Assert.Equal(72.5, metadata.CurrentTime);
        Assert.StartsWith("1:12", Text(player, ".amc-videoplayer-cast .amc-videoplayer-timeduration"));
    }

    [Fact(Skip = "BUG: VideoPlayerCast.HandleCastJsEvent parses the position with double.Parse(value.ToString()) in the current culture, so the number 72.5 sent by the script throws FormatException under fr-FR and is read as 725 under de-DE")]
    public async Task The_receiver_position_is_read_the_same_in_every_culture()
    {
        var (player, cast, metadata) = await StartCastingAsync();

        using (new CultureScope(new CultureInfo("fr-FR")))
            await player.InvokeAsync(() => cast.Instance.HandleCastJsEvent("timeupdate", "72.5"));

        Assert.Equal(72.5, metadata.CurrentTime);
    }

    [Fact]
    public async Task The_static_script_callback_reaches_the_latest_cast_view()
    {
        var (player, _, metadata) = await StartCastingAsync();

        await player.InvokeAsync(() => VideoPlayerCast.HandleCastJsEventStatic("connect", "Bedroom"));

        Assert.Equal("Bedroom", Text(player, ".amc-videoplayer-cast-status"));
        Assert.True(metadata.IsCasting);
    }

    [Fact]
    public async Task The_cast_play_and_pause_buttons_drive_the_receiver()
    {
        var (player, _, _) = await StartCastingAsync();

        await ClickAsync(player.Find(".amc-videoplayer-cast .amc-videoplayer-play button"));
        await ClickAsync(player.Find(".amc-videoplayer-cast .amc-videoplayer-pause button"));

        Assert.Single(Calls("amcVideoPlayerPlayCasting"));
        Assert.Single(Calls("amcVideoPlayerPauseCasting"));
    }

    [Fact]
    public async Task Closing_the_cast_view_disconnects_and_resumes_local_playback()
    {
        var (player, _, metadata) = await StartCastingAsync();
        await player.InvokeAsync(() => VideoPlayerCast.HandleCastJsEventStatic("connect", "TV"));

        await ClickAsync(player.Find(".amc-videoplayer-cast .amc-videoplayer-castbutton button"));

        Assert.Single(Calls("amcVideoPlayerStopCasting"));
        Assert.False(metadata.IsCasting);
        player.WaitForAssertion(() => Assert.Empty(player.FindAll(".amc-videoplayer-cast")));
        Assert.Single(Calls("amcVideoPlayerPlay"));
        Assert.DoesNotContain("_casting", Classes(player).Split(' '));
    }

    [Fact]
    public async Task The_stop_button_in_the_cast_view_also_ends_the_cast()
    {
        var (player, _, metadata) = await StartCastingAsync(NewMetadata(m => m.ShowStopButton = true));

        await ClickAsync(player.Find(".amc-videoplayer-cast .amc-videoplayer-stop button"));

        Assert.Single(Calls("amcVideoPlayerStopCasting"));
        Assert.False(metadata.IsCasting);
    }

    [Fact]
    public async Task A_local_playing_event_while_casting_pauses_the_local_video_again()
    {
        var (player, _, metadata) = await StartCastingAsync();
        int pausesBefore = Calls("amcVideoPlayerPause").Count();

        await RaiseAsync(player, "playing");

        Assert.Equal(pausesBefore + 1, Calls("amcVideoPlayerPause").Count());
        Assert.NotEqual(PlayingStates.Playing, metadata.PlayingState);
    }

    [Fact]
    public async Task Autoplay_does_not_start_the_local_video_while_casting()
    {
        var (player, _, _) = await StartCastingAsync(NewMetadata(m => m.Autoplay = true));
        int playsBefore = Calls("amcVideoPlayerPlay").Count();

        await LoadMetadataAsync(player);

        Assert.Equal(playsBefore, Calls("amcVideoPlayerPlay").Count());
    }

    [Fact]
    public async Task Changing_the_url_while_casting_sends_the_new_video_to_the_receiver()
    {
        var (player, _, metadata) = await StartCastingAsync();

        metadata.VideoUrl = "https://media.example/next.mp4";
        player.Render(p => p.Add(x => x.Metadata, metadata));

        player.WaitForAssertion(() => Assert.Contains(Calls("amcVideoPlayerStartCasting"), call => (string?)call.Arguments[0] == "https://media.example/next.mp4"));
    }

    /// <summary>Switches the current culture for the duration of a call and restores it afterwards.</summary>
    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _previous = CultureInfo.CurrentCulture;

        public CultureScope(CultureInfo culture) => CultureInfo.CurrentCulture = culture;

        public void Dispose() => CultureInfo.CurrentCulture = _previous;
    }
}
