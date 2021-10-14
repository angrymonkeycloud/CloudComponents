using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using System;

namespace AngryMonkey.Cloud.Components
{
    public partial class VolumeBar
    {
		private Task<IJSObjectReference> _module;
		private Task<IJSObjectReference> Module => _module ??= GeneralMethods.GetIJSObjectReference(jsRuntime, "volumebar/volumebar.js");

		private bool IsMuted = false;
		private bool DoShowVolumeControls = false;

		[Parameter] public double Volume { get; set; } = 1;

		private string DisplayVolume
		{
			get
			{
				return $"{Volume * 100}";
			}
		}
	}
}
