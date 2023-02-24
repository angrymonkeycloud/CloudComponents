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
        [Parameter] public required string Source { get; set; }
        [Parameter] public string? JQuery { get; set; }
        [Parameter] public bool MinOnRelease { get; set; } = true;
        [Parameter] public string? AddOns { get; set; }
        [Parameter] public bool Defer { get; set; } = true;
        [Parameter] public bool Async { get; set; } = false;

        private string BuildVersion => GetHashString(File.GetLastWriteTimeUtc($"wwwroot/{Source}").ToString());

        private static byte[] GetHash(string inputString)
        {
            return SHA256.HashData(Encoding.UTF8.GetBytes(inputString));
        }

        private static string GetHashString(string inputString)
        {
            StringBuilder stringBuild = new();

            foreach (byte b in GetHash(inputString))
                stringBuild.Append(b.ToString("X2"));

            return stringBuild.ToString();
        }

        private string? Result
        {
            get
            {
                if (!string.IsNullOrEmpty(JQuery))
                    return $"<script crossorigin=\"anonymous\" src=\"https://code.jquery.com/jquery-{JQuery}.min.js\" integrity=\"sha256-CSXorXvZcTkaix6Yvo6HppcZGetbYMGWSFlBw8HfCJo=\" defer=\"\"></script>";

                if (string.IsNullOrEmpty(Source) || !Source.Contains('.'))
                    return null;

                SourceTypes? sourceType = Source.Split('.').Last().Trim().ToLower() switch
                {
                    "css" => SourceTypes.CSS,
                    "js" => SourceTypes.JS,
                    _ => null,
                };

                if (sourceType == null)
                    return null;

                List<string> segments = new()
                {
                    sourceType == SourceTypes.CSS? "<link" : "<script"
                };

                string source = Source;

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

                segments.Add(sourceType == SourceTypes.CSS ? $"href=\"{source}\"" : $"src=\"{source}\"");

                if (sourceType == SourceTypes.JS)
                {
                    if (Defer)
                        segments.Add("defer");

                    if (Async)
                        segments.Add("async");
                }

                if (!string.IsNullOrEmpty(AddOns))
                    segments.Add(AddOns);

                segments.Add(sourceType == SourceTypes.CSS ? "rel=\"stylesheet\">" : "></script>");

                return string.Join(" ", segments);

                //return Source.Split('.').Last().Trim().ToLower() switch
                //{
                //    "css" => $"<link href=\"{source}\" rel=\"stylesheet\">",
                //    "js" => $"<script src=\"{source}\" {(Defer ? "defer" : null)} {(Async ? "async" : null)}></script>",
                //    _ => null,
                //};
            }
        }

        private enum SourceTypes
        {
            JS,
            CSS,
        }
    }

}