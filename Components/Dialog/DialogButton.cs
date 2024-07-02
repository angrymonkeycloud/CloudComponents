using System;

namespace AngryMonkey.Cloud.Components
{
	public class DialogButton
	{
		public required string Text { get; set; }
		public Action? OnReply { get; set; }
		public bool AutoClose { get; set; } = true;
    }
}
