using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        private void Repaint()
        {
            ProgressBarStyle = HideControls ? ProgressBarStyle.Flat : ProgressBarStyle.Circle;

            List<string> attributes = new();

            if (HideControls)
            {
                DoShowVolumeControls = false;
                attributes.Add("_hidecontrols");
            }

            if (IsVideoPlaying)
                attributes.Add("_playing");

            if (IsFullScreen)
                attributes.Add("_fullscreen");

            if (ShowSeekingInfo)
                attributes.Add("_showseekinginfo");

            ClassAttributes = string.Join(' ', attributes);
        }

        #region Time / Duration

        private string GetTime(double? seconds)
        {
            if (seconds == null)
                return string.Empty;

            TimeSpan time = TimeSpan.FromSeconds(seconds.Value);
            int timeLevel = GetTimeLevel();

            string result = $"{time:ss}";

            if (timeLevel > 0)
            {
                result = $"{time:mm}:{result}";

                if (timeLevel > 1)
                {
                    result = $"{time:hh}:{result}";

                    if (timeLevel > 2)
                        result = $"{time:dd}:{result}";
                }
            }

            return result[0] == '0' ? result.Remove(0, 1) : result;
        }

        private int GetTimeLevel()
        {
            TimeSpan time = TimeSpan.FromSeconds(CurrentVideoInfo?.Duration ?? 0);

            if (time.TotalMinutes < 1)
                return 0;

            if (time.TotalHours < 1)
                return 1;

            if (time.TotalDays < 1)
                return 2;

            return 3;
        }

        #endregion

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
            Loop = !Loop;

            ShowSideBarLoop = false;
        }

        #endregion

        protected async Task OnProgressMouseDown(MouseEventArgs args)
        {
            IsUserChangingProgress = true;
            await ProgressiveDelay();
        }

        protected async Task OnProgressTouchStart(TouchEventArgs args)
        {
            IsUserChangingProgress = true;
            await ProgressiveDelay();
        }

        protected async Task OnProgressChanged(ProgressBarChangeEventArgs args)
        {
            if (CurrentVideoInfo == null || CurrentVideoInfo.Duration == null)
                return;

            IsSeeking = false;
            ShowSeekingInfo = false;

            Repaint();

            if (args.PreviousValue.HasValue)
            {
                double durationDifference = args.NewValue - args.PreviousValue.Value;

                if (durationDifference > -1 && durationDifference < 1)
                    return;
            }

            var module = await Module;

            await module.InvokeVoidAsync("changeCurrentTime", ComponentElement, args.NewValue);

            IsUserChangingProgress = false;

            if (CurrentTime == CurrentVideoInfo.Duration)
                await StopVideo();

            await ProgressiveDelay();
        }

        protected async Task OnProgressChanging(ProgressBarChangeEventArgs args)
        {
            if (CurrentVideoInfo == null || CurrentVideoInfo.Duration == null)
                return;

            IsSeeking = true;
            ShowSeekingInfo = true;
            Repaint();
            SeekInfoTime = args.NewValue;

            var module = await Module;
            await module.InvokeVoidAsync("seeking", ComponentElement, SeekInfoTime, CurrentVideoInfo.Duration);
        }

        protected async Task OnProgressMouseMove(MouseEventArgs args)
        {
            if (IsSeeking || HasError || Status == VideoStatus.Streaming)
                return;

            ShowSeekingInfo = true;
            Repaint();

            var module = await Module;

            double newValue = await module.InvokeAsync<double>("seeking", ComponentElement, args.ClientX);

            if (newValue < 0)
                return;

            SeekInfoTime = newValue;
        }

        protected async Task OnProgressMouseOut(MouseEventArgs args)
        {
            if (IsSeeking)
                return;

            ShowSeekingInfo = false;
            Repaint();
        }

        public async Task OnVideoChange(ChangeEventArgs? args)
        {
            if (HasError || string.IsNullOrEmpty(VideoUrl))
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
                IsVideoPlaying = !eventData.State.Paused;
                Repaint();
            }

            if (!VideoReady && !StreamInitialized && RequireStreamInit())
            {
                Status = VideoStatus.Streaming;
                IsStream = true;
                await StopVideo();
                try
                {
                    var module = await Module;

                    bool isPlayableNatively = await module.InvokeAsync<bool>("IsStreamingPlayableNatively", ComponentElement);

                    if (!isPlayableNatively)
                        await module.InvokeAsync<string>("initializeStreamingUrl", ComponentElement, VideoUrl);

                    StreamInitialized = true;

                    await VideoLoaded();
                }
                catch
                {
                    if (StreamInitialized)
                        return;

                    await OnVideoError.InvokeAsync();
                    HasError = true;

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

                            if (CurrentVideoInfo == null)
                                await Task.Delay(200);
                            else
                            {
                                Status = VideoStatus.Stoped;

                                await OnVideoReady.InvokeAsync();

                                if (Autoplay)
                                    await PlayVideo();
                            }
                        } while (CurrentVideoInfo == null);
                        break;

                    case VideoEvents.TimeUpdate:

                        if (!IsUserChangingProgress)
                        {
                            CurrentTime = eventData.State.CurrentTime;

                            if (CurrentVideoInfo == null || CurrentVideoInfo.Duration == null)
                                return;

                            if (CurrentTime == CurrentVideoInfo.Duration)
                            {
                                await StopVideo();

                                if (Loop)
                                    await PlayVideo();
                            }
                        }
                        break;

                    case VideoEvents.Waiting:
                        Status = VideoStatus.Buffering;
                        break;

                    case VideoEvents.Playing:
                        Status = VideoStatus.Playing;
                        break;

                    case VideoEvents.Play:
                        Status = VideoStatus.Playing;
                        break;

                    case VideoEvents.Pause:
                        Status = CurrentTime == 0 ? VideoStatus.Stoped : VideoStatus.Paused;
                        break;

                    default: break;
                }

                await OnEvent.InvokeAsync(eventData);
            }
        }

        protected async Task OnEmptyTouch(TouchEventArgs args)
        {
            _isEmptyTouched = true;

            if (IsVideoPlaying && !HideControls)
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

            if (IsVideoPlaying)
                await PauseVideo();
            else await PlayVideo();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                if (!string.IsNullOrEmpty(VideoUrl))
                    _videoUrl = VideoUrl;

                await Init();

                if (Autoplay)
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

            if (ReserveAspectRatio != _reserveAspectRatio)
            {
                _reserveAspectRatio = ReserveAspectRatio;

                await CallReserveAspectRatio();
            }

            if (IsMuted != _isMuted)
            {
                _isMuted = IsMuted;
                await MuteVolume();
            }

            if (VideoUrl != _videoUrl)
            {
                if (IsVideoPlaying)
                    await StopVideo();

                bool wasError = HasError;
                VideoReady = false;

                try
                {
                    var module = await Module;
                    await module.InvokeAsync<string>("disposeStreaming");
                }
                catch { }

                StreamInitialized = false;
                IsStream = false;
                HasError = false;
                _videoUrl = VideoUrl;

                StateHasChanged();

                if (wasError)
                {
                    await Init();
                    await OnVideoChange(null);
                }
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

            CurrentVideoInfo = await module.InvokeAsync<VideoInfo>("getVideoInfo", ComponentElement);
            VideoReady = true;
            await CallReserveAspectRatio();
        }

        public async Task PlayVideo()
        {
            if (HasError)
                return;

            if (CurrentVideoInfo == null)
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
            IsFullScreen = !IsFullScreen;

            Repaint();
        }

        public async Task StopVideo()
        {
            CurrentTime = 0;

            var module = await Module;

            await module.InvokeVoidAsync("stop", ComponentElement);
            Status = VideoStatus.Stoped;

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
            while (CurrentVideoInfo == null)
                await Task.Delay(1000);

            var module = await Module;

            if (ReserveAspectRatio)
                _resizeListener = await module.InvokeAsync<object>("AddReserveAspectRatioListener", ComponentElement, CurrentVideoInfo.Width, CurrentVideoInfo.Height);
            else await module.InvokeVoidAsync("RemoveReserveAspectRatioListener", ComponentElement, _resizeListener);
        }

        protected async Task OnMouseWheel(WheelEventArgs args)
        {
            if (DoShowVolumeControls)
            {
                double newValue;

                if (args.DeltaY < 0)
                    newValue = Volume <= .9 ? Volume + .1 : 1;
                else
                    newValue = Volume >= .1 ? Volume - .1 : 0;

                newValue = Math.Round(newValue, 1);

                //await OnVolumeChanging(new ProgressBarChangeEventArgs() { NewValue = newValue });
            }

            await ProgressiveDelay();
        }

        #region Volume Methods

        private async Task MuteVolume()
        {
            var module = await Module;

            await module.InvokeVoidAsync("muteVolume", ComponentElement, IsMuted);
        }

        protected void OnVolumeButtonClick()
        {
            DoShowVolumeControls = !DoShowVolumeControls;
        }

        protected async Task OnVolumeChanging(VolumeBarChangeEventArgs args)
        {
            Volume = args.NewValue;

            if (args.IsMuted != args.WasMuted)
            {
                IsMuted = args.IsMuted;
                await MuteVolume();
            }

            var module = await Module;

            await module.InvokeVoidAsync("changeVolume", ComponentElement, Volume);

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
    }
}
