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
            if (Metadata.IsSeeking || Metadata.VideoState == VideoStates.Error || Metadata.IsLive)
                return;

            Metadata.ShowSeekingInfo = true;
            Repaint();

            var module = await Module;

            try
            {
                double? newValue = await module.InvokeAsync<double?>("seeking", ComponentElement, args.ClientX);

                if (newValue == null || newValue < 0)
                    return;

                Metadata.SeekInfoTime = newValue.Value;
            }
            catch { }
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
            if (!Metadata.ShowVideoElement || Metadata.VideoState == VideoStates.Error || string.IsNullOrEmpty(Metadata.VideoUrl))
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
                //Metadata.IsVideoPlaying = !eventData.State.Paused;
                Repaint();
            }

            if (Metadata.VideoState != VideoStates.Ready && !Metadata.LiveInitialized && Metadata.IsLive)
            {
                await StopVideo();

                try
                {
                    var module = await Module;

                    bool isPlayableNatively = await module.InvokeAsync<bool>("IsStreamingPlayableNatively", ComponentElement);

                    if (!isPlayableNatively)
                        await module.InvokeAsync<string>("initializeStreamingUrl", ComponentElement, Metadata.VideoUrl);

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
                        {
                            await PauseVideo();
                        }
                        break;

                    case VideoEvents.Play:
                        Metadata.PlayingState = PlayingStates.Playing;
                        break;

                    case VideoEvents.Pause:
                        Metadata.PlayingState = Metadata.CurrentTime == 0 ? PlayingStates.NotPlaying : PlayingStates.Paused;
                        break;

                    default: break;
                }

                await OnEvent.InvokeAsync(eventData);
            }
        }

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

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await Init();

                if (string.IsNullOrEmpty(Metadata.VideoUrl))
                    return;

                _videoUrl = Metadata.VideoUrl;

                Metadata.IsLive = RequireStreamInit();

                if (Metadata.Autoplay)
                    await PlayVideo();
            }
        }

        private async Task Init()
        {
            if (!Metadata.ShowVideoElement)
                return;

            if (string.IsNullOrEmpty(Metadata.VideoUrl))
                Metadata.VideoState = VideoStates.NoVideo;
            else Metadata.VideoState = VideoStates.Loading;

            var module = await Module;

            try
            {
                await module.InvokeVoidAsync("init", ComponentElement);
            }
            catch (Exception e)
            {
                Metadata.VideoState = VideoStates.Error;

                //await Task.Delay(100);
                //await Init();
                //return;
            }

            await Implement(VideoEvents.TimeUpdate);
            await Implement(VideoEvents.Play);
            await Implement(VideoEvents.Playing);
            await Implement(VideoEvents.Pause);
            await Implement(VideoEvents.Waiting);
            await Implement(VideoEvents.LoadedMetadata);

            Metadata.VideoState = VideoStates.Ready;


            //await module.InvokeVoidAsync("checkPoster", ComponentElement);


            StateHasChanged();
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
                    var module = await Module;
                    await module.InvokeAsync<string>("disposeStreaming");
                }
                catch { }

                Metadata.LiveInitialized = false;
                Metadata.IsLive = RequireStreamInit();

                StateHasChanged();

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
            if (!Metadata.ShowVideoElement)
                return;

            var module = await Module;

            Metadata.CurrentVideoInfo = await module.InvokeAsync<VideoInfo>("getVideoInfo", ComponentElement);
            Metadata.VideoState = VideoStates.Ready;
            await CallReserveAspectRatio();
        }

        public async Task PlayVideo()
        {
            if (Metadata.VideoState == VideoStates.Error)
                return;

            if (Metadata.CurrentVideoInfo == null)
                await VideoLoaded();

            if (Metadata.ShowVideoElement)
            {
                var module = await Module;

                await module.InvokeVoidAsync("play", ComponentElement);
                Metadata.PlayingState = PlayingStates.Playing;
            }

            await OnAction.InvokeAsync(new() { Action = ActionCodes.Play });
        }

        public async Task PauseVideo()
        {
            if (Metadata.ShowVideoElement)
            {
                var module = await Module;

                await module.InvokeVoidAsync("pause", ComponentElement);
                Metadata.PlayingState = PlayingStates.Paused;
            }
            await OnAction.InvokeAsync(new() { Action = ActionCodes.Pause });
        }

        #region Full Screen

        public async Task EnterFullScreen()
        {
            if (Metadata.ShowVideoElement)
            {
                var module = await Module;

                await module.InvokeVoidAsync("enterFullScreen", ComponentElement);
            }

            await OnAction.InvokeAsync(new() { Action = ActionCodes.FullScreen });
        }

        public async Task ExitFullScreen()
        {
            if (Metadata.ShowVideoElement)
            {
                var module = await Module;

                await module.InvokeVoidAsync("exitFullScreen", ComponentElement);
            }

            await OnAction.InvokeAsync(new() { Action = ActionCodes.FullScreen });
        }

        public async Task OnFullScreenChange(EventArgs args)
        {
            Metadata.IsFullScreen = !Metadata.IsFullScreen;

            Repaint();
        }

        #endregion

        public async Task StopVideo()
        {
            Metadata.CurrentTime = 0;

            var module = await Module;

            try
            {
                await module.InvokeVoidAsync("stop", ComponentElement);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error while Stoping: {e.Message}");
            }

            Metadata.PlayingState = PlayingStates.NotPlaying;

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

        public async Task Cast()
        {
            if (Metadata.IsFullScreen)
                await ExitFullScreen();

            await PauseVideo();

            if (Metadata.ShowVideoElement)
            {
                if (Metadata.IsCasting)
                    await VideoPlayerCast.StartCast();
                else Metadata.CastStatus = VideoPlayerMetadata.CastStatuses.Initializing;
            }

            await OnAction.InvokeAsync(new() { Action = ActionCodes.Cast });

            Repaint();
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
