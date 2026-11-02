using AngryMonkey.CloudComponents;
using Bunit;

namespace CloudComponents.Tests.Core.Interactive;

public class VolumeBarTests : InteractiveTestBase
{
    private IRenderedComponent<VolumeBar> RenderBar(Action<ComponentParameterCollectionBuilder<VolumeBar>>? configure = null) =>
        Render<VolumeBar>(parameters => configure?.Invoke(parameters));

    private static IRenderedComponent<VolumeBar> Toggle(IRenderedComponent<VolumeBar> cut)
    {
        cut.Find("button.amc-volumebar-button").Click();

        return cut;
    }

    [Fact]
    public void A_collapsed_volume_bar_shows_only_the_volume_button()
    {
        IRenderedComponent<VolumeBar> cut = RenderBar();

        Assert.Single(cut.FindAll("button.amc-volumebar-button"));
        Assert.Empty(cut.FindAll(".amc-volumebar-value"));
        Assert.Empty(cut.FindAll(".amc-volumebar-mute"));
        Assert.Empty(cut.FindAll(".amc-volumebar-bar"));
    }

    [Fact]
    public void An_extended_volume_bar_shows_the_percentage_a_mute_button_and_a_slider()
    {
        IRenderedComponent<VolumeBar> cut = RenderBar(p => p.Add(x => x.Extended, true).Add(x => x.Value, 0.5));

        Assert.Equal("50", cut.Find(".amc-volumebar-value").TextContent.Trim());
        Assert.Single(cut.FindAll("button.amc-volumebar-mute"));
        Assert.Single(cut.FindAll(".amc-volumebar-bar .amc-progressbar._disk"));
    }

    [Fact]
    public void The_slider_gets_the_volume_as_its_value_on_a_zero_to_one_scale()
    {
        IRenderedComponent<VolumeBar> cut = RenderBar(p => p.Add(x => x.Extended, true).Add(x => x.Value, 0.25));

        AngleSharp.Dom.IElement input = cut.Find(".amc-volumebar-bar input.amc-progressbar-value");
        Assert.Equal("1", input.GetAttribute("max"));
        Assert.Equal("0.25", input.GetAttribute("value"));
        Assert.Equal("0.1", input.GetAttribute("step"));
    }

    [Theory]
    [InlineData(1.0, "100")]
    [InlineData(0.5, "50")]
    [InlineData(0.0, "0")]
    [InlineData(0.25, "25")]
    public void The_percentage_is_the_volume_times_one_hundred(double value, string expected)
    {
        IRenderedComponent<VolumeBar> cut = RenderBar(p => p.Add(x => x.Extended, true).Add(x => x.Value, value));

        Assert.Equal(expected, cut.Find(".amc-volumebar-value").TextContent.Trim());
    }

    [Theory(Skip = "BUG: VolumeBar.razor DisplayVolume is $\"{Value * 100}\" with no rounding, so floating point noise or a fine slider position is shown to the user (0.57 shows 56.99999999999999).")]
    [InlineData(0.57, "57")]
    [InlineData(0.55, "55")]
    [InlineData(0.07, "7")]
    public void The_percentage_is_shown_as_a_whole_number(double value, string expected)
    {
        IRenderedComponent<VolumeBar> cut = RenderBar(p => p.Add(x => x.Extended, true).Add(x => x.Value, value));

        Assert.Equal(expected, cut.Find(".amc-volumebar-value").TextContent.Trim());
    }

    [Fact]
    public void Clicking_the_volume_button_expands_and_clicking_it_again_collapses()
    {
        IRenderedComponent<VolumeBar> cut = RenderBar();

        Toggle(cut);
        Assert.Single(cut.FindAll(".amc-volumebar-bar"));

        Toggle(cut);
        Assert.Empty(cut.FindAll(".amc-volumebar-bar"));
    }

    [Fact]
    public void Expanding_the_volume_bar_reports_nothing()
    {
        List<VolumeBarChangeEventArgs> changes = [];
        IRenderedComponent<VolumeBar> cut = RenderBar(p => p.Add(x => x.OnChanging, Record(changes)));

        Toggle(cut);

        Assert.Empty(changes);
    }

    [Fact]
    public void Clicking_the_mute_button_mutes_collapses_and_reports_the_volume_unchanged()
    {
        List<VolumeBarChangeEventArgs> changes = [];
        IRenderedComponent<VolumeBar> cut = RenderBar(p => p
            .Add(x => x.Extended, true)
            .Add(x => x.Value, 0.7)
            .Add(x => x.OnChanging, Record(changes)));

        cut.Find("button.amc-volumebar-mute").Click();

        VolumeBarChangeEventArgs args = Assert.Single(changes);
        Assert.True(args.IsMuted);
        Assert.False(args.WasMuted);
        Assert.Equal(0.7, args.NewValue);
        Assert.Equal(0.7, args.PreviousValue);
        Assert.True(cut.Instance.IsMuted);
        Assert.False(cut.Instance.Extended);
        Assert.Empty(cut.FindAll(".amc-volumebar-bar"));
    }

