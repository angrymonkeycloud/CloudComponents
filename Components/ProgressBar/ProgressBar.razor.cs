using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngryMonkey.Cloud.Components
{
	public partial class ProgressBar
	{
		#region Parameters

		[Parameter] public double BufferValue { get; set; }

		[Parameter] public ProgressBarStyle Style { get; set; } = ProgressBarStyle.Flat;

		[Parameter] public double Step { get; set; } = 1;

		private double? _value;
		[Parameter] public double? Value { get; set; }

		[Parameter] public double ChangingValue { get; set; } = -1;

		private double _total = 0;
		[Parameter] public double Total { get; set; }

		#endregion

		#region Events Parameters

		[Parameter] public EventCallback<ProgressBarChangeEventArgs> OnChanging { get; set; }
		[Parameter] public EventCallback<ProgressBarChangeEventArgs> OnChanged { get; set; }

		#endregion

		#region Common

		private ElementReference ComponentElement { get; set; }

		private Task<IJSObjectReference> _module;
		private Task<IJSObjectReference> Module => _module ??= GeneralMethods.GetIJSObjectReference(jsRuntime, "progressbar/progressbar.js");

		public async ValueTask DisposeAsync()
		{
			if (_module != null)
			{
				var module = await _module;
				await module.DisposeAsync();
			}
		}

		#endregion

		protected override async void OnParametersSet()
		{
			base.OnParametersSet();

			if (Value != _value)
			{
				_value = Value;
				await Repaint();
			}

			if (Total != _total)
			{
				_total = Total;
				await Repaint();
			}
		}

		private async Task Repaint()
		{
			var module = await Module;

			await module.InvokeVoidAsync("repaint", ComponentElement, Value, Total);
		}

		private async Task OnComponentMouseDown(MouseEventArgs e)
		{
			var module = await Module;

			await module.InvokeVoidAsync("mouseDown", ComponentElement, e.ClientX);
		}

		protected async Task OnComponentTouchStart(TouchEventArgs args)
		{
			var module = await Module;

			await module.InvokeVoidAsync("touchDown", ComponentElement, args.Touches[0].ClientX);
		}

		private async Task OnRangeChange(ChangeEventArgs args)
		{
			double newValue = Convert.ToDouble(args.Value);

			ProgressBarChangeEventArgs changeArgs = new()
			{
				PreviousValue = Value,
				NewValue = newValue
			};

			if (Value == newValue && Value != ChangingValue)
				return;

			Value = newValue;
			changeArgs.SeekButtonInfo = await GetInfo();

			await OnChanged.InvokeAsync(changeArgs);

			ChangingValue = -1;
		}

		protected async Task OnChangingRangeChange(ChangeEventArgs args)
		{
			double newValue = Convert.ToDouble(args.Value);

			ProgressBarChangeEventArgs changeArgs = new()
			{
				PreviousValue = ChangingValue,
				NewValue = newValue
			};

			if (ChangingValue == newValue)
				return;

			ChangingValue = newValue;
			changeArgs.SeekButtonInfo = await GetInfo();

			await OnChanging.InvokeAsync(changeArgs);
		}

		private async Task<ProgressBarSeekButtonInfo> GetInfo()
		{
			var module = await Module;

			return await module.InvokeAsync<ProgressBarSeekButtonInfo>("getInfo", ComponentElement);
		}
	}
}
