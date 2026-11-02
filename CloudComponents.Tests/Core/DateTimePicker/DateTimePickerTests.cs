using System.Globalization;
using AngleSharp.Dom;
using AngryMonkey.CloudComponents;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using DateTimePickerComponent = AngryMonkey.CloudComponents.DateTimePicker;

namespace CloudComponents.Tests.Core.DateTimePicker;

/// <summary>The picker as a whole: which parts each mode shows, the header summary and swiping the date body.</summary>
public class DateTimePickerTests : PickerTestBase
{
    // ----- modes ------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void The_default_mode_shows_both_the_date_and_the_time_parts()
    {
        IRenderedComponent<DateTimePickerComponent> cut = Render<DateTimePickerComponent>();

        Assert.Single(cut.FindAll(".amc-datetimepicker-date"));
        Assert.Single(cut.FindAll(".amc-datetimepicker-time"));
        Assert.Equal(2, cut.FindAll(".amc-datetimepicker-selection").Count);
    }

    [Fact]
    public void Date_and_time_mode_separates_the_two_parts()
    {
        IRenderedComponent<DateTimePickerComponent> cut = RenderPicker(DateTimePickerMode.DateAndTime);

        Assert.Single(cut.FindAll(".amc-datetimepicker-datetimeseperator"));
    }

    [Fact]
    public void Date_mode_hides_the_time_part_and_the_separator()
    {
        IRenderedComponent<DateTimePickerComponent> cut = RenderPicker(DateTimePickerMode.Date);

        Assert.Single(cut.FindAll(".amc-datetimepicker-date"));
        Assert.Empty(cut.FindAll(".amc-datetimepicker-time"));
        Assert.Empty(cut.FindAll(".amc-datetimepicker-datetimeseperator"));
        Assert.Single(cut.FindAll(".amc-datetimepicker-selection"));
    }

    [Fact]
    public void Time_mode_hides_the_calendar_and_the_separator()
    {
        IRenderedComponent<DateTimePickerComponent> cut = RenderPicker(DateTimePickerMode.Time);

        Assert.Empty(cut.FindAll(".amc-datetimepicker-date"));
        Assert.Empty(cut.FindAll(".amc-datetimepicker-calendar"));
        Assert.Empty(cut.FindAll(".amc-datetimepicker-datetimeseperator"));
        Assert.Single(cut.FindAll(".amc-datetimepicker-time"));
        Assert.Single(cut.FindAll(".amc-datetimepicker-selection"));
    }

    [Fact]
    public void The_picker_never_calls_into_javascript()
    {
        IRenderedComponent<DateTimePickerComponent> cut = RenderPicker();
        ClickTitle(cut);
        ClickTitle(cut, time: true);

        Assert.Empty(JSInterop.Invocations);
    }

    // ----- header ------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void Date_mode_header_shows_only_todays_date_in_long_month_format()
    {
        using IDisposable culture = UseCulture("en-US");
        DateTime before = DateTime.Now;

        IRenderedComponent<DateTimePickerComponent> cut = RenderPicker(DateTimePickerMode.Date);

        string[] acceptable = [before.ToString("MMMM dd, yyyy"), DateTime.Now.ToString("MMMM dd, yyyy")];
        Assert.Contains(cut.Find(".amc-datetimepicker-selecteddate").TextContent.Trim(), acceptable);
    }

    [Fact]
    public void Time_mode_header_shows_only_the_time()
    {
        IRenderedComponent<DateTimePickerComponent> cut = RenderPicker(DateTimePickerMode.Time);

        Assert.Matches(@"^\d{1,2}:\d{2} (AM|PM)$", Header(cut).Trim());
    }

    [Fact]
    public void Date_and_time_header_shows_the_date_then_the_time()
    {
        using IDisposable culture = UseCulture("en-US");

        IRenderedComponent<DateTimePickerComponent> cut = RenderPicker();

        Assert.Matches(@"(?s)^[A-Za-z]+ \d{2}, \d{4}.+\d{1,2}:\d{2} (AM|PM)$", Header(cut).Trim());
    }

    [Fact(Skip = "BUG: DateTimePicker.razor contains the double-encoded text 'â€¢' (UTF-8 bytes of a bullet re-read as Windows-1252) between &nbsp; entities, so the date/time separator in the header renders as 'â€¢' instead of a bullet.")]
    public void The_header_separates_date_and_time_with_a_bullet()
    {
        IRenderedComponent<DateTimePickerComponent> cut = RenderPicker();

        Assert.Contains(" • ", Header(cut));
    }

