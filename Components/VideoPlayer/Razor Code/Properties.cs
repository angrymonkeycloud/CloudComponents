using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
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
        private ElementReference ComponentElement { get; set; }

        private Task<IJSObjectReference> _module;
        private Task<IJSObjectReference> Module => _module ??= GeneralMethods.GetIJSObjectReference(jsRuntime, "videoplayer/videoplayer.js");

        private string ClassAttributes { get; set; } = string.Empty;

        private bool IsUserChangingProgress = false;
        private bool IsVideoPlaying = false;
        private bool IsFullScreen = false;

        private bool _isMuted = false;
        private bool IsMuted = false;


        private bool DoShowVolumeControls = false;
        private bool IsSeeking = false;
        private bool ShowSeekingInfo = false;
        private bool IsStream { get; set; }
        private bool StreamInitialized { get; set; } = false;

        private bool RequireStreamInit()
        {
            string[] extentions = new[] { "m3u8" };

            return extentions.Any(ex => VideoUrl.EndsWith($".{ex}", StringComparison.OrdinalIgnoreCase));
        }

        //private void UpdateStreamValue()
        //{
        //    string[] extentions = new[] { "m3u8" };

        //    IsStream = extentions.Any(ex => VideoUrl.EndsWith($".{ex}", StringComparison.OrdinalIgnoreCase));
        //}

        private bool ShowProgressBar => !IsStream;
        private bool ShowDuration => !IsStream;

        private Dictionary<string, string> VideoSettingsInfo
        {
            get
            {
                Dictionary<string, string> info = new();

                if (!string.IsNullOrEmpty(Title))
                    info.Add("Title", Title);

                if (CurrentVideoInfo != null)
                {
                    info.Add("Duration", GetTime(CurrentVideoInfo.Duration));
                    info.Add("Aspect Ratio", CurrentVideoInfo.DisplayAspectRatio);
                }

                info.Add("Status", Status.ToString());

                return info;
            }
        }

        private VideoInfo? CurrentVideoInfo { get; set; }

        private bool IsUserInteracting = false;

        private bool HideControls => IsVideoPlaying && !IsUserInteracting && !IsUserChangingProgress && !ShowSideBar;
        [Parameter] public string Title { get; set; }
        [Parameter] public bool Loop { get; set; } = false;
        [Parameter] public bool Autoplay { get; set; } = false;
        [Parameter] public bool ShowStopButton { get; set; } = false;

        public bool EnableLoop => !IsStream;

        private bool _reserveAspectRatio = false;
        [Parameter] public bool ReserveAspectRatio { get; set; } = false;

        private string DisplayLoop => Loop ? "On" : "Off";

        private string _videoUrl { get; set; }
        [Parameter] public string VideoUrl { get; set; }

        [Parameter] public double Volume { get; set; } = 1;

        private string DisplayVolume
        {
            get
            {
                return $"{Volume * 100}";
            }
        }

        public double CurrentTime { get; set; } = 0;

        private ProgressBarStyle ProgressBarStyle = ProgressBarStyle.Circle;

        [Parameter] public Action<VideoState> TimeUpdate { get; set; }

        [Parameter] public EventCallback<VideoState> TimeUpdateEvent { get; set; }
        bool TimeUpdateRequired => TimeUpdate is object;
        bool TimeUpdateEventRequired => TimeUpdateEvent.HasDelegate;
        bool EventFiredEventRequired => EventFiredEvent.HasDelegate;
        bool EventFiredRequired => EventFired is object;
        [Parameter] public Action<VideoEventData> EventFired { get; set; }
        [Parameter] public EventCallback<VideoEventData> EventFiredEvent { get; set; }
        [Parameter] public Dictionary<VideoEvents, VideoStateOptions> VideoEventOptions { get; set; }
        bool RegisterEventFired => EventFiredEventRequired || EventFiredRequired;

        [Parameter] public VideoPlayerSettings Settings { get; set; }

        private Guid latestId = Guid.Empty;

        #region Time / Duration

        private string DisplayTimeDuration => $"{GetTime(CurrentTime)} / {GetTime(CurrentVideoInfo?.Duration ?? 0)}";

        public double SeekInfoTime { get; set; }
        private string DisplaySeekInfoTime => GetTime(SeekInfoTime);

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

        private VideoStatus Status { get; set; } = VideoStatus.Loading;

        private enum VideoStatus
        {
            Loading,
            Playing,
            Paused,
            Stoped,
            Buffering,
            Streaming,
            Unknown
        }

        private bool _isEmptyTouched = false;
        private bool _forceHideControls = false;

    }
}
