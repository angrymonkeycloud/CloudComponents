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
		private string ClassAttributes { get; set; } = string.Empty;

		[Parameter] public bool Extended { get; set; } = false;
		[Parameter] public bool IsMuted { get; set; } = false;
		[Parameter] public double Value { get; set; } = 1;

		#region Events Parameters

		[Parameter] public EventCallback<VolumeBarChangeEventArgs> OnChanging { get; set; }
		[Parameter] public EventCallback<VolumeBarChangeEventArgs> OnChanged { get; set; }

		#endregion

		private string DisplayVolume
		{
			get
			{
				return $"{Value * 100}";
			}
		}
	}
}
