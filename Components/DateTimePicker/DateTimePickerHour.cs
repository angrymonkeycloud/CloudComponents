using System.Collections.Generic;

namespace AngryMonkey.Cloud.Components
{
	public partial class DateTimePicker
	{
		protected class DateTimePickerHour
		{
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
			
			public DateTimePickerHour(int hour,string amORpm)
			{
				Hour = hour;
				AmOrPm = amORpm;
			}
		}
	}
}
