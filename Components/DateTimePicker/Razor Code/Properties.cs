using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AngryMonkey.Cloud.Components
{
	public partial class DateTimePicker
	{
		private string SelectedDateTimeDisplay => Mode switch
		{
			DateTimePickerMode.Date => SelectedDateDisplay,
			DateTimePickerMode.Time => SelectedTimeDisplay,
			_ => SelectedDateDisplay + "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" + SelectedTimeDisplay,
		};

		#region date

		[Parameter] public DateTimePickerMode Mode { get; set; } = DateTimePickerMode.DateAndTime;

		private bool ShowDate => Mode != DateTimePickerMode.Time;

		private readonly string[] WeekDaysNames = new string[]
		{
			"Su",
			"Mo",
			"Tu",
			"We",
			"Th",
			"Fr",
			"Sa",
		};

		private DateTimePickerDate SelectedDate { get; set; }
		private DatePickerSelectionState DateSelectionState { get; set; } = DatePickerSelectionState.Day;
		private int NavigatedMonth { get; set; } = 1;
		private int NavigatedYear { get; set; } = 1;
		private int StartCurrentDecadeYear => NavigatedYear / 10 * 10;
		private int EndCurrentDecadeYear => (NavigatedYear / 10 * 10) + 9;

		protected string DisplaySelectionInfo => DateSelectionState switch
		{
			DatePickerSelectionState.Year => $"{StartCurrentDecadeYear} - {EndCurrentDecadeYear}",
			DatePickerSelectionState.Month => NavigatedYear.ToString(),
			_ => $"{DateTimePickerMonth.Names[NavigatedMonth - 1]} {NavigatedYear.ToString()}",
		};

		private string SelectedDateDisplay => SelectedDate.ToDateTime().ToString("MMMM dd, yyyy");

		private DateTimePickerDate[] Days { get; set; } = Array.Empty<DateTimePickerDate>();
		private DateTimePickerMonth[] Months { get; set; } = Array.Empty<DateTimePickerMonth>();
		private DateTimePickerYear[] Years { get; set; } = Array.Empty<DateTimePickerYear>();

		#endregion

		#region time

		private DateTimePickerTime SelectedTime { get; set; }
		private bool ShowTime => Mode != DateTimePickerMode.Date;
		private TimePickerSelectionState TimeSelectionState { get; set; } = TimePickerSelectionState.Minute;
		private int NavigatedHour { get; set; } = 0;
		private string DisplayNavigatedHour
		{
			get
			{
				int result = NavigatedHour;

				if (result > 12)
					result -= 12;
				else if (result == 0)
					result = 12;

				return result.ToString();
			}
		}
		private string ChoosingAmOrPm { get; set; } = "AM";
		private string DisplayTimeInfo => TimeSelectionState switch
		{
			TimePickerSelectionState.Minute => DisplayNavigatedHour + "  " + ChoosingAmOrPm,
			_ => ChoosingAmOrPm,
		};
		private string SelectedTimeDisplay => SelectedTime.TimeDisplay();
		private DateTimePickerTime[] Minutes { get; set; } = Array.Empty<DateTimePickerTime>();
		private DateTimePickerHour[] Hours { get; set; } = Array.Empty<DateTimePickerHour>();
		#endregion

		double bodySwapClientX;
		DateTime bodySwapTime;
	}
}
