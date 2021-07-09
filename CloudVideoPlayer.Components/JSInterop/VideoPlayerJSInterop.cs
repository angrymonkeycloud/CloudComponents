using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace AngryMonkey.Cloud.Components
{
	public class VideoPlayerJsInterop : IAsyncDisposable
	{
		private readonly Lazy<Task<IJSObjectReference>> moduleTask;

		public VideoPlayerJsInterop(IJSRuntime jsRuntime)
		{
			moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
			   "import", "./_content/AngryMonkey.Cloud.Components/cloudvideoplayer.min.js").AsTask());
		}

		public async ValueTask Init(ElementReference video)
		{
			var module = await moduleTask.Value;

			await module.InvokeVoidAsync("init", video);
		}

		public async ValueTask Play(ElementReference video)
		{
			var module = await moduleTask.Value;
			await module.InvokeVoidAsync("play", video);
		}

		public async ValueTask Stop(ElementReference video)
		{
			var module = await moduleTask.Value;
			await module.InvokeVoidAsync("stop", video);
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
