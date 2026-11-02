using Bunit;
using CloudIcons.Icons;

namespace CloudComponents.Tests.Icons;

/// <summary>The few non-logo icons that take parameters: Expand (Style) and SortIcon (Down).</summary>
public class ParameterizedIconTests : BunitContext
{
    [Fact]
    public void Expand_defaults_to_the_regular_style()
    {
        IRenderedComponent<Expand> cut = Render<Expand>();

        Assert.Equal(Render<Expand>(p => p.Add(x => x.Style, Expand.Styles.Regular)).Markup, cut.Markup);
    }

    [Fact]
    public void Expand_styles_draw_a_different_single_path_in_the_same_viewBox()
    {
        var regular = Render<Expand>(p => p.Add(x => x.Style, Expand.Styles.Regular));
        var filled = Render<Expand>(p => p.Add(x => x.Style, Expand.Styles.Filled));

        Assert.Single(regular.FindAll("path"));
        Assert.Single(filled.FindAll("path"));
        Assert.NotEqual(regular.Find("path").GetAttribute("d"), filled.Find("path").GetAttribute("d"));
        Assert.Equal(regular.Find("svg").GetAttribute("viewBox"), filled.Find("svg").GetAttribute("viewBox"));
    }

    [Fact]
    public void Expand_exposes_exactly_the_regular_and_filled_styles()
    {
        Assert.Equal(["Regular", "Filled"], Enum.GetNames<Expand.Styles>());
    }

    [Fact]
    public void SortIcon_points_up_by_default()
    {
        var cut = Render<SortIcon>();

        Assert.Equal("_up", cut.Find("svg").ClassName);
        Assert.Equal("M1 5 5 1 9 5", cut.Find("path").GetAttribute("d"));
    }

    [Fact]
    public void SortIcon_down_flips_the_class_and_the_chevron()
    {
        var down = Render<SortIcon>(p => p.Add(x => x.Down, true));
        var up = Render<SortIcon>(p => p.Add(x => x.Down, false));

        Assert.Equal("_down", down.Find("svg").ClassName);
        Assert.Equal("M1 1 5 5 9 1", down.Find("path").GetAttribute("d"));
        Assert.NotEqual(up.Find("path").GetAttribute("d"), down.Find("path").GetAttribute("d"));
    }

    [Fact]
    public void SortIcon_switches_direction_when_the_parameter_changes_on_the_same_instance()
    {
        var cut = Render<SortIcon>(p => p.Add(x => x.Down, false));

        cut.Render(p => p.Add(x => x.Down, true));

        Assert.Equal("_down", cut.Find("svg").ClassName);
    }
}
