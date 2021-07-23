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

        private string[] monthNames = new string[12] {
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

        private int SelectedDay{ get; set; } = 1;
        private int SelectedMonth { get; set; } = 1;
        private int SelectedYear { get; set; } = 1;
        private string SelectedMonthName => monthNames[SelectedMonth - 1];

        private string FullDate => new DateTime(SelectedYear, SelectedMonth, SelectedDay).ToString("dddd, MMMM dd, yyyy");

        private bool _prevMonthSelected { get; set; } = false;
        private string prevSelectedMonthName { get; set; } = string.Empty;
        private string prevMonthFullDate { get; set; } = string.Empty;

        private DateTimePickerDay[] Days { get; set; } = new DateTimePickerDay[0];

        public class DateTimePickerDay
        {
            public int Value { get; set; }
            public bool IsToday { get; set; } = false;
            public bool CurrentMonth { get; set; } = true;
        }

        #endregion

        protected override async Task OnInitializedAsync()
        {
            SelectedDay= DateTime.Now.Day;
            SelectedMonth = DateTime.Now.Month;
            SelectedYear= DateTime.Now.Year;

            FillDaysArray();
        }

        protected async Task ChangeDayOfPreviousMonth(int dayVal)
        {
            SelectedDay = dayVal;
            _prevMonthSelected = true;

            if (SelectedMonth == 1)
            {
                prevMonthFullDate = new DateTime(SelectedYear - 1, 12, SelectedDay).ToString("dddd, MMMM dd, yyyy");
                prevSelectedMonthName = monthNames[11];
            }
            else
            {
                prevMonthFullDate = new DateTime(SelectedYear, SelectedMonth - 1, SelectedDay).ToString("dddd, MMMM dd, yyyy");
                prevSelectedMonthName = monthNames[SelectedMonth - 2];
            }

            foreach (DateTimePickerDay day in Days)
            {
                if (day.IsToday)
                    day.IsToday = false;
                if (day.Value == dayVal && !day.CurrentMonth)
                    day.IsToday = true;
            }
        }

        protected async Task ChangeDayOfCurrentMonth(int dayVal)
        {
            SelectedDay = dayVal;
            if(_prevMonthSelected) _prevMonthSelected = false;

            foreach (DateTimePickerDay day in Days) {
                if (day.IsToday)
                    day.IsToday = false;
                if (day.Value == dayVal && day.CurrentMonth)
                    day.IsToday = true;
            }
        }

        protected async Task OnNextClick()
        {
            SelectedDay = 1;
            if(_prevMonthSelected) _prevMonthSelected = false;

            if (SelectedMonth < 12)
                SelectedMonth++;
            else
            {
                SelectedMonth = 1;
                SelectedYear++;
            }

            FillDaysArray(SelectedDay);
        }

        protected async Task OnPrevClick()
        {
            SelectedDay = 1;
            if (_prevMonthSelected) _prevMonthSelected = false;

            if (SelectedMonth > 1)
                SelectedMonth--;
            else
            {
                SelectedMonth = 12;
                SelectedYear--;
            }

            FillDaysArray(SelectedDay);
        }

        private void FillDaysArray()
        {
            int daysInPreviousMonth;
            if (SelectedMonth == 1)
                daysInPreviousMonth = DateTime.DaysInMonth(SelectedYear - 1, 12);
            else if (SelectedMonth == 12)
                daysInPreviousMonth = DateTime.DaysInMonth(SelectedYear + 1, 1);
            else
                daysInPreviousMonth = DateTime.DaysInMonth(SelectedYear , SelectedMonth - 1);
            

            int daysInCurrentMonth = DateTime.DaysInMonth(SelectedYear, SelectedMonth);
            
            List<DateTimePickerDay> days = new();

            for (int i = (int)new DateTime(SelectedYear, SelectedMonth, 1).DayOfWeek; i > 0; i--)
                days.Add(new DateTimePickerDay()
                {
                    CurrentMonth = false,
                    Value = daysInPreviousMonth - i + 1
                });

            for (int i = 1; i <= daysInCurrentMonth; ++i)
                days.Add(new DateTimePickerDay()
                {
                    CurrentMonth = true,
                    Value = i,
                    IsToday = DateTime.Now.Day == i
                });

            Days = days.ToArray();
        }

        private void FillDaysArray(int dayValue)
        {
            int daysInPreviousMonth;
            if (SelectedMonth == 1)
                daysInPreviousMonth = DateTime.DaysInMonth(SelectedYear - 1, 12);
            else if (SelectedMonth == 12)
                daysInPreviousMonth = DateTime.DaysInMonth(SelectedYear + 1, 1);
            else
                daysInPreviousMonth = DateTime.DaysInMonth(SelectedYear, SelectedMonth - 1);


            int daysInCurrentMonth = DateTime.DaysInMonth(SelectedYear, SelectedMonth);

            List<DateTimePickerDay> days = new();

            for (int i = (int)new DateTime(SelectedYear, SelectedMonth, 1).DayOfWeek; i > 0; i--)
                days.Add(new DateTimePickerDay()
                {
                    CurrentMonth = false,
                    Value = daysInPreviousMonth - i + 1
                });

            for (int i = 1; i <= daysInCurrentMonth; ++i)
            {
                if (i==dayValue)
                {
                    days.Add(new DateTimePickerDay()
                    {
                        CurrentMonth = true,
                        Value = i,
                        IsToday = true
                    });
                }
                else
                days.Add(new DateTimePickerDay()
                {
                    CurrentMonth = true,
                    Value = i
                });
            }

            Days = days.ToArray();
        }
    }
}
