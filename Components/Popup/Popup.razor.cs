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
	public partial class Popup : IClosable
	{
		[Parameter] public RenderFragment ChildContent { get; set; }
		private ElementReference ComponentElement { get; set; }
		public bool Visible { get; set; } = false;
		[Parameter] public bool CloseOnAnywhereClick { get; set; } = true;

		public void Open()
		{
			Visible = true;
		}

		public void Close()
		{
			Visible = false;
		}

		protected void ComponentClicked()
		{
			if (CloseOnAnywhereClick)
				Close();
		}
	}
}
