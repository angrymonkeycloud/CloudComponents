# Angry Monkey Cloud Components

Free Blazor components built on .NET 6.

## Video Player

Open source Blazor Video Player.

[Video Player](https://github.com/angrymonkeycloud/CloudComponents/tree/main/Components/VideoPlayer)


## Blazor Server Initialization

This section provides instructions for building a Blazor Server App on top of Cloud Components.

- Add Cloud Web as a service under **Program.cs**.

```cs
builder.Services.AddCloudWeb(new CloudWebOptions()
{
 DefaultTitle = "My Blazor app", // Your app main title
 TitlePrefix = "My Blazor app: ", // Your app suffix that would be added to a page title if exists
 TitleSuffix = " - My Blazor app", // Your app suffix that would be added to a page title if exists
 SiteBundles = new List<CloudBundle>() // Bundles that should be added to the layout
 {
  new CloudBundle(){ JQuery = "3.4.1"}, // Adding JQuery
  new CloudBundle(){ Source = "ServerDemo.styles.css", MinOnRelease = false},
  new CloudBundle(){ Source = "css/site.css"},
  new CloudBundle(){ Source = "js/site.js"}
 }
});
```

- Change Layout value under **_Host.cshtml** to **Layout.cshtml**. (You can delete _layout.cshtml file)

```cshtml
@{
 Layout = "Layout";
}
```

- Update **MainLayout.razor** to the following.

```html
@inherits LayoutComponentBase

<header>
 @*Header here*@
</header>

 <main>
  @Body
 </main>

<footer>
 @*Footer here*@
</footer>
</footer>
```

- Add cloud components using to **_Imports.razor**.

```razor
@using AngryMonkey.Cloud.Components
```

- Update page title and other head tags as follows

```html
<HeadContent>
 <CloudHead Title="My First Page Title"
            Keywords="key1,key2"
            Description="My First Page Description">
 </CloudHead>

 <CloudBundle Source="myfirstpage.css" />
 <CloudBundle Source="myfirstpage.js" />
</HeadContent>
```