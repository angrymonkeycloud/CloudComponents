using AngleSharp.Dom;
using AngryMonkey.CloudComponents;
using Bunit;
using DateTimePickerComponent = AngryMonkey.CloudComponents.DateTimePicker;

namespace CloudComponents.Tests.Core.DateTimePicker;

/// <summary>The date part of the picker: the day grid, the month and year grids and moving between them.</summary>
public class DateTimePickerCalendarTests : PickerTestBase
{
    private IRenderedComponent<DateTimePickerComponent> DatePicker() => RenderPicker(DateTimePickerMode.Date);

    private static IReadOnlyList<string> Texts(IEnumerable<IElement> cells) => [.. cells.Select(cell => cell.TextContent.Trim())];

    private static IElement CurrentMonthDay(IRenderedComponent<DateTimePickerComponent> cut, int day) =>
        DayCells(cut).Single(cell => cell.ClassList.Contains("_current") && cell.TextContent.Trim() == day.ToString());

    // ----- first render ------------------------------------------------------------------------------------------------------

    [Fact]
    public void The_day_view_opens_on_the_current_month_titled_with_month_and_year()
    {
        DateTime before = DateTime.Now;

        IRenderedComponent<DateTimePickerComponent> cut = DatePicker();

        string[] acceptable = [$"{Name(before.Month)} {before.Year}", $"{Name(DateTime.Now.Month)} {DateTime.Now.Year}"];
        Assert.Contains(Title(cut), acceptable);
    }

    [Fact]
    public void The_weekday_header_lists_Su_to_Sa_starting_on_sunday()
    {
        IRenderedComponent<DateTimePickerComponent> cut = DatePicker();

        Assert.Equal(["Su", "Mo", "Tu", "We", "Th", "Fr", "Sa"], Texts(cut.FindAll(".amc-datetimepicker-calendar-week")));
    }

    [Fact]
    public void The_day_grid_always_has_six_weeks_of_cells()
    {
        IRenderedComponent<DateTimePickerComponent> cut = DatePicker();

        Assert.Equal(42, DayCells(cut).Count);
    }

    [Fact]
    public void Todays_cell_is_both_selected_and_marked_today_when_the_picker_opens()
    {
        DateTime before = DateTime.Now.Date;

        IRenderedComponent<DateTimePickerComponent> cut = DatePicker();

        if (DateTime.Now.Date != before)
            return; // the date rolled over mid-test; nothing meaningful to compare

        IElement today = Assert.Single(cut.FindAll(".amc-datetimepicker-calendar-day._today"));
        Assert.Equal(before.Day.ToString(), today.TextContent.Trim());
        Assert.Contains("_selected", today.ClassList);
        Assert.Contains("_current", today.ClassList);
        Assert.Single(cut.FindAll(".amc-datetimepicker-calendar-day._selected"));
    }

    [Fact]
    public void The_today_marker_is_not_shown_in_a_month_that_does_not_contain_today()
    {
        IRenderedComponent<DateTimePickerComponent> cut = DatePicker();

        GoToMonth(cut, 2001, 5);

        Assert.Empty(cut.FindAll(".amc-datetimepicker-calendar-day._today"));
        Assert.Empty(cut.FindAll(".amc-datetimepicker-calendar-day._selected"));
    }

    // ----- grid layout per month ---------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(2024, 2, 29)] // leap year
    [InlineData(2023, 2, 28)]
    [InlineData(2000, 2, 29)] // divisible by 400
    [InlineData(1900, 2, 28)] // divisible by 100 but not 400
    [InlineData(2100, 2, 28)]
    [InlineData(2024, 1, 31)]
    [InlineData(2024, 4, 30)]
    [InlineData(2024, 12, 31)]
    public void A_month_shows_exactly_its_own_number_of_days_as_current(int year, int month, int days)
    {
        IRenderedComponent<DateTimePickerComponent> cut = DatePicker();

        GoToMonth(cut, year, month);

        IReadOnlyList<IElement> current = cut.FindAll(".amc-datetimepicker-calendar-day._current");
        Assert.Equal(days, current.Count);
        Assert.Equal(Enumerable.Range(1, days).Select(day => day.ToString()), Texts(current));
        Assert.Equal(42, DayCells(cut).Count);
    }

