using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AngryMonkey.Cloud.Components
{
	public class VideoInfo
	{
		public double Duration { get; set; }
		public double Width { get; set; }
		public double Height { get; set; }
		public double AspectRatio => Width / Height;

		public string DisplayAspectRatio
		{
			get
			{
				double rounderNumber = Math.Round(AspectRatio, 2);

				return rounderNumber switch
				{
					1 => "1:1",
					2 => "2:1",
					.5 => "1:2",
					1.78 or 1.77 => "16:9",
					.56 => "9:16",
					1.6 => "16:10",
					.62 => "10:16",
					1.25 => "5:4",
					.8 => "4:5",
					1.5 => "3:2",
					.66 or .67 => "2:3",
					2.35 => "47:20",
					.42 => "20:47",
					_ => rounderNumber.ToString(),
				};
			}
		}
	}
}
