using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace AngryMonkey.Cloud.Components
{
	public class ProgressBarJsInterop : IAsyncDisposable
	{
		private readonly Lazy<Task<IJSObjectReference>> moduleTask;

		public ProgressBarJsInterop(IJSRuntime jsRuntime)
		{
			string min = ".min";

#if DEBUG
			min = string.Empty;
#endif

			moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
			   "import", $"./_content/{Assembly.GetExecutingAssembly().GetName().Name}/progressbar/progressbar{min}.js?v={Guid.NewGuid()}").AsTask());
		}
		public async ValueTask MouseDown(ElementReference component, double clientX)
		{
			var module = await moduleTask.Value;

			await module.InvokeVoidAsync("mouseDown", component, clientX);
		}

		//public async ValueTask Init(ElementReference component)
		//{
		//	var module = await moduleTask.Value;

		//	await module.InvokeVoidAsync("init", component);
		//}

		public async ValueTask Repaint(ElementReference component, double value, double total)
		{
			var module = await moduleTask.Value;

			await module.InvokeVoidAsync("repaint", component, value, total);
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
