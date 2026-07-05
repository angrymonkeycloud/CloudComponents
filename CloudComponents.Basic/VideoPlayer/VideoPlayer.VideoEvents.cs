using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.Json;
using System.Threading.Tasks;

namespace CloudComponents.Basic
{
 public partial class VideoPlayer
 {
 public async Task OnVideoChange(ChangeEventArgs args)
 {
 if (Metadata.VideoState == VideoStates.Error || string.IsNullOrEmpty(Metadata.VideoUrl))
 return;

 VideoEventData? eventData = null;

 if (args is not null)
 try
 {
 eventData = JsonSerializer.Deserialize<VideoEventData>((string)args.Value);
 }
 catch { }

 if (eventData != null)
 {
 //Metadata.IsVideoPlaying = !eventData.State.Paused;
 Repaint();
 }

 if (Metadata.VideoState != VideoStates.Ready && !Metadata.LiveInitialized && Metadata.IsLive)
 {
 await StopVideo();

 try
 {
 Metadata.LivePlaysNatively = await JS.InvokeAsync<bool>("amcVideoPlayerIsStreamingPlayableNatively", ComponentElement);

 if (!Metadata.LivePlaysNatively)
 await JS.InvokeAsync<string>("amcVideoPlayerInitializeStreamingUrl", ComponentElement, Metadata.VideoUrl);

 Metadata.LiveInitialized = true;

 await VideoLoaded();
 }
 catch
 {
 if (Metadata.LiveInitialized)
 return;

 Metadata.VideoState = VideoStates.Error;

 return;
 }
 }

 if (eventData != null)
 {
 switch (eventData.EventName)
 {
 case VideoEvents.LoadedMetadata:
 do
 {
 await VideoLoaded();

 if (Metadata.CurrentVideoInfo == null)
 await Task.Delay(200);
 else
 {
 Metadata.PlayingState = PlayingStates.NotPlaying;
 Metadata.VideoState = VideoStates.Ready;

 if (Metadata.Autoplay && !Metadata.IsCasting)
 await PlayVideo();
 }
 } while (Metadata.CurrentVideoInfo == null);
 break;

 case VideoEvents.TimeUpdate:

 if (!Metadata.IsUserChangingProgress)
 {
 Metadata.CurrentTime = eventData.State.CurrentTime;

 if (Metadata.CurrentVideoInfo?.Duration == null)
 return;

 if (Metadata.CurrentTime == Metadata.CurrentVideoInfo.Duration)
 {
 await StopVideo();

 if (Metadata.Loop)
 await PlayVideo();
 }
 }
 break;

 case VideoEvents.Waiting:
 Metadata.PlayingState = PlayingStates.Buffering;
 break;

 case VideoEvents.Playing:

 if (!Metadata.IsCasting)
 Metadata.PlayingState = PlayingStates.Playing;
 else
 await PauseVideo();

 break;

 case VideoEvents.Play:
 Metadata.PlayingState = PlayingStates.Playing;
 break;

 case VideoEvents.Pause:
 Metadata.PlayingState = Metadata.CurrentTime ==0 ? PlayingStates.NotPlaying : PlayingStates.Paused;
 break;

 default: break;
 }

 await OnEvent.InvokeAsync(eventData);
 }
 }
 }
}
