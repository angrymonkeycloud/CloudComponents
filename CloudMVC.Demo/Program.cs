using AngryMonkey.Cloud.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddCloudWeb(new()
{
    PageDefaults = new()
    {
        Title = "MVC Website Demo",
        AutoAppendBlazorStyles = false,
        Bundles = new()
        {
            new CloudBundle(){ Source = "/css/site.css"}
        }
    },
    TitlePrefix = "Pre - ",
    TitleSuffix = " - Suffix"

});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseBlazorFrameworkFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapBlazorHub();

await app.RunAsync();
