using AngleSharp.Dom;
using AngryMonkey.CloudComponents;
using Bunit;
using DateTimePickerComponent = AngryMonkey.CloudComponents.DateTimePicker;

namespace CloudComponents.Tests.Core.DateTimePicker;

/// <summary>The time part of the picker: the minute grid, the hour grid, AM/PM and stepping between hours.</summary>
public class DateTimePickerTimeTests : PickerTestBase
{
    private IRenderedComponent<DateTimePickerComponent> TimePicker() => RenderPicker(DateTimePickerMode.Time);

    private static IReadOnlyList<string> Texts(IEnumerable<IElement> cells) => [.. cells.Select(cell => cell.TextContent.Trim())];

    private static void ClickMinute(IRenderedComponent<DateTimePickerComponent> cut, int minute) =>
        MinuteCells(cut).Single(cell => cell.TextContent.Trim() == minute.ToString()).Click();

    // ----- minute grid -----------------------------------------------------------------------------------------------------------

    [Fact]
    public void The_time_view_opens_on_the_minute_grid_of_sixty_minutes()
    {
        IRenderedComponent<DateTimePickerComponent> cut = TimePicker();

        Assert.Empty(HourCells(cut));
        Assert.Equal(Enumerable.Range(0, 60).Select(minute => minute.ToString()), Texts(MinuteCells(cut)));
    }

    [Fact]
    public void Every_minute_cell_carries_a_class_with_its_minute()
    {
        IRenderedComponent<DateTimePickerComponent> cut = TimePicker();

        IReadOnlyList<IElement> cells = MinuteCells(cut);

        for (int minute = 0; minute < 60; minute++)
            Assert.Contains($"_{minute}", cells[minute].ClassList);
    }

    [Fact]
    public void Every_fifth_minute_is_primary_and_current_and_no_other_is()
    {
        IRenderedComponent<DateTimePickerComponent> cut = TimePicker();

        IReadOnlyList<IElement> cells = MinuteCells(cut);

        for (int minute = 0; minute < 60; minute++)
        {
            bool five = minute % 5 == 0;
            Assert.Equal(five, cells[minute].ClassList.Contains("_primary"));
            Assert.Equal(five, cells[minute].ClassList.Contains("_current"));
        }
    }

    [Fact]
    public void The_present_minute_is_selected_when_the_picker_opens_and_matches_the_header()
    {
        IRenderedComponent<DateTimePickerComponent> cut = TimePicker();

        IElement selected = Assert.Single(cut.FindAll(".amc-datetimepicker-minutes-minute._selected"));

        Assert.Equal(HeaderTimeOf(cut).Minute.ToString(), selected.TextContent.Trim());
    }

    [Fact]
    public void The_title_reads_hour_then_AM_or_PM_separated_by_two_spaces_and_matches_the_header_hour()
    {
        IRenderedComponent<DateTimePickerComponent> cut = TimePicker();

        Assert.Matches(@"^\d{1,2}  (AM|PM)$", Title(cut, time: true));
        Assert.Equal(HeaderTimeOf(cut).Hour.ToString(), Title(cut, time: true).Split(' ')[0]);
    }

    [Fact]
    public void Clicking_a_minute_selects_it_and_updates_the_header()
    {
        IRenderedComponent<DateTimePickerComponent> cut = TimePicker();
        (int Hour, int Minute, string Suffix) before = HeaderTimeOf(cut);
        int target = (before.Minute + 17) % 60;

        ClickMinute(cut, target);

        IElement selected = Assert.Single(cut.FindAll(".amc-datetimepicker-minutes-minute._selected"));
        Assert.Equal(target.ToString(), selected.TextContent.Trim());
        Assert.Equal((before.Hour, target, before.Suffix), HeaderTimeOf(cut));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(59)]
    public void The_header_minute_is_always_two_digits(int minute)
    {
        IRenderedComponent<DateTimePickerComponent> cut = TimePicker();

        ClickMinute(cut, minute);

        Assert.Matches($@":{minute:00} (AM|PM)", Header(cut));
    }

    [Fact]
    public void Clicking_the_same_minute_twice_keeps_it_selected()
    {
        IRenderedComponent<DateTimePickerComponent> cut = TimePicker();

        ClickMinute(cut, 20);
        ClickMinute(cut, 20);

        Assert.Single(cut.FindAll(".amc-datetimepicker-minutes-minute._selected"));
        Assert.Equal(20, HeaderTimeOf(cut).Minute);
    }

