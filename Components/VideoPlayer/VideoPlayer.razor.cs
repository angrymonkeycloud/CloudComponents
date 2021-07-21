using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AngryMonkey.Cloud.Components
{
	public partial class VideoPlayer
	{
		private ElementReference ComponentElement { get; set; }

		[Parameter]
		public int HideControlsDelay { get; set; } = 3000;

		private Task<IJSObjectReference> _module;
		private Task<IJSObjectReference> Module => _module ??= GeneralMethods.GetIJSObjectReference(jsRuntime, "videoplayer/videoplayer.js");

		private string ClassAttributes { get; set; } = string.Empty;

		private bool IsUserChangingProgress = false;
		private bool IsVideoPlaying = false;
		private bool IsFullScreen = false;
		private bool ShowSideBar = false;
		private bool ShowSideBarInfo = false;
		private bool ShowSideBarAbout = false;
		private bool ShowSideBarMenu => !ShowSideBarInfo && !ShowSideBarAbout;
		private bool IsMuted = false;
		private bool DoShowVolumeControls = false;

		private bool IsUserInteracting = false;

		private bool HideControls => IsVideoPlaying && !IsUserInteracting;

		private void Repaint()
		{
			ProgressBarStyle = HideControls ? ProgressBarStyle.Flat : ProgressBarStyle.Circle;

			List<string> attributes = new();

			if (HideControls)
			{
				DoShowVolumeControls = false;
				attributes.Add("_hidecontrols");
			}

			if (IsVideoPlaying)
				attributes.Add("_playing");

			if (IsFullScreen)
				attributes.Add("_fullscreen");

			ClassAttributes = string.Join(' ', attributes);
		}

		[Parameter]
		public string Title { get; set; }

		[Parameter]
		public string VideoUrl { get; set; }

		[Parameter]
		public double Volume { get; set; } = 1;

		private string DisplayVolume
		{
			get
			{
				return $"{Volume * 100}";
			}
		}

		public double Duration { get; set; } = 0;
		public double CurrentTime { get; set; } = 0;

		private ProgressBarStyle ProgressBarStyle = ProgressBarStyle.Circle;

		[Parameter]
		public Action<VideoState> TimeUpdate { get; set; }

		[Parameter]
		public EventCallback<VideoState> TimeUpdateEvent { get; set; }
		bool TimeUpdateRequired => TimeUpdate is object;
		bool TimeUpdateEventRequired => TimeUpdateEvent.HasDelegate;
		bool EventFiredEventRequired => EventFiredEvent.HasDelegate;
		bool EventFiredRequired => EventFired is object;
		[Parameter] public Action<VideoEventData> EventFired { get; set; }
		[Parameter] public EventCallback<VideoEventData> EventFiredEvent { get; set; }

		[Parameter]
		public Dictionary<VideoEvents, VideoStateOptions> VideoEventOptions { get; set; }
		bool RegisterEventFired => EventFiredEventRequired || EventFiredRequired;

		[Parameter]
		public VideoPlayerSettings Settings { get; set; }

		private Guid latestId = Guid.Empty;

		#region Volume Methods

		public async Task MuteVolume()
		{
			IsMuted = true;

			DoShowVolumeControls = false;

			var module = await Module;

			await module.InvokeVoidAsync("muteVolume", ComponentElement, IsMuted);
		}

		private async Task OnVolumeButtonClick(MouseEventArgs args)
		{
			if (IsMuted)
			{
				IsMuted = false;

				var module = await Module;

				await module.InvokeVoidAsync("muteVolume", ComponentElement, IsMuted);
			}
			else DoShowVolumeControls = !DoShowVolumeControls;
		}

		protected async Task OnVolumeChanging(ProgressBarChangeEventArgs args)
		{
			Volume = args.NewValue;

			var module = await Module;

			await module.InvokeVoidAsync("changeVolume", ComponentElement, Volume);

			await ProgressiveDelay();
		}

		protected async Task OnVolumeChanged(ProgressBarChangeEventArgs args)
		{
			DoShowVolumeControls = false;

			if (Convert.ToDouble(args.NewValue) == 0)
			{
				IsMuted = true;
				Volume = 1;
			}
		}

		#endregion

		#region Time / Duration

		private string DisplayTimeDuration
		{
			get
			{
				return $"{GetTime(CurrentTime)} / {GetTime(Duration)}";
			}
		}

		private string GetTime(double seconds)
		{
			TimeSpan time = TimeSpan.FromSeconds(seconds);
			int timeLevel = GetTimeLevel();

			string result = $"{time:ss}";

			if (timeLevel > 0)
			{
				result = $"{time:mm}:{result}";

				if (timeLevel > 1)
				{
					result = $"{time:hh}:{result}";

					if (timeLevel > 2)
						result = $"{time:dd}:{result}";
				}
			}

			return result[0] == '0' ? result.Remove(0, 1) : result;
		}

		private int GetTimeLevel()
		{
			TimeSpan time = TimeSpan.FromSeconds(Duration);

			if (time.TotalMinutes < 1)
				return 0;

			if (time.TotalHours < 1)
				return 1;

			if (time.TotalDays < 1)
				return 2;

			return 3;
		}

		#endregion

		#region More Button Methods

		public async Task MoreButtonInfo()
		{
			ShowSideBar = !ShowSideBar;
		}

		public void ShowVideoInfo()
		{
			ShowSideBarInfo = true;
		}
		public void ShowVideoAbout()
		{
			ShowSideBarAbout = true;
		}

		#endregion

		public async Task OnProgressMouseDown(MouseEventArgs args)
		{
			IsUserChangingProgress = true;
			await ProgressiveDelay();
		}

		public async Task OnProgressChange(ProgressBarChangeEventArgs args)
		{
			var module = await Module;

			await module.InvokeVoidAsync("changeCurrentTime", ComponentElement, args.NewValue);

			IsUserChangingProgress = false;

			if (CurrentTime == Duration)
				await StopVideo();

			await ProgressiveDelay();
		}

		public async Task OnVideoChange(ChangeEventArgs args)
		{
			VideoEventData eventData = JsonSerializer.Deserialize<VideoEventData>((string)args.Value);

			IsVideoPlaying = !eventData.State.Paused;
			Repaint();

			switch (eventData.EventName)
			{
				case VideoEvents.TimeUpdate:

					if (!IsUserChangingProgress)
					{
						CurrentTime = eventData.State.CurrentTime;

						if (CurrentTime == Duration)
							await StopVideo();
					}
					break;

				default: break;
			}
		}

		public async Task OnEmptyClick(MouseEventArgs args)
		{
			if (ShowSideBar == false)
			{
				if (IsVideoPlaying)
					await PauseVideo();
				else await PlayVideo();
			}
			else
			{
				if (ShowSideBarMenu)
					ShowSideBar = false;
				else
				{
					ShowSideBarInfo = false;
					ShowSideBarAbout = false;
				}
			}
		}

		protected override async Task OnAfterRenderAsync(bool firstRender)
		{
			if (firstRender)
			{
				var module = await Module;

				await module.InvokeVoidAsync("init", ComponentElement);

				await Implement(VideoEvents.TimeUpdate);
				await Implement(VideoEvents.Play);
				await Implement(VideoEvents.Pause);
			}
		}

		async Task Implement(VideoEvents eventName)
		{

			VideoStateOptions options = new VideoStateOptions() { All = true };
			VideoEventOptions?.TryGetValue(eventName, out options);

			var module = await Module;

			await module.InvokeVoidAsync("registerCustomEventHandler", ComponentElement, eventName.ToString().ToLower(), options.GetPayload());
		}

		public async Task VideoLoaded()
		{
			var module = await Module;

			VideoInfo videoInfo = await module.InvokeAsync<VideoInfo>("getVideoInfo", ComponentElement);

			Duration = Convert.ToDouble(videoInfo.Duration);
		}

		public async Task PlayVideo()
		{
			var module = await Module;

			await module.InvokeVoidAsync("play", ComponentElement);
		}

		public async Task PauseVideo()
		{
			var module = await Module;

			await module.InvokeVoidAsync("pause", ComponentElement);
		}

		public async Task EnterFullScreen()
		{
			var module = await Module;

			await module.InvokeVoidAsync("enterFullScreen", ComponentElement);
		}

		public async Task ExitFullScreen()
		{
			var module = await Module;

			await module.InvokeVoidAsync("exitFullScreen", ComponentElement);
		}

		public async Task OnFullScreenChange(EventArgs args)
		{
			IsFullScreen = !IsFullScreen;

			Repaint();
		}

		public async Task StopVideo()
		{
			CurrentTime = 0;

			var module = await Module;

			await module.InvokeVoidAsync("stop", ComponentElement);

			Repaint();
		}

		public async ValueTask DisposeAsync()
		{
			if (_module != null)
			{
				var module = await _module;
				await module.DisposeAsync();
			}
		}

		protected override async void OnParametersSet()
		{
			base.OnParametersSet();
		}

		private async Task OnComponentClick(MouseEventArgs args)
		{
			await ProgressiveDelay();
		}

		protected async Task OnMouseWheel(WheelEventArgs args)
		{

			if (DoShowVolumeControls)
			{
				double newValue;

				if (args.DeltaY < 0)
					newValue = Volume <= .9 ? Volume + .1 : 1;
				else
					newValue = Volume >= .1 ? Volume - .1 : 0;

				newValue = Math.Round(newValue, 1);

				await OnVolumeChanging(new ProgressBarChangeEventArgs() { NewValue = newValue });
			}

			await ProgressiveDelay();
		}

		public async Task MainMouseMove(MouseEventArgs args)
		{
			await ProgressiveDelay();
		}

		private async Task ProgressiveDelay()
		{
			IsUserInteracting = true;

			Repaint();

			Guid id = Guid.NewGuid();
			latestId = id;

			await Task.Delay(HideControlsDelay);

			if (id != latestId)
				return;

			IsUserInteracting = false;
			Repaint();
		}
	}
}
