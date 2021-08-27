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
			public bool IsPrimary => Minute % 5 == 0;
			public bool IsNow => DateTime.Now.TimeOfDay.Hours == Hour && DateTime.Now.TimeOfDay.Minutes == Minute;
			public bool IsCurrent { get; set; } = false;
			public string DisplayMinute => Minute.ToString();
			public string CssClasses
			{
				get
				{
					List<string> classes = new()
					{
						$"_{Minute}"
					};

					if (IsSelected)
						classes.Add("_selected");

					if (IsNow)
						classes.Add("_now");

					if (IsCurrent)
						classes.Add("_current");

					if (IsPrimary)
						classes.Add("_primary");

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

				IsCurrent = IsPrimary;
			}

			public DateTimePickerTime(DateTime date)
			{
				Hour = date.TimeOfDay.Hours;
				Minute = date.TimeOfDay.Minutes;

				IsCurrent = IsPrimary;
			}
		}
	}
}
