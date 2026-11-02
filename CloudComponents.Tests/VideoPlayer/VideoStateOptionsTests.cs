using System.Text;
using global::CloudComponents.VideoPlayer;

namespace CloudComponents.Tests.VideoPlayer;

public class VideoStateOptionsTests
{
    private static readonly string[] AllPayloadNames =
    [
        "autoplay", "controls", "crossOrigin", "currentSrc", "currentTime", "defaultMuted", "defaultPlaybackRate",
        "duration", "ended", "error", "loop", "mediaGroup", "muted", "networkState", "paused", "playbackRate",
        "preload", "readyState", "seeking", "src", "startDate", "volume"
    ];

    [Fact]
    public void Nothing_selected_sends_no_state_properties()
    {
        Assert.Empty(new VideoStateOptions().GetPayload());
    }

    [Fact]
    public void All_selects_every_state_property_the_browser_can_serialise()
    {
        string[] payload = new VideoStateOptions { All = true }.GetPayload();

        Assert.Equal(AllPayloadNames.OrderBy(name => name), payload.OrderBy(name => name));
    }

    [Fact]
    public void A_single_flag_selects_only_its_camel_cased_name()
    {
        Assert.Equal(["currentTime"], new VideoStateOptions { CurrentTime = true }.GetPayload());
        Assert.Equal(["defaultPlaybackRate"], new VideoStateOptions { DefaultPlaybackRate = true }.GetPayload());
    }

    [Fact]
    public void Several_flags_are_returned_in_a_stable_order()
    {
        string[] payload = new VideoStateOptions { Volume = true, Paused = true, Duration = true }.GetPayload();

        Assert.Equal(["duration", "paused", "volume"], payload);
    }

    [Theory]
    [InlineData(nameof(VideoStateOptions.AudioTracks))]
    [InlineData(nameof(VideoStateOptions.Buffered))]
    [InlineData(nameof(VideoStateOptions.TextTracks))]
    [InlineData(nameof(VideoStateOptions.VideoTracks))]
    public void Properties_the_browser_cannot_serialise_are_never_requested(string property)
    {
        object options = new VideoStateOptions();
        typeof(VideoStateOptions).GetProperty(property)!.SetValue(options, true);

        Assert.Empty(((VideoStateOptions)options).GetPayload());
        Assert.Empty(((VideoStateOptions)options).GetPayloads());
    }

    [Fact]
    public void All_never_requests_the_unserialisable_properties()
    {
        string[] payload = new VideoStateOptions { All = true }.GetPayload();

        Assert.DoesNotContain("audioTracks", payload);
        Assert.DoesNotContain("buffered", payload);
        Assert.DoesNotContain("textTracks", payload);
        Assert.DoesNotContain("videoTracks", payload);
    }

    [Fact]
    public void The_script_payload_is_empty_text_when_nothing_is_selected()
    {
        Assert.Equal(string.Empty, new VideoStateOptions().GetPayloads());
    }

    [Fact]
    public void The_script_payload_is_an_object_literal_reading_each_property_from_the_element()
    {
        Assert.Equal("{Paused: this.paused}", new VideoStateOptions { Paused = true }.GetPayloads());
    }

    [Fact]
    public void The_script_payload_separates_several_properties_with_commas()
    {
        Assert.Equal(
            "{CurrentTime: this.currentTime,Volume: this.volume}",
            new VideoStateOptions { CurrentTime = true, Volume = true }.GetPayloads());
    }

    [Fact]
    public void StringBuilder_append_adds_the_first_value_without_a_comma_and_later_ones_with_one()
    {
        StringBuilder builder = new("{");

        builder.Append(true, "a");
        builder.Append(false, "skipped");
        builder.Append(true, "b");

        Assert.Equal("{a,b", builder.ToString());
    }

    [Fact]
    public void List_append_adds_the_value_only_when_the_condition_holds()
    {
        List<int> list = [];

        list.Append(false, 1);
        list.Append(true, 2);

        Assert.Equal([2], list);
    }
}
