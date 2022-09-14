//using Microsoft.Extensions.DependencyInjection;
using AngryMonkey.Cloud.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection;

public class CloudWeb
{
	IServiceCollection AddCloudWeb { get; }
	public CloudWebOptions Options { get; set; }
}

public static class MvcServiceCollectionExtensions
{

	public static CloudWeb AddCloudWeb(this IServiceCollection services, CloudWebOptions options)
	{
		TransferFileToProject("CloudWeb/css/cloud.css", "wwwroot/amc/css/cloud.css");
		TransferFileToProject("CloudWeb/css/cloud.min.css", "wwwroot/amc/css/cloud.min.css");

		TransferFileToProject("CloudWeb/js/cloud.js", "wwwroot/amc/js/cloud.js");
		TransferFileToProject("CloudWeb/js/cloud.min.js", "wwwroot/amc/js/cloud.min.js");

		string assemblyName = Assembly.GetCallingAssembly().GetName().Name;

		// JS
		options.SiteBundles.Insert(0, new CloudBundle() { Source = "amc/js/cloud.js" });

		// CSS
		options.SiteBundles.Insert(0, new CloudBundle() { Source = $"{assemblyName}.styles.css", MinOnRelease = false });
		options.SiteBundles.Insert(0, new CloudBundle() { Source = "amc/css/cloud.css" });

		services.AddSingleton(new CloudWeb() { Options = options });

		return null;
	}

	private static void TransferFileToProject(string source, string destination)
	{
		Assembly assembly = Assembly.GetExecutingAssembly();
		string fullFileName = $"AngryMonkey.Cloud.Components.{source.Replace("/", ".")}";

		using Stream stream = assembly.GetManifestResourceStream(fullFileName);

		string destinationFilePath = Path.Combine(Directory.GetCurrentDirectory(), destination);

		Directory.CreateDirectory(destinationFilePath[..^Path.GetFileName(destination).Length]);

		using var fileStream = new FileStream(destinationFilePath, FileMode.Create);
		stream.CopyTo(fileStream);
	}
}

public class CloudWebOptions
{
	public List<CloudBundle> SiteBundles { get; set; } = new List<CloudBundle>();
	public string DefaultTitle { get; set; } = string.Empty;
	public string TitlePrefix { get; set; } = string.Empty;
	public string TitleSuffix { get; set; } = string.Empty;
	public string BaseUrl { get; set; } = "/";
}