    [Theory]
    [InlineData(2024, 2)]
    [InlineData(2023, 2)]
    [InlineData(2024, 1)]
    [InlineData(2024, 3)]
    [InlineData(2023, 12)]
    [InlineData(2024, 12)]
    [InlineData(2021, 8)]
    [InlineData(1999, 1)]
    public void The_first_of_the_month_sits_in_the_column_of_its_weekday_with_the_neighbour_months_around_it(int year, int month)
    {
        IRenderedComponent<DateTimePickerComponent> cut = DatePicker();

        GoToMonth(cut, year, month);

        IReadOnlyList<IElement> cells = DayCells(cut);
        int firstColumn = (int)new DateTime(year, month, 1).DayOfWeek;
        int daysInMonth = DateTime.DaysInMonth(year, month);
        DateTime previous = new DateTime(year, month, 1).AddMonths(-1);
        int previousLength = DateTime.DaysInMonth(previous.Year, previous.Month);

        // Leading cells are the tail of the previous month, in order, and are not part of the current month.
        for (int index = 0; index < firstColumn; index++)
        {
            Assert.Equal((previousLength - firstColumn + 1 + index).ToString(), cells[index].TextContent.Trim());
            Assert.DoesNotContain("_current", cells[index].ClassList);
        }

        // The month itself starts exactly at its weekday column.
        Assert.Equal("1", cells[firstColumn].TextContent.Trim());
        Assert.Contains("_current", cells[firstColumn].ClassList);
        Assert.Equal(daysInMonth.ToString(), cells[firstColumn + daysInMonth - 1].TextContent.Trim());

        // Trailing cells count up from the first of the following month and are not current either.
        for (int index = firstColumn + daysInMonth; index < 42; index++)
        {
            Assert.Equal((index - firstColumn - daysInMonth + 1).ToString(), cells[index].TextContent.Trim());
            Assert.DoesNotContain("_current", cells[index].ClassList);
        }
    }

    [Fact]
    public void February_2024_is_led_by_the_last_four_days_of_january_and_trailed_by_march_first_to_ninth()
    {
        IRenderedComponent<DateTimePickerComponent> cut = DatePicker();

        GoToMonth(cut, 2024, 2);

        IReadOnlyList<string> texts = Texts(DayCells(cut));
        Assert.Equal(["28", "29", "30", "31", "1"], texts.Take(5));
        Assert.Equal(["1", "2", "3", "4", "5", "6", "7", "8", "9"], texts.Skip(33));
    }

    [Fact]
    public void A_month_that_starts_on_sunday_has_no_leading_days()
    {
        IRenderedComponent<DateTimePickerComponent> cut = DatePicker();

        GoToMonth(cut, 2024, 12); // 1 December 2024 is a Sunday

        IElement first = DayCells(cut)[0];
        Assert.Equal("1", first.TextContent.Trim());
        Assert.Contains("_current", first.ClassList);
    }

    [Fact]
    public void January_is_led_by_days_of_december_of_the_previous_year()
    {
        IRenderedComponent<DateTimePickerComponent> cut = DatePicker();

        GoToMonth(cut, 2024, 1); // 1 January 2024 is a Monday

        IReadOnlyList<IElement> cells = DayCells(cut);
        Assert.Equal("31", cells[0].TextContent.Trim());
        Assert.DoesNotContain("_current", cells[0].ClassList);
        Assert.Equal("1", cells[1].TextContent.Trim());
    }

    [Fact]
    public void December_is_trailed_by_days_of_january_of_the_next_year()
    {
        IRenderedComponent<DateTimePickerComponent> cut = DatePicker();

        GoToMonth(cut, 2023, 12); // 1 December 2023 is a Friday: 5 leading + 31 + 6 trailing

        IReadOnlyList<IElement> cells = DayCells(cut);
        Assert.Equal(["1", "2", "3", "4", "5", "6"], Texts(cells.Skip(36)));
        Assert.All(cells.Skip(36), cell => Assert.DoesNotContain("_current", cell.ClassList));
    }

    // ----- month stepping ----------------------------------------------------------------------------------------------------

    [Fact]
    public void The_next_arrow_moves_one_month_forward()
    {
        IRenderedComponent<DateTimePickerComponent> cut = DatePicker();
        GoToMonth(cut, 2024, 5);

        ClickNext(cut);

        Assert.Equal("June 2024", Title(cut));
    }

    [Fact]
    public void The_previous_arrow_moves_one_month_back()
    {
        IRenderedComponent<DateTimePickerComponent> cut = DatePicker();
        GoToMonth(cut, 2024, 5);

        ClickPrevious(cut);

        Assert.Equal("April 2024", Title(cut));
    }

