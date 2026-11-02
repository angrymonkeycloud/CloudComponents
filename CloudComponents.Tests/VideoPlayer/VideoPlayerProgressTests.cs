using Bunit;
using global::CloudComponents.VideoPlayer;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Player = global::CloudComponents.VideoPlayer.VideoPlayer;

namespace CloudComponents.Tests.VideoPlayer;

public class VideoPlayerProgressTests : PlayerTestBase
{
    private async Task<IRenderedComponent<Player>> RenderLoadedAsync(VideoPlayerMetadata? metadata = null)
    {
        IRenderedComponent<Player> cut = RenderPlayer(metadata);
        await LoadMetadataAsync(cut);
        return cut;
    }

    private static Task DragTo(IRenderedComponent<Player> cut, double seconds) =>
        cut.Find(".amc-progressbar-changingvalue").FireChangeAsync(new ChangeEventArgs { Value = seconds.ToString(System.Globalization.CultureInfo.InvariantCulture) });

    private static Task ReleaseAt(IRenderedComponent<Player> cut, double seconds) =>
        cut.Find(".amc-progressbar-value").FireChangeAsync(new ChangeEventArgs { Value = seconds.ToString(System.Globalization.CultureInfo.InvariantCulture) });

    private string SeekText(IRenderedComponent<Player> cut) => cut.Find(".amc-videoplayer-seekinfo-duration").TextContent.Trim();

    [Fact]
    public void A_normal_video_has_a_progress_bar_once_it_is_ready()
    {
        Assert.Single(RenderPlayer().FindAll(".amc-videoplayer-progress"));
    }

    [Fact]
    public async Task The_progress_bar_range_is_the_video_duration()
    {
        IRenderedComponent<Player> cut = await RenderLoadedAsync();

        Assert.Equal("125", cut.Find(".amc-progressbar-value").GetAttribute("max"));
    }

    [Fact]
    public async Task Dragging_shows_the_time_under_the_thumb_and_asks_the_script_to_position_it()
    {
        IRenderedComponent<Player> cut = await RenderLoadedAsync();

        await DragTo(cut, 30);

        Assert.Equal("0:30", SeekText(cut));
        Assert.Contains("_showseekinginfo", Classes(cut).Split(' '));
        object?[] arguments = Assert.Single(Calls("amcVideoPlayerSeeking")).Arguments.ToArray();
        Assert.Equal(30d, arguments[1]);
        Assert.Equal(125d, arguments[2]);
    }

    [Fact]
    public async Task Releasing_the_thumb_seeks_the_video_and_hides_the_seek_info()
    {
        IRenderedComponent<Player> cut = await RenderLoadedAsync();
        await DragTo(cut, 30);

        await ReleaseAt(cut, 30);

        object?[] arguments = Assert.Single(Calls("amcVideoPlayerChangeCurrentTime")).Arguments.ToArray();
        Assert.Equal(30d, arguments[1]);
        Assert.DoesNotContain("_showseekinginfo", Classes(cut).Split(' '));
    }

    [Fact]
    public async Task A_change_of_less_than_one_second_does_not_seek()
    {
        IRenderedComponent<Player> cut = await RenderLoadedAsync();
        await RaiseAsync(cut, "timeupdate", new { currentTime = 20 });

        await ReleaseAt(cut, 20.6);

        Assert.Empty(Calls("amcVideoPlayerChangeCurrentTime"));
    }

    [Fact]
    public async Task Seeking_backwards_is_allowed()
    {
        IRenderedComponent<Player> cut = await RenderLoadedAsync();
        await RaiseAsync(cut, "timeupdate", new { currentTime = 100 });

        await ReleaseAt(cut, 40);

        Assert.Equal(40d, Assert.Single(Calls("amcVideoPlayerChangeCurrentTime")).Arguments[1]);
    }

    [Fact]
    public async Task Nothing_is_sent_to_the_script_when_the_duration_is_not_known()
    {
        IRenderedComponent<Player> cut = RenderPlayer();

        await DragTo(cut, 30);
        await ReleaseAt(cut, 30);

        Assert.Empty(Calls("amcVideoPlayerSeeking"));
        Assert.Empty(Calls("amcVideoPlayerChangeCurrentTime"));
    }