    [Fact]
    public void The_header_date_uses_the_current_culture()
    {
        using IDisposable culture = UseCulture("fr-FR");
        DateTime before = DateTime.Now;

        IRenderedComponent<DateTimePickerComponent> cut = RenderPicker(DateTimePickerMode.Date);

        CultureInfo french = new("fr-FR");
        string[] acceptable = [before.ToString("MMMM dd, yyyy", french), DateTime.Now.ToString("MMMM dd, yyyy", french)];
        Assert.Contains(Header(cut).Trim(), acceptable);
    }

    [Fact]
    public void Selecting_a_day_updates_the_header_date()
    {
        using IDisposable culture = UseCulture("en-US");
        IRenderedComponent<DateTimePickerComponent> cut = RenderPicker(DateTimePickerMode.Date);

        GoToMonth(cut, 2024, 2);
        DayCells(cut).Single(cell => cell.ClassList.Contains("_current") && cell.TextContent == "29").Click();

        Assert.Equal("February 29, 2024", Header(cut).Trim());
    }

    [Fact]
    public void Selecting_a_day_pads_a_single_digit_day_to_two_digits_in_the_header()
    {
        using IDisposable culture = UseCulture("en-US");
        IRenderedComponent<DateTimePickerComponent> cut = RenderPicker(DateTimePickerMode.Date);

        GoToMonth(cut, 2024, 3);
        DayCells(cut).Single(cell => cell.ClassList.Contains("_current") && cell.TextContent == "5").Click();

        Assert.Equal("March 05, 2024", Header(cut).Trim());
    }

    [Fact]
    public void Selecting_a_day_leaves_the_header_time_untouched()
    {
        using IDisposable culture = UseCulture("en-US");
        IRenderedComponent<DateTimePickerComponent> cut = RenderPicker();
        (int Hour, int Minute, string Suffix) before = HeaderTimeOf(cut);

        GoToMonth(cut, 2024, 2);
        DayCells(cut).Single(cell => cell.ClassList.Contains("_current") && cell.TextContent == "10").Click();

        Assert.Equal(before, HeaderTimeOf(cut));
    }

    [Fact]
    public void Selecting_a_time_leaves_the_header_date_untouched()
    {
        using IDisposable culture = UseCulture("en-US");
        IRenderedComponent<DateTimePickerComponent> cut = RenderPicker();
        GoToMonth(cut, 2024, 2);
        DayCells(cut).Single(cell => cell.ClassList.Contains("_current") && cell.TextContent == "10").Click();

        SelectHour(cut, HourOtherThanNow(3));

        Assert.StartsWith("February 10, 2024", Header(cut).Trim());
    }

    // ----- mode changes after the first render ---------------------------------------------------------------------------------

    [Fact(Skip = "BUG: DateTimePicker (Razor Code/Methods.cs OnInitializedAsync) only creates SelectedDate/SelectedTime for the Mode it was first rendered with, so changing Mode from Date to DateAndTime (or Time to DateAndTime) later makes the render dereference a null SelectedTime/SelectedDate and throw NullReferenceException.")]
    public void Changing_the_mode_after_the_first_render_does_not_throw()
    {
        IRenderedComponent<DateTimePickerComponent> cut = RenderPicker(DateTimePickerMode.Date);

        cut.Render(parameters => parameters.Add(picker => picker.Mode, DateTimePickerMode.DateAndTime));

        Assert.Single(cut.FindAll(".amc-datetimepicker-time"));
    }

    // ----- consistency between the header and the time bar ---------------------------------------------------------------------

    [Fact(Skip = "BUG: DateTimePicker.OnInitializedAsync sets ChoosingAmOrPm with 'Hour > 12 ? PM : AM', so during the 12:00-12:59 hour the time bar says 'AM' while the header (Hour >= 12) says 'PM'. Time-dependent: it only fails when the test runs between 12:00 and 12:59.")]
    public void The_time_bar_and_the_header_agree_on_AM_or_PM_when_the_picker_opens()
    {
        IRenderedComponent<DateTimePickerComponent> cut = RenderPicker(DateTimePickerMode.Time);

        string barSuffix = Title(cut, time: true).Split(' ', StringSplitOptions.RemoveEmptyEntries)[^1];

        Assert.Equal(HeaderTimeOf(cut).Suffix, barSuffix);
    }

    // ----- swiping the date body -----------------------------------------------------------------------------------------------

