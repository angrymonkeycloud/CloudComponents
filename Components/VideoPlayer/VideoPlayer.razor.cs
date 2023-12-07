using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        #region Properties

        [Parameter] public RenderFragment? ChildContent { get; set; }

        private string? _Poster { get; set; }
        private bool ShowContent => Metadata.EnableControls || ChildContent != null;

        [Parameter]
        public string? Poster
        {
            get => _Poster;
            set
            {
                if (_Poster == value)
                    return;

                _Poster = value;

                if (string.IsNullOrEmpty(_Poster))
                    Task.Run(async () =>
                    {
                        await JS.InvokeVoidAsync("amcVideoPlayerRemovePoster", ComponentElement);
                        await InvokeAsync(StateHasChanged);
                    });
            }
        }

        private ElementReference ComponentElement { get; set; }

        private string ClassAttributes { get; set; } = string.Empty;

        [Parameter] public required VideoPlayerMetadata Metadata { get; set; }

        private bool RequireStreamInit()
        {
            if (string.IsNullOrEmpty(Metadata?.VideoUrl))
                return false;

            string[] extentions = ["m3u8"];

            return extentions.Any(ex => new FileInfo(Metadata.VideoUrl).Extension.StartsWith($".{ex}", StringComparison.OrdinalIgnoreCase));
        }

        //private void UpdateStreamValue()
        //{
        //    string[] extentions = new[] { "m3u8" };

        //    IsStream = extentions.Any(ex => VideoUrl.EndsWith($".{ex}", StringComparison.OrdinalIgnoreCase));
        //}

        [Parameter] public Action<VideoState> TimeUpdate { get; set; }


        private bool _reserveAspectRatio = false;

        private string DisplayLoop => Metadata.Loop ? "On" : "Off";

        private string? _videoUrl { get; set; }

        private string DisplayVolume => $"{Metadata.Volume * 100}";


        private ProgressBarStyle ProgressBarStyle = ProgressBarStyle.Circle;


        private bool IsUserInteracting = false;
        private bool ShowBottomSections => Metadata.VideoState == VideoStates.Ready;
        private bool HideControls => Metadata.IsPlayingState && !IsUserInteracting && !Metadata.IsUserChangingProgress && !ShowSideBar;
        bool TimeUpdateRequired => TimeUpdate is object;
        bool TimeUpdateEventRequired => TimeUpdateEvent.HasDelegate;
        bool EventFiredEventRequired => OnEvent.HasDelegate;
        bool EventFiredRequired => EventFired is object;
        [Parameter] public Action<VideoEventData> EventFired { get; set; }
        [Parameter] public Dictionary<VideoEvents, VideoStateOptions> VideoEventOptions { get; set; }
        bool RegisterEventFired => EventFiredEventRequired || EventFiredRequired;

        [Parameter] public VideoPlayerSettings Settings { get; set; }

        private Guid latestId = Guid.Empty;

        #region Events Callbacks

        [Parameter] public EventCallback<VideoEventData> OnEvent { get; set; }
        [Parameter] public EventCallback<PlayerAction> OnAction { get; set; }
        [Parameter] public EventCallback<VideoState> TimeUpdateEvent { get; set; }
        [Parameter] public EventCallback OnVideoError { get; set; }
        [Parameter] public EventCallback OnVideoReady { get; set; }

        [Parameter] public EventCallback<PlayingStates> OnPlayingStateChanged { get; set; }
        [Parameter] public EventCallback<VideoStates> OnVideoStateChanged { get; set; }
        #endregion

        #region Settings Menu

        private bool ShowSideBar = false;
        private bool ShowSideBarInfo = false;
        private bool ShowSideBarPlaybackSpeed = false;
        private bool ShowSideBarLoop = false;
        private bool ShowSideBarMenu => !ShowSideBarInfo && !ShowSideBarPlaybackSpeed && !ShowSideBarLoop;

        private double PlaybackSpeed = 1;
        private Dictionary<double, string> PlaybackSpeedOptions = new() { { 0.25, "0.25" }, { 0.5, "0.5" }, { 0.75, "0.75" }, { 1, "Normal" }, { 1.25, "1.25" }, { 1.5, "1.5" }, { 1.75, "1.75" }, { 2, "2" } };
        private string DisplayPlaybackSpeed => PlaybackSpeedOptions[PlaybackSpeed];

        #endregion


        private bool _isEmptyTouched = false;
        private bool _forceHideControls = false;


        #endregion

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

            attributes.Add($"_{Metadata.VideoState.ToString().ToLower()}");

            ClassAttributes = string.Join(' ', attributes);
        }

        #region Settings Menu

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

            await JS.InvokeVoidAsync("amcVideoPlayerChangeCurrentTime", ComponentElement, args.NewValue);

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

            await JS.InvokeVoidAsync("amcVideoPlayerSeeking", ComponentElement, Metadata.SeekInfoTime, Metadata.CurrentVideoInfo.Duration);
        }

        protected async Task OnProgressMouseMove(MouseEventArgs args)
        {
            if (Metadata.IsSeeking || Metadata.VideoState == VideoStates.Error || Metadata.IsLive)
                return;

            Metadata.ShowSeekingInfo = true;
            Repaint();

            try
            {
                double? newValue = await JS.InvokeAsync<double?>("amcVideoPlayerSeeking", ComponentElement, args.ClientX);

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
            if (Metadata.VideoState == VideoStates.Error || string.IsNullOrEmpty(Metadata.VideoUrl))
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
            await base.OnAfterRenderAsync(firstRender);

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

        async Task Implement(VideoEvents eventName)
        {
            VideoStateOptions options = new() { All = true };
            VideoEventOptions?.TryGetValue(eventName, out options);

            await JS.InvokeVoidAsync("amcVideoPlayerRegisterCustomEventHandler", ComponentElement, eventName.ToString().ToLower(), options.GetPayload());
        }

        public async Task VideoLoaded()
        {
            Metadata.CurrentVideoInfo = await JS.InvokeAsync<VideoInfo>("amcVideoPlayerGetVideoInfo", ComponentElement);
            Metadata.VideoState = VideoStates.Ready;
            await CallReserveAspectRatio();
        }

        public async Task PlayVideo()
        {
            if (Metadata.VideoState == VideoStates.Error)
                return;

            if (Metadata.CurrentVideoInfo == null)
                await VideoLoaded();

            await JS.InvokeVoidAsync("amcVideoPlayerPlay", ComponentElement);
            Metadata.PlayingState = PlayingStates.Playing;

            await OnAction.InvokeAsync(new() { Action = ActionCodes.Play });
        }

        public async Task PauseVideo()
        {
            await JS.InvokeVoidAsync("amcVideoPlayerPause", ComponentElement);
            Metadata.PlayingState = PlayingStates.Paused;

            await OnAction.InvokeAsync(new() { Action = ActionCodes.Pause });
        }

        #region Full Screen

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

        #endregion

        public async Task StopVideo()
        {
            Metadata.CurrentTime = 0;

            try
            {
                await JS.InvokeVoidAsync("amcVideoPlayerStop", ComponentElement);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error while Stoping: {e.Message}");
            }

            Metadata.PlayingState = PlayingStates.NotPlaying;

            Repaint();

            await OnAction.InvokeAsync(new() { Action = ActionCodes.Stop });
        }

        private object _resizeListener = null;

        private async Task CallReserveAspectRatio()
        {
            while (Metadata.CurrentVideoInfo == null)
                await Task.Delay(1000);

            if (Metadata.ReserveAspectRatio)
                _resizeListener = await JS.InvokeAsync<object>("amcVideoPlayerAddReserveAspectRatioListener", ComponentElement, Metadata.CurrentVideoInfo.Width, Metadata.CurrentVideoInfo.Height);
            else await JS.InvokeVoidAsync("amcVideoPlayerRemoveReserveAspectRatioListener", ComponentElement, _resizeListener);
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
            await JS.InvokeVoidAsync("amcVideoPlayerMuteVolume", ComponentElement, Metadata.IsMuted);
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

            await JS.InvokeVoidAsync("amcVideoPlayerChangeVolume", ComponentElement, Metadata.Volume);

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

            await Task.Delay(2000);

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

            if (Metadata.IsCasting)
                await VideoPlayerCast.StartCast();
            else Metadata.CastStatus = VideoPlayerMetadata.CastStatuses.Initializing;

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
