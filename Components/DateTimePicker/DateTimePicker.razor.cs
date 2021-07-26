using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngryMonkey.Cloud.Components
{
    public partial class DateTimePicker
    {
        #region common

        private readonly string[] MonthNames = new string[] {
            "January",
            "February",
            "March",
            "April",
            "May",
            "june",
            "July",
            "August",
            "September",
            "October",
            "November",
            "December"
        };

        private readonly string[] WeekDaysNames = new string[]
        {
            "Sun",
            "Mon",
            "Tue",
            "Wed",
            "Thu",
            "Fri",
            "Sat",
        };

        private DateTimePickerDate SelectedDate { get; set; }

        private int SelectedMonth { get; set; } = 1;
        private int SelectedYear { get; set; } = 1;
        private string SelectedMonthName => MonthNames[SelectedMonth - 1];

        private string FullDate => new DateTime(SelectedDate.Year, SelectedDate.Month, SelectedDate.Day).ToString("dddd, MMMM dd, yyyy");

        private DateTimePickerDate[] Days { get; set; } = new DateTimePickerDate[0];

        protected class DateTimePickerDate
        {
            public int Day { get; set; }
            public int Month { get; set; }
            public int Year { get; set; }
            public bool IsToday => DateTime.Now.Date.Equals(new DateTime(Year, Month, Day));
            public bool IsSelected { get; set; } = false;
            public bool CurrentMonth { get; set; } = true;
            public string Classes
            {
                get
                {
                    List<string> classes = new();

                    if (IsSelected)
                        classes.Add("_selected");

                    if (IsToday)
                        classes.Add("_today");

                    if (CurrentMonth)
                        classes.Add("_currentmonth");

                    return string.Join(' ', classes);
                }
            }

            public DateTimePickerDate(int year, int month, int day)
            {
                Year = year;
                Month = month;
                Day = day;
            }

            public DateTimePickerDate(DateTime date)
            {
                Year = date.Year;
                Month = date.Month;
                Day = date.Day;
            }
        }

        #endregion

        protected override async Task OnInitializedAsync()
        {
            SelectedDate = new DateTimePickerDate(DateTime.Now);
            SelectedMonth = SelectedDate.Month;
            SelectedYear = SelectedDate.Year;

            FillDaysArray();
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

        protected async Task OnNextClick()
        {
            if (SelectedMonth < 12)
                SelectedMonth++;
            else
            {
                SelectedMonth = 1;
                SelectedYear++;
            }

            FillDaysArray();
        }

        protected async Task OnPrevClick()
        {
            if (SelectedMonth > 1)
                SelectedMonth--;
            else
            {
                SelectedMonth = 12;
                SelectedYear--;
            }

            FillDaysArray();
        }

        private void FillDaysArray()
        {
            int daysInPreviousMonth;
            if (SelectedMonth == 1)
                daysInPreviousMonth = DateTime.DaysInMonth(SelectedYear - 1, 12);
            else
                daysInPreviousMonth = DateTime.DaysInMonth(SelectedYear, SelectedMonth - 1);


            int daysInCurrentMonth = DateTime.DaysInMonth(SelectedYear, SelectedMonth);

            List<DateTimePickerDate> days = new();

            if (SelectedMonth == 1)
            {
                for (int i = (int)new DateTime(SelectedYear, SelectedMonth, 1).DayOfWeek; i > 0; i--)
                    days.Add(GetDate(SelectedYear - 1, 1, daysInPreviousMonth - i + 1));
            }
            else
            {
                for (int i = (int)new DateTime(SelectedYear, SelectedMonth, 1).DayOfWeek; i > 0; i--)
                    days.Add(GetDate(SelectedYear, SelectedMonth - 1, daysInPreviousMonth - i + 1));
            }

            for (int i = 1; i <= daysInCurrentMonth; ++i)
                days.Add(GetDate(SelectedYear, SelectedMonth, i));

            int index = 1;

            if (SelectedMonth == 12)
            {
                while (days.Count < 42)
                    days.Add(GetDate(SelectedYear + 1, 1, index++));
            }
            else if (SelectedMonth == 1)
            {
                while (days.Count < 42)
                    days.Add(GetDate(SelectedYear - 1, 12, index++));
            }
            else
            {
                while (days.Count < 42)
                    days.Add(GetDate(SelectedYear, SelectedMonth + 1, index++));
            }

            Days = days.ToArray();
        }

        private DateTimePickerDate GetDate(int year, int month, int day)
        {
            return new DateTimePickerDate(year, month, day)
            {
                CurrentMonth = SelectedYear == year && SelectedMonth == month,
                IsSelected = SelectedDate.Year == year && SelectedDate.Month == month && SelectedDate.Day == day
            };
        }
    }
}
