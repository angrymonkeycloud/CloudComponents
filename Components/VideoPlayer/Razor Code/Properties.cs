using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.IO;
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

        [Parameter] public required VideoPlayerMetadata Metadata { get; set; }

        private bool RequireStreamInit()
        {
            string[] extentions = new[] { "m3u8" };

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

        private string DisplayVolume
        {
            get
            {
                return $"{Metadata.Volume * 100}";
            }
        }


        private ProgressBarStyle ProgressBarStyle = ProgressBarStyle.Circle;


        private bool IsUserInteracting = false;
        private bool ShowBottomSections => Metadata.VideoReady;
        private bool HideControls => Metadata.IsVideoPlaying && !IsUserInteracting && !Metadata.IsUserChangingProgress && !ShowSideBar;
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
        [Parameter] public EventCallback<VideoState> TimeUpdateEvent { get; set; }
        [Parameter] public EventCallback OnVideoError { get; set; }
        [Parameter] public EventCallback OnVideoReady { get; set; }

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

    }
}
