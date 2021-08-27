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
		protected override async Task OnInitializedAsync()
		{
			if (ShowDate)
			{
				SelectedDate = new DateTimePickerDate(DateTime.Now);
				NavigatedMonth = SelectedDate.Month;
				NavigatedYear = SelectedDate.Year;
				FillDaysArray();
			}
			if (ShowTime)
			{
				SelectedTime = new DateTimePickerTime(DateTime.Now);
				NavigatedHour = SelectedTime.Hour;
				ChoosingAmOrPm = SelectedTime.Hour > 12 ? "PM" : "AM";
				FillMinutesArray();
			}
		}

		protected async Task OnMinuteClick(DateTimePickerTime minute)
		{
			SelectedTime = minute;

			DateTimePickerTime oldTime = Minutes.FirstOrDefault(key => key.IsSelected);

			if (oldTime != null)
				oldTime.IsSelected = false;

			DateTimePickerTime newTime = Minutes.FirstOrDefault(key => key.Minute == SelectedTime.Minute && key.Hour == SelectedTime.Hour);

			if (newTime != null)
				newTime.IsSelected = true;
		}

		protected async Task OnHourClick(DateTimePickerHour hour)
		{
			bool differentHour = NavigatedHour != hour.Hour;

			NavigatedHour = hour.Hour;
			FillMinutesArray();
			TimeSelectionState = TimePickerSelectionState.Minute;

			if (differentHour)
				await OnMinuteClick(new DateTimePickerTime(hour.Hour, 0));
		}

		protected async Task OnPrevTimeSelectionStateClick()
		{
			switch (TimeSelectionState)
			{
				case TimePickerSelectionState.Hour:
					ChoosingAmOrPm = ChoosingAmOrPm == "AM" ? "PM" : "AM";
					FillHoursArray();
					break;

				default:
					if (NavigatedHour == 0)
						NavigatedHour = 23;

					else
						NavigatedHour -= 1;

					if (NavigatedHour >= 12)
						ChoosingAmOrPm = "PM";
					else ChoosingAmOrPm = "AM";

					FillMinutesArray();
					break;
			}
		}

		protected async Task OnNextTimeSelectionStateClick()
		{
			switch (TimeSelectionState)
			{
				case TimePickerSelectionState.Hour:
					ChoosingAmOrPm = ChoosingAmOrPm == "AM" ? "PM" : "AM";
					FillHoursArray();
					break;

				default:
					if (NavigatedHour == 23)
						NavigatedHour = 0;
					else
						NavigatedHour += 1;

					if (NavigatedHour >= 12)
						ChoosingAmOrPm = "PM";
					else ChoosingAmOrPm = "AM";

					FillMinutesArray();
					break;
			}
		}

		protected async Task ChangeTimeSelectionState()
		{
			switch (TimeSelectionState)
			{
				case (TimePickerSelectionState.Hour):
					OnNextTimeSelectionStateClick();
					break;

				default:
					FillHoursArray();
					TimeSelectionState = TimePickerSelectionState.Hour;
					break;
			}
		}

		protected async Task ChangeDateSelectionState()
		{
			switch (DateSelectionState)
			{
				case DatePickerSelectionState.Year:
					break;

				case DatePickerSelectionState.Month:
					FillYearsArray();
					DateSelectionState = DatePickerSelectionState.Year;
					break;

				default:
					DateSelectionState = DatePickerSelectionState.Month;
					FillMonthsArray();
					break;
			}
		}

		protected async Task OnNextClick()
		{
			switch (DateSelectionState)
			{
				case DatePickerSelectionState.Year:
					NavigatedYear += 10;
					NavigatedYear = StartCurrentDecadeYear;
					FillYearsArray();
					break;

				case DatePickerSelectionState.Month:

					NavigatedYear++;
					FillMonthsArray();
					break;

				default:
					if (NavigatedMonth < 12)
						NavigatedMonth++;
					else
					{
						NavigatedMonth = 1;
						NavigatedYear++;
					}

					FillDaysArray();
					break;
			}
		}

		protected async Task OnPrevClick()
		{
			switch (DateSelectionState)
			{
				case DatePickerSelectionState.Year:
					NavigatedYear -= 10;
					NavigatedYear = StartCurrentDecadeYear;
					FillYearsArray();
					break;

				case DatePickerSelectionState.Month:

					NavigatedYear--;
					FillMonthsArray();
					break;

				default:
					if (NavigatedMonth > 1)
						NavigatedMonth--;
					else
					{
						NavigatedMonth = 12;
						NavigatedYear--;
					}

					FillDaysArray();
					break;
			}
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

		protected async Task OnMonthSelected(int month)
		{
			NavigatedMonth = month;
			FillDaysArray();
			DateSelectionState = DatePickerSelectionState.Day;
		}

		protected async Task OnYearSelected(int year)
		{
			NavigatedYear = year;
			FillMonthsArray();
			DateSelectionState = DatePickerSelectionState.Month;
		}

		#region Body Swapping

		protected async Task OnBodyTouchStart(TouchEventArgs args)
		{
			bodySwapClientX = args.ChangedTouches[0].ClientX;
			bodySwapTime = DateTime.Now;
		}

		protected async Task OnBodyTouchEnd(TouchEventArgs args)
		{
			if (DateTime.Now.Subtract(bodySwapTime).TotalSeconds > 1)
				return;

			bodySwapClientX = args.ChangedTouches[0].ClientX - bodySwapClientX;

			if (bodySwapClientX > -10 && bodySwapClientX < 10)
				return;

			if (bodySwapClientX > 0)
				await OnPrevClick();
			else await OnNextClick();
		}

		#endregion

		#region Date Helpers

		private void FillDaysArray()
		{
			int daysInPreviousMonth;
			if (NavigatedMonth == 1)
				daysInPreviousMonth = DateTime.DaysInMonth(NavigatedYear - 1, 12);
			else
				daysInPreviousMonth = DateTime.DaysInMonth(NavigatedYear, NavigatedMonth - 1);


			int daysInCurrentMonth = DateTime.DaysInMonth(NavigatedYear, NavigatedMonth);

			List<DateTimePickerDate> days = new();

			if (NavigatedMonth == 1)
			{
				for (int i = (int)new DateTime(NavigatedYear, NavigatedMonth, 1).DayOfWeek; i > 0; i--)
					days.Add(NewDate(NavigatedYear - 1, 12, daysInPreviousMonth - i + 1));
			}
			else
			{
				for (int i = (int)new DateTime(NavigatedYear, NavigatedMonth, 1).DayOfWeek; i > 0; i--)
					days.Add(NewDate(NavigatedYear, NavigatedMonth - 1, daysInPreviousMonth - i + 1));
			}

			for (int i = 1; i <= daysInCurrentMonth; ++i)
				days.Add(NewDate(NavigatedYear, NavigatedMonth, i));

			int index = 1;

			if (NavigatedMonth == 12)
			{
				while (days.Count < 42)
					days.Add(NewDate(NavigatedYear + 1, 1, index++));
			}
			else
			{
				while (days.Count < 42)
					days.Add(NewDate(NavigatedYear, NavigatedMonth + 1, index++));
			}

			Days = days.ToArray();
		}

		private void FillMonthsArray()
		{
			List<DateTimePickerMonth> months = new();

			for (int i = 1; i <= 12; i++)
				months.Add(new DateTimePickerMonth(NavigatedYear, i)
				{
					IsSelected = i == SelectedDate.Month && SelectedDate.Year == NavigatedYear,
					IsToday = i == DateTime.Now.Month && DateTime.Now.Year == NavigatedYear
				});

			Months = months.ToArray();
		}

		private DateTimePickerDate NewDate(int year, int month, int day)
		{
			return new DateTimePickerDate(year, month, day)
			{
				CurrentMonth = NavigatedYear == year && NavigatedMonth == month,
				IsSelected = SelectedDate.Year == year && SelectedDate.Month == month && SelectedDate.Day == day
			};
		}

		private void FillYearsArray()
		{
			List<DateTimePickerYear> years = new();

			years.Add(new DateTimePickerYear(StartCurrentDecadeYear - 1));

			for (int i = StartCurrentDecadeYear; i <= EndCurrentDecadeYear; i++)
				years.Add(new DateTimePickerYear(i)
				{
					IsToday = DateTime.Now.Year == i,
					IsSelected = SelectedDate.Year == i,
					IsCurrentDecade = true
				});

			years.Add(new DateTimePickerYear(EndCurrentDecadeYear + 1));

			Years = years.ToArray();
		}

		#endregion

		#region TimeHelpers

		private void FillHoursArray()
		{
			List<DateTimePickerHour> hours = new();

			int startingHour = ChoosingAmOrPm == "AM" ? 1 : 13;
			int endingHour = ChoosingAmOrPm == "AM" ? 12 : 24;

			for (int i = startingHour; i <= endingHour; i++)
				hours.Add(new DateTimePickerHour(i)
				{
					IsSelected = i == SelectedTime.Hour
				});

			Hours = hours.ToArray();
		}

		private void FillMinutesArray()
		{
			List<DateTimePickerTime> minutes = new();

			for (int i = 0; i < 60; i++)
				minutes.Add(new DateTimePickerTime(NavigatedHour, i)
				{
					IsSelected = i == SelectedTime.Minute && NavigatedHour == SelectedTime.Hour
				});

			Minutes = minutes.ToArray();
		}

		#endregion
	}
}