    [Fact]
    public void The_next_arrow_from_december_goes_to_january_of_the_next_year()
    {
        IRenderedComponent<DateTimePickerComponent> cut = DatePicker();
        GoToMonth(cut, 2024, 12);

        ClickNext(cut);

        Assert.Equal("January 2025", Title(cut));
    }

    [Fact]
    public void The_previous_arrow_from_january_goes_to_december_of_the_previous_year()
    {
        IRenderedComponent<DateTimePickerComponent> cut = DatePicker();
        GoToMonth(cut, 2024, 1);

        ClickPrevious(cut);

        Assert.Equal("December 2023", Title(cut));
        Assert.Equal(31, cut.FindAll(".amc-datetimepicker-calendar-day._current").Count);
    }

    [Fact]
    public void Stepping_forward_twelve_times_lands_on_the_same_month_a_year_later_with_leap_days_kept()
    {
        IRenderedComponent<DateTimePickerComponent> cut = DatePicker();
        GoToMonth(cut, 2023, 2);

        for (int step = 0; step < 12; step++)
            ClickNext(cut);

        Assert.Equal("February 2024", Title(cut));
        Assert.Equal(29, cut.FindAll(".amc-datetimepicker-calendar-day._current").Count);
    }

    // ----- selecting a day ---------------------------------------------------------------------------------------------------

    [Fact]
    public void Clicking_a_day_selects_only_that_day()
    {
        IRenderedComponent<DateTimePickerComponent> cut = DatePicker();
        GoToMonth(cut, 2024, 2);

        CurrentMonthDay(cut, 29).Click();

        IElement selected = Assert.Single(cut.FindAll(".amc-datetimepicker-calendar-day._selected"));
        Assert.Equal("29", selected.TextContent.Trim());
        Assert.Contains("_current", selected.ClassList);
    }

    [Fact]
    public void Clicking_another_day_moves_the_selection()
    {
        IRenderedComponent<DateTimePickerComponent> cut = DatePicker();
        GoToMonth(cut, 2024, 2);
        CurrentMonthDay(cut, 29).Click();

        CurrentMonthDay(cut, 1).Click();

        IElement selected = Assert.Single(cut.FindAll(".amc-datetimepicker-calendar-day._selected"));
        Assert.Equal("1", selected.TextContent.Trim());
        Assert.Contains("_current", selected.ClassList);
    }

    [Fact]
    public void Clicking_the_selected_day_again_keeps_it_selected()
    {
        IRenderedComponent<DateTimePickerComponent> cut = DatePicker();
        GoToMonth(cut, 2024, 2);
        CurrentMonthDay(cut, 14).Click();

        CurrentMonthDay(cut, 14).Click();

        Assert.Single(cut.FindAll(".amc-datetimepicker-calendar-day._selected"));
    }

    [Fact]
    public void The_selected_day_is_still_selected_after_leaving_and_returning_to_its_month()
    {
        IRenderedComponent<DateTimePickerComponent> cut = DatePicker();
        GoToMonth(cut, 2024, 2);
        CurrentMonthDay(cut, 20).Click();

        ClickNext(cut);
        Assert.Empty(cut.FindAll(".amc-datetimepicker-calendar-day._selected"));
        ClickPrevious(cut);

        IElement selected = Assert.Single(cut.FindAll(".amc-datetimepicker-calendar-day._selected"));
        Assert.Equal("20", selected.TextContent.Trim());
    }

    [Fact]
    public void A_selected_day_that_spills_into_the_neighbour_month_grid_is_selected_there_too()
    {
        IRenderedComponent<DateTimePickerComponent> cut = DatePicker();
        GoToMonth(cut, 2024, 1);
        CurrentMonthDay(cut, 31).Click(); // 31 January 2024 is also the fourth leading cell of the February grid

        ClickNext(cut);

        Assert.Equal("February 2024", Title(cut));
        IElement selected = Assert.Single(cut.FindAll(".amc-datetimepicker-calendar-day._selected"));
        Assert.Equal("31", selected.TextContent.Trim());
        Assert.DoesNotContain("_current", selected.ClassList);
    }

    [Fact]
    public void Clicking_a_day_of_the_previous_month_selects_it_without_changing_the_shown_month()
    {
        using IDisposable culture = UseCulture("en-US");
        IRenderedComponent<DateTimePickerComponent> cut = DatePicker();
        GoToMonth(cut, 2024, 2);

        DayCells(cut)[3].Click(); // the leading "31" of January

        Assert.Equal("February 2024", Title(cut));
        Assert.StartsWith("January 31, 2024", Header(cut).Trim());
    }

