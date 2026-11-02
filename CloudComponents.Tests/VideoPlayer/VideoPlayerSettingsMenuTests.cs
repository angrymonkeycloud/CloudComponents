using Bunit;
using global::CloudComponents.VideoPlayer;
using Player = global::CloudComponents.VideoPlayer.VideoPlayer;

namespace CloudComponents.Tests.VideoPlayer;

public class VideoPlayerSettingsMenuTests : PlayerTestBase
{
    private async Task<IRenderedComponent<Player>> OpenMenuAsync(VideoPlayerMetadata? metadata = null, bool loadVideo = true)
    {
        IRenderedComponent<Player> cut = RenderPlayer(metadata);

        if (loadVideo)
            await LoadMetadataAsync(cut);

        await ClickAsync(cut.Find(".amc-videoplayer-more button"));
        return cut;
    }

    private static string[] SpeedLabels(IRenderedComponent<Player> cut) =>
        [.. cut.FindAll(".amc-videoplayer-sidebar-playbackspeed button").Select(button => button.TextContent.Trim())];

    [Fact]
    public void The_settings_button_is_hidden_when_settings_are_turned_off()
    {
        IRenderedComponent<Player> cut = RenderPlayer(NewMetadata(m => m.ShowSettings = false));

        Assert.Empty(cut.FindAll(".amc-videoplayer-more"));
    }

    [Fact]
    public void The_settings_menu_is_closed_until_the_button_is_pressed()
    {
        Assert.Empty(RenderPlayer().FindAll(".amc-videoplayer-sidebar"));
    }

    [Fact]
    public async Task The_settings_button_opens_the_menu_with_info_speed_and_loop()
    {
        IRenderedComponent<Player> cut = await OpenMenuAsync();

        Assert.Single(cut.FindAll(".amc-videoplayer-sidebar-menu"));
        Assert.NotEmpty(cut.FindAll(".amc-videoplayer-menu-info"));
        Assert.NotEmpty(cut.FindAll(".amc-videoplayer-menu-playbackspeed"));
        Assert.NotEmpty(cut.FindAll(".amc-videoplayer-menu-loop"));
    }

    [Fact]
    public async Task The_settings_button_closes_the_menu_again()
    {
        IRenderedComponent<Player> cut = await OpenMenuAsync();

        await ClickAsync(cut.Find(".amc-videoplayer-more button"));

        Assert.Empty(cut.FindAll(".amc-videoplayer-sidebar"));
    }

    [Fact]
    public async Task Opening_the_menu_hides_the_child_content_header()
    {
        VideoPlayerMetadata metadata = NewMetadata();
        IRenderedComponent<Player> cut = RenderPlayer(metadata, p => p.AddChildContent("<b>Episode 1</b>"));
        Assert.Contains("Episode 1", cut.Find(".amc-videoplayer-header").TextContent);

        await ClickAsync(cut.Find(".amc-videoplayer-more button"));

        Assert.Empty(cut.FindAll(".amc-videoplayer-header"));
    }

    [Fact]
    public async Task The_menu_shows_the_current_status_speed_and_loop_values()
    {
        IRenderedComponent<Player> cut = await OpenMenuAsync();

        Assert.Equal("NotPlaying", cut.Find(".amc-videoplayer-menu-info .amc-videoplayer-settings-subvalue").TextContent.Trim());
        Assert.Equal("Normal", cut.Find(".amc-videoplayer-menu-playbackspeed .amc-videoplayer-settings-subvalue").TextContent.Trim());
        Assert.Equal("Off", cut.Find(".amc-videoplayer-menu-loop .amc-videoplayer-settings-subvalue").TextContent.Trim());
    }

    [Fact]
    public async Task Video_info_lists_title_duration_aspect_ratio_and_status()
    {
        IRenderedComponent<Player> cut = await OpenMenuAsync();

        await ClickAsync(cut.Find(".amc-videoplayer-menu-info"));

        string[] labels = [.. cut.FindAll(".amc-videoplayer-sidebar-section label").Select(label => label.TextContent)];
        string[] values = [.. cut.FindAll(".amc-videoplayer-sidebar-section .amc-videoplayer-value").Select(value => value.TextContent)];

        Assert.Equal(["Title", "Duration", "Aspect Ratio", "Status"], labels);
        Assert.Equal(["Clip", "2:05", "16:9", "NotPlaying"], values);
    }

    [Fact]
    public async Task Video_info_leaves_out_what_is_not_known_yet()
    {
        IRenderedComponent<Player> cut = await OpenMenuAsync(NewMetadata(m => m.Title = null), loadVideo: false);

        await ClickAsync(cut.Find(".amc-videoplayer-menu-info"));

        Assert.Equal(["Status"], cut.FindAll(".amc-videoplayer-sidebar-section label").Select(label => label.TextContent));
    }

