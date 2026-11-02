using Bunit;
using CloudIcons.Icons;

namespace CloudComponents.Tests.Icons;

public class VolumeIconTests : BunitContext
{
    private IRenderedComponent<Volume> RenderVolume(double? value = null, bool? isMuted = null) =>
        Render<Volume>(parameters =>
        {
            if (value is not null)
                parameters.Add(p => p.Value, value.Value);

            if (isMuted is not null)
                parameters.Add(p => p.IsMuted, isMuted.Value);
        });

    private static bool HasMuteCross(IRenderedComponent<Volume> cut) =>
        cut.FindAll("line").Count == 2 && cut.FindAll("line").All(line => line.GetAttribute("x1") is "6.12" or "7.62");

    [Fact]
    public void Defaults_to_full_volume_with_one_level_bar_and_no_mute_cross()
    {
        IRenderedComponent<Volume> cut = RenderVolume();

        Assert.Single(cut.FindAll("line"));
        Assert.False(HasMuteCross(cut));
    }

    [Theory]
    [InlineData(1.0, "2", "6")]
    [InlineData(0.5, "3", "5")]
    [InlineData(0.25, "3.5", "4.5")]
    public void The_level_bar_is_centred_and_grows_with_the_value(double value, string y1, string y2)
    {
        var line = RenderVolume(value).Find("line");

        Assert.Equal(y1, line.GetAttribute("y1"));
        Assert.Equal(y2, line.GetAttribute("y2"));
        Assert.Equal("6.5", line.GetAttribute("x1"));
        Assert.Equal("6.5", line.GetAttribute("x2"));
    }

    [Fact]
    public void A_louder_value_draws_a_longer_bar_than_a_quieter_one()
    {
        static double Length(IRenderedComponent<Volume> cut)
        {
            var line = cut.Find("line");
            return double.Parse(line.GetAttribute("y2")!, System.Globalization.CultureInfo.InvariantCulture)
                 - double.Parse(line.GetAttribute("y1")!, System.Globalization.CultureInfo.InvariantCulture);
        }

        Assert.True(Length(RenderVolume(0.8)) > Length(RenderVolume(0.2)));
    }

    [Fact]
    public void A_zero_value_shows_the_mute_cross_instead_of_a_level_bar()
    {
        IRenderedComponent<Volume> cut = RenderVolume(0);

        Assert.True(HasMuteCross(cut));
    }

    [Fact]
    public void Muted_shows_the_mute_cross_whatever_the_volume_is()
    {
        IRenderedComponent<Volume> cut = RenderVolume(0.8, isMuted: true);

        Assert.True(HasMuteCross(cut));
        Assert.Empty(cut.FindAll("line[x1='6.5']"));
    }

    [Fact]
    public void Unmuting_brings_the_level_bar_back()
    {
        IRenderedComponent<Volume> cut = RenderVolume(0.6, isMuted: true);

        cut.Render(parameters => parameters.Add(p => p.IsMuted, false).Add(p => p.Value, 0.6));

        Assert.False(HasMuteCross(cut));
        Assert.Single(cut.FindAll("line[x1='6.5']"));
    }

    [Fact]
    public void The_speaker_body_is_always_drawn()
    {
        Assert.Single(RenderVolume(0, isMuted: true).FindAll("path"));
        Assert.Single(RenderVolume(1).FindAll("path"));
    }
}
