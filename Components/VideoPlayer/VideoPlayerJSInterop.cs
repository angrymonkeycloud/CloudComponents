using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AngryMonkey.Cloud.Components
{
	public class VideoPlayerJsInterop : IAsyncDisposable
	{
		private readonly Lazy<Task<IJSObjectReference>> moduleTask;

		public VideoPlayerJsInterop(IJSRuntime jsRuntime)
		{
			moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
			   "import", $"./lib/amc/components/videoplayer/videoplayer.js?v={Guid.NewGuid()}").AsTask());
		}

		public async ValueTask Init(ElementReference component)
		{
			var module = await moduleTask.Value;

			await module.InvokeVoidAsync("init", component);
		}

		public async ValueTask ChangeCurrentTime(ElementReference component, double newCurrentTime)
		{
			var module = await moduleTask.Value;

			await module.InvokeVoidAsync("changeCurrentTime", component, newCurrentTime);
		}

		public async ValueTask<VideoInfo> GetVideoInfo(ElementReference component)
		{
			var module = await moduleTask.Value;

			return await module.InvokeAsync<VideoInfo>("getVideoInfo", component);
		}

		public async ValueTask Play(ElementReference component)
		{
			var module = await moduleTask.Value;
			await module.InvokeVoidAsync("play", component);
		}

		public async ValueTask Pause(ElementReference component)
		{
			var module = await moduleTask.Value;
			await module.InvokeVoidAsync("pause", component);
		}

		public async ValueTask Stop(ElementReference component)
		{
			var module = await moduleTask.Value;
			await module.InvokeVoidAsync("stop", component);
		}
		public async ValueTask EnterFullScreen(ElementReference component)
		{
			var module = await moduleTask.Value;
			await module.InvokeVoidAsync("enterFullScreen", component);
		}
		public async ValueTask ExitFullScreen(ElementReference component)
		{
			var module = await moduleTask.Value;
			await module.InvokeVoidAsync("exitFullScreen", component);
		}
		public async Task Implement(ElementReference component, Dictionary<VideoEvents, VideoStateOptions> videoEventOptions, VideoEvents eventName)
		{
			VideoStateOptions options = new VideoStateOptions()
			{
				All = true
			};
			videoEventOptions?.TryGetValue(eventName, out options);

			var module = await moduleTask.Value;

			await module.InvokeVoidAsync("registerCustomEventHandler", component, eventName.ToString().ToLower(), options.GetPayload());
		}

		public async ValueTask DisposeAsync()
		{
			if (moduleTask.IsValueCreated)
			{
				var module = await moduleTask.Value;
				await module.DisposeAsync();
			}
		}
	}
}
