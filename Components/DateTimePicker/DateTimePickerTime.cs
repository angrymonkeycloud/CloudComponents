using System.Collections.Generic;

namespace AngryMonkey.Cloud.Components
{
    public partial class DateTimePicker
    {
        protected class DateTimePickerTime
        {
            public int Minute { get; set; }
            public int Hour { get; set; }
            public string AmOrPm { get; set; }
            public bool IsSelected { get; set; } = false;
            public string CssClasses
            {
                get
                {
                    List<string> classes = new();

                    if (IsSelected)
                        classes.Add("_selected");

                    return string.Join(' ', classes);
                }
            }
            public string TimeDisplay()
            {
                return $"{Hour.ToString("00")}:{Minute.ToString("00")} {AmOrPm}";
            }
            public DateTimePickerTime(string amORpm,int hour, int minute)
            {
                AmOrPm = amORpm;
                Hour = hour;
                Minute = minute;
            }

        }
    }
}