    [Fact]
    public void Clicking_a_minute_does_not_change_the_hour_or_leave_the_minute_grid()
    {
        IRenderedComponent<DateTimePickerComponent> cut = TimePicker();
        (int Hour, int Minute, string Suffix) before = HeaderTimeOf(cut);

        ClickMinute(cut, (before.Minute + 7) % 60);

        Assert.Equal(60, MinuteCells(cut).Count);
        Assert.Equal(before.Hour, HeaderTimeOf(cut).Hour);
        Assert.Equal(before.Suffix, HeaderTimeOf(cut).Suffix);
    }

    // ----- hour grid --------------------------------------------------------------------------------------------------------------

    [Fact]
    public void Clicking_the_title_opens_the_hour_grid_titled_AM_or_PM()
    {
        IRenderedComponent<DateTimePickerComponent> cut = TimePicker();

        ClickTitle(cut, time: true);

        Assert.Empty(MinuteCells(cut));
        Assert.Contains(Title(cut, time: true), new[] { "AM", "PM" });
    }

    [Fact]
    public void The_hour_grid_shows_twelve_hours_labelled_one_to_twelve_in_both_halves_of_the_day()
    {
        IRenderedComponent<DateTimePickerComponent> cut = TimePicker();
        ClickTitle(cut, time: true);
        IReadOnlyList<string> expected = Enumerable.Range(1, 12).Select(hour => hour.ToString()).ToArray();

        Assert.Equal(expected, Texts(HourCells(cut)));

        ClickTitle(cut, time: true);
        Assert.Equal(expected, Texts(HourCells(cut)));
    }

    [Fact]
    public void Every_hour_cell_is_marked_current()
    {
        IRenderedComponent<DateTimePickerComponent> cut = TimePicker();
        ClickTitle(cut, time: true);

        Assert.All(HourCells(cut), cell => Assert.Contains("_current", cell.ClassList));
    }

    [Fact]
    public void Clicking_the_title_in_the_hour_grid_switches_between_AM_and_PM()
    {
        IRenderedComponent<DateTimePickerComponent> cut = TimePicker();
        ClickTitle(cut, time: true);
        string first = Title(cut, time: true);

        ClickTitle(cut, time: true);
        string second = Title(cut, time: true);
        ClickTitle(cut, time: true);

        Assert.NotEqual(first, second);
        Assert.Equal(first, Title(cut, time: true));
        Assert.Equal(12, HourCells(cut).Count);
    }

    [Fact]
    public void Both_arrows_in_the_hour_grid_switch_between_AM_and_PM()
    {
        IRenderedComponent<DateTimePickerComponent> cut = TimePicker();
        ClickTitle(cut, time: true);
        string first = Title(cut, time: true);

        ClickNext(cut, time: true);
        string afterNext = Title(cut, time: true);
        ClickPrevious(cut, time: true);
        string afterPrevious = Title(cut, time: true);

        Assert.NotEqual(first, afterNext);
        Assert.Equal(first, afterPrevious);
    }

    [Fact]
    public void Switching_the_hour_grid_to_the_other_half_of_the_day_does_not_change_the_selected_time()
    {
        IRenderedComponent<DateTimePickerComponent> cut = TimePicker();
        (int Hour, int Minute, string Suffix) before = HeaderTimeOf(cut);
        ClickTitle(cut, time: true);

        ClickNext(cut, time: true);

        Assert.Equal(before, HeaderTimeOf(cut));
    }

    [Fact]
    public void The_hour_grid_highlights_the_selected_hour_in_its_half_of_the_day_only()
    {
        IRenderedComponent<DateTimePickerComponent> cut = TimePicker();
        int hour = HourOtherThanNow(3);
        SelectHour(cut, hour);

        ClickTitle(cut, time: true);
        Assert.Equal("AM", Title(cut, time: true));
        IElement selected = Assert.Single(cut.FindAll(".amc-datetimepicker-hours-hour._selected"));
        Assert.Equal(hour.ToString(), selected.TextContent.Trim());

        ClickTitle(cut, time: true);
        Assert.Equal("PM", Title(cut, time: true));
        Assert.Empty(cut.FindAll(".amc-datetimepicker-hours-hour._selected"));
    }

    [Fact]
    public void Picking_an_hour_returns_to_the_minute_grid()
    {
        IRenderedComponent<DateTimePickerComponent> cut = TimePicker();

        SelectHour(cut, HourOtherThanNow(4));

        Assert.Empty(HourCells(cut));
        Assert.Equal(60, MinuteCells(cut).Count);
    }

