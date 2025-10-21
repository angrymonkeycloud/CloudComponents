using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AngryMonkey.Cloud.Components
{
 public partial class VideoPlayer
 {
 #region Properties

 [Parameter] public RenderFragment? ChildContent { get; set; }

 private string? _Poster;
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
 await JSRuntimeExtensions.InvokeVoidAsync(JS, "amcVideoPlayerRemovePoster", ComponentElement);
 await InvokeAsync(StateHasChanged);
 });
 }
 }

 private ElementReference ComponentElement { get; set; }

 private string ClassAttributes { get; set; } = string.Empty;

 [Parameter] public required VideoPlayerMetadata Metadata { get; set; }

 [Parameter] public Action<VideoState> TimeUpdate { get; set; }

 private bool _reserveAspectRatio = false;

 private string DisplayLoop => Metadata.Loop ? "On" : "Off";

 private string? _videoUrl { get; set; }

 private string DisplayVolume => $"{Metadata.Volume *100}";

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

 // Reference to cast child component
 public VideoPlayerCast VideoPlayerCast { get; set; }

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

 private double PlaybackSpeed =1;
 private Dictionary<double, string> PlaybackSpeedOptions = new() { {0.25, "0.25" }, {0.5, "0.5" }, {0.75, "0.75" }, {1, "Normal" }, {1.25, "1.25" }, {1.5, "1.5" }, {1.75, "1.75" }, {2, "2" } };
 private string DisplayPlaybackSpeed => PlaybackSpeedOptions[PlaybackSpeed];

 #endregion

 private bool _isEmptyTouched = false;
 private bool _forceHideControls = false;

 private object _resizeListener = null;

 #endregion
 }
}
