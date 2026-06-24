using Microsoft.JSInterop;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CloudComponents.VideoPlayer
{
 public partial class VideoPlayer
 {
 private async Task ChangePlaybackSpeed(double newSpeed)
 {
 PlaybackSpeed = newSpeed;

 await JS.InvokeVoidAsync("amcVideoPlayerSetVideoPlaybackSpeed", ComponentElement, PlaybackSpeed);

 ShowSideBarPlaybackSpeed = false;
 }

 public void ResetSettingsMenu()
 {
 ShowSideBarInfo = false;
 ShowSideBarPlaybackSpeed = false;
 ShowSideBarLoop = false;
 }

 public async Task MoreButtonInfo()
 {
 ResetSettingsMenu();
 ShowSideBar = !ShowSideBar;
 }

 public void ShowVideoInfo()
 {
 ShowSideBarInfo = true;
 }

 public void ShowVideoPlaybackSpeedOptions()
 {
 ShowSideBarPlaybackSpeed = true;
 }

 public void ShowVideoLoop()
 {
 ShowSideBarLoop = true;
 }

 protected void ChangeLoop()
 {
 Metadata.Loop = !Metadata.Loop;

 ShowSideBarLoop = false;
 }
 }
}
