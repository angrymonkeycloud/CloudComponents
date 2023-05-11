using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngryMonkey.Cloud.Components;

public class VolumeBarChangeEventArgs : EventArgs
{
    public double? PreviousValue { get; set; }
    public double NewValue { get; set; }
    public bool WasMuted { get; set; }
    public bool IsMuted { get; set; }
}