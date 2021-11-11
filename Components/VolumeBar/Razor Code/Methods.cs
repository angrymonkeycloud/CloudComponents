using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace AngryMonkey.Cloud.Components
{
	public partial class VolumeBar
	{
		public async Task MuteVolume()
		{
			IsMuted = true;

			Extended = false;

			VolumeBarChangeEventArgs changeArgs = new()
			{
				PreviousValue = Value,
				NewValue = Value,
				WasMuted = false,
				IsMuted = true
			};

			await OnChanging.InvokeAsync(changeArgs);
		}

		private async Task OnVolumeButtonClick(MouseEventArgs args)
		{
			if (IsMuted)
			{
				IsMuted = false;

				VolumeBarChangeEventArgs changeArgs = new()
				{
					PreviousValue = Value,
					NewValue = Value,
					WasMuted = true,
					IsMuted = false
				};

				await OnChanging.InvokeAsync(changeArgs);
			}
			else Extended = !Extended;
		}

		protected async Task OnProgressBarChanging(ProgressBarChangeEventArgs args)
		{
			VolumeBarChangeEventArgs changeArgs = new()
			{
				PreviousValue = args.PreviousValue,
				NewValue = args.NewValue,
				WasMuted = IsMuted,
				IsMuted = IsMuted
			};

			Value = args.NewValue;

			await OnChanging.InvokeAsync(changeArgs);
		}

		protected async Task OnProgressBarChanged(ProgressBarChangeEventArgs args)
		{
			Extended = false;

			VolumeBarChangeEventArgs changeArgs = new()
			{
				PreviousValue = args.PreviousValue,
				WasMuted = IsMuted,
			};

			if (Convert.ToDouble(args.NewValue) == 0)
			{
				IsMuted = true;
				Value = 1;
			}

			changeArgs.IsMuted = IsMuted;
			changeArgs.NewValue = Value;

			await OnChanging.InvokeAsync(changeArgs);
		}
	}
}
