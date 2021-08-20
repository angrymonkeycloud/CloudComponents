using System;
using System.Text.Json.Serialization;

namespace AngryMonkey.Cloud.Components
{
	public class VideoEventData
	{
		[JsonPropertyName("name")]
		public string Name { get; set; } = VideoEvents.NotSet.ToString();
		public VideoEvents EventName =>
			(VideoEvents)Enum.Parse(typeof(VideoEvents), Name, true);

		[JsonPropertyName("state")]
		public VideoState State { get; set; }
	}
}
