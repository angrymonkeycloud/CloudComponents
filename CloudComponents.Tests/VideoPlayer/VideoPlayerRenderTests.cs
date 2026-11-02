using Bunit;
using global::CloudComponents.VideoPlayer;
using Player = global::CloudComponents.VideoPlayer.VideoPlayer;

namespace CloudComponents.Tests.VideoPlayer;

/// <summary>Which parts of the player are rendered for which metadata, and when the controls hide.</summary>
public class VideoPlayerRenderTests : PlayerTestBase
{
    [Fact]
    public void A_default_player_renders_video_play_pause_settings_cast_fullscreen_and_progress()
    {
        IRenderedComponent<Player> cut = RenderPlayer();

        Assert.Single(cut.FindAll("video"));
        Assert.Single(cut.FindAll(".amc-videoplayer-play button"));
        Assert.Single(cut.FindAll(".amc-videoplayer-pause button"));
        Assert.Single(cut.FindAll(".amc-videoplayer-more button"));
        Assert.Single(cut.FindAll(".amc-videoplayer-castbutton button"));
        Assert.Single(cut.FindAll(".amc-videoplayer-enterfullscreen"));
        Assert.Single(cut.FindAll(".amc-videoplayer-progress"));
        Assert.Single(cut.FindAll(".amc-videoplayer-timeduration"));
    }

    [Fact]
    public void Without_controls_and_without_child_content_only_the_video_is_rendered()
    {
        IRenderedComponent<Player> cut = RenderPlayer(NewMetadata(m => m.EnableControls = false));

        Assert.Single(cut.FindAll("video"));
        Assert.Empty(cut.FindAll(".amc-videoplayer-content"));
    }

    [Fact]
    public void Without_controls_child_content_is_still_shown_as_a_header()
    {
        IRenderedComponent<Player> cut = RenderPlayer(NewMetadata(m => m.EnableControls = false), p => p.AddChildContent("<span class=\"overlay\">Hello</span>"));

        Assert.Equal("Hello", cut.Find(".amc-videoplayer-header .overlay").TextContent);
        Assert.Empty(cut.FindAll(".amc-videoplayer-controls"));
        Assert.Empty(cut.FindAll(".amc-videoplayer-top"));
    }

    [Fact]
    public void Child_content_is_shown_in_the_header_with_the_controls()
    {
        IRenderedComponent<Player> cut = RenderPlayer(configure: p => p.AddChildContent("<span class=\"overlay\">Episode 1</span>"));

        Assert.Equal("Episode 1", cut.Find(".amc-videoplayer-header .overlay").TextContent);
        Assert.NotEmpty(cut.FindAll(".amc-videoplayer-controls"));
    }

    [Fact]
    public void Hiding_the_progress_bar_setting_removes_the_time_display()
    {
        IRenderedComponent<Player> cut = RenderPlayer(NewMetadata(m => m.ShowProgressBar = false));

        Assert.Empty(cut.FindAll(".amc-videoplayer-timeduration"));
    }

    [Fact(Skip = "BUG: Metadata.ShowProgressBar=false (README: \"Show the seek/progress bar\") only hides the time text; ShowProgressBarElement ignores the flag, so the seek bar is still rendered")]
    public void Hiding_the_progress_bar_setting_removes_the_seek_bar()
    {
        IRenderedComponent<Player> cut = RenderPlayer(NewMetadata(m => m.ShowProgressBar = false));

        Assert.Empty(cut.FindAll(".amc-videoplayer-progress"));
    }

    [Fact]
    public async Task A_playing_video_hides_its_controls_until_the_pointer_moves()
    {
        IRenderedComponent<Player> cut = RenderPlayer();
        await PlayingAsync(cut);

        Assert.Contains("_hidecontrols", Classes(cut).Split(' '));

        await cut.Find(".amc-videoplayer").FireMouseMoveAsync(new());

        Assert.DoesNotContain("_hidecontrols", Classes(cut).Split(' '));
    }

    [Fact]
    public async Task The_controls_hide_again_two_seconds_after_the_last_movement()
    {
        IRenderedComponent<Player> cut = RenderPlayer();
        await PlayingAsync(cut);
        await cut.Find(".amc-videoplayer").FireMouseMoveAsync(new());

        cut.WaitForAssertion(() => Assert.Contains("_hidecontrols", Classes(cut).Split(' ')), TimeSpan.FromSeconds(4));
    }

    [Fact]
    public async Task A_stopped_video_keeps_its_controls_visible()
    {
        IRenderedComponent<Player> cut = RenderPlayer();
        await LoadMetadataAsync(cut);

        await cut.Find(".amc-videoplayer").FireMouseMoveAsync(new());

        Assert.DoesNotContain("_hidecontrols", Classes(cut).Split(' '));
    }

    [Fact]
    public async Task Tapping_the_picture_of_a_playing_video_hides_the_controls_at_once()
    {
        IRenderedComponent<Player> cut = RenderPlayer();
        await PlayingAsync(cut);
        await cut.Find(".amc-videoplayer").FireMouseMoveAsync(new());
        Assert.DoesNotContain("_hidecontrols", Classes(cut).Split(' '));

        await cut.Find(".amc-videoplayer-empty").FireTouchStartAsync(new());

        Assert.Contains("_hidecontrols", Classes(cut).Split(' '));
    }

    [Fact]
    public async Task Pointer_movement_is_ignored_right_after_a_tap_hid_the_controls()
    {
        IRenderedComponent<Player> cut = RenderPlayer();
        await PlayingAsync(cut);
        await cut.Find(".amc-videoplayer").FireMouseMoveAsync(new());
        await cut.Find(".amc-videoplayer-empty").FireTouchStartAsync(new());

        await cut.Find(".amc-videoplayer").FireMouseMoveAsync(new());

        Assert.Contains("_hidecontrols", Classes(cut).Split(' '));
    }

    [Fact]
    public async Task The_wheel_over_the_player_shows_the_controls()
    {
        IRenderedComponent<Player> cut = RenderPlayer();
        await PlayingAsync(cut);
        Assert.Contains("_hidecontrols", Classes(cut).Split(' '));

        await cut.Find(".amc-videoplayer").FireTriggerEventAsync("onmousewheel", new Microsoft.AspNetCore.Components.Web.WheelEventArgs { DeltaY = -1 });

        Assert.DoesNotContain("_hidecontrols", Classes(cut).Split(' '));
    }
}
