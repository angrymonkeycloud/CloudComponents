using CloudComponents.Maps.Demo;
using CloudComponents.Maps.Demo.Services;
using AngryMonkey.CloudComponents.Maps.Extensions;
using AngryMonkey.CloudComponents.Maps.Services;
using AngryMonkey.CloudComponents.Maps.Web.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Register the configuration service for managing Azure Maps key
builder.Services.AddScoped<AzureMapsConfigurationService>();

// Register geolocation service
builder.Services.AddScoped<ILocationService, WebLocationService>();

// Register sample tracking data service
builder.Services.AddSingleton<SampleTrackingService>();

// Register demo persistence service for the saved-location workflow
builder.Services.AddScoped<SavedLocationService>();

// Register Azure Maps with a placeholder key initially
// The actual key will be loaded from SessionStorage when needed
builder.Services.AddAzureMaps("placeholder-key");

await builder.Build().RunAsync();
