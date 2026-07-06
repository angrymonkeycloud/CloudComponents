using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace CloudComponents.VideoPlayer
{
 public partial class VideoPlayer
 {
 protected async Task OnProgressMouseDown(MouseEventArgs args)
 {
 Metadata.IsUserChangingProgress = true;
 await ProgressiveDelay();
 }

 protected async Task OnProgressTouchStart(TouchEventArgs args)
 {
 Metadata.IsUserChangingProgress = true;
 await ProgressiveDelay();
 }

 protected async Task OnProgressChanged( AngryMonkey.CloudComponents.ProgressBarChangeEventArgs args)
 {
 if (Metadata.CurrentVideoInfo?.Duration == null)
 return;

 Metadata.IsSeeking = false;
 Metadata.ShowSeekingInfo = false;

 Repaint();

 if (args.PreviousValue.HasValue)
 {
 double durationDifference = args.NewValue - args.PreviousValue.Value;

 if (durationDifference > -1 && durationDifference <1)
 return;
 }

 await JS.InvokeVoidAsync("amcVideoPlayerChangeCurrentTime", ComponentElement, args.NewValue);

 Metadata.IsUserChangingProgress = false;

 if (Metadata.CurrentTime == Metadata.CurrentVideoInfo.Duration)
 await StopVideo();

 await ProgressiveDelay();
 }

 protected async Task OnProgressChanging( AngryMonkey.CloudComponents.ProgressBarChangeEventArgs args)
 {
 if (Metadata.CurrentVideoInfo == null || Metadata.CurrentVideoInfo.Duration == null)
 return;

 Metadata.IsSeeking = true;
 Metadata.ShowSeekingInfo = true;
 Repaint();
 Metadata.SeekInfoTime = args.NewValue;

 await JS.InvokeVoidAsync("amcVideoPlayerSeeking", ComponentElement, Metadata.SeekInfoTime, Metadata.CurrentVideoInfo.Duration);
 }

 protected async Task OnProgressMouseMove(MouseEventArgs args)
 {
 if (Metadata.IsSeeking || Metadata.VideoState == VideoStates.Error || Metadata.IsLive)
 return;

 Metadata.ShowSeekingInfo = true;
 Repaint();

 try
 {
 double? newValue = await JS.InvokeAsync<double?>("amcVideoPlayerSeeking", ComponentElement, args.ClientX);

 if (newValue == null || newValue <0)
 return;

 Metadata.SeekInfoTime = newValue.Value;
 }
 catch { }
 }

 protected async Task OnProgressMouseOut(MouseEventArgs args)
 {
 if (Metadata.IsSeeking)
 return;

 Metadata.ShowSeekingInfo = false;
 Repaint();
 }
 }
}