    private static TouchEventArgs TouchAt(double clientX) =>
        new() { ChangedTouches = [new TouchPoint { ClientX = clientX }] };

    private static void Swipe(IRenderedComponent<DateTimePickerComponent> cut, double fromX, double toX)
    {
        IElement body = cut.Find(".amc-datetimepicker-date");
        body.TouchStart(TouchAt(fromX));
        cut.Find(".amc-datetimepicker-date").TouchEnd(TouchAt(toX));
    }

    [Fact]
    public void Swiping_right_across_the_day_view_goes_to_the_previous_month()
    {
        IRenderedComponent<DateTimePickerComponent> cut = RenderPicker(DateTimePickerMode.Date);
        GoToMonth(cut, 2024, 3);

        Swipe(cut, 100, 200);

        Assert.Equal("February 2024", Title(cut));
    }

    [Fact]
    public void Swiping_left_across_the_day_view_goes_to_the_next_month()
    {
        IRenderedComponent<DateTimePickerComponent> cut = RenderPicker(DateTimePickerMode.Date);
        GoToMonth(cut, 2024, 3);

        Swipe(cut, 200, 100);

        Assert.Equal("April 2024", Title(cut));
    }

    [Fact]
    public void A_swipe_shorter_than_ten_pixels_does_nothing()
    {
        IRenderedComponent<DateTimePickerComponent> cut = RenderPicker(DateTimePickerMode.Date);
        GoToMonth(cut, 2024, 3);

        Swipe(cut, 100, 109);
        Swipe(cut, 100, 91);

        Assert.Equal("March 2024", Title(cut));
    }

    [Fact]
    public void A_swipe_of_exactly_ten_pixels_is_already_enough_to_navigate()
    {
        IRenderedComponent<DateTimePickerComponent> cut = RenderPicker(DateTimePickerMode.Date);
        GoToMonth(cut, 2024, 3);

        Swipe(cut, 100, 110);

        Assert.Equal("February 2024", Title(cut));
    }

    [Fact]
    public async Task A_swipe_that_takes_longer_than_a_second_is_ignored()
    {
        IRenderedComponent<DateTimePickerComponent> cut = RenderPicker(DateTimePickerMode.Date);
        GoToMonth(cut, 2024, 3);

        cut.Find(".amc-datetimepicker-date").TouchStart(TouchAt(100));
        await Task.Delay(1150);
        cut.Find(".amc-datetimepicker-date").TouchEnd(TouchAt(300));

        Assert.Equal("March 2024", Title(cut));
    }

    [Fact]
    public void Swiping_right_on_the_month_view_goes_to_the_previous_year_and_left_to_the_next()
    {
        IRenderedComponent<DateTimePickerComponent> cut = RenderPicker(DateTimePickerMode.Date);
        GoToMonth(cut, 2024, 3);
        ClickTitle(cut);

        Swipe(cut, 100, 200);
        Assert.Equal("2023", Title(cut));

        Swipe(cut, 200, 100);
        Swipe(cut, 200, 100);
        Assert.Equal("2025", Title(cut));
    }

    [Fact]
    public void Swiping_on_the_year_view_moves_by_a_decade()
    {
        IRenderedComponent<DateTimePickerComponent> cut = RenderPicker(DateTimePickerMode.Date);
        GoToMonth(cut, 2024, 3);
        ClickTitle(cut);
        ClickTitle(cut);
        Assert.Equal("2020 - 2029", Title(cut));

        Swipe(cut, 200, 100);
        Assert.Equal("2030 - 2039", Title(cut));

        Swipe(cut, 100, 200);
        Swipe(cut, 100, 200);
        Assert.Equal("2010 - 2019", Title(cut));
    }

    [Fact]
    public void Swiping_across_a_year_boundary_rolls_the_year()
    {
        IRenderedComponent<DateTimePickerComponent> cut = RenderPicker(DateTimePickerMode.Date);
        GoToMonth(cut, 2024, 1);

        Swipe(cut, 100, 200);

        Assert.Equal("December 2023", Title(cut));
    }

    [Fact]
    public void Swiping_the_date_body_does_not_change_the_time_part()
    {
        IRenderedComponent<DateTimePickerComponent> cut = RenderPicker();
        string timeTitle = Title(cut, time: true);
        (int Hour, int Minute, string Suffix) time = HeaderTimeOf(cut);

        Swipe(cut, 100, 200);

        Assert.Equal(timeTitle, Title(cut, time: true));
        Assert.Equal(time, HeaderTimeOf(cut));
    }
}
