using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngryMonkey.Cloud.Components;

public enum VideoStates
{
    NoVideo,
    Loading,
    Ready,
    Error
}

public enum PlayingStates
{
    NotPlaying,
    Buffering,
    Playing,
    Paused
}

public enum VolumeType
{
    Full,
    Mute,
    None
}

public class VideoPlayerMetadata
{
    // Public

    public string? Title { get; set; }
    public bool Loop { get; set; } = false;
    public bool Autoplay { get; set; } = false;
    public bool ShowStopButton { get; set; } = false;
    public bool ReserveAspectRatio { get; set; } = false;
    public string? VideoUrl { get; set; }
    public double Volume { get; set; } = 1;
    public bool IsLive { get; set; } = false;
    public bool ShowSettings { get; set; } = true;
    public bool ShowProgressBar { get; set; } = true;
    public VolumeType VolumeType { get; set; } = VolumeType.Full;
    public bool IsFullScreen = false;
    public double CurrentTime { get; set; } = 0;
    public bool IsPlayingState { get; private set; }
    public bool EnableControls { get; set; } = true;
    public bool LivePlaysNatively { get; internal set; } = false;

    // Internal

    internal bool IsUserChangingProgress = false;
    //internal bool IsVideoPlaying = false;
    internal bool _isMuted = false;
    internal bool IsMuted = false;
    internal bool DoShowVolumeControls = false;
    internal bool IsSeeking = false;
    internal bool ShowSeekingInfo = false;
    //internal bool IsStream { get; set; }
    internal bool LiveInitialized { get; set; } = false;
    internal bool ShowProgressBarElement => !IsLive && VideoState == VideoStates.Ready;
    internal bool ShowDuration => ShowProgressBar && !IsLive;
    internal bool EnableLoop => !IsLive;

    internal VideoInfo? CurrentVideoInfo { get; set; }
    internal VideoPlayer? Player { get; set; }


    private PlayingStates _playingState = PlayingStates.NotPlaying;
    public PlayingStates PlayingState
    {
        get { return _playingState; }
        set
        {
            if (value == _playingState)
                return;

            _playingState = value;

            if (_playingState == PlayingStates.Playing)
                IsPlayingState = true;
            else if (_playingState == PlayingStates.NotPlaying)
                IsPlayingState = false;

            if (Player == null)
                return;

            Task.Run(async () =>
            {
                await Player.OnPlayingStateChanged.InvokeAsync(PlayingState);
            });
        }
    }

    private VideoStates _videoState = VideoStates.NoVideo;
    public VideoStates VideoState
    {
        get { return _videoState; }
        set
        {
            if (value == _videoState)
                return;

            _videoState = value;
            IsPlayingState = false;

            if (Player == null)
                return;

            Task.Run(async () =>
            {
                if (_videoState == VideoStates.Ready)
                    await Player.OnVideoReady.InvokeAsync();
                else if (_videoState == VideoStates.Error)
                    await Player.OnVideoError.InvokeAsync();

                await Player.OnVideoStateChanged.InvokeAsync(VideoState);
            });
        }
    }

    #region Time / Duration

    internal string DisplayTimeDuration => $"{GetTime(CurrentTime)} / {GetTime(CurrentVideoInfo?.Duration ?? 0)}";

    internal double SeekInfoTime { get; set; }
    internal string DisplaySeekInfoTime => GetTime(SeekInfoTime);

    #endregion

    internal Dictionary<string, string> VideoSettingsInfo
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

            info.Add("Status", PlayingState.ToString());

            return info;
        }
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

    #region Cast

    public bool IsCasting => CastStatus != CastStatuses.NotCasting;
    internal bool CastingInitialized { get; set; } = false;
    internal CastStatuses CastStatus { get; set; } = CastStatuses.NotCasting;
    internal enum CastStatuses
    {
        NotCasting,
        Initializing,
        Connecting,
        Casting
    }

    #endregion
}
