using System.Collections.Generic;
using System.Text;

public static class VideoStateOptionsExt
{
	public static string GetPayloads(this VideoStateOptions options)
	{
		var list = new StringBuilder("{");

		// doesn't serialize in the browser
		// sb.Append(options.All || options.AudioTracks, FormatAsPayload(nameof(options.AudioTracks)));
		list.Append(options.All || options.Autoplay, FormatAsPayload(nameof(options.Autoplay)));
		// doesn't serialize in the browser
		// sb.Append(options.All || options.Buffered, FormatAsPayload(nameof(options.Buffered)));
		list.Append(options.All || options.Controls, FormatAsPayload(nameof(options.Controls)));
		list.Append(options.All || options.CrossOrigin, FormatAsPayload(nameof(options.CrossOrigin)));
		list.Append(options.All || options.CurrentSrc, FormatAsPayload(nameof(options.CurrentSrc)));
		list.Append(options.All || options.CurrentTime, FormatAsPayload(nameof(options.CurrentTime)));
		list.Append(options.All || options.DefaultMuted, FormatAsPayload(nameof(options.DefaultMuted)));
		list.Append(options.All || options.DefaultPlaybackRate, FormatAsPayload(nameof(options.DefaultPlaybackRate)));
		list.Append(options.All || options.Duration, FormatAsPayload(nameof(options.Duration)));
		list.Append(options.All || options.Ended, FormatAsPayload(nameof(options.Ended)));
		list.Append(options.All || options.Error, FormatAsPayload(nameof(options.Error)));
		list.Append(options.All || options.Loop, FormatAsPayload(nameof(options.Loop)));
		list.Append(options.All || options.MediaGroup, FormatAsPayload(nameof(options.MediaGroup)));
		list.Append(options.All || options.Muted, FormatAsPayload(nameof(options.Muted)));
		list.Append(options.All || options.NetworkState, FormatAsPayload(nameof(options.NetworkState)));
		list.Append(options.All || options.Paused, FormatAsPayload(nameof(options.Paused)));
		list.Append(options.All || options.PlaybackRate, FormatAsPayload(nameof(options.PlaybackRate)));
		list.Append(options.All || options.Played, FormatAsPayload(nameof(options.Played)));
		list.Append(options.All || options.Preload, FormatAsPayload(nameof(options.Preload)));
		list.Append(options.All || options.ReadyState, FormatAsPayload(nameof(options.ReadyState)));
		list.Append(options.All || options.Seekable, FormatAsPayload(nameof(options.Seekable)));
		list.Append(options.All || options.Seeking, FormatAsPayload(nameof(options.Seeking)));
		list.Append(options.All || options.Src, FormatAsPayload(nameof(options.Src)));
		list.Append(options.All || options.StartDate, FormatAsPayload(nameof(options.StartDate)));
		// doesn't serialize in the browser
		// sb.Append(options.All || options.TextTracks, FormatAsPayload(nameof(options.TextTracks)));
		// sb.Append(options.All || options.VideoTracks, FormatAsPayload(nameof(options.VideoTracks)));
		list.Append(options.All || options.Volume, FormatAsPayload(nameof(options.Volume)));

		if (list.Length == 1)
		{
			return string.Empty;
		}

		list.Append("}");
		return list.ToString();

		static string FormatAsPayload(string name)
		{
			var payload = $"this.{name.Substring(0, 1).ToLower()}{name.Substring(1)}";
			return $"{name}: {payload}";
		}
	}
	public static StringBuilder Append(this StringBuilder sb, bool condition, string value)
		=> condition ? (sb.Length > 1 ? sb.Append(",").Append(value) : sb.Append(value)) : sb;
	public static void Append<T>(this List<T> sb, bool condition, T value)
	{
		if (condition) sb.Add(value);
	}
	public static string[] GetPayload(this VideoStateOptions options)
	{
		var list = new List<string>();

		//list.Append(options.All || options.AudioTracks, FormatAsPayload(nameof(options.AudioTracks)));
		list.Append(options.All || options.Autoplay, FormatAsPayload(nameof(options.Autoplay)));
		//list.Append(options.All || options.Buffered, FormatAsPayload(nameof(options.Buffered)));
		list.Append(options.All || options.Controls, FormatAsPayload(nameof(options.Controls)));
		list.Append(options.All || options.CrossOrigin, FormatAsPayload(nameof(options.CrossOrigin)));
		list.Append(options.All || options.CurrentSrc, FormatAsPayload(nameof(options.CurrentSrc)));
		list.Append(options.All || options.CurrentTime, FormatAsPayload(nameof(options.CurrentTime)));
		list.Append(options.All || options.DefaultMuted, FormatAsPayload(nameof(options.DefaultMuted)));
		list.Append(options.All || options.DefaultPlaybackRate, FormatAsPayload(nameof(options.DefaultPlaybackRate)));
		list.Append(options.All || options.Duration, FormatAsPayload(nameof(options.Duration)));
		list.Append(options.All || options.Ended, FormatAsPayload(nameof(options.Ended)));
		list.Append(options.All || options.Error, FormatAsPayload(nameof(options.Error)));
		list.Append(options.All || options.Loop, FormatAsPayload(nameof(options.Loop)));
		list.Append(options.All || options.MediaGroup, FormatAsPayload(nameof(options.MediaGroup)));
		list.Append(options.All || options.Muted, FormatAsPayload(nameof(options.Muted)));
		list.Append(options.All || options.NetworkState, FormatAsPayload(nameof(options.NetworkState)));
		list.Append(options.All || options.Paused, FormatAsPayload(nameof(options.Paused)));
		list.Append(options.All || options.PlaybackRate, FormatAsPayload(nameof(options.PlaybackRate)));
		//list.Append(options.All || options.Played, FormatAsPayload(nameof(options.Played)));
		list.Append(options.All || options.Preload, FormatAsPayload(nameof(options.Preload)));
		list.Append(options.All || options.ReadyState, FormatAsPayload(nameof(options.ReadyState)));
		//list.Append(options.All || options.Seekable, FormatAsPayload(nameof(options.Seekable)));
		list.Append(options.All || options.Seeking, FormatAsPayload(nameof(options.Seeking)));
		list.Append(options.All || options.Src, FormatAsPayload(nameof(options.Src)));
		list.Append(options.All || options.StartDate, FormatAsPayload(nameof(options.StartDate)));
		//list.Append(options.All || options.TextTracks, FormatAsPayload(nameof(options.TextTracks)));
		//list.Append(options.All || options.VideoTracks, FormatAsPayload(nameof(options.VideoTracks)));
		list.Append(options.All || options.Volume, FormatAsPayload(nameof(options.Volume)));

		return list.ToArray();

		static string FormatAsPayload(string name)
			=> $"{name.Substring(0, 1).ToLower()}{name.Substring(1)}";
	}
}
