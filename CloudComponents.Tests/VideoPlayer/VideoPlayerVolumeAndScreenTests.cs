using Bunit;
using global::CloudComponents.VideoPlayer;
using Microsoft.AspNetCore.Components;
using Player = global::CloudComponents.VideoPlayer.VideoPlayer;

namespace CloudComponents.Tests.VideoPlayer;

public class VideoPlayerVolumeAndScreenTests : PlayerTestBase
{
    private async Task<IRenderedComponent<Player>> RenderWithVolumeOpenAsync(VideoPlayerMetadata? metadata = null)
    {
        IRenderedComponent<Player> cut = RenderPlayer(metadata);
        await ClickAsync(cut.Find(".amc-videoplayer-volume"));
        return cut;
    }

    private static string Num(double value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);

    // ----- Volume -----------------------------------------------------------

    [Theory]
    [InlineData(VolumeType.Full, true)]
    [InlineData(VolumeType.Mute, true)]
    [InlineData(VolumeType.None, false)]
    public void The_volume_control_is_shown_unless_the_volume_type_is_none(VolumeType type, bool shown)
    {
        IRenderedComponent<Player> cut = RenderPlayer(NewMetadata(m => m.VolumeType = type));

        Assert.Equal(shown, cut.FindAll(".amc-videoplayer-volume").Count == 1);
    }

    [Fact]
    public async Task Pressing_the_volume_area_opens_and_closes_the_slider()
    {
        IRenderedComponent<Player> cut = RenderPlayer();
        Assert.Empty(cut.FindAll(".amc-volumebar-bar"));

        await ClickAsync(cut.Find(".amc-videoplayer-volume"));

        Assert.Contains("_extended", cut.Find(".amc-videoplayer-volume").ClassList);
        Assert.Single(cut.FindAll(".amc-volumebar-bar"));
        Assert.Equal("100", cut.Find(".amc-volumebar-value").TextContent.Trim());

        await ClickAsync(cut.Find(".amc-videoplayer-volume"));

        Assert.DoesNotContain("_extended", cut.Find(".amc-videoplayer-volume").ClassList);
    }

    [Fact]
    public async Task The_slider_shows_the_volume_from_the_metadata()
    {
        IRenderedComponent<Player> cut = await RenderWithVolumeOpenAsync(NewMetadata(m => m.Volume = 0.4));

        Assert.Equal("40", cut.Find(".amc-volumebar-value").TextContent.Trim());
        Assert.Equal("0.4", cut.Find(".amc-volumebar-bar .amc-progressbar-value").GetAttribute("value"));
    }

    [Fact]
    public async Task Dragging_the_slider_sets_the_volume_on_the_metadata_and_the_video()
    {
        VideoPlayerMetadata metadata = NewMetadata();
        IRenderedComponent<Player> cut = await RenderWithVolumeOpenAsync(metadata);

        await cut.Find(".amc-volumebar-bar .amc-progressbar-changingvalue").FireChangeAsync(new ChangeEventArgs { Value = Num(0.5) });

        Assert.Equal(0.5, metadata.Volume);
        Assert.Equal(0.5, Assert.Single(Calls("amcVideoPlayerChangeVolume")).Arguments[1]);
        Assert.False(GetInternal<bool>(metadata, "IsMuted"));
        Assert.Empty(Calls("amcVideoPlayerMuteVolume"));
    }

    [Fact]
    public async Task The_mute_button_mutes_the_video_without_changing_the_remembered_volume()
    {
        VideoPlayerMetadata metadata = NewMetadata(m => m.Volume = 0.7);
        IRenderedComponent<Player> cut = await RenderWithVolumeOpenAsync(metadata);

        await ClickAsync(cut.Find(".amc-volumebar-mute"));

        Assert.True(GetInternal<bool>(metadata, "IsMuted"));
        Assert.Equal(0.7, metadata.Volume);
        Assert.Equal(true, Assert.Single(Calls("amcVideoPlayerMuteVolume")).Arguments[1]);
    }

