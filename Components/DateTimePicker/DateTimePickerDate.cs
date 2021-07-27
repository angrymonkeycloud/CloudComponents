using System;
using System.Collections.Generic;

namespace AngryMonkey.Cloud.Components
{
    public partial class DateTimePicker
    {
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

            public DateTime ToDateTime()
            {
                return new DateTime(Year, Month, Day);
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
    }
}
