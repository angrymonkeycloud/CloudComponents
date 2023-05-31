using AngryMonkey.Cloud.Components;
using AngryMonkey.CloudWeb;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

//CloudWebConfig webConfig = new()
//{
//};

CloudWebConfig cloudWeb = new()
{
	TitleSuffix = " - Angry Monkey Cloud Components",
    PageDefaults = new()
    {
        Title = "Angry Monkey Cloud Components",
        AutoAppendBlazorStyles = true,
        CallingAssemblyName = "ServerDemo",
        BlazorRenderMode = CloudPageBlazorRenderModes.Server,
        Bundles = new()
        {
            new CloudBundle(){ Source = "/css/site.css"},
            new CloudBundle(){ Source = "/js/site.js"},
            //new CloudBundle(){ Source = "/js/video.js"},
            new CloudBundle(){ Source = "https://cdnjs.cloudflare.com/ajax/libs/hls.js/1.4.3/hls.min.js"},
        }
    }
};

//webConfig.PageDefaults.SetBlazor(CloudPageBlazorRenderModes.Server)
//	.SetTitle("Angry Monkey Cloud Components")
//	.SetCallingAssemblyName("ServerDemo")
//	.AppendBundle("css/site.css")
//	.AppendBundle("js/site.js");

builder.Services.AddCloudWeb(cloudWeb);

builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
