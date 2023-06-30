using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngryMonkey.Cloud.Components;

public class VideoPlayerMetadata
{
    // Public

    public string Title { get; set; }
    public bool Loop { get; set; } = false;
    public bool Autoplay { get; set; } = false;
    public bool ShowStopButton { get; set; } = false;
    public bool ReserveAspectRatio { get; set; } = false;
    public string? VideoUrl { get; set; }
    public double Volume { get; set; } = 1;

    // Internal

    internal bool IsUserChangingProgress = false;
    internal bool IsVideoPlaying = false;
    internal bool IsFullScreen = false;
    internal bool _isMuted = false;
    internal bool IsMuted = false;
    internal bool DoShowVolumeControls = false;
    internal bool IsSeeking = false;
    internal bool ShowSeekingInfo = false;
    internal bool IsStream { get; set; }
    internal bool StreamInitialized { get; set; } = false;
    internal bool HasError { get; set; } = false;
    internal bool ShowProgressBar => !IsStream;
    internal bool ShowDuration => !IsStream;
    internal bool VideoReady { get; set; } = false;
    internal bool EnableLoop => !IsStream;

    internal VideoInfo? CurrentVideoInfo { get; set; }
    internal double CurrentTime { get; set; } = 0;

    public VideoStatus Status { get; set; } = VideoStatus.Loading;

    public enum VideoStatus
    {
        Loading,
        Playing,
        Paused,
        Stoped,
        Buffering,
        Streaming,
        Unknown
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

            info.Add("Status", Status.ToString());

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

    internal bool IsCasting => CastStatus != CastStatuses.NotCasting;
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