    [Fact]
    public async Task Pressing_the_bar_holds_the_displayed_time_until_the_seek_finishes()
    {
        IRenderedComponent<Player> cut = await RenderLoadedAsync();
        await cut.Find(".amc-videoplayer-progress").FireMouseDownAsync(new());
        await RaiseAsync(cut, "timeupdate", new { currentTime = 50 });
        Assert.StartsWith("0:00", cut.Find(".amc-videoplayer-timeduration").TextContent.Trim());

        await ReleaseAt(cut, 70);
        await RaiseAsync(cut, "timeupdate", new { currentTime = 70 });

        Assert.StartsWith("1:10", cut.Find(".amc-videoplayer-timeduration").TextContent.Trim());
    }

    [Fact]
    public async Task Touching_the_bar_also_holds_the_displayed_time()
    {
        IRenderedComponent<Player> cut = await RenderLoadedAsync();
        await cut.Find(".amc-videoplayer-progress").FireTouchStartAsync(new());

        await RaiseAsync(cut, "timeupdate", new { currentTime = 50 });

        Assert.StartsWith("0:00", cut.Find(".amc-videoplayer-timeduration").TextContent.Trim());
    }

    [Fact]
    public async Task Hovering_the_bar_previews_the_time_under_the_pointer()
    {
        JSInterop.Setup<double?>("amcVideoPlayerSeeking", call => call.Arguments.Count == 2).SetResult(42d);
        IRenderedComponent<Player> cut = await RenderLoadedAsync();

        await cut.Find(".amc-videoplayer-progress").FireMouseMoveAsync(new MouseEventArgs { ClientX = 300 });

        Assert.Equal("0:42", SeekText(cut));
        Assert.Contains("_showseekinginfo", Classes(cut).Split(' '));
        Assert.Equal(300d, Assert.Single(Calls("amcVideoPlayerSeeking")).Arguments[1]);
    }

    [Theory]
    [InlineData(-1d)]
    [InlineData(null)]
    public async Task A_hover_position_outside_the_bar_is_ignored(double? scriptResult)
    {
        JSInterop.Setup<double?>("amcVideoPlayerSeeking", call => call.Arguments.Count == 2).SetResult(scriptResult);
        IRenderedComponent<Player> cut = await RenderLoadedAsync();

        await cut.Find(".amc-videoplayer-progress").FireMouseMoveAsync(new MouseEventArgs { ClientX = 3 });

        Assert.Equal("0:00", SeekText(cut));
    }

    [Fact]
    public async Task A_script_failure_while_hovering_is_swallowed()
    {
        JSInterop.Setup<double?>("amcVideoPlayerSeeking", call => call.Arguments.Count == 2).SetException(new Microsoft.JSInterop.JSException("no layout"));
        IRenderedComponent<Player> cut = await RenderLoadedAsync();

        await cut.Find(".amc-videoplayer-progress").FireMouseMoveAsync(new MouseEventArgs { ClientX = 3 });

        Assert.Equal("0:00", SeekText(cut));
    }

    [Fact]
    public async Task Moving_the_pointer_off_the_bar_hides_the_preview()
    {
        JSInterop.Setup<double?>("amcVideoPlayerSeeking", call => call.Arguments.Count == 2).SetResult(42d);
        IRenderedComponent<Player> cut = await RenderLoadedAsync();
        await cut.Find(".amc-videoplayer-progress").FireMouseMoveAsync(new MouseEventArgs { ClientX = 300 });

        await cut.Find(".amc-videoplayer-progress").FireMouseOutAsync(new MouseEventArgs());

        Assert.DoesNotContain("_showseekinginfo", Classes(cut).Split(' '));
    }

    [Fact]
    public async Task Hovering_does_not_interrupt_an_active_drag()
    {
        IRenderedComponent<Player> cut = await RenderLoadedAsync();
        await DragTo(cut, 30);
        int seekingCalls = Calls("amcVideoPlayerSeeking").Count();

        await cut.Find(".amc-videoplayer-progress").FireMouseMoveAsync(new MouseEventArgs { ClientX = 300 });
        await cut.Find(".amc-videoplayer-progress").FireMouseOutAsync(new MouseEventArgs());

        Assert.Equal(seekingCalls, Calls("amcVideoPlayerSeeking").Count());
        Assert.Contains("_showseekinginfo", Classes(cut).Split(' '));
    }
}