    [Fact]
    public async Task Playback_speed_offers_eight_speeds_with_normal_selected()
    {
        IRenderedComponent<Player> cut = await OpenMenuAsync();

        await ClickAsync(cut.Find(".amc-videoplayer-menu-playbackspeed"));

        Assert.Equal(["0.25", "0.5", "0.75", "Normal", "1.25", "1.5", "1.75", "2"], SpeedLabels(cut));
        Assert.Equal(["Normal"], cut.FindAll(".amc-videoplayer-sidebar-playbackspeed button._selected").Select(button => button.TextContent.Trim()));
    }

    [Fact]
    public async Task Choosing_a_speed_sets_it_on_the_video_and_returns_to_the_menu()
    {
        IRenderedComponent<Player> cut = await OpenMenuAsync();
        await ClickAsync(cut.Find(".amc-videoplayer-menu-playbackspeed"));

        await ClickAsync(cut.FindAll(".amc-videoplayer-sidebar-playbackspeed button").Single(button => button.TextContent.Trim() == "1.5"));

        object?[] arguments = Assert.Single(Calls("amcVideoPlayerSetVideoPlaybackSpeed")).Arguments.ToArray();
        Assert.Equal(1.5, arguments[1]);
        Assert.Single(cut.FindAll(".amc-videoplayer-sidebar-menu"));
        Assert.Equal("1.5", cut.Find(".amc-videoplayer-menu-playbackspeed .amc-videoplayer-settings-subvalue").TextContent.Trim());
    }

    [Fact]
    public async Task The_chosen_speed_is_the_selected_one_next_time()
    {
        IRenderedComponent<Player> cut = await OpenMenuAsync();
        await ClickAsync(cut.Find(".amc-videoplayer-menu-playbackspeed"));
        await ClickAsync(cut.FindAll(".amc-videoplayer-sidebar-playbackspeed button").Single(button => button.TextContent.Trim() == "2"));

        await ClickAsync(cut.Find(".amc-videoplayer-menu-playbackspeed"));

        Assert.Equal(["2"], cut.FindAll(".amc-videoplayer-sidebar-playbackspeed button._selected").Select(button => button.TextContent.Trim()));
    }

    [Fact]
    public async Task The_loop_option_toggles_looping()
    {
        VideoPlayerMetadata metadata = NewMetadata();
        IRenderedComponent<Player> cut = await OpenMenuAsync(metadata);

        await ClickAsync(cut.Find(".amc-videoplayer-menu-loop"));
        Assert.Equal("Off", cut.Find(".amc-videoplayer-sidebar-section button").TextContent.Trim());

        await ClickAsync(cut.Find(".amc-videoplayer-sidebar-section button"));

        Assert.True(metadata.Loop);
        Assert.Equal("On", cut.Find(".amc-videoplayer-menu-loop .amc-videoplayer-settings-subvalue").TextContent.Trim());

        await ClickAsync(cut.Find(".amc-videoplayer-menu-loop"));
        await ClickAsync(cut.Find(".amc-videoplayer-sidebar-section button"));

        Assert.False(metadata.Loop);
    }

    [Fact]
    public async Task Pressing_the_picture_inside_a_submenu_goes_back_to_the_menu_and_then_closes_it()
    {
        IRenderedComponent<Player> cut = await OpenMenuAsync();
        await ClickAsync(cut.Find(".amc-videoplayer-menu-playbackspeed"));
        Assert.NotEmpty(cut.FindAll(".amc-videoplayer-sidebar-playbackspeed"));

        await ClickAsync(cut.Find(".amc-videoplayer-empty"));
        Assert.Empty(cut.FindAll(".amc-videoplayer-sidebar-playbackspeed"));
        Assert.Single(cut.FindAll(".amc-videoplayer-sidebar-menu"));

        await ClickAsync(cut.Find(".amc-videoplayer-empty"));
        Assert.Empty(cut.FindAll(".amc-videoplayer-sidebar"));
        Assert.Empty(Calls("amcVideoPlayerPlay"));
    }

    [Fact]
    public async Task Closing_and_reopening_the_menu_starts_at_the_main_menu()
    {
        IRenderedComponent<Player> cut = await OpenMenuAsync();
        await ClickAsync(cut.Find(".amc-videoplayer-menu-info"));

        await ClickAsync(cut.Find(".amc-videoplayer-more button"));
        await ClickAsync(cut.Find(".amc-videoplayer-more button"));

        Assert.Single(cut.FindAll(".amc-videoplayer-sidebar-menu"));
    }

    [Fact]
    public async Task Controls_stay_visible_while_the_menu_is_open_even_during_playback()
    {
        IRenderedComponent<Player> cut = await OpenMenuAsync();

        await RaiseAsync(cut, "playing");

        Assert.DoesNotContain("_hidecontrols", Classes(cut).Split(' '));
    }

    [Fact]
    public async Task The_menu_links_to_the_vendor_site()
    {
        IRenderedComponent<Player> cut = await OpenMenuAsync();

        var link = cut.Find("a.amc-videoplayer-settings-angrymonkeycloud");

        Assert.Equal("https://angrymonkeycloud.com/", link.GetAttribute("href"));
        Assert.Equal("_blank", link.GetAttribute("target"));
    }
}
