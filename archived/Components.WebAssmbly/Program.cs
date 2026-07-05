using AngryMonkey.Cloud.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddSingleton<DateTimePicker>();

await builder.Build().RunAsync();
