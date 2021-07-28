using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AngryMonkey.Cloud.Components
{
    public partial class DateTimePicker
    {
        #region common

        private readonly string[] WeekDaysNames = new string[]
        {
            "Su",
            "Mo",
            "Tu",
            "We",
            "Th",
            "Fr",
            "Sa",
        };

        private DateTimePickerDate SelectedDate { get; set; }
        private DateTimePickerSelectionState SelectionState { get; set; } = DateTimePickerSelectionState.Day;
        private int NavigatedMonth { get; set; } = 1;
        private int NavigatedYear { get; set; } = 1;
        private int StartCurrentDecadeYear => NavigatedYear / 10 * 10;
        private int EndCurrentDecadeYear => (NavigatedYear / 10 * 10) + 9;

        protected string DisplaySelectionInfo => SelectionState switch
        {
            DateTimePickerSelectionState.Year => $"{StartCurrentDecadeYear} - {EndCurrentDecadeYear}",
            DateTimePickerSelectionState.Month => NavigatedYear.ToString(),
            _ => $"{DateTimePickerMonth.Names[NavigatedMonth - 1]} {NavigatedYear.ToString()}",
        };

        private string SelectedDateDisplay => SelectedDate.ToDateTime().ToString("dddd, MMMM dd, yyyy");

        private DateTimePickerDate[] Days { get; set; } = Array.Empty<DateTimePickerDate>();
        private DateTimePickerMonth[] Months { get; set; } = Array.Empty<DateTimePickerMonth>();
        private DateTimePickerYear[] Years { get; set; } = Array.Empty<DateTimePickerYear>();

        #endregion

        protected override async Task OnInitializedAsync()
        {
            SelectedDate = new DateTimePickerDate(DateTime.Now);
            NavigatedMonth = SelectedDate.Month;
            NavigatedYear = SelectedDate.Year;

            FillDaysArray();
        }

        protected async Task ChangeSelectionState()
        {
            switch (SelectionState)
            {
                case DateTimePickerSelectionState.Month:
                    FillYearsArray();
                    SelectionState = DateTimePickerSelectionState.Year;
                    break;

                default:
                    SelectionState = DateTimePickerSelectionState.Month;
                    FillMonthsArray();
                    break;
            }
        }

        protected async Task OnNextClick()
        {
            switch (SelectionState)
            {
                case DateTimePickerSelectionState.Year:
                    NavigatedYear += 10;
                    NavigatedYear = StartCurrentDecadeYear;
                    FillYearsArray();
                    break;

                case DateTimePickerSelectionState.Month:

                    NavigatedYear++;
                    FillMonthsArray();
                    break;

                default:
                    if (NavigatedMonth < 12)
                        NavigatedMonth++;
                    else
                    {
                        NavigatedMonth = 1;
                        NavigatedYear++;
                    }

                    FillDaysArray();
                    break;
            }
        }

        protected async Task OnPrevClick()
        {
            switch (SelectionState)
            {
                case DateTimePickerSelectionState.Year:
                    NavigatedYear -= 10;
                    NavigatedYear = StartCurrentDecadeYear;
                    FillYearsArray();
                    break;

                case DateTimePickerSelectionState.Month:

                    NavigatedYear--;
                    FillMonthsArray();
                    break;

                default:
                    if (NavigatedMonth > 1)
                        NavigatedMonth--;
                    else
                    {
                        NavigatedMonth = 12;
                        NavigatedYear--;
                    }

                    FillDaysArray();
                    break;
            }
        }

        protected async Task OnDateSelected(DateTimePickerDate day)
        {
            SelectedDate = day;

            DateTimePickerDate oldDay = Days.FirstOrDefault(key => key.IsSelected);

            if (oldDay != null)
                oldDay.IsSelected = false;

            DateTimePickerDate newDay = Days.FirstOrDefault(key => key.Day == SelectedDate.Day && key.Month == SelectedDate.Month && key.Year == SelectedDate.Year);

            if (newDay != null)
                newDay.IsSelected = true;
        }

        protected async Task OnMonthSelected(int month)
        {
            NavigatedMonth = month;

            SelectionState = DateTimePickerSelectionState.Day;
            FillDaysArray();
        }

        protected async Task OnYearSelected(int year)
        {
            NavigatedYear = year;
            SelectionState = DateTimePickerSelectionState.Month;
            FillMonthsArray();
        }

        private void FillDaysArray()
        {
            int daysInPreviousMonth;
            if (NavigatedMonth == 1)
                daysInPreviousMonth = DateTime.DaysInMonth(NavigatedYear - 1, 12);
            else
                daysInPreviousMonth = DateTime.DaysInMonth(NavigatedYear, NavigatedMonth - 1);


            int daysInCurrentMonth = DateTime.DaysInMonth(NavigatedYear, NavigatedMonth);

            List<DateTimePickerDate> days = new();

            if (NavigatedMonth == 1)
            {
                for (int i = (int)new DateTime(NavigatedYear, NavigatedMonth, 1).DayOfWeek; i > 0; i--)
                    days.Add(NewDate(NavigatedYear - 1, 12, daysInPreviousMonth - i + 1));
            }
            else
            {
                for (int i = (int)new DateTime(NavigatedYear, NavigatedMonth, 1).DayOfWeek; i > 0; i--)
                    days.Add(NewDate(NavigatedYear, NavigatedMonth - 1, daysInPreviousMonth - i + 1));
            }

            for (int i = 1; i <= daysInCurrentMonth; ++i)
                days.Add(NewDate(NavigatedYear, NavigatedMonth, i));

            int index = 1;

            if (NavigatedMonth == 12)
            {
                while (days.Count < 42)
                    days.Add(NewDate(NavigatedYear + 1, 1, index++));
            }
            else
            {
                while (days.Count < 42)
                    days.Add(NewDate(NavigatedYear, NavigatedMonth + 1, index++));
            }

            Days = days.ToArray();
        }

        private void FillMonthsArray()
        {
            List<DateTimePickerMonth> months = new();

            for (int i = 1; i <= 12; i++)
                months.Add(new DateTimePickerMonth(NavigatedYear, i) { IsSelected = i == SelectedDate.Month && SelectedDate.Year == NavigatedYear });

            Months = months.ToArray();
        }

        private void FillYearsArray()
        {
            List<DateTimePickerYear> years = new();

            years.Add(new DateTimePickerYear(StartCurrentDecadeYear - 1));

            for (int i = StartCurrentDecadeYear; i <= EndCurrentDecadeYear; i++)
                years.Add(new DateTimePickerYear(i)
                {
                    IsSelected = SelectedDate.Year == i,
                    IsCurrentDecade = true
                });

            years.Add(new DateTimePickerYear(EndCurrentDecadeYear + 1));

            Years = years.ToArray();
        }

        #region Helpers

        private DateTimePickerDate NewDate(int year, int month, int day)
        {
            return new DateTimePickerDate(year, month, day)
            {
                CurrentMonth = NavigatedYear == year && NavigatedMonth == month,
                IsSelected = SelectedDate.Year == year && SelectedDate.Month == month && SelectedDate.Day == day
            };
        }

        #endregion
    }
}
