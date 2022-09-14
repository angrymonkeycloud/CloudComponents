using AngryMonkey.Cloud.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddCloudWeb(new CloudWebOptions()
{
	DefaultTitle = "Angry Monkey Cloud Components",
	TitleSuffix = " - Angry Monkey Cloud Components",
	SiteBundles = new List<CloudBundle>()
	{
		//new CloudBundle(){ JQuery = "3.4.1"},
		new CloudBundle(){ Source = "css/site.css"},
		new CloudBundle(){ Source = "js/site.js"}
	}
});

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
