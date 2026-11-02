using System.Globalization;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngryMonkey.CloudComponents;
using Bunit;
using DateTimePickerComponent = AngryMonkey.CloudComponents.DateTimePicker;

namespace CloudComponents.Tests.Core.DateTimePicker;

/// <summary>Shared rendering and navigation helpers for the picker tests; everything goes through the rendered markup, as a user would.</summary>
public abstract partial class PickerTestBase : BunitContext
{
    protected IRenderedComponent<DateTimePickerComponent> RenderPicker(DateTimePickerMode mode = DateTimePickerMode.DateAndTime) =>
        Render<DateTimePickerComponent>(parameters => parameters.Add(picker => picker.Mode, mode));

    /// <summary>Runs the rest of the test with the given culture as the current one.</summary>
    protected static IDisposable UseCulture(string name) => new CultureScope(new CultureInfo(name));

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _original = CultureInfo.CurrentCulture;

        public CultureScope(CultureInfo culture) => CultureInfo.CurrentCulture = culture;

        public void Dispose() => CultureInfo.CurrentCulture = _original;
    }

    // ----- navigation bars (date bar first, time bar last) ---------------------------------------------------------------------

    private static IElement Info(IRenderedComponent<DateTimePickerComponent> cut, bool time)
    {
        IReadOnlyList<IElement> infos = cut.FindAll(".amc-datetimepicker-selection-info");
        return time ? infos[^1] : infos[0];
    }

    private static IElement Bar(IRenderedComponent<DateTimePickerComponent> cut, bool time)
    {
        IReadOnlyList<IElement> bars = cut.FindAll(".amc-datetimepicker-selection");
        return time ? bars[^1] : bars[0];
    }

    protected static string Title(IRenderedComponent<DateTimePickerComponent> cut, bool time = false) => Info(cut, time).TextContent.Trim();

    protected static void ClickTitle(IRenderedComponent<DateTimePickerComponent> cut, bool time = false) => Info(cut, time).Click();

    protected static void ClickPrevious(IRenderedComponent<DateTimePickerComponent> cut, bool time = false) =>
        Bar(cut, time).QuerySelector(".amc-datetimepicker-selection-previous")!.Click();

    protected static void ClickNext(IRenderedComponent<DateTimePickerComponent> cut, bool time = false) =>
        Bar(cut, time).QuerySelector(".amc-datetimepicker-selection-next")!.Click();

    // ----- date views ---------------------------------------------------------------------------------------------------------

    protected static IReadOnlyList<IElement> DayCells(IRenderedComponent<DateTimePickerComponent> cut) =>
        cut.FindAll(".amc-datetimepicker-calendar-day");

    protected static IReadOnlyList<IElement> MonthCells(IRenderedComponent<DateTimePickerComponent> cut) =>
        cut.FindAll(".amc-datetimepicker-months-month");

    protected static IReadOnlyList<IElement> YearCells(IRenderedComponent<DateTimePickerComponent> cut) =>
        cut.FindAll(".amc-datetimepicker-years-year");

    protected static string Name(int month) => CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month);

    /// <summary>From the day view, opens the year grid, walks to the decade of <paramref name="year"/> and picks that year and month.</summary>
    protected static void GoToMonth(IRenderedComponent<DateTimePickerComponent> cut, int year, int month)
    {
        ClickTitle(cut);
        ClickTitle(cut);

        while (true)
        {
            int decadeStart = int.Parse(Title(cut).Split(' ')[0], CultureInfo.InvariantCulture);

            if (year < decadeStart)
                ClickPrevious(cut);
            else if (year > decadeStart + 9)
                ClickNext(cut);
            else
                break;
        }

        cut.FindAll(".amc-datetimepicker-years-year._current")
            .Single(cell => cell.TextContent.Trim() == year.ToString(CultureInfo.InvariantCulture))
            .Click();

        MonthCells(cut)[month - 1].Click();
    }

    // ----- header and time ----------------------------------------------------------------------------------------------------

    protected static string Header(IRenderedComponent<DateTimePickerComponent> cut) =>
        cut.Find(".amc-datetimepicker-selecteddate").TextContent;

    private static readonly Regex HeaderTime = HeaderTimePattern();

    [GeneratedRegex(@"(\d{1,2}):(\d{2}) (AM|PM)")]
    private static partial Regex HeaderTimePattern();

    /// <summary>The time shown in the header, as hour on the twelve-hour clock, minute and AM/PM.</summary>
    protected static (int Hour, int Minute, string Suffix) HeaderTimeOf(IRenderedComponent<DateTimePickerComponent> cut)
    {
        Match match = HeaderTime.Match(Header(cut));
        Assert.True(match.Success, $"No time found in the header text '{Header(cut)}'.");

        return (int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture), int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture), match.Groups[3].Value);
    }

    protected static IReadOnlyList<IElement> MinuteCells(IRenderedComponent<DateTimePickerComponent> cut) =>
        cut.FindAll(".amc-datetimepicker-minutes-minute");

    protected static IReadOnlyList<IElement> HourCells(IRenderedComponent<DateTimePickerComponent> cut) =>
        cut.FindAll(".amc-datetimepicker-hours-hour");

    /// <summary>
    /// Picks a 24-hour clock hour through the hour grid (1..11 in the AM list, 13..23 in the PM list) and leaves the picker on that
    /// hour's minute grid. Hours 0 and 12 cannot be reached the way a user would and are not supported here.
    /// </summary>
    protected static void SelectHour(IRenderedComponent<DateTimePickerComponent> cut, int hour24)
    {
        Assert.True(hour24 is (>= 1 and <= 11) or (>= 13 and <= 23), "Use an hour from 1 to 11 or 13 to 23.");

        if (HourCells(cut).Count == 0)
            ClickTitle(cut, time: true);

        string wanted = hour24 < 12 ? "AM" : "PM";

        if (Title(cut, time: true) != wanted)
            ClickTitle(cut, time: true);

        string label = (hour24 % 12).ToString(CultureInfo.InvariantCulture);
        HourCells(cut).Single(cell => cell.TextContent.Trim() == label).Click();
    }

    /// <summary>An hour from 1..11 that is never the current hour, so selecting it always replaces the selected time.</summary>
    protected static int HourOtherThanNow(int preferred) => DateTime.Now.Hour == preferred ? preferred + 1 : preferred;
}
