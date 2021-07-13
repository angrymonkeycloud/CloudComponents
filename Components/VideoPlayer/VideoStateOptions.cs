public struct VideoStateOptions
{
	/// <summary>
	/// Returns the entire object
	/// </summary>
	public bool All { get; set; }
	/// <summary>
	/// Returns an AudioTrackList object representing available audio tracks
	/// </summary>
	public bool AudioTracks { get; set; }
	/// <summary>
	/// Returns whether the audio/video should start playing as soon as it is loaded
	/// </summary>
	public bool Autoplay { get; set; }
	/// <summary>
	/// Returns a TimeRanges object representing the buffered parts of the audio/video
	/// </summary>
	public bool Buffered { get; set; }
	/// <summary>
	/// Returns whether the audio/video should display controls (like play/pause etc.)
	/// </summary>
	public bool Controls { get; set; }
	/// <summary>
	/// Returns the CORS settings of the audio/video
	/// </summary>
	public bool CrossOrigin { get; set; }
	/// <summary>
	/// Returns the URL of the current audio/video
	/// </summary>
	public bool CurrentSrc { get; set; }
	/// <summary>
	/// Returns the current playback position in the audio/video (in seconds)
	/// </summary>
	public bool CurrentTime { get; set; }
	/// <summary>
	/// Returns whether the audio/video should be muted by default
	/// </summary>
	public bool DefaultMuted { get; set; }
	/// <summary>
	/// Returns the default speed of the audio/video playback
	/// </summary>
	public bool DefaultPlaybackRate { get; set; }
	/// <summary>
	/// Returns the length of the current audio/video (in seconds)
	/// </summary>
	public bool Duration { get; set; }
	/// <summary>
	/// Returns whether the playback of the audio/video has ended or not
	/// </summary>
	public bool Ended { get; set; }
	/// <summary>
	/// Returns a MediaError object representing the error state of the audio/video
	/// </summary>
	public bool Error { get; set; }
	/// <summary>
	/// Returns whether the audio/video should start over again when finished
	/// </summary>
	public bool Loop { get; set; }
	/// <summary>
	/// Returns the group the audio/video belongs to (used to link multiple audio/video elements)
	/// </summary>
	public bool MediaGroup { get; set; }
	/// <summary>
	/// Returns whether the audio/video is muted or not
	/// </summary>
	public bool Muted { get; set; }
	/// <summary>
	/// Returns the current network state of the audio/video
	/// </summary>
	public bool NetworkState { get; set; }
	/// <summary>
	/// Returns whether the audio/video is paused or not
	/// </summary>
	public bool Paused { get; set; }
	/// <summary>
	/// Returns the speed of the audio/video playback
	/// </summary>
	public bool PlaybackRate { get; set; }
	/// <summary>
	/// Returns a TimeRanges object representing the played parts of the audio/video
	/// </summary>
	public bool Played { get; set; }
	/// <summary>
	/// Returns whether the audio/video should be loaded when the page loads
	/// </summary>
	public bool Preload { get; set; }
	/// <summary>
	/// Returns the current ready state of the audio/video
	/// </summary>
	public bool ReadyState { get; set; }
	/// <summary>
	/// Returns a TimeRanges object representing the seekable parts of the audio/video
	/// </summary>
	public bool Seekable { get; set; }
	/// <summary>
	/// Returns whether the user is currently seeking in the audio/video
	/// </summary>
	public bool Seeking { get; set; }
	/// <summary>
	/// Returns the current source of the audio/video element
	/// </summary>
	public bool Src { get; set; }
	/// <summary>
	/// Returns a Date object representing the current time offset
	/// </summary>
	public bool StartDate { get; set; }
	/// <summary>
	/// Returns a TextTrackList object representing the available text tracks
	/// </summary>
	public bool TextTracks { get; set; }
	/// <summary>
	/// Returns a VideoTrackList object representing the available video tracks
	/// </summary>
	public bool VideoTracks { get; set; }
	/// <summary>
	/// Returns the volume of the audio/video
	/// </summary>
	public bool Volume { get; set; }
}
