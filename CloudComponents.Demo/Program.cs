using CloudComponents.Demo;
using CloudComponents.Demo.Services;
using CloudComponents.Maps.Extensions;
using CloudComponents.Maps.Services;
using CloudComponents.Maps.Web.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton<SampleDataService>();
builder.Services.AddScoped<AzureMapsConfigurationService>();
builder.Services.AddScoped<ILocationService, WebLocationService>();
builder.Services.AddSingleton<SampleTrackingService>();
builder.Services.AddScoped<SavedLocationService>();
builder.Services.AddAzureMaps("placeholder-key");

await builder.Build().RunAsync();
