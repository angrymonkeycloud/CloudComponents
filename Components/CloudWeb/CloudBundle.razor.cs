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
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace AngryMonkey.Cloud.Components
{
	public partial class CloudBundle
	{
		[Parameter]
		public string Source { get; set; }

		[Parameter]
		public string JQuery { get; set; }

		[Parameter]
		public bool MinOnRelease { get; set; } = true;

		private static string BuildVersion => !string.IsNullOrEmpty(Assembly.GetExecutingAssembly().Location) ? GetHashString(new FileInfo(Assembly.GetExecutingAssembly().Location).LastWriteTime.ToString()) : Assembly.GetEntryAssembly().GetName().Version.ToString();

		private static byte[] GetHash(string inputString)
		{
			using HashAlgorithm algorithm = SHA256.Create();

			return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
		}

		private static string GetHashString(string inputString)
		{
			StringBuilder stringBuild = new();

			foreach (byte b in GetHash(inputString))
				stringBuild.Append(b.ToString("X2"));

			return stringBuild.ToString();
		}

		private string Result
		{
			get
			{
				if (!string.IsNullOrEmpty(JQuery))
					return $"<script crossorigin=\"anonymous\" src=\"https://code.jquery.com/jquery-{JQuery}.min.js\" integrity=\"sha256-CSXorXvZcTkaix6Yvo6HppcZGetbYMGWSFlBw8HfCJo=\" defer=\"\"></script>";

				if (string.IsNullOrEmpty(Source) || !Source.Contains('.'))
					return null;

				string source = Source;

#if !DEBUG

				if (MinOnRelease && !source.Contains(".min.", StringComparison.OrdinalIgnoreCase))
				{
					List<string> sourceSplitted = Source.Split('.').ToList();

					sourceSplitted.Insert(sourceSplitted.Count - 1, "min");
					source = string.Join('.', sourceSplitted);
				}

				if (!source.StartsWith("http", StringComparison.OrdinalIgnoreCase))
				{
					string separator = source.Contains('?') ? "&" : "?";

					source = $"{source}{separator}v={BuildVersion}";
				}

#endif

				switch (Source.Split('.').Last().Trim().ToLower())
				{
					case "css":
						return $"<link href=\"{source}\" rel=\"stylesheet\">";

					case "js":
						return $"<script src=\"{source}\" defer></script>";

					default: return null;
				}
			}
		}
	}
}