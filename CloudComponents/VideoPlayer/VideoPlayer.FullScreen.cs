using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace CloudComponents.Basic
{
 public partial class VideoPlayer
 {
 public async Task EnterFullScreen()
 {
 await JS.InvokeVoidAsync("amcVideoPlayerEnterFullScreen", ComponentElement);

 await OnAction.InvokeAsync(new() { Action = ActionCodes.FullScreen });
 }

 public async Task ExitFullScreen()
 {
 await JS.InvokeVoidAsync("amcVideoPlayerExitFullScreen", ComponentElement);

 await OnAction.InvokeAsync(new() { Action = ActionCodes.FullScreen });
 }

 public async Task OnFullScreenChange(EventArgs args)
 {
 Metadata.IsFullScreen = !Metadata.IsFullScreen;

 Repaint();
 }
 }
}