    [Fact]
    public void Changing_the_selected_day_does_not_move_the_view()
    {
        IRenderedComponent<DateTimePickerComponent> cut = DatePicker();
        GoToMonth(cut, 2024, 2);

        CurrentMonthDay(cut, 3).Click();

        Assert.Equal("February 2024", Title(cut));
    }

    // ----- month grid --------------------------------------------------------------------------------------------------------

    [Fact]
    public void Clicking_the_title_opens_the_month_grid_titled_with_the_year()
    {
        IRenderedComponent<DateTimePickerComponent> cut = DatePicker();
        GoToMonth(cut, 2024, 7);

        ClickTitle(cut);

        Assert.Equal("2024", Title(cut));
        Assert.Empty(cut.FindAll(".amc-datetimepicker-calendar"));
        Assert.Equal(12, MonthCells(cut).Count);
    }

    [Fact]
    public void The_month_grid_lists_the_twelve_month_names_in_order()
    {
        IRenderedComponent<DateTimePickerComponent> cut = DatePicker();
        ClickTitle(cut);

        Assert.Equal(Enumerable.Range(1, 12).Select(Name), Texts(MonthCells(cut)));
        Assert.All(MonthCells(cut), cell => Assert.Contains("_current", cell.ClassList));
    }

    [Fact]
    public void The_month_grid_marks_the_selected_month_only_in_the_selected_year()
    {
        IRenderedComponent<DateTimePickerComponent> cut = DatePicker();
        GoToMonth(cut, 2024, 7);
        CurrentMonthDay(cut, 4).Click();

        ClickTitle(cut);
        IElement selected = Assert.Single(cut.FindAll(".amc-datetimepicker-months-month._selected"));
        Assert.Equal("July", selected.TextContent.Trim());

        ClickNext(cut);
        Assert.Equal("2025", Title(cut));
        Assert.Empty(cut.FindAll(".amc-datetimepicker-months-month._selected"));

        ClickPrevious(cut);
        ClickPrevious(cut);
        Assert.Equal("2023", Title(cut));
        Assert.Empty(cut.FindAll(".amc-datetimepicker-months-month._selected"));
    }

    [Fact]
    public void The_month_grid_marks_the_current_month_only_in_the_current_year()
    {
        DateTime now = DateTime.Now;
        IRenderedComponent<DateTimePickerComponent> cut = DatePicker();

        ClickTitle(cut);
        IElement today = Assert.Single(cut.FindAll(".amc-datetimepicker-months-month._today"));
        if (DateTime.Now.Month == now.Month)
            Assert.Equal(Name(now.Month), today.TextContent.Trim());

        ClickNext(cut);
        Assert.Empty(cut.FindAll(".amc-datetimepicker-months-month._today"));
    }

    [Fact]
    public void The_month_grid_arrows_step_a_year_at_a_time()
    {
        IRenderedComponent<DateTimePickerComponent> cut = DatePicker();
        GoToMonth(cut, 2024, 7);
        ClickTitle(cut);

        ClickNext(cut);
        ClickNext(cut);
        Assert.Equal("2026", Title(cut));

        for (int step = 0; step < 5; step++)
            ClickPrevious(cut);
        Assert.Equal("2021", Title(cut));
    }

    [Fact]
    public void Picking_a_month_returns_to_the_day_view_of_that_month_in_the_shown_year()
    {
        IRenderedComponent<DateTimePickerComponent> cut = DatePicker();
        GoToMonth(cut, 2024, 7);
        ClickTitle(cut);
        ClickNext(cut);

        MonthCells(cut)[8].Click(); // September

        Assert.Equal("September 2025", Title(cut));
        Assert.Equal(42, DayCells(cut).Count);
        Assert.Equal(30, cut.FindAll(".amc-datetimepicker-calendar-day._current").Count);
    }

    // ----- year grid ---------------------------------------------------------------------------------------------------------

    [Fact]
    public void Clicking_the_title_twice_opens_the_year_grid_titled_with_the_decade()
    {
        IRenderedComponent<DateTimePickerComponent> cut = DatePicker();
        GoToMonth(cut, 2024, 7);

        ClickTitle(cut);
        ClickTitle(cut);

        Assert.Equal("2020 - 2029", Title(cut));
        Assert.Empty(cut.FindAll(".amc-datetimepicker-months"));
    }

