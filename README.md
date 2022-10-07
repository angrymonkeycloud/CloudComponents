# Angry Monkey Cloud Components

Free Blazor components built on .NET 6.

## Video Player

Open source Blazor Video Player.

[Video Player](https://github.com/angrymonkeycloud/CloudComponents/tree/main/Components/VideoPlayer)


## Blazor Initialization

This section provides instructions for building a Blazor Server app OR Blazor Webassembly app based on Cloud Components.

### **Blazor Server App**
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

```java
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


### **Blazor Webassembly App**


- Under **Program.cs**, remove

```cs
builder.RootComponents.Add<HeadOutlet>("head::after");
```

- Add Cloud Web as a service & Cloud Head Initialization as a component.

```cs
csbuilder.Services.AddCloudWeb(new CloudWebOptions()
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

builder.RootComponents.Add<CloudHeadInit>("head::after");
```

- Update **MainLayout.razor** to the following.

```html
@inherits LayoutComponentBase

<Components />

<div class="page">
    <main>
        @Body
    </main>
</div>
```

- Add cloud components using to **_Imports.razor**.


```java
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