using Microsoft.AspNetCore.Components;
using System;

namespace AngryMonkey.Cloud.Components
{
	public class DialogButton
	{
		public string Text { get; set; }
		public Action OnReply { get; set; }
	}
}
