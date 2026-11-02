using AngryMonkey.CloudComponents;
using Bunit;

namespace CloudComponents.Tests.Core.Interactive;

public class SwitchTests : InteractiveTestBase
{
    private IRenderedComponent<Switch> RenderSwitch(bool? value, Action<ComponentParameterCollectionBuilder<Switch>>? configure = null, List<bool?>? changes = null) =>
        Render<Switch>(parameters =>
        {
            parameters.Add(p => p.Value, value);

            if (changes is not null)
                parameters.Add(p => p.ValueChanged, Record(changes));

            configure?.Invoke(parameters);
        });

    [Theory]
    [InlineData(true, "_true", "on")]
    [InlineData(false, "_false", "off")]
    [InlineData(null, "_none", "not set")]
    public void The_value_selects_the_state_class_and_the_default_text(bool? value, string expectedClass, string expectedText)
    {
        IRenderedComponent<Switch> cut = RenderSwitch(value);

        AngleSharp.Dom.IElement root = cut.Find(".amc-switch");
        Assert.Contains(expectedClass, root.ClassList);
        Assert.Single(root.ClassList, name => name.StartsWith('_'));
        Assert.Equal(expectedText, cut.Find(".amc-switch-text").TextContent);
    }

    [Fact]
    public void Custom_texts_replace_the_defaults_for_each_state()
    {
        void Configure(ComponentParameterCollectionBuilder<Switch> p) => p
            .Add(x => x.TrueDefaultText, "enabled")
            .Add(x => x.FalseDefaultText, "disabled")
            .Add(x => x.NoneDefaultText, "unknown");

        Assert.Equal("enabled", RenderSwitch(true, Configure).Find(".amc-switch-text").TextContent);
        Assert.Equal("disabled", RenderSwitch(false, Configure).Find(".amc-switch-text").TextContent);
        Assert.Equal("unknown", RenderSwitch(null, Configure).Find(".amc-switch-text").TextContent);
    }

    [Fact]
    public void The_text_can_be_hidden()
    {
        IRenderedComponent<Switch> cut = RenderSwitch(true, p => p.Add(x => x.DisplayText, false));

        Assert.Empty(cut.FindAll(".amc-switch-text"));
    }

    [Fact]
    public void The_switch_always_renders_its_slider_and_a_button()
    {
        IRenderedComponent<Switch> cut = RenderSwitch(false, p => p.Add(x => x.DisplayText, false));

        Assert.NotNull(cut.Find(".amc-switch-slider .amc-switch-circle"));
        Assert.Single(cut.FindAll("button"));
    }

    [Fact]
    public void Clicking_an_on_switch_turns_it_off_and_reports_it()
    {
        List<bool?> changes = [];
        IRenderedComponent<Switch> cut = RenderSwitch(true, changes: changes);
        changes.Clear();

        cut.Find("button").Click();

        Assert.Equal([false], changes);
        Assert.Contains("_false", cut.Find(".amc-switch").ClassList);
        Assert.Equal("off", cut.Find(".amc-switch-text").TextContent);
    }

    [Fact]
    public void Clicking_an_off_switch_turns_it_on_when_unset_is_not_allowed()
    {
        List<bool?> changes = [];
        IRenderedComponent<Switch> cut = RenderSwitch(false, changes: changes);
        changes.Clear();

        cut.Find("button").Click();

        Assert.Equal([true], changes);
        Assert.Contains("_true", cut.Find(".amc-switch").ClassList);
    }

    [Fact]
    public void Clicking_an_off_switch_clears_it_when_unset_is_allowed()
    {
        List<bool?> changes = [];
        IRenderedComponent<Switch> cut = RenderSwitch(false, p => p.Add(x => x.AllowNone, true), changes);
        changes.Clear();

        cut.Find("button").Click();

        Assert.Equal([(bool?)null], changes);
        Assert.Contains("_none", cut.Find(".amc-switch").ClassList);
        Assert.Equal("not set", cut.Find(".amc-switch-text").TextContent);
    }

    [Fact]
    public void Clicking_an_unset_switch_turns_it_on()
    {
        List<bool?> changes = [];
        IRenderedComponent<Switch> cut = RenderSwitch(null, changes: changes);
        changes.Clear();

        cut.Find("button").Click();

        Assert.Equal([true], changes);
    }

    [Fact]
    public void With_unset_allowed_repeated_clicks_cycle_on_off_unset()
    {
        List<bool?> changes = [];
        IRenderedComponent<Switch> cut = RenderSwitch(null, p => p.Add(x => x.AllowNone, true), changes);
        changes.Clear();

        for (int click = 0; click < 6; click++)
            cut.Find("button").Click();

        Assert.Equal([true, false, null, true, false, null], changes);
    }

    [Fact]
    public void Without_unset_allowed_repeated_clicks_toggle_between_on_and_off()
    {
        List<bool?> changes = [];
        IRenderedComponent<Switch> cut = RenderSwitch(true, changes: changes);
        changes.Clear();

        for (int click = 0; click < 4; click++)
            cut.Find("button").Click();

        Assert.Equal([false, true, false, true], changes);
    }

    [Fact]
    public void A_parent_can_change_the_value_and_the_switch_follows()
    {
        IRenderedComponent<Switch> cut = RenderSwitch(false);

        cut.Render(p => p.Add(x => x.Value, true));

        Assert.Contains("_true", cut.Find(".amc-switch").ClassList);
        Assert.Equal("on", cut.Find(".amc-switch-text").TextContent);
    }

    [Fact]
    public void The_value_is_unset_and_text_visible_by_default()
    {
        IRenderedComponent<Switch> cut = Render<Switch>();

        Assert.Null(cut.Instance.Value);
        Assert.False(cut.Instance.AllowNone);
        Assert.True(cut.Instance.DisplayText);
        Assert.Contains("_none", cut.Find(".amc-switch").ClassList);
    }
}
