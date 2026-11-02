using AngryMonkey.CloudComponents;
using DateTimePickerComponent = AngryMonkey.CloudComponents.DateTimePicker;

namespace CloudComponents.Tests.Core.DateTimePicker;

/// <summary>
/// The picker's cell models are protected nested classes of <see cref="DateTimePickerComponent"/>; this subclass is the only
/// way in, and exposes their public members as plain values.
/// </summary>
internal sealed class DateTimePickerProbe : DateTimePickerComponent
{
    public static string HourDisplay(int hour) => new DateTimePickerHour(hour).DisplayHour;

    public static bool HourIsNow(int hour) => new DateTimePickerHour(hour).IsNow;

    public static string HourClasses(int hour, bool selected) => new DateTimePickerHour(hour) { IsSelected = selected }.CssClasses;

    public static string TimeDisplay(int hour, int minute) => new DateTimePickerTime(hour, minute).TimeDisplay();

    public static bool TimeIsPrimary(int minute) => new DateTimePickerTime(0, minute).IsPrimary;

    public static bool TimeIsCurrent(int minute) => new DateTimePickerTime(0, minute).IsCurrent;

    public static string TimeDisplayMinute(int minute) => new DateTimePickerTime(0, minute).DisplayMinute;

    public static bool TimeIsNow(int hour, int minute) => new DateTimePickerTime(hour, minute).IsNow;

    public static (int Hour, int Minute) TimeFrom(DateTime date)
    {
        DateTimePickerTime time = new(date);
        return (time.Hour, time.Minute);
    }

    public static string TimeClasses(int hour, int minute, bool selected) => new DateTimePickerTime(hour, minute) { IsSelected = selected }.CssClasses;

    public static string TimeClassesWithCurrentOverride(int hour, int minute, bool current) => new DateTimePickerTime(hour, minute) { IsCurrent = current }.CssClasses;

    public static DateTime DateToDateTime(int year, int month, int day) => new DateTimePickerDate(year, month, day).ToDateTime();

    public static (int Year, int Month, int Day) DateFrom(DateTime date)
    {
        DateTimePickerDate picked = new(date);
        return (picked.Year, picked.Month, picked.Day);
    }

    public static bool DateIsToday(int year, int month, int day) => new DateTimePickerDate(year, month, day).IsToday;

    public static string DateClasses(int year, int month, int day, bool selected, bool currentMonth) =>
        new DateTimePickerDate(year, month, day) { IsSelected = selected, CurrentMonth = currentMonth }.CssClasses;

    public static bool DateDefaultsToCurrentMonth() => new DateTimePickerDate(2024, 1, 1).CurrentMonth;

    public static string MonthName(int month) => new DateTimePickerMonth(2024, month).Name;

    public static string MonthClasses(bool selected, bool today) => new DateTimePickerMonth(2024, 1) { IsSelected = selected, IsToday = today }.CssClasses;

    public static IReadOnlyList<string> MonthNames => DateTimePickerMonth.Names;

    public static string YearClasses(bool decade, bool selected, bool today) =>
        new DateTimePickerYear(2024) { IsCurrentDecade = decade, IsSelected = selected, IsToday = today }.CssClasses;
}

/// <summary>The value objects behind the picker's day, month, year, hour and minute cells.</summary>
public class DateTimePickerModelTests
{
    // ----- hours ------------------------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(0, "12")]
    [InlineData(1, "1")]
    [InlineData(9, "9")]
    [InlineData(11, "11")]
    [InlineData(12, "12")]
    [InlineData(13, "1")]
    [InlineData(18, "6")]
    [InlineData(23, "11")]
    public void An_hour_is_displayed_on_a_twelve_hour_clock(int hour, string expected)
    {
        Assert.Equal(expected, DateTimePickerProbe.HourDisplay(hour));
    }

    [Fact]
    public void The_current_hour_is_flagged_as_now_and_the_next_hour_is_not()
    {
        DateTime before = DateTime.Now;
        bool nowFlag = DateTimePickerProbe.HourIsNow(before.Hour);
        bool otherFlag = DateTimePickerProbe.HourIsNow((before.Hour + 1) % 24);

        // An hour boundary crossed between the two reads would make this comparison meaningless, so only judge it when it was not.
        if (DateTime.Now.Hour == before.Hour)
        {
            Assert.True(nowFlag);
            Assert.False(otherFlag);
        }
    }

    [Fact]
    public void A_plain_hour_cell_is_only_current()
    {
        int hourOtherThanNow = (DateTime.Now.Hour + 5) % 24;

        Assert.Equal("_current", DateTimePickerProbe.HourClasses(hourOtherThanNow, selected: false));
    }

    [Fact]
    public void A_selected_hour_cell_is_current_and_selected_in_that_order()
    {
        int hourOtherThanNow = (DateTime.Now.Hour + 5) % 24;

        Assert.Equal("_current _selected", DateTimePickerProbe.HourClasses(hourOtherThanNow, selected: true));
    }