    [Fact]
    public void Picking_a_different_hour_selects_that_hour_at_minute_zero()
    {
        IRenderedComponent<DateTimePickerComponent> cut = TimePicker();

        int hour = HourOtherThanNow(3);

        SelectHour(cut, hour);

        Assert.Equal((hour, 0, "AM"), HeaderTimeOf(cut));
        Assert.Equal($"{hour}  AM", Title(cut, time: true));
        IElement selected = Assert.Single(cut.FindAll(".amc-datetimepicker-minutes-minute._selected"));
        Assert.Equal("0", selected.TextContent.Trim());
    }

    [Fact]
    public void Picking_an_afternoon_hour_shows_PM()
    {
        IRenderedComponent<DateTimePickerComponent> cut = TimePicker();
        int hour = HourOtherThanNow(15);

        SelectHour(cut, hour);

        Assert.Equal((hour - 12, 0, "PM"), HeaderTimeOf(cut));
        Assert.Equal($"{hour - 12}  PM", Title(cut, time: true));
    }

    [Fact]
    public void Picking_the_hour_that_is_already_shown_keeps_the_selected_minute()
    {
        IRenderedComponent<DateTimePickerComponent> cut = TimePicker();
        int hour = HourOtherThanNow(3);
        SelectHour(cut, hour);
        ClickMinute(cut, 42);

        SelectHour(cut, hour);

        Assert.Equal((hour, 42, "AM"), HeaderTimeOf(cut));
    }

    [Fact]
    public void Picking_another_hour_after_choosing_a_minute_resets_the_minute_to_zero()
    {
        IRenderedComponent<DateTimePickerComponent> cut = TimePicker();
        SelectHour(cut, HourOtherThanNow(3));
        ClickMinute(cut, 42);

        SelectHour(cut, 9);

        Assert.Equal((9, 0, "AM"), HeaderTimeOf(cut));
    }

    [Fact]
    public void The_time_part_works_the_same_next_to_the_date_part()
    {
        IRenderedComponent<DateTimePickerComponent> cut = RenderPicker(DateTimePickerMode.DateAndTime);

        int hour = HourOtherThanNow(3);
        SelectHour(cut, hour);
        ClickMinute(cut, 15);

        Assert.Equal((hour, 15, "AM"), HeaderTimeOf(cut));
        Assert.Equal($"{hour}  AM", Title(cut, time: true));
    }

    // ----- hour arrows in the minute view -------------------------------------------------------------------------------------

    [Fact]
    public void The_next_arrow_in_the_minute_grid_shows_the_following_hour()
    {
        IRenderedComponent<DateTimePickerComponent> cut = TimePicker();
        SelectHour(cut, 5);

        ClickNext(cut, time: true);

        Assert.Equal("6  AM", Title(cut, time: true));
        Assert.Equal(60, MinuteCells(cut).Count);
    }

    [Fact]
    public void The_previous_arrow_in_the_minute_grid_shows_the_hour_before()
    {
        IRenderedComponent<DateTimePickerComponent> cut = TimePicker();
        SelectHour(cut, 5);

        ClickPrevious(cut, time: true);

        Assert.Equal("4  AM", Title(cut, time: true));
    }

    [Fact]
    public void Browsing_to_another_hour_leaves_the_selected_time_alone_and_hides_the_selected_minute_marker()
    {
        IRenderedComponent<DateTimePickerComponent> cut = TimePicker();
        int hour = HourOtherThanNow(5);
        SelectHour(cut, hour);
        (int Hour, int Minute, string Suffix) selected = HeaderTimeOf(cut);

        ClickNext(cut, time: true);

        Assert.Equal(selected, HeaderTimeOf(cut));
        Assert.Empty(cut.FindAll(".amc-datetimepicker-minutes-minute._selected"));

        ClickPrevious(cut, time: true);
        IElement back = Assert.Single(cut.FindAll(".amc-datetimepicker-minutes-minute._selected"));
        Assert.Equal("0", back.TextContent.Trim());
    }

    [Fact]
    public void Clicking_a_minute_on_a_browsed_hour_selects_that_hour_and_minute()
    {
        IRenderedComponent<DateTimePickerComponent> cut = TimePicker();
        int hour = HourOtherThanNow(5);
        SelectHour(cut, hour);
        ClickNext(cut, time: true);

        ClickMinute(cut, 25);

        Assert.Equal((hour + 1, 25, "AM"), HeaderTimeOf(cut));
    }

