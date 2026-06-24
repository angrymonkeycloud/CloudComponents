using System.Collections.Generic;
using System.Threading.Tasks;

namespace CloudComponents.VideoPlayer
{
 public partial class VideoPlayer
 {
 internal void Repaint()
 {
 ProgressBarStyle = HideControls ? AngryMonkey.Cloud.Components.ProgressBarStyle.Flat : AngryMonkey.Cloud.Components.ProgressBarStyle.Circle;

 List<string> attributes = [];

 if (HideControls && !Metadata.IsCasting)
 {
 Metadata.DoShowVolumeControls = false;
 attributes.Add("_hidecontrols");
 }

 if (Metadata.PlayingState == PlayingStates.Playing)
 attributes.Add("_playing");

 if (Metadata.IsFullScreen)
 attributes.Add("_fullscreen");

 if (Metadata.IsFullScreen)
 attributes.Add("_error");

 if (Metadata.ShowSeekingInfo)
 attributes.Add("_showseekinginfo");

 if (Metadata.IsCasting)
 attributes.Add("_casting");

 attributes.Add($"_{Metadata.VideoState.ToString().ToLower()}");

 ClassAttributes = string.Join(' ', attributes);
 }

 public async Task UpdatedExternally()
 {
 await InvokeAsync(() =>
 {
 Repaint();
 StateHasChanged();
 });
 }
 }
}