    [Fact]
    public void The_hour_cell_for_the_current_hour_carries_the_now_class_last()
    {
        DateTime before = DateTime.Now;
        string classes = DateTimePickerProbe.HourClasses(before.Hour, selected: true);

        if (DateTime.Now.Hour == before.Hour)
            Assert.Equal("_current _selected _now", classes);
    }

    // ----- minutes and times -----------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(0, true)]
    [InlineData(5, true)]
    [InlineData(55, true)]
    [InlineData(1, false)]
    [InlineData(4, false)]
    [InlineData(59, false)]
    public void Only_multiples_of_five_minutes_are_primary(int minute, bool expected)
    {
        Assert.Equal(expected, DateTimePickerProbe.TimeIsPrimary(minute));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(35, true)]
    [InlineData(7, false)]
    public void A_minute_cell_is_current_exactly_when_it_is_primary(int minute, bool expected)
    {
        Assert.Equal(expected, DateTimePickerProbe.TimeIsCurrent(minute));
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(7, "7")]
    [InlineData(59, "59")]
    public void The_minute_label_is_not_zero_padded(int minute, string expected)
    {
        Assert.Equal(expected, DateTimePickerProbe.TimeDisplayMinute(minute));
    }

    [Theory]
    [InlineData(0, 0, "12:00 AM")]
    [InlineData(0, 30, "12:30 AM")]
    [InlineData(10, 15, "10:15 AM")]
    [InlineData(11, 59, "11:59 AM")]
    [InlineData(12, 0, "12:00 PM")]
    [InlineData(12, 45, "12:45 PM")]
    [InlineData(22, 5, "10:05 PM")]
    [InlineData(23, 59, "11:59 PM")]
    public void A_time_is_displayed_with_a_twelve_hour_clock_and_an_AM_PM_suffix(int hour, int minute, string expected)
    {
        Assert.Equal(expected, DateTimePickerProbe.TimeDisplay(hour, minute));
    }

    [Theory(Skip = "BUG: DateTimePickerTime.TimeDisplay (Classes/DateTimePickerTime.cs) formats the hour with {hour.DisplayHour:00}, but DisplayHour is a string so the '00' format is ignored: 3:05 AM is shown as '3:05 AM' instead of the zero-padded '03:05 AM' the format string asks for.")]
    [InlineData(3, 5, "03:05 AM")]
    [InlineData(13, 7, "01:07 PM")]
    public void A_single_digit_hour_is_zero_padded_as_the_format_string_intends(int hour, int minute, string expected)
    {
        Assert.Equal(expected, DateTimePickerProbe.TimeDisplay(hour, minute));
    }

    [Fact]
    public void A_time_built_from_a_date_takes_its_hour_and_minute()
    {
        Assert.Equal((17, 42), DateTimePickerProbe.TimeFrom(new DateTime(2024, 2, 29, 17, 42, 59)));
    }

    [Fact]
    public void A_time_built_from_midnight_is_hour_zero_minute_zero()
    {
        Assert.Equal((0, 0), DateTimePickerProbe.TimeFrom(new DateTime(2024, 2, 29)));
    }

    [Fact]
    public void A_time_cell_for_an_ordinary_minute_has_only_its_minute_class()
    {
        // Pick an hour that is not the current hour so the _now class cannot apply.
        int hour = (DateTime.Now.Hour + 5) % 24;

        Assert.Equal("_7", DateTimePickerProbe.TimeClasses(hour, 7, selected: false));
    }

    [Fact]
    public void A_time_cell_for_a_five_minute_mark_is_current_and_primary()
    {
        int hour = (DateTime.Now.Hour + 5) % 24;

        Assert.Equal("_30 _current _primary", DateTimePickerProbe.TimeClasses(hour, 30, selected: false));
    }

    [Fact]
    public void A_selected_time_cell_has_the_selected_class_after_its_minute_class()
    {
        int hour = (DateTime.Now.Hour + 5) % 24;

        Assert.Equal("_30 _selected _current _primary", DateTimePickerProbe.TimeClasses(hour, 30, selected: true));
    }

    [Fact]
    public void The_current_flag_of_a_minute_cell_can_be_overridden_by_the_caller()
    {
        int hour = (DateTime.Now.Hour + 5) % 24;

        Assert.Equal("_7 _current", DateTimePickerProbe.TimeClassesWithCurrentOverride(hour, 7, current: true));
    }

    [Fact]
    public void A_time_cell_for_the_present_minute_carries_the_now_class()
    {
        DateTime before = DateTime.Now;
        bool isNow = DateTimePickerProbe.TimeIsNow(before.Hour, before.Minute);

        if (DateTime.Now.Minute == before.Minute)
            Assert.True(isNow);
    }

