using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Linq;
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
            "June",
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
        private DateTimePickerSelectionState SelectionState { get; set; } = DateTimePickerSelectionState.Day;
        private int SelectedMonth { get; set; } = 1;
        private int SelectedYear { get; set; } = 1;
        private string SelectedMonthName => MonthNames[SelectedMonth - 1];

        private string SelectedDateDisplay => SelectedDate.ToDateTime().ToString("dddd, MMMM dd, yyyy");

        private DateTimePickerDate[] Days { get; set; } = new DateTimePickerDate[0];

        private int SelectingYear { get; set; } = 1;

        private DecadeYears[] Decade { get; set; } = new DecadeYears[0];
        private int StartCurrentDecadeYear { get; set; } = 0;
        private int EndCurrentDecadeYear { get; set; } = 0;

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
            if (SelectionState == DateTimePickerSelectionState.Day)
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
            else if (SelectionState == DateTimePickerSelectionState.Month)
            {
                SelectingYear++;
            }
            else
            {
                SelectingYear += 10;
                FillDecadeArray(SelectingYear);
            }
        }

        protected async Task OnPrevClick()
        {
            if (SelectionState == DateTimePickerSelectionState.Day)
            {
                if (SelectedMonth > 1)
                    SelectedMonth--;
                else
                {
                    SelectedMonth = 12;
                    SelectedYear--;
                }

                FillDaysArray();
            }else if (SelectionState == DateTimePickerSelectionState.Month)
            {
                SelectingYear--;
            }
            else
            {
                SelectingYear -= 10;
                FillDecadeArray(SelectingYear);
            }
        }

        protected async Task ChangeSelectionState()
        {
            if (SelectionState == DateTimePickerSelectionState.Day) { 
                SelectingYear = SelectedYear;
                SelectionState = DateTimePickerSelectionState.Month;
            }else if (SelectionState == DateTimePickerSelectionState.Month)
            {
                FillDecadeArray(SelectingYear);
                SelectionState = DateTimePickerSelectionState.Year;
            }
        }

        //protected async Task OnPrevYearClick()
        //{
        //    SelectingYear--;
        //}

        //protected async Task OnNextYearClick()
        //{
        //    SelectingYear++;
        //}

        protected async Task SelectedNewDate(string month)
        {
            switch (month)
            {
                case "January":
                    SelectedMonth = 1;
                    break;
                case "February":
                    SelectedMonth = 2;
                    break;
                case "March":
                    SelectedMonth = 3;
                    break;
                case "April":
                    SelectedMonth = 4;
                    break;
                case "May":
                    SelectedMonth = 5;
                    break;
                case "June":
                    SelectedMonth = 6;
                    break;
                case "July":
                    SelectedMonth = 7;
                    break;
                case "August":
                    SelectedMonth = 8;
                    break;
                case "September":
                    SelectedMonth = 9;
                    break;
                case "October":
                    SelectedMonth = 10;
                    break;
                case "November":
                    SelectedMonth = 11;
                    break;
                case "December":
                    SelectedMonth = 12;
                    break;
            }

            SelectedYear = SelectingYear;
            SelectionState = DateTimePickerSelectionState.Day;
            FillDaysArray();
        }

        //protected async Task SelectNewYears()
        //{
        //    FillDecadeArray(SelectingYear);
        //    selectingNewYears = !selectingNewYears;
        //}

        //protected async Task OnPrevDecadeClick()
        //{
        //    SelectingYear -= 10;
        //    FillDecadeArray(SelectingYear);
        //}

        //protected async Task OnNextDecadeClick()
        //{
        //    SelectingYear += 10;
        //    FillDecadeArray(SelectingYear);
        //}

        protected async Task SetSelectingYear(int year)
        {
            SelectingYear = year;
            SelectionState = DateTimePickerSelectionState.Month;
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
                    days.Add(GetDate(SelectedYear - 1, 12, daysInPreviousMonth - i + 1));
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

        private void FillDecadeArray(int selectingYear)
        {
            StartCurrentDecadeYear = selectingYear / 10 * 10;
            EndCurrentDecadeYear = StartCurrentDecadeYear + 9;
            List<DecadeYears> DecadeList = new();


            for (int i = 3; i > 0; --i)
            {
                DecadeList.Add(GetDecade(StartCurrentDecadeYear - i, StartCurrentDecadeYear, EndCurrentDecadeYear));
            }
            for (int i = 0; i < 10; ++i)
            {
                DecadeList.Add(GetDecade(StartCurrentDecadeYear + i, StartCurrentDecadeYear, EndCurrentDecadeYear));
            }
            for (int i = 1; i < 4; ++i)
            {
                DecadeList.Add(GetDecade(StartCurrentDecadeYear + 9 + i, StartCurrentDecadeYear, EndCurrentDecadeYear));
            }

            Decade = DecadeList.ToArray();
        }

        private DecadeYears GetDecade(int year, int startDecadeYear, int endDecadeYear)
        {
            return new DecadeYears(year, startDecadeYear, endDecadeYear)
            {
                CurrentDeacde = year >= startDecadeYear && year <= endDecadeYear,
                CurrentYear = year == DateTime.Now.Year
            };
        }
    }
}
