using System.Threading.Tasks;

namespace CloudComponents.Basic
{
 public partial class VideoPlayer
 {
 public async Task Cast()
 {
 if (Metadata.IsFullScreen)
 await ExitFullScreen();

 await PauseVideo();

 if (Metadata.IsCasting)
 await VideoPlayerCast?.StartCast();
 else Metadata.CastStatus = VideoPlayerMetadata.CastStatuses.Initializing;

 await OnAction.InvokeAsync(new() { Action = ActionCodes.Cast });

 Repaint();
 }
 }
}
