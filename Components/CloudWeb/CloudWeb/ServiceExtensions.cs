//using Microsoft.Extensions.DependencyInjection;
using AngryMonkey.Cloud.Components;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

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
		services.AddSingleton(new CloudWeb() { Options = options });

		return null;
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