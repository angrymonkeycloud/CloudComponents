using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace CloudComponents.VideoPlayer
{
 public partial class VideoPlayer
 {
 public async Task PlayVideo()
 {
 if (Metadata.VideoState == VideoStates.Error)
 return;

 if (Metadata.CurrentVideoInfo == null)
 await VideoLoaded();

 await JS.InvokeVoidAsync("amcVideoPlayerPlay", ComponentElement);
 Metadata.PlayingState = PlayingStates.Playing;

 await OnAction.InvokeAsync(new() { Action = ActionCodes.Play });
 }

 public async Task PauseVideo()
 {
 await JS.InvokeVoidAsync("amcVideoPlayerPause", ComponentElement);
 Metadata.PlayingState = PlayingStates.Paused;

 await OnAction.InvokeAsync(new() { Action = ActionCodes.Pause });
 }

 public async Task StopVideo()
 {
 Metadata.CurrentTime =0;

 try
 {
 await JS.InvokeVoidAsync("amcVideoPlayerStop", ComponentElement);
 }
 catch (Exception e)
 {
 Console.WriteLine($"Error while Stoping: {e.Message}");
 }

 Metadata.PlayingState = PlayingStates.NotPlaying;

 Repaint();

 await OnAction.InvokeAsync(new() { Action = ActionCodes.Stop });
 }
 }
}
