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

		public void Open()
		{
			IsOpened = true;
		}

		public void Close()
		{
			IsOpened = false;
		}

		protected void ButtonClicked(DialogButton button)
		{
			if (button.OnReply != null)
				button.OnReply.Invoke();

			Close();
		}
	}
}
