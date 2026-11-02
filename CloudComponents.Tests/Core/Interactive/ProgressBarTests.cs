using System.Globalization;
using AngryMonkey.CloudComponents;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace CloudComponents.Tests.Core.Interactive;

public class ProgressBarTests : InteractiveTestBase
{
    private static readonly ProgressBarSeekButtonInfo SeekInfo = new() { Left = 12, Top = 3, Width = 20, Height = 20 };

    public ProgressBarTests()
    {
        Js.Responder = call => call.Identifier == "amcProgressBarGetInfo" ? SeekInfo : null;
    }

    private IRenderedComponent<ProgressBar> RenderBar(Action<ComponentParameterCollectionBuilder<ProgressBar>>? configure = null) =>
        Render<ProgressBar>(parameters => configure?.Invoke(parameters));

    [Fact]
    public void A_progress_bar_defaults_to_the_flat_style()
    {
        IRenderedComponent<ProgressBar> cut = RenderBar();

        Assert.Contains("_flat", cut.Find(".amc-progressbar").ClassList);
    }

    [Theory]
    [InlineData(ProgressBarStyle.Flat, "_flat")]
    [InlineData(ProgressBarStyle.Circle, "_circle")]
    [InlineData(ProgressBarStyle.Disk, "_disk")]
    public void The_style_selects_the_matching_css_class(ProgressBarStyle style, string expected)
    {
        IRenderedComponent<ProgressBar> cut = RenderBar(p => p.Add(x => x.Style, style));

        AngleSharp.Dom.IElement root = cut.Find(".amc-progressbar");
        Assert.Contains(expected, root.ClassList);
        Assert.Single(root.ClassList, name => name.StartsWith('_'));
    }

    [Fact]
    public void The_bar_renders_its_left_middle_and_right_parts_and_two_range_inputs()
    {
        IRenderedComponent<ProgressBar> cut = RenderBar(p => p.Add(x => x.Total, 10));

        Assert.NotNull(cut.Find(".amc-progressbar-left"));
        Assert.NotNull(cut.Find(".amc-progressbar-middle"));
        Assert.NotNull(cut.Find(".amc-progressbar-right"));
        Assert.Equal("range", cut.Find("input.amc-progressbar-value").GetAttribute("type"));
        Assert.Equal("range", cut.Find("input.amc-progressbar-changingvalue").GetAttribute("type"));
    }

    [Fact]
    public void The_seek_button_is_only_shown_when_there_is_something_to_seek_through()
    {
        Assert.Empty(RenderBar().FindAll(".amc-progressbar-middle button"));
        Assert.Empty(RenderBar(p => p.Add(x => x.Total, 0)).FindAll(".amc-progressbar-middle button"));
        Assert.Single(RenderBar(p => p.Add(x => x.Total, 1)).FindAll(".amc-progressbar-middle button"));
    }

    [Fact]
    public void The_range_inputs_get_the_total_as_maximum_the_value_and_the_step()
    {
        IRenderedComponent<ProgressBar> cut = RenderBar(p => p.Add(x => x.Total, 200).Add(x => x.Value, (double?)35).Add(x => x.Step, 5));

        foreach (string selector in new[] { "input.amc-progressbar-value", "input.amc-progressbar-changingvalue" })
        {
            AngleSharp.Dom.IElement input = cut.Find(selector);
            Assert.Equal("0", input.GetAttribute("min"));
            Assert.Equal("200", input.GetAttribute("max"));
            Assert.Equal("35", input.GetAttribute("value"));
            Assert.Equal("5", input.GetAttribute("step"));
        }
    }

    [Fact]
    public void A_fractional_step_and_value_are_rendered_with_a_decimal_point()
    {
        IRenderedComponent<ProgressBar> cut = RenderBar(p => p.Add(x => x.Total, 1).Add(x => x.Value, (double?)0.5).Add(x => x.Step, 0.1));

        AngleSharp.Dom.IElement input = cut.Find("input.amc-progressbar-value");
        Assert.Equal("0.1", input.GetAttribute("step"));
        Assert.Equal("0.5", input.GetAttribute("value"));
    }

