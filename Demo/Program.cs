using System;
using System.Net.Http;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using AngryMonkey.Cloud.Components.Demo;
using AngryMonkey.CloudWeb;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

builder.Services.AddCloudWeb(new CloudWebConfig()
{
	TitleSuffix = " - Angry Monkey Cloud Components",
	PageDefaults = new()
    {
        Title = "Angry Monkey Cloud Components",
        Bundles= new List<CloudBundle>()
        {
		    //new CloudBundle(){ JQuery = "3.4.1"},
		    new CloudBundle(){ Source = "css/site.css"},
            new CloudBundle(){ Source = "js/site.js"}
        }
    }
});

builder.RootComponents.Add<CloudHeadInit>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();