    [Fact]
    public void A_time_cell_for_another_minute_is_not_now()
    {
        DateTime before = DateTime.Now;
        bool isNow = DateTimePickerProbe.TimeIsNow(before.Hour, (before.Minute + 2) % 60);

        if (DateTime.Now.Minute == before.Minute)
            Assert.False(isNow);
    }

    // ----- days -------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void A_date_converts_back_to_the_same_calendar_day()
    {
        Assert.Equal(new DateTime(2024, 2, 29), DateTimePickerProbe.DateToDateTime(2024, 2, 29));
    }

    [Fact]
    public void The_thirtieth_of_february_is_not_a_date()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DateTimePickerProbe.DateToDateTime(2024, 2, 30));
    }

    [Fact]
    public void The_twenty_ninth_of_february_in_a_non_leap_year_is_not_a_date()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DateTimePickerProbe.DateToDateTime(2023, 2, 29));
    }

    [Fact]
    public void A_date_built_from_a_date_time_drops_the_time_of_day()
    {
        Assert.Equal((2024, 12, 31), DateTimePickerProbe.DateFrom(new DateTime(2024, 12, 31, 23, 59, 59)));
    }

    [Fact]
    public void Todays_date_is_flagged_as_today_and_tomorrows_is_not()
    {
        DateTime before = DateTime.Now.Date;
        DateTime tomorrow = before.AddDays(1);

        bool today = DateTimePickerProbe.DateIsToday(before.Year, before.Month, before.Day);
        bool other = DateTimePickerProbe.DateIsToday(tomorrow.Year, tomorrow.Month, tomorrow.Day);

        if (DateTime.Now.Date == before)
        {
            Assert.True(today);
            Assert.False(other);
        }
    }

    [Fact]
    public void A_date_defaults_to_being_in_the_current_month_and_unselected()
    {
        Assert.True(DateTimePickerProbe.DateDefaultsToCurrentMonth());
        Assert.DoesNotContain("_selected", DateTimePickerProbe.DateClasses(2000, 1, 1, selected: false, currentMonth: true));
    }

    [Theory]
    [InlineData(false, false, "")]
    [InlineData(true, false, "_selected")]
    [InlineData(false, true, "_current")]
    [InlineData(true, true, "_selected _current")]
    public void A_date_cell_lists_its_selected_and_current_month_classes(bool selected, bool currentMonth, string expected)
    {
        // 2000-01-01 is far from today, so the _today class never mixes in.
        Assert.Equal(expected, DateTimePickerProbe.DateClasses(2000, 1, 1, selected, currentMonth));
    }

    [Fact]
    public void Todays_date_cell_has_the_today_class_between_selected_and_current()
    {
        DateTime before = DateTime.Now.Date;
        string classes = DateTimePickerProbe.DateClasses(before.Year, before.Month, before.Day, selected: true, currentMonth: true);

        if (DateTime.Now.Date == before)
            Assert.Equal("_selected _today _current", classes);
    }

    // ----- months and years -------------------------------------------------------------------------------------------------------

    [Fact]
    public void The_month_names_are_the_twelve_english_month_names_in_calendar_order()
    {
        Assert.Equal(
            ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"],
            DateTimePickerProbe.MonthNames);
    }

    [Theory]
    [InlineData(1, "January")]
    [InlineData(6, "June")]
    [InlineData(12, "December")]
    public void A_month_cell_is_named_after_its_month_number(int month, string expected)
    {
        Assert.Equal(expected, DateTimePickerProbe.MonthName(month));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void A_month_number_outside_one_to_twelve_has_no_name(int month)
    {
        Assert.Throws<IndexOutOfRangeException>(() => DateTimePickerProbe.MonthName(month));
    }

    [Theory]
    [InlineData(false, false, "_current")]
    [InlineData(true, false, "_current _selected")]
    [InlineData(false, true, "_current _today")]
    [InlineData(true, true, "_current _selected _today")]
    public void A_month_cell_is_always_current_and_adds_selected_and_today(bool selected, bool today, string expected)
    {
        Assert.Equal(expected, DateTimePickerProbe.MonthClasses(selected, today));
    }

    [Theory]
    [InlineData(false, false, false, "")]
    [InlineData(true, false, false, "_current")]
    [InlineData(true, true, false, "_current _selected")]
    [InlineData(true, false, true, "_current _today")]
    [InlineData(true, true, true, "_current _selected _today")]
    [InlineData(false, true, true, "_selected _today")]
    public void A_year_cell_is_current_only_inside_the_shown_decade(bool decade, bool selected, bool today, string expected)
    {
        Assert.Equal(expected, DateTimePickerProbe.YearClasses(decade, selected, today));
    }

    [Fact]
    public void The_component_defaults_to_date_and_time_mode()
    {
        Assert.Equal(DateTimePickerMode.DateAndTime, new DateTimePickerComponent().Mode);
    }
}
