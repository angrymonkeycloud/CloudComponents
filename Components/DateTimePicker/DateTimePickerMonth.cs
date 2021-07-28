using System.Collections.Generic;

namespace AngryMonkey.Cloud.Components
{
    public partial class DateTimePicker
    {
        protected class DateTimePickerMonth
        {
            public DateTimePickerMonth(int year, int month)
            {
                Year = year;
                Month = month;
            }

            public int Month { get; set; }
            public int Year { get; set; }
            public bool IsSelected { get; set; } = false;
            public bool IsToday { get; set; } = false;
            public string Name => Names[Month - 1];
            public string CssClasses
            {
                get
                {
                    List<string> classes = new();

                    classes.Add("_current");

                    if (IsSelected)
                        classes.Add("_selected");

                    if (IsToday)
                        classes.Add("_today");

                    return string.Join(' ', classes);
                }
            }

            public static readonly string[] Names = new string[]
            {
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
        }
    }
}