    [Fact]
    public void The_year_grid_shows_the_decade_flanked_by_one_year_of_each_neighbour_decade()
    {
        IRenderedComponent<DateTimePickerComponent> cut = DatePicker();
        GoToMonth(cut, 2024, 7);
        ClickTitle(cut);
        ClickTitle(cut);

        IReadOnlyList<IElement> years = YearCells(cut);

        Assert.Equal(12, years.Count);
        Assert.Equal(Enumerable.Range(2019, 12).Select(year => year.ToString()), Texts(years));
        Assert.DoesNotContain("_current", years[0].ClassList);
        Assert.DoesNotContain("_current", years[11].ClassList);
        Assert.All(years.Skip(1).Take(10), cell => Assert.Contains("_current", cell.ClassList));
    }

    [Fact]
    public void Clicking_the_title_in_the_year_grid_stays_in_the_year_grid()
    {
        IRenderedComponent<DateTimePickerComponent> cut = DatePicker();
        GoToMonth(cut, 2024, 7);
        ClickTitle(cut);
        ClickTitle(cut);

        ClickTitle(cut);

        Assert.Equal("2020 - 2029", Title(cut));
        Assert.Equal(12, YearCells(cut).Count);
    }

    [Fact]
    public void The_year_grid_marks_the_selected_year_and_current_year()
    {
        IRenderedComponent<DateTimePickerComponent> cut = DatePicker();
        GoToMonth(cut, 2024, 7);
        CurrentMonthDay(cut, 4).Click();
        ClickTitle(cut);
        ClickTitle(cut);

        IElement selected = Assert.Single(cut.FindAll(".amc-datetimepicker-years-year._selected"));
        Assert.Equal("2024", selected.TextContent.Trim());

        int thisYear = DateTime.Now.Year;
        IReadOnlyList<IElement> todays = cut.FindAll(".amc-datetimepicker-years-year._today");

        if (thisYear is >= 2020 and <= 2029)
            Assert.Equal(thisYear.ToString(), Assert.Single(todays).TextContent.Trim());
        else
            Assert.Empty(todays);
    }

    [Fact]
    public void The_year_grid_arrows_step_a_decade_at_a_time()
    {
        IRenderedComponent<DateTimePickerComponent> cut = DatePicker();
        GoToMonth(cut, 2024, 7);
        ClickTitle(cut);
        ClickTitle(cut);

        ClickNext(cut);
        Assert.Equal("2030 - 2039", Title(cut));

        ClickPrevious(cut);
        ClickPrevious(cut);
        Assert.Equal("2010 - 2019", Title(cut));
        Assert.Equal(Enumerable.Range(2009, 12).Select(year => year.ToString()), Texts(YearCells(cut)));
    }

    [Fact]
    public void Stepping_decades_snaps_the_year_to_the_start_of_the_decade_before_picking()
    {
        IRenderedComponent<DateTimePickerComponent> cut = DatePicker();
        GoToMonth(cut, 2029, 7);
        ClickTitle(cut);
        ClickTitle(cut);

        ClickNext(cut);
        YearCells(cut)[1].Click(); // 2030

        Assert.Equal("2030", Title(cut));
    }

    [Fact]
    public void Picking_a_year_opens_the_month_grid_of_that_year()
    {
        IRenderedComponent<DateTimePickerComponent> cut = DatePicker();
        GoToMonth(cut, 2024, 7);
        ClickTitle(cut);
        ClickTitle(cut);

        YearCells(cut).Single(cell => cell.TextContent.Trim() == "2027").Click();

        Assert.Equal("2027", Title(cut));
        Assert.Equal(12, MonthCells(cut).Count);
    }

    [Fact]
    public void Picking_a_year_from_a_neighbour_decade_cell_opens_that_year()
    {
        IRenderedComponent<DateTimePickerComponent> cut = DatePicker();
        GoToMonth(cut, 2024, 7);
        ClickTitle(cut);
        ClickTitle(cut);

        YearCells(cut)[0].Click(); // 2019, shown greyed out before the decade

        Assert.Equal("2019", Title(cut));
    }

    [Fact]
    public void Walking_year_to_month_to_day_ends_on_the_chosen_month_and_year()
    {
        IRenderedComponent<DateTimePickerComponent> cut = DatePicker();

        GoToMonth(cut, 1999, 12);

        Assert.Equal("December 1999", Title(cut));
        Assert.Equal(31, cut.FindAll(".amc-datetimepicker-calendar-day._current").Count);
    }
}
