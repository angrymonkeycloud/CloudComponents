using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace AngryMonkey.Cloud.Components
{
    public partial class VideoPlayer
    {
        internal void Repaint()
        {
            ProgressBarStyle = HideControls ? ProgressBarStyle.Flat : ProgressBarStyle.Circle;

            List<string> attributes = new();

            if (HideControls && !Metadata.IsCasting)
            {
                Metadata.DoShowVolumeControls = false;
                attributes.Add("_hidecontrols");
            }

            if (Metadata.IsVideoPlaying)
                attributes.Add("_playing");

            if (Metadata.IsFullScreen)
                attributes.Add("_fullscreen");

            if (Metadata.ShowSeekingInfo)
                attributes.Add("_showseekinginfo");

            if (Metadata.IsCasting)
                attributes.Add("_casting");

            ClassAttributes = string.Join(' ', attributes);
        }

        #region Settings Menu

        private async Task ChangePlaybackSpeed(double newSpeed)
        {
            PlaybackSpeed = newSpeed;

            var module = await Module;

            await module.InvokeVoidAsync("setVideoPlaybackSpeed", ComponentElement, PlaybackSpeed);

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

        #endregion

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

        protected async Task OnProgressChanged(ProgressBarChangeEventArgs args)
        {
            if (Metadata.CurrentVideoInfo?.Duration == null)
                return;

            Metadata.IsSeeking = false;
            Metadata.ShowSeekingInfo = false;

            Repaint();

            if (args.PreviousValue.HasValue)
            {
                double durationDifference = args.NewValue - args.PreviousValue.Value;

                if (durationDifference > -1 && durationDifference < 1)
                    return;
            }

            var module = await Module;

            await module.InvokeVoidAsync("changeCurrentTime", ComponentElement, args.NewValue);

            Metadata.IsUserChangingProgress = false;

            if (Metadata.CurrentTime == Metadata.CurrentVideoInfo.Duration)
                await StopVideo();

            await ProgressiveDelay();
        }

        protected async Task OnProgressChanging(ProgressBarChangeEventArgs args)
        {
            if (Metadata.CurrentVideoInfo == null || Metadata.CurrentVideoInfo.Duration == null)
                return;

            Metadata.IsSeeking = true;
            Metadata.ShowSeekingInfo = true;
            Repaint();
            Metadata.SeekInfoTime = args.NewValue;

            var module = await Module;
            await module.InvokeVoidAsync("seeking", ComponentElement, Metadata.SeekInfoTime, Metadata.CurrentVideoInfo.Duration);
        }

        protected async Task OnProgressMouseMove(MouseEventArgs args)
        {
            if (Metadata.IsSeeking || Metadata.HasError || Metadata.Status == VideoPlayerMetadata.VideoStatus.Streaming)
                return;

            Metadata.ShowSeekingInfo = true;
            Repaint();

            var module = await Module;

            double newValue = await module.InvokeAsync<double>("seeking", ComponentElement, args.ClientX);

            if (newValue < 0)
                return;

            Metadata.SeekInfoTime = newValue;
        }

        protected async Task OnProgressMouseOut(MouseEventArgs args)
        {
            if (Metadata.IsSeeking)
                return;

            Metadata.ShowSeekingInfo = false;
            Repaint();
        }

        public async Task OnVideoChange(ChangeEventArgs? args)
        {
            if (Metadata.HasError || string.IsNullOrEmpty(Metadata.VideoUrl))
                return;

            VideoEventData? eventData = null;

            if (args != null)
                try
                {
                    eventData = JsonSerializer.Deserialize<VideoEventData>((string)args.Value);
                }
                catch { }

            if (eventData != null)
            {
                Metadata.IsVideoPlaying = !eventData.State.Paused;
                Repaint();
            }

            if (!Metadata.VideoReady && !Metadata.StreamInitialized && RequireStreamInit())
            {
                Metadata.Status = VideoPlayerMetadata.VideoStatus.Streaming;
                Metadata.IsStream = true;
                await StopVideo();
                try
                {
                    var module = await Module;

                    bool isPlayableNatively = await module.InvokeAsync<bool>("IsStreamingPlayableNatively", ComponentElement);

                    if (!isPlayableNatively)
                        await module.InvokeAsync<string>("initializeStreamingUrl", ComponentElement, Metadata.VideoUrl);

                    Metadata.StreamInitialized = true;

                    await VideoLoaded();
                }
                catch
                {
                    if (Metadata.StreamInitialized)
                        return;

                    await OnVideoError.InvokeAsync();
                    Metadata.HasError = true;

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
                                Metadata.Status = VideoPlayerMetadata.VideoStatus.Stoped;

                                await OnVideoReady.InvokeAsync();

                                if (Metadata.Autoplay)
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
                        Metadata.Status = VideoPlayerMetadata.VideoStatus.Buffering;
                        break;

                    case VideoEvents.Playing:

                        if (!Metadata.IsCasting)
                            Metadata.Status = VideoPlayerMetadata.VideoStatus.Playing;
                        else
                        {
                            await PauseVideo();
                        }
                        break;

                    case VideoEvents.Play:
                        Metadata.Status = VideoPlayerMetadata.VideoStatus.Playing;
                        break;

                    case VideoEvents.Pause:
                        Metadata.Status = Metadata.CurrentTime == 0 ? VideoPlayerMetadata.VideoStatus.Stoped : VideoPlayerMetadata.VideoStatus.Paused;
                        break;

                    default: break;
                }

                await OnEvent.InvokeAsync(eventData);
            }
        }

        protected async Task OnEmptyTouch(TouchEventArgs args)
        {
            _isEmptyTouched = true;

            if (Metadata.IsVideoPlaying && !HideControls)
            {
                _forceHideControls = true;
                IsUserInteracting = false;
                Repaint();
            }
        }

        protected async Task OnEmptyClick(MouseEventArgs args)
        {
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

            if (Metadata.IsVideoPlaying)
                await PauseVideo();
            else await PlayVideo();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                if (!string.IsNullOrEmpty(Metadata.VideoUrl))
                    _videoUrl = Metadata.VideoUrl;

                await Init();

                if (Metadata.Autoplay)
                    await PlayVideo();
            }
        }

        private async Task Init()
        {
            var module = await Module;

            try
            {
                await module.InvokeVoidAsync("init", ComponentElement);
            }
            catch
            {
                await Task.Delay(100);
                await Init();
            }

            await Implement(VideoEvents.TimeUpdate);
            await Implement(VideoEvents.Play);
            await Implement(VideoEvents.Playing);
            await Implement(VideoEvents.Pause);
            await Implement(VideoEvents.Waiting);
            await Implement(VideoEvents.LoadedMetadata);
        }

        protected override async void OnParametersSet()
        {
            base.OnParametersSet();

            Metadata.Title = Metadata.Title;

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
                if (Metadata.IsVideoPlaying)
                    await StopVideo();

                bool wasError = Metadata.HasError;
                Metadata.VideoReady = false;

                try
                {
                    var module = await Module;
                    await module.InvokeAsync<string>("disposeStreaming");
                }
                catch { }

                Metadata.StreamInitialized = false;
                Metadata.IsStream = false;
                Metadata.HasError = false;
                _videoUrl = Metadata.VideoUrl;

                StateHasChanged();

                if (wasError)
                {
                    await Init();
                    await OnVideoChange(null);
                }

                if (Metadata.IsCasting)
                    await Cast();
            }
        }

        async Task Implement(VideoEvents eventName)
        {
            VideoStateOptions options = new() { All = true };
            VideoEventOptions?.TryGetValue(eventName, out options);

            var module = await Module;

            await module.InvokeVoidAsync("registerCustomEventHandler", ComponentElement, eventName.ToString().ToLower(), options.GetPayload());
        }

        public async Task VideoLoaded()
        {
            var module = await Module;

            Metadata.CurrentVideoInfo = await module.InvokeAsync<VideoInfo>("getVideoInfo", ComponentElement);
            Metadata.VideoReady = true;
            await CallReserveAspectRatio();
        }

        public async Task PlayVideo()
        {
            if (Metadata.HasError)
                return;

            if (Metadata.CurrentVideoInfo == null)
                await VideoLoaded();

            var module = await Module;

            await module.InvokeVoidAsync("play", ComponentElement);
        }

        public async Task PauseVideo()
        {
            var module = await Module;

            await module.InvokeVoidAsync("pause", ComponentElement);
        }

        public async Task EnterFullScreen()
        {
            var module = await Module;

            await module.InvokeVoidAsync("enterFullScreen", ComponentElement);
        }

        public async Task ExitFullScreen()
        {
            var module = await Module;

            await module.InvokeVoidAsync("exitFullScreen", ComponentElement);
        }

        public async Task OnFullScreenChange(EventArgs args)
        {
            Metadata.IsFullScreen = !Metadata.IsFullScreen;

            Repaint();
        }

        public async Task StopVideo()
        {
            Metadata.CurrentTime = 0;

            var module = await Module;

            await module.InvokeVoidAsync("stop", ComponentElement);
            Metadata.Status = VideoPlayerMetadata.VideoStatus.Stoped;

            Repaint();
        }

        public async ValueTask DisposeAsync()
        {
            if (_module != null)
            {
                var module = await _module;
                await module.DisposeAsync();
            }
        }

        private object _resizeListener = null;

        private async Task CallReserveAspectRatio()
        {
            while (Metadata.CurrentVideoInfo == null)
                await Task.Delay(1000);

            var module = await Module;

            if (Metadata.ReserveAspectRatio)
                _resizeListener = await module.InvokeAsync<object>("AddReserveAspectRatioListener", ComponentElement, Metadata.CurrentVideoInfo.Width, Metadata.CurrentVideoInfo.Height);
            else await module.InvokeVoidAsync("RemoveReserveAspectRatioListener", ComponentElement, _resizeListener);
        }

        protected async Task OnMouseWheel(WheelEventArgs args)
        {
            if (Metadata.DoShowVolumeControls)
            {
                double newValue;

                if (args.DeltaY < 0)
                    newValue = Metadata.Volume <= .9 ? Metadata.Volume + .1 : 1;
                else
                    newValue = Metadata.Volume >= .1 ? Metadata.Volume - .1 : 0;

                newValue = Math.Round(newValue, 1);

                //await OnVolumeChanging(new ProgressBarChangeEventArgs() { NewValue = newValue });
            }

            await ProgressiveDelay();
        }

        #region Volume Methods

        private async Task MuteVolume()
        {
            var module = await Module;

            await module.InvokeVoidAsync("muteVolume", ComponentElement, Metadata.IsMuted);
        }

        protected void OnVolumeButtonClick()
        {
            Metadata.DoShowVolumeControls = !Metadata.DoShowVolumeControls;
        }

        protected async Task OnVolumeChanging(VolumeBarChangeEventArgs args)
        {
            Metadata.Volume = args.NewValue;

            if (args.IsMuted != args.WasMuted)
            {
                Metadata.IsMuted = args.IsMuted;
                await MuteVolume();
            }

            var module = await Module;

            await module.InvokeVoidAsync("changeVolume", ComponentElement, Metadata.Volume);

            await ProgressiveDelay();
        }

        #endregion

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

            await Task.Delay(3000);

            if (id != latestId)
                return;

            IsUserInteracting = false;
            Repaint();
        }

        #region Validations

        //public async Task<bool> IsValidM3u8Url()
        //{
        //    using var httpClient = new HttpClient();
        //    var request = new HttpRequestMessage(HttpMethod.Head, VideoUrl);

        //    try
        //    {
        //        using HttpResponseMessage response = await httpClient.SendAsync(request);
        //        return response.Content.Headers.ContentType?.MediaType?.Equals("application/vnd.apple.mpegurl") ?? false;
        //    }
        //    catch { return false; }
        //}

        #endregion

        public VideoPlayerCast VideoPlayerCast { get; set; }

        private async Task Cast()
        {
            if (Metadata.IsFullScreen)
                await ExitFullScreen();

            await PauseVideo();

            if (Metadata.IsCasting)
                await VideoPlayerCast.StartCast();
            else Metadata.CastStatus = VideoPlayerMetadata.CastStatuses.Initializing;

            Repaint();
        }
    }
}
