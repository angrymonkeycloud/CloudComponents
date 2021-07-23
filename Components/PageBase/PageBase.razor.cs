using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngryMonkey.Cloud.Components
{

	public partial class PageBase
	{
		[Parameter] public string Title { get; set; }
		[Parameter] public string Description { get; set; }
		[Parameter] public string Keywords { get; set; }

		private static Task<IJSObjectReference> _module;
		private Task<IJSObjectReference> Module => _module ??= GeneralMethods.GetIJSObjectReference(jsRuntime, "pagebase/pagebase.js");

		public async ValueTask DisposeAsync()
		{
			if (_module == null)
				return;

			var module = await _module;
			await module.DisposeAsync();
		}

		protected override async Task OnAfterRenderAsync(bool firstRender)
		{
			var module = await Module;

			dynamic data = new ExpandoObject();

			// Title

			if (string.IsNullOrEmpty(Title))
				data.Title = pageSettings.DefaultTitle;
			else data.Title = $"{pageSettings.TitlePrefix}{Title}{pageSettings.TitleSuffix}";

			// Description

			if (Description != null)
				data.Description = Description.Length > 160 ? $"{Description.Substring(0, 157)}..." : Description;

			// Keywords

			if (Keywords != null)
				data.Keywords = Keywords;

			// Google Analytics

			data.Scripts = new List<ExpandoObject>();

			if (!string.IsNullOrEmpty(pageSettings.GoogleAnalyticsId))
			{
				if (pageSettings.GoogleAnalyticsId.StartsWith("G", StringComparison.OrdinalIgnoreCase))
				{
					dynamic script = new ExpandoObject();
					script.Src = $"{"https"}://www.googletagmanager.com/gtag/js?id={pageSettings.GoogleAnalyticsId}";
					data.Scripts.Add(script);

					dynamic contentScript = new ExpandoObject();
					contentScript.Content = $"window.dataLayer = window.dataLayer || [];function gtag(){"{"}dataLayer.push(arguments);{"}"}gtag('js', new Date());gtag('config', '{pageSettings.GoogleAnalyticsId}');";
					data.Scripts.Add(contentScript);
				}
				else
				{
					dynamic contentScript = new ExpandoObject();
					contentScript.Content = $"window.ga = window.ga || function () {"{"} (ga.q = ga.q || []).push(arguments) {"}"}; ga.l = +new Date;ga('create', '{pageSettings.GoogleAnalyticsId}', 'auto');ga('send', 'pageview');";
					data.Scripts.Add(contentScript);

					dynamic script = new ExpandoObject();
					script.Src = $"{"https"}://www.google-analytics.com/analytics.js";
					data.Scripts.Add(script);
				}

			}

			await module.InvokeVoidAsync("updateTitle", (object)data);
		}
	}
}
