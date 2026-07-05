using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace CloudComponents.VideoPlayer
{
 public partial class VideoPlayer
 {
 private async Task MuteVolume()
 {
 await JS.InvokeVoidAsync("amcVideoPlayerMuteVolume", ComponentElement, Metadata.IsMuted);
 }

 protected void OnVolumeButtonClick()
 {
 Metadata.DoShowVolumeControls = !Metadata.DoShowVolumeControls;
 }

 protected async Task OnVolumeChanging(CloudComponents.Basic.VolumeBarChangeEventArgs args)
 {
 Metadata.Volume = args.NewValue;

 if (args.IsMuted != args.WasMuted)
 {
 Metadata.IsMuted = args.IsMuted;
 await MuteVolume();
 }

 await JS.InvokeVoidAsync("amcVideoPlayerChangeVolume", ComponentElement, Metadata.Volume);

 await ProgressiveDelay();
 }

 protected async Task OnMouseWheel(WheelEventArgs args)
 {
 if (Metadata.DoShowVolumeControls)
 {
 double newValue;

 if (args.DeltaY <0)
 newValue = Metadata.Volume <= .9 ? Metadata.Volume + .1 :1;
 else
 newValue = Metadata.Volume >= .1 ? Metadata.Volume - .1 :0;

 newValue = Math.Round(newValue,1);

 //await OnVolumeChanging(new ProgressBarChangeEventArgs() { NewValue = newValue });
 }

 await ProgressiveDelay();
 }
 }
}
