using System;
using System.Text.Json.Serialization;

namespace AngryMonkey.Cloud.Components
{
	public class VideoState
	{
		/// <summary>
		/// Returns whether the audio/video should start playing as soon as it is loaded
		/// </summary>
		public bool Autoplay { get; set; }
		/// <summary>
		/// Returns whether the audio/video should display controls (like play/pause etc.)
		/// </summary>
		public bool Controls { get; set; }
		/// <summary>
		/// Returns the CORS settings of the audio/video
		/// </summary>
		public string CrossOrigin { get; set; }
		/// <summary>
		/// Returns the URL of the current audio/video
		/// </summary>
		public Uri CurrentSrc { get; set; }
		/// <summary>
		/// Returns the current playback position in the audio/video (in seconds)
		/// </summary>
		[JsonPropertyName("currentTime")]
		public double CurrentTime { get; set; }
		/// <summary>
		/// Returns whether the audio/video should be muted by default
		/// </summary>
		public bool DefaultMuted { get; set; }
		/// <summary>
		/// Returns the default speed of the audio/video playback
		/// </summary>
		public double DefaultPlaybackRate { get; set; }
		/// <summary>
		/// Returns the length of the current audio/video (in seconds)
		/// </summary>
		public double Duration { get; set; }
		/// <summary>
		/// Returns whether the playback of the audio/video has ended or not
		/// </summary>
		public bool Ended { get; set; }
		/// <summary>
		/// Returns whether the audio/video should start over again when finished
		/// </summary>
		public bool Loop { get; set; }
		/// <summary>
		/// Returns the group the audio/video belongs to (used to link multiple audio/video elements)
		/// </summary>
		public string MediaGroup { get; set; }
		/// <summary>
		/// Returns whether the audio/video is muted or not
		/// </summary>
		public bool Muted { get; set; }
		/// <summary>
		/// Returns whether the audio/video is paused or not
		/// </summary>
		public bool Paused { get; set; }
		/// <summary>
		/// Returns the speed of the audio/video playback
		/// </summary>
		public double PlaybackRate { get; set; }
		/// <summary>
		/// Returns whether the audio/video should be loaded when the page loads
		/// </summary>
		public string Preload { get; set; }
		/// <summary>
		/// Returns whether the user is currently seeking in the audio/video
		/// </summary>
		public bool Seeking { get; set; }
		/// <summary>
		/// Returns the current source of the audio/video element
		/// </summary>
		public Uri Src { get; set; }
		/// <summary>
		/// Returns a Date object representing the current time offset
		/// </summary>
		public DateTime StartDate { get; set; }
		/// <summary>
		/// Returns the volume of the audio/video
		/// </summary>
		public double Volume { get; set; }
	}
}