    [Fact]
    public void The_step_defaults_to_one()
    {
        IRenderedComponent<ProgressBar> cut = RenderBar(p => p.Add(x => x.Total, 10));

        Assert.Equal("1", cut.Find("input.amc-progressbar-value").GetAttribute("step"));
    }

    [Fact(Skip = "BUG: ProgressBar.razor renders step=\"@Step\" and value=\"@Value\" (doubles) with the current culture, so under fr-FR a step of 0.1 is written as step=\"0,1\", which is not a valid range input step (VolumeBar uses Step 0.1).")]
    public void A_fractional_step_and_value_stay_valid_in_a_culture_that_writes_a_decimal_comma()
    {
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

        IRenderedComponent<ProgressBar> cut = RenderBar(p => p.Add(x => x.Total, 1).Add(x => x.Step, 0.1).Add(x => x.Value, (double?)0.5));

        AngleSharp.Dom.IElement input = cut.Find("input.amc-progressbar-value");
        Assert.Equal("0.1", input.GetAttribute("step"));
        Assert.Equal("0.5", input.GetAttribute("value"));
    }

    [Fact]
    public void Setting_a_value_asks_the_script_to_move_the_bar_with_the_value_and_total()
    {
        RenderBar(p => p.Add(x => x.Value, (double?)30).Add(x => x.Total, 60));

        JsCall last = Js.RuntimeCalls("amcProgressBarRepaint").Last();
        Assert.Equal(30d, last.Args[1]);
        Assert.Equal(60d, last.Args[2]);
    }

    [Fact]
    public void Changing_the_value_repaints_the_bar_with_the_new_value()
    {
        IRenderedComponent<ProgressBar> cut = RenderBar(p => p.Add(x => x.Value, (double?)10).Add(x => x.Total, 100));
        int before = Js.RuntimeCalls("amcProgressBarRepaint").Length;

        cut.Render(p => p.Add(x => x.Value, (double?)55));

        JsCall[] added = [.. Js.RuntimeCalls("amcProgressBarRepaint").Skip(before)];
        JsCall call = Assert.Single(added);
        Assert.Equal(55d, call.Args[1]);
        Assert.Equal(100d, call.Args[2]);
    }

    [Fact]
    public void Changing_the_total_repaints_the_bar_with_the_new_total()
    {
        IRenderedComponent<ProgressBar> cut = RenderBar(p => p.Add(x => x.Value, (double?)10).Add(x => x.Total, 100));
        int before = Js.RuntimeCalls("amcProgressBarRepaint").Length;

        cut.Render(p => p.Add(x => x.Total, 200));

        JsCall call = Assert.Single(Js.RuntimeCalls("amcProgressBarRepaint").Skip(before));
        Assert.Equal(10d, call.Args[1]);
        Assert.Equal(200d, call.Args[2]);
    }

    [Fact]
    public void Re_rendering_with_the_same_value_and_total_does_not_repaint()
    {
        IRenderedComponent<ProgressBar> cut = RenderBar(p => p.Add(x => x.Value, (double?)10).Add(x => x.Total, 100));
        int before = Js.RuntimeCalls("amcProgressBarRepaint").Length;

        cut.Render(p => p.Add(x => x.BufferValue, 5));
        cut.Render(p => p.Add(x => x.Value, (double?)10));

        Assert.Equal(before, Js.RuntimeCalls("amcProgressBarRepaint").Length);
    }

    [Fact]
    public void A_failing_repaint_is_swallowed_and_the_bar_still_renders()
    {
        Js.FailWith("amcProgressBarRepaint", new JSException("element is gone"));

        IRenderedComponent<ProgressBar> cut = RenderBar(p => p.Add(x => x.Value, (double?)10).Add(x => x.Total, 100));
        cut.Render(p => p.Add(x => x.Value, (double?)20));

        Assert.Equal("20", cut.Find("input.amc-progressbar-value").GetAttribute("value"));
    }

    [Fact(Skip = "BUG: ProgressBar.razor.cs OnParametersSet repaints with ComponentElement before the first render, so the script receives an empty element reference and the initial value is never painted until Value/Total change.")]
    public void The_initial_position_is_painted_with_a_real_element_reference()
    {
        RenderBar(p => p.Add(x => x.Value, (double?)30).Add(x => x.Total, 60));

        Assert.Contains(Js.RuntimeCalls("amcProgressBarRepaint"), call => call.Args[0] is ElementReference { Id: { Length: > 0 } });
    }

