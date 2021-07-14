using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AngryMonkey.Cloud.Components
{
	public class ProgressBarJsInterop : IAsyncDisposable
	{
		private readonly Lazy<Task<IJSObjectReference>> moduleTask;

		public ProgressBarJsInterop(IJSRuntime jsRuntime)
		{
			moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
			   "import", $"./lib/amc/components/progressbar/progressbar.js?v={Guid.NewGuid()}").AsTask());

			//string[] path = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames();

			//Stream stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("AngryMonkey.Cloud.Components.wwwroot.progressbar.progressbar.js");

			//stream.Position = 0;
			//StreamReader reader = new(stream, System.Text.Encoding.UTF8);

			//string test = reader.ReadToEnd();
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
