using System;

namespace AngryMonkey.CloudComponents
{
	public class DialogButton
	{
		public required string Text { get; set; }
		public Action? OnReply { get; set; }
		public bool AutoClose { get; set; } = true;
    }
}