    [Fact]
    public void Pressing_the_mouse_on_the_bar_hands_the_pointer_position_to_the_script()
    {
        IRenderedComponent<ProgressBar> cut = RenderBar(p => p.Add(x => x.Total, 100));

        cut.Find(".amc-progressbar").MouseDown(new MouseEventArgs { ClientX = 123.5 });

        JsCall call = Assert.Single(Js.RuntimeCalls("amcProgressBarMouseDown"));
        Assert.IsType<ElementReference>(call.Args[0]);
        Assert.Equal(123.5, call.Args[1]);
    }

    [Fact]
    public void Touching_the_bar_hands_the_first_touch_position_to_the_script()
    {
        IRenderedComponent<ProgressBar> cut = RenderBar(p => p.Add(x => x.Total, 100));

        cut.Find(".amc-progressbar").TouchStart(new TouchEventArgs { Touches = [new TouchPoint { ClientX = 77 }, new TouchPoint { ClientX = 500 }] });

        JsCall call = Assert.Single(Js.RuntimeCalls("amcProgressBarTouchDown"));
        Assert.Equal(77d, call.Args[1]);
    }

    [Fact]
    public void Releasing_at_a_new_position_reports_the_change_with_the_previous_value_and_the_seek_button_position()
    {
        List<ProgressBarChangeEventArgs> changed = [];
        IRenderedComponent<ProgressBar> cut = RenderBar(p => p
            .Add(x => x.Total, 100)
            .Add(x => x.Value, (double?)20)
            .Add(x => x.OnChanged, Record(changed)));

        cut.Find("input.amc-progressbar-value").Change("65");

        ProgressBarChangeEventArgs args = Assert.Single(changed);
        Assert.Equal(20d, args.PreviousValue);
        Assert.Equal(65d, args.NewValue);
        Assert.Same(SeekInfo, args.SeekButtonInfo);
    }

    [Fact]
    public void The_value_follows_the_change_so_the_next_change_reports_it_as_previous()
    {
        List<ProgressBarChangeEventArgs> changed = [];
        IRenderedComponent<ProgressBar> cut = RenderBar(p => p
            .Add(x => x.Total, 100)
            .Add(x => x.Value, (double?)20)
            .Add(x => x.OnChanged, Record(changed)));

        cut.Find("input.amc-progressbar-value").Change("65");
        cut.Find("input.amc-progressbar-value").Change("80");

        Assert.Equal([20d, 65d], changed.Select(args => args.PreviousValue));
        Assert.Equal(80d, cut.Instance.Value);
    }

    [Fact]
    public void A_change_to_the_value_it_already_has_is_not_reported()
    {
        List<ProgressBarChangeEventArgs> changed = [];
        IRenderedComponent<ProgressBar> cut = RenderBar(p => p
            .Add(x => x.Total, 100)
            .Add(x => x.Value, (double?)20)
            .Add(x => x.OnChanged, Record(changed)));

        cut.Find("input.amc-progressbar-value").Change("20");

        Assert.Empty(changed);
    }

    [Fact]
    public void A_first_change_without_any_value_has_no_previous_value()
    {
        List<ProgressBarChangeEventArgs> changed = [];
        IRenderedComponent<ProgressBar> cut = RenderBar(p => p.Add(x => x.Total, 100).Add(x => x.OnChanged, Record(changed)));

        cut.Find("input.amc-progressbar-value").Change("5");

        Assert.Null(Assert.Single(changed).PreviousValue);
    }

    [Fact]
    public void Dragging_reports_the_changing_value_with_the_seek_button_position()
    {
        List<ProgressBarChangeEventArgs> changing = [];
        IRenderedComponent<ProgressBar> cut = RenderBar(p => p.Add(x => x.Total, 100).Add(x => x.OnChanging, Record(changing)));

        cut.Find("input.amc-progressbar-changingvalue").Change("40");

        ProgressBarChangeEventArgs args = Assert.Single(changing);
        Assert.Equal(40d, args.NewValue);
        Assert.Same(SeekInfo, args.SeekButtonInfo);
        Assert.Equal(40d, cut.Instance.ChangingValue);
    }

