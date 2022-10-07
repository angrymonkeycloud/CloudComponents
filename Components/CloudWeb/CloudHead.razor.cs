using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Web;
using System.Text.Json;
using System.Reflection;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using AngryMonkey.Cloud.Components.Icons;

namespace AngryMonkey.Cloud.Components
{
	public partial class CloudHead
	{
		[Parameter]
		public string Title { get; set; }

		[Parameter]
		public List<string> TitleAds { get; set; } = new List<string>();

		[Parameter]
		public string Keywords { get; set; }

		[Parameter]
		public string Description{ get; set; }

		private string TitleResult
		{
			get
			{
				if (string.IsNullOrEmpty(Title))
					return cloudWeb.Options.DefaultTitle;

				StringBuilder title = new($"{cloudWeb.Options.TitlePrefix}{Title}{cloudWeb.Options.TitleSuffix}");

				if (TitleAds.Any())
					foreach (string addText in TitleAds)
						if (title.Length + addText.Length + 1 <= 64)
							title.Append($" {addText}");

				return title.ToString();
			}
		}

		private string KeywordsResult
		{
			get
			{
				return Keywords;
			}
		}

		private string DescriptionResult
		{
			get
			{
				return Description;
			}
		}
	}
}