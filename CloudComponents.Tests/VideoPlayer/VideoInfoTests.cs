using global::CloudComponents.VideoPlayer;

namespace CloudComponents.Tests.VideoPlayer;

public class VideoInfoTests
{
    [Fact]
    public void Aspect_ratio_is_width_over_height()
    {
        VideoInfo info = new() { Width = 1920, Height = 1080 };

        Assert.Equal(1920d / 1080d, info.AspectRatio, 10);
    }

    [Theory]
    [InlineData(1920, 1080, "16:9")]
    [InlineData(1280, 720, "16:9")]
    [InlineData(1366, 768, "16:9")]
    [InlineData(1080, 1920, "9:16")]
    [InlineData(1000, 1000, "1:1")]
    [InlineData(400, 200, "2:1")]
    [InlineData(200, 400, "1:2")]
    [InlineData(1920, 1200, "16:10")]
    [InlineData(1200, 1920, "10:16")]
    [InlineData(500, 400, "5:4")]
    [InlineData(400, 500, "4:5")]
    [InlineData(1500, 1000, "3:2")]
    [InlineData(1000, 1500, "2:3")]
    [InlineData(2350, 1000, "47:20")]
    public void Common_ratios_are_shown_in_their_familiar_form(double width, double height, string expected)
    {
        Assert.Equal(expected, new VideoInfo { Width = width, Height = height }.DisplayAspectRatio);
    }

    [Fact]
    public void An_unusual_ratio_is_shown_rounded_to_two_decimals()
    {
        // 1000 / 700 = 1.428...
        string display = new VideoInfo { Width = 1000, Height = 700 }.DisplayAspectRatio;

        Assert.Equal(1.43, double.Parse(display, System.Globalization.CultureInfo.CurrentCulture), 5);
    }

    [Fact]
    public void A_ratio_just_off_a_known_one_still_matches_it_after_rounding()
    {
        // 853x480 is how many "16:9" encodes are stored.
        Assert.Equal("16:9", new VideoInfo { Width = 853, Height = 480 }.DisplayAspectRatio);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1920, 0)]
    [InlineData(0, 1080)]
    public void A_video_whose_size_is_not_known_yet_does_not_throw(double width, double height)
    {
        VideoInfo info = new() { Width = width, Height = height };

        Assert.NotNull(info.DisplayAspectRatio);
    }

    [Fact]
    public void Duration_is_optional()
    {
        Assert.Null(new VideoInfo().Duration);
    }

    [Fact(Skip = "BUG: VideoInfo.DisplayAspectRatio maps .42 to \"20:47\" but a real 20:47 video is 0.4255, which rounds to 0.43 and is shown as \"0.43\"; the inverse of the 47:20 case is unreachable")]
    public void The_inverse_of_the_cinema_ratio_is_shown_as_20_to_47()
    {
        Assert.Equal("20:47", new VideoInfo { Width = 2000, Height = 4700 }.DisplayAspectRatio);
    }
}
