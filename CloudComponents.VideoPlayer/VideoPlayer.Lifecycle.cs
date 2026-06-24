using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CloudComponents.VideoPlayer
{
 public partial class VideoPlayer
 {
 protected override async Task OnAfterRenderAsync(bool firstRender)
 {
     if (firstRender)
     {
         await Init();

         if (string.IsNullOrEmpty(Metadata.VideoUrl))
             return;

         _videoUrl = Metadata.VideoUrl;

         Metadata.IsLive = RequireStreamInit();

         if (Metadata.IsLive)
         {
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
                 Metadata.VideoState = VideoStates.Error;
             }
         }

         if (Metadata.Autoplay)
             await PlayVideo();
     }
 }

 protected override async void OnParametersSet()
 {
 base.OnParametersSet();

 Metadata.Title = Metadata.Title;
 Metadata.Player = this;

 if (Metadata.ReserveAspectRatio != _reserveAspectRatio)
 {
 _reserveAspectRatio = Metadata.ReserveAspectRatio;

 await CallReserveAspectRatio();
 }

 if (Metadata.IsMuted != Metadata._isMuted)
 {
 Metadata._isMuted = Metadata.IsMuted;
 await MuteVolume();
 }

 if (Metadata.VideoUrl != _videoUrl)
 {
 _videoUrl = Metadata.VideoUrl;

 if (Metadata.IsPlayingState)
 await StopVideo();

 Metadata.VideoState = VideoStates.Loading;

 try
 {
 await JS.InvokeVoidAsync("amcVideoPlayerDisposeStreaming");
 }
 catch { }

 Metadata.LiveInitialized = false;
 Metadata.IsLive = RequireStreamInit();

 StateHasChanged();

 if (Metadata.IsCasting)
 await Cast();
 }
 }

 private async Task Init()
 {
 if (string.IsNullOrEmpty(Metadata.VideoUrl))
 Metadata.VideoState = VideoStates.NoVideo;
 else Metadata.VideoState = VideoStates.Loading;

 try
 {
 await JS.InvokeVoidAsync("amcVideoPlayerInit", ComponentElement);
 }
 catch (Exception e)
 {
 Metadata.VideoState = VideoStates.Error;
 }

 await Implement(VideoEvents.TimeUpdate);
 await Implement(VideoEvents.Play);
 await Implement(VideoEvents.Playing);
 await Implement(VideoEvents.Pause);
 await Implement(VideoEvents.Waiting);
 await Implement(VideoEvents.LoadedMetadata);

 Metadata.VideoState = VideoStates.Ready;

 StateHasChanged();
 }

 private async Task Implement(VideoEvents eventName)
 {
 VideoStateOptions options = new() { All = true };
 VideoEventOptions?.TryGetValue(eventName, out options);

 await JS.InvokeVoidAsync("amcVideoPlayerRegisterCustomEventHandler", ComponentElement, eventName.ToString().ToLower(), options.GetPayload());
 }

 private bool RequireStreamInit()
 {
 if (string.IsNullOrEmpty(Metadata?.VideoUrl))
 return false;

 string[] extentions = ["m3u8"];

 return extentions.Any(ex => new FileInfo(Metadata.VideoUrl).Extension.StartsWith($".{ex}", StringComparison.OrdinalIgnoreCase));
 }

 public async Task VideoLoaded()
 {
 Metadata.CurrentVideoInfo = await JS.InvokeAsync<VideoInfo>("amcVideoPlayerGetVideoInfo", ComponentElement);
 Metadata.VideoState = VideoStates.Ready;
 await CallReserveAspectRatio();
 }
 }
}
