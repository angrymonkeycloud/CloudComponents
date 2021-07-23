using System;

namespace AngryMonkey.Cloud.Components
{
	public partial class PageBaseSettings
	{
		private static string _buildID;
		public string BuildID => _buildID ??= System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
		public string TitlePrefix { get; set; }
		public string TitleSuffix { get; set; }
		public string DefaultTitle { get; set; }
		public string GoogleAnalyticsId { get; set; }
	}
}