    [Fact]
    public void Stepping_forward_from_eleven_AM_goes_to_twelve_PM_and_then_one_PM()
    {
        IRenderedComponent<DateTimePickerComponent> cut = TimePicker();
        SelectHour(cut, 11);

        ClickNext(cut, time: true);
        Assert.Equal("12  PM", Title(cut, time: true));

        ClickNext(cut, time: true);
        Assert.Equal("1  PM", Title(cut, time: true));
    }

    [Fact]
    public void Stepping_back_from_one_PM_goes_to_twelve_PM_and_then_eleven_AM()
    {
        IRenderedComponent<DateTimePickerComponent> cut = TimePicker();
        SelectHour(cut, 13);

        ClickPrevious(cut, time: true);
        Assert.Equal("12  PM", Title(cut, time: true));

        ClickPrevious(cut, time: true);
        Assert.Equal("11  AM", Title(cut, time: true));
    }

    [Fact]
    public void Stepping_forward_from_eleven_PM_wraps_around_to_twelve_AM()
    {
        IRenderedComponent<DateTimePickerComponent> cut = TimePicker();
        SelectHour(cut, 23);
        Assert.Equal("11  PM", Title(cut, time: true));

        ClickNext(cut, time: true);
        Assert.Equal("12  AM", Title(cut, time: true));

        ClickNext(cut, time: true);
        Assert.Equal("1  AM", Title(cut, time: true));
    }

    [Fact]
    public void Stepping_back_from_one_AM_goes_to_twelve_AM_and_wraps_to_eleven_PM()
    {
        IRenderedComponent<DateTimePickerComponent> cut = TimePicker();
        SelectHour(cut, 1);
        Assert.Equal("1  AM", Title(cut, time: true));

        ClickPrevious(cut, time: true);
        Assert.Equal("12  AM", Title(cut, time: true));

        ClickPrevious(cut, time: true);
        Assert.Equal("11  PM", Title(cut, time: true));
    }

    [Fact]
    public void Stepping_through_a_whole_day_comes_back_to_the_starting_hour()
    {
        IRenderedComponent<DateTimePickerComponent> cut = TimePicker();
        SelectHour(cut, HourOtherThanNow(3));
        string start = Title(cut, time: true);

        for (int step = 0; step < 24; step++)
            ClickNext(cut, time: true);

        Assert.Equal(start, Title(cut, time: true));
    }

    [Fact]
    public void The_hour_grid_keeps_the_half_of_the_day_the_minute_view_had_reached()
    {
        IRenderedComponent<DateTimePickerComponent> cut = TimePicker();
        SelectHour(cut, 11);
        ClickNext(cut, time: true); // 12 PM

        ClickTitle(cut, time: true);

        Assert.Equal("PM", Title(cut, time: true));
    }

    // ----- midnight and noon in the hour grid -----------------------------------------------------------------------------------

    [Fact(Skip = "BUG: DateTimePicker.FillHoursArray (Razor Code/Methods.cs) lists hours 1..12 for AM and 13..24 for PM instead of 12(0),1..11 and 12..23. The AM 'twelve' cell is hour 12, so choosing 12 in the AM list selects 12:00 PM, and midnight (hour 0) can never be chosen from the hour grid.")]
    public void Choosing_twelve_in_the_AM_hour_grid_selects_midnight()
    {
        IRenderedComponent<DateTimePickerComponent> cut = TimePicker();
        ClickTitle(cut, time: true);

        if (Title(cut, time: true) != "AM")
            ClickTitle(cut, time: true);

        HourCells(cut).Single(cell => cell.TextContent.Trim() == "12").Click();

        Assert.Equal((12, 0, "AM"), HeaderTimeOf(cut));
    }

    [Fact(Skip = "BUG: DateTimePicker.FillHoursArray offers hour 24 for the PM 'twelve' cell, so after choosing it the hour arrows continue from 24 to 25 and the title reads '13  PM' instead of '1  PM'.")]
    public void Choosing_twelve_in_the_PM_hour_grid_and_stepping_forward_shows_one_PM()
    {
        IRenderedComponent<DateTimePickerComponent> cut = TimePicker();
        ClickTitle(cut, time: true);

        if (Title(cut, time: true) != "PM")
            ClickTitle(cut, time: true);

        HourCells(cut).Single(cell => cell.TextContent.Trim() == "12").Click();
        Assert.Equal((12, 0, "PM"), HeaderTimeOf(cut));

        ClickNext(cut, time: true);

        Assert.Equal("1  PM", Title(cut, time: true));
    }
}