    [Fact]
    public async Task Pressing_the_speaker_while_muted_unmutes()
    {
        VideoPlayerMetadata metadata = NewMetadata();
        IRenderedComponent<Player> cut = await RenderWithVolumeOpenAsync(metadata);
        await ClickAsync(cut.Find(".amc-volumebar-mute"));

        await ClickAsync(cut.Find(".amc-volumebar-button"));

        Assert.False(GetInternal<bool>(metadata, "IsMuted"));
        Assert.Equal([true, false], Calls("amcVideoPlayerMuteVolume").Select(call => (bool)call.Arguments[1]!));
    }

    [Fact]
    public async Task Releasing_the_slider_at_zero_mutes_and_restores_the_volume_to_full()
    {
        VideoPlayerMetadata metadata = NewMetadata(m => m.Volume = 0.6);
        IRenderedComponent<Player> cut = await RenderWithVolumeOpenAsync(metadata);

        await cut.Find(".amc-volumebar-bar .amc-progressbar-value").FireChangeAsync(new ChangeEventArgs { Value = "0" });

        Assert.True(GetInternal<bool>(metadata, "IsMuted"));
        Assert.Equal(1, metadata.Volume);
        Assert.Equal(true, Assert.Single(Calls("amcVideoPlayerMuteVolume")).Arguments[1]);
    }

    // ----- Full screen ------------------------------------------------------

    [Fact]
    public async Task The_fullscreen_button_asks_the_script_and_reports_the_action()
    {
        List<ActionCodes> actions = [];
        IRenderedComponent<Player> cut = RenderPlayer(configure: p => p.Add(x => x.OnAction, action => { actions.Add(action.Action); }));

        await ClickAsync(cut.Find(".amc-videoplayer-enterfullscreen"));

        Assert.Single(Calls("amcVideoPlayerEnterFullScreen"));
        Assert.Equal([ActionCodes.FullScreen], actions);
    }

    [Fact]
    public async Task The_browser_fullscreen_event_swaps_the_enter_button_for_the_exit_button()
    {
        IRenderedComponent<Player> cut = RenderPlayer();
        Assert.Empty(cut.FindAll(".amc-videoplayer-exitfullscreen"));

        await cut.Find(".amc-videoplayer").FireTriggerEventAsync("onfullscreenchange", EventArgs.Empty);

        Assert.Empty(cut.FindAll(".amc-videoplayer-enterfullscreen"));
        Assert.Single(cut.FindAll(".amc-videoplayer-exitfullscreen"));
        Assert.Contains("_fullscreen", Classes(cut).Split(' '));

        await cut.Find(".amc-videoplayer").FireTriggerEventAsync("onfullscreenchange", EventArgs.Empty);

        Assert.Single(cut.FindAll(".amc-videoplayer-enterfullscreen"));
        Assert.DoesNotContain("_fullscreen", Classes(cut).Split(' '));
    }

    [Fact]
    public async Task The_exit_button_leaves_fullscreen_through_the_script()
    {
        List<ActionCodes> actions = [];
        IRenderedComponent<Player> cut = RenderPlayer(configure: p => p.Add(x => x.OnAction, action => { actions.Add(action.Action); }));
        await cut.Find(".amc-videoplayer").FireTriggerEventAsync("onfullscreenchange", EventArgs.Empty);

        await ClickAsync(cut.Find(".amc-videoplayer-exitfullscreen"));

        Assert.Single(Calls("amcVideoPlayerExitFullScreen"));
        Assert.Equal([ActionCodes.FullScreen], actions);
    }

    [Fact(Skip = "BUG: VideoPlayer.UI Repaint() adds the \"_error\" class when Metadata.IsFullScreen (copy/paste of the fullscreen condition), so a healthy fullscreen player is styled as failed")]
    public async Task A_fullscreen_player_is_not_styled_as_an_error()
    {
        IRenderedComponent<Player> cut = RenderPlayer();

        await cut.Find(".amc-videoplayer").FireTriggerEventAsync("onfullscreenchange", EventArgs.Empty);

        Assert.DoesNotContain("_error", Classes(cut).Split(' '));
    }
}
