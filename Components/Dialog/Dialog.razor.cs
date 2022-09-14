using AngryMonkey.Cloud.Components.Icons;
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
	public partial class Dialog : IClosable
	{
		private ElementReference ComponentElement { get; set; }
		[Parameter] public RenderFragment ChildContent { get; set; }
		[Parameter] public string Title { get; set; }
		[Parameter] public List<DialogButton> Buttons { get; set; }
		[Parameter] public bool IsOpened { get; set; }

		private Task<IJSObjectReference> _module;
		private Task<IJSObjectReference> Module => _module ??= GeneralMethods.GetIJSObjectReference(jsRuntime, "dialog/dialog.js");

		public async Task Open()
		{
			IsOpened = true;

			var module = await Module;

			await module.InvokeVoidAsync("DialogOpened", ComponentElement);
		}

		public async Task Close()
		{
			IsOpened = false;

			var module = await Module;

			await module.InvokeVoidAsync("DialogClosed", ComponentElement);
		}

		protected async Task ButtonClicked(DialogButton button)
		{
			if (button.OnReply != null)
				button.OnReply.Invoke();

			await Close();
		}
	}
}
