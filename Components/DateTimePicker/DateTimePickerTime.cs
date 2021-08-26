using System;
using System.Collections.Generic;

namespace AngryMonkey.Cloud.Components
{
    public partial class DateTimePicker
    {
        protected class DateTimePickerTime
        {
            public int Minute { get; set; }
            public int Hour { get; set; }
            public bool IsSelected { get; set; } = false;
            public bool IsNow => DateTime.Now.TimeOfDay.Hours == Hour && DateTime.Now.TimeOfDay.Minutes == Minute;
            public string DisplayMinute => Minute.ToString();
			public string CssClasses
            {
                get
                {
                    List<string> classes = new();

                    if (IsSelected)
                        classes.Add("_selected");

                    if (IsNow)
                        classes.Add("_now");

                    if (Minute % 5 == 0)
                        classes.Add("_current");

                    if (Minute == 0)
                        classes.Add("_strong");

                    return string.Join(' ', classes);
                }
            }

            public string TimeDisplay()
            {
                string amPM = Hour >= 12 ? "PM" : "AM";

                DateTimePickerHour hour = new(Hour);

                return $"{hour.DisplayHour:00}:{Minute:00} {amPM}";
            }

            public DateTimePickerTime(int hour, int minute)
            {
                Hour = hour;
                Minute = minute;
            }

            public DateTimePickerTime(DateTime date)
            {
                Hour = date.TimeOfDay.Hours;
                Minute = date.TimeOfDay.Minutes;
            }
        }
    }
}