    [Fact]
    public void While_dragging_each_changing_event_reports_the_one_before_as_previous()
    {
        List<ProgressBarChangeEventArgs> changing = [];
        IRenderedComponent<ProgressBar> cut = RenderBar(p => p.Add(x => x.Total, 100).Add(x => x.OnChanging, Record(changing)));

        cut.Find("input.amc-progressbar-changingvalue").Change("40");
        cut.Find("input.amc-progressbar-changingvalue").Change("45");
        cut.Find("input.amc-progressbar-changingvalue").Change("45");

        Assert.Equal([40d, 45d], changing.Select(args => args.NewValue));
        Assert.Equal(40d, changing[1].PreviousValue);
    }

    [Fact]
    public void Releasing_after_a_drag_ends_the_dragging_state()
    {
        List<ProgressBarChangeEventArgs> changed = [];
        IRenderedComponent<ProgressBar> cut = RenderBar(p => p.Add(x => x.Total, 100).Add(x => x.OnChanged, Record(changed)));

        cut.Find("input.amc-progressbar-changingvalue").Change("40");
        cut.Find("input.amc-progressbar-value").Change("40");

        Assert.Equal(40d, Assert.Single(changed).NewValue);
        Assert.Equal(-1d, cut.Instance.ChangingValue);
    }

    [Fact]
    public void Without_listeners_changes_are_accepted_silently()
    {
        IRenderedComponent<ProgressBar> cut = RenderBar(p => p.Add(x => x.Total, 100));

        cut.Find("input.amc-progressbar-changingvalue").Change("10");
        cut.Find("input.amc-progressbar-value").Change("10");

        Assert.Equal(10d, cut.Instance.Value);
    }

    [Fact(Skip = "BUG: ProgressBar.razor.cs OnRangeChange/OnChangingRangeChange parse the range value with Convert.ToDouble(object), which uses the current culture, so a decimal point from the browser throws FormatException in a culture that uses a decimal comma (e.g. fr-FR).")]
    public void A_fractional_value_from_the_browser_is_understood_in_a_culture_that_writes_a_decimal_comma()
    {
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
        List<ProgressBarChangeEventArgs> changed = [];
        IRenderedComponent<ProgressBar> cut = RenderBar(p => p.Add(x => x.Total, 1).Add(x => x.OnChanged, Record(changed)));

        cut.Find("input.amc-progressbar-value").Change("0.5");

        Assert.Equal(0.5, Assert.Single(changed).NewValue);
    }

    [Fact(Skip = "BUG: ProgressBar OnChanging reports the internal -1 'not dragging' sentinel as PreviousValue on the first changing event of a drag instead of the value the bar had.")]
    public void The_first_changing_event_of_a_drag_reports_the_current_value_as_previous()
    {
        List<ProgressBarChangeEventArgs> changing = [];
        IRenderedComponent<ProgressBar> cut = RenderBar(p => p
            .Add(x => x.Total, 100)
            .Add(x => x.Value, (double?)20)
            .Add(x => x.OnChanging, Record(changing)));

        cut.Find("input.amc-progressbar-changingvalue").Change("40");

        Assert.Equal(20d, Assert.Single(changing).PreviousValue);
    }

    [Fact]
    public void The_models_have_neutral_defaults()
    {
        ProgressBarChangeEventArgs args = new();
        ProgressBarSeekButtonInfo info = new();

        Assert.Null(args.PreviousValue);
        Assert.Equal(0d, args.NewValue);
        Assert.Null(args.SeekButtonInfo);
        Assert.Equal([0d, 0d, 0d, 0d], new[] { info.Left, info.Top, info.Width, info.Height });
    }

    [Fact]
    public void Defaults_are_a_flat_unbuffered_empty_bar_with_nothing_being_dragged()
    {
        IRenderedComponent<ProgressBar> cut = RenderBar();

        Assert.Equal(ProgressBarStyle.Flat, cut.Instance.Style);
        Assert.Equal(0d, cut.Instance.BufferValue);
        Assert.Equal(0d, cut.Instance.Total);
        Assert.Null(cut.Instance.Value);
        Assert.Equal(-1d, cut.Instance.ChangingValue);
        Assert.Equal(1d, cut.Instance.Step);
    }
}