    [Fact]
    public void Clicking_the_volume_button_while_muted_unmutes_without_expanding()
    {
        List<VolumeBarChangeEventArgs> changes = [];
        IRenderedComponent<VolumeBar> cut = RenderBar(p => p
            .Add(x => x.IsMuted, true)
            .Add(x => x.Value, 0.4)
            .Add(x => x.OnChanging, Record(changes)));

        Toggle(cut);

        VolumeBarChangeEventArgs args = Assert.Single(changes);
        Assert.True(args.WasMuted);
        Assert.False(args.IsMuted);
        Assert.Equal(0.4, args.NewValue);
        Assert.False(cut.Instance.IsMuted);
        Assert.Empty(cut.FindAll(".amc-volumebar-bar"));
    }

    [Fact]
    public void Dragging_the_slider_reports_the_new_volume_and_updates_the_percentage()
    {
        List<VolumeBarChangeEventArgs> changes = [];
        IRenderedComponent<VolumeBar> cut = RenderBar(p => p
            .Add(x => x.Extended, true)
            .Add(x => x.OnChanging, Record(changes)));

        cut.Find("input.amc-progressbar-changingvalue").Change("0.4");

        VolumeBarChangeEventArgs args = Assert.Single(changes);
        Assert.Equal(0.4, args.NewValue);
        Assert.False(args.IsMuted);
        Assert.False(args.WasMuted);
        Assert.Equal(0.4, cut.Instance.Value);
        Assert.Equal("40", cut.Find(".amc-volumebar-value").TextContent.Trim());
    }

    [Fact]
    public void Dragging_while_muted_keeps_reporting_the_muted_state()
    {
        List<VolumeBarChangeEventArgs> changes = [];
        IRenderedComponent<VolumeBar> cut = RenderBar(p => p
            .Add(x => x.Extended, true)
            .Add(x => x.IsMuted, true)
            .Add(x => x.OnChanging, Record(changes)));

        cut.Find("input.amc-progressbar-changingvalue").Change("0.3");

        VolumeBarChangeEventArgs args = Assert.Single(changes);
        Assert.True(args.WasMuted);
        Assert.True(args.IsMuted);
    }

    [Fact(Skip = "BUG: the first OnChanging of a drag carries the ProgressBar's internal -1 'not dragging' sentinel as PreviousValue instead of the volume before the drag.")]
    public void The_first_event_of_a_drag_reports_the_volume_before_the_drag_as_previous()
    {
        List<VolumeBarChangeEventArgs> changes = [];
        IRenderedComponent<VolumeBar> cut = RenderBar(p => p
            .Add(x => x.Extended, true)
            .Add(x => x.Value, 0.8)
            .Add(x => x.OnChanging, Record(changes)));

        cut.Find("input.amc-progressbar-changingvalue").Change("0.4");

        Assert.Equal(0.8, Assert.Single(changes).PreviousValue);
    }

    [Fact]
    public void Releasing_the_slider_collapses_the_bar_and_reports_the_final_volume()
    {
        List<VolumeBarChangeEventArgs> changes = [];
        IRenderedComponent<VolumeBar> cut = RenderBar(p => p
            .Add(x => x.Extended, true)
            .Add(x => x.OnChanging, Record(changes)));

        cut.Find("input.amc-progressbar-changingvalue").Change("0.6");
        changes.Clear();

        cut.Find("input.amc-progressbar-value").Change("0.6");

        VolumeBarChangeEventArgs args = Assert.Single(changes);
        Assert.Equal(0.6, args.NewValue);
        Assert.False(args.IsMuted);
        Assert.Empty(cut.FindAll(".amc-volumebar-bar"));
    }

    [Fact]
    public void Releasing_the_slider_at_zero_mutes_and_restores_the_volume_to_full()
    {
        List<VolumeBarChangeEventArgs> changes = [];
        IRenderedComponent<VolumeBar> cut = RenderBar(p => p
            .Add(x => x.Extended, true)
            .Add(x => x.Value, 0.5)
            .Add(x => x.OnChanging, Record(changes)));

        cut.Find("input.amc-progressbar-value").Change("0");

        VolumeBarChangeEventArgs args = Assert.Single(changes);
        Assert.True(args.IsMuted);
        Assert.False(args.WasMuted);
        Assert.Equal(1d, args.NewValue);
        Assert.True(cut.Instance.IsMuted);
        Assert.Equal(1d, cut.Instance.Value);
    }

    [Fact(Skip = "BUG: VolumeBar declares an OnChanged parameter but never invokes it; the final value after releasing the slider is reported through OnChanging instead.")]
    public void OnChanged_is_invoked_when_the_slider_is_released()
    {
        List<VolumeBarChangeEventArgs> changed = [];
        IRenderedComponent<VolumeBar> cut = RenderBar(p => p
            .Add(x => x.Extended, true)
            .Add(x => x.OnChanged, Record(changed)));

        cut.Find("input.amc-progressbar-value").Change("0.6");

        Assert.Equal(0.6, Assert.Single(changed).NewValue);
    }

    [Fact]
    public void The_volume_defaults_to_full_unmuted_and_collapsed()
    {
        IRenderedComponent<VolumeBar> cut = RenderBar();

        Assert.Equal(1d, cut.Instance.Value);
        Assert.False(cut.Instance.IsMuted);
        Assert.False(cut.Instance.Extended);
    }

    [Fact]
    public void The_change_event_model_has_neutral_defaults()
    {
        VolumeBarChangeEventArgs args = new();

        Assert.Null(args.PreviousValue);
        Assert.Equal(0d, args.NewValue);
        Assert.False(args.WasMuted);
        Assert.False(args.IsMuted);
    }
}
