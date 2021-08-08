using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngryMonkey.Cloud.Components
{
	public class ProgressBarChangeEventArgs : EventArgs
	{
		public double? PreviousValue { get; set; }
		public double NewValue { get; set; }
		public ProgressBarSeekButtonInfo SeekButtonInfo { get; set; }
	}
}
