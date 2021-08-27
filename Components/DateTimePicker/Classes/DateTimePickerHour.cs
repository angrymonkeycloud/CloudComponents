using System;
using System.Collections.Generic;

namespace AngryMonkey.Cloud.Components
{
	public partial class DateTimePicker
	{
		protected class DateTimePickerHour
		{
			public int Hour { get; set; }
			public bool IsNow => DateTime.Now.TimeOfDay.Hours == Hour;

			public string DisplayHour
			{
				get
				{
					int value = Hour;

					if (value > 12)
						value -= 12;
					else if (value == 0)
						value = 12;

					return value.ToString();
				}
			}

			public bool IsSelected { get; set; } = false;
			public string CssClasses
			{
				get
				{
					List<string> classes = new();

					classes.Add("_current");

					if (IsSelected)
						classes.Add("_selected");

					if (IsNow)
						classes.Add("_now");

					return string.Join(' ', classes);
				}
			}

			public DateTimePickerHour(int hour)
			{
				Hour = hour;
			}
		}
	}
}
