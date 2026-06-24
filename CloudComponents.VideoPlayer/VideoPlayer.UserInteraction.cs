using Microsoft.AspNetCore.Components.Web;
using System;
using System.Threading.Tasks;

namespace CloudComponents.VideoPlayer
{
 public partial class VideoPlayer
 {
 protected async Task OnEmptyTouch(TouchEventArgs args)
 {
 _isEmptyTouched = true;

 if (Metadata.IsPlayingState && !HideControls)
 {
 _forceHideControls = true;
 IsUserInteracting = false;
 Repaint();
 }
 }

 protected async Task OnEmptyClick(MouseEventArgs args)
 {
 if (Metadata.VideoState != VideoStates.Ready)
 return;

 if (ShowSideBar == true)
 {
 if (ShowSideBarMenu)
 ShowSideBar = false;
 else ResetSettingsMenu();

 return;
 }

 if (_isEmptyTouched)
 {
 _isEmptyTouched = false;

 return;
 }

 if (Metadata.PlayingState == PlayingStates.Playing)
 await PauseVideo();
 else await PlayVideo();
 }

 private async Task OnComponentClick(MouseEventArgs args)
 {
 if (_forceHideControls)
 {
 _forceHideControls = false;
 return;
 }

 await ProgressiveDelay();
 }

 public async Task MainMouseMove(MouseEventArgs args)
 {
 if (_forceHideControls)
 return;

 await ProgressiveDelay();
 }

 private async Task ProgressiveDelay()
 {
 IsUserInteracting = true;

 Repaint();

 Guid id = Guid.NewGuid();
 latestId = id;

 await Task.Delay(2000);

 if (id != latestId)
 return;

 IsUserInteracting = false;
 Repaint();
 }
 }
}
