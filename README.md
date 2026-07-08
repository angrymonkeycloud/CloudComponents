# AngryMonkey CloudComponents

Free, open-source Blazor component libraries for **.NET 10**.

[![Demo](https://img.shields.io/badge/Live%20Demo-GitHub%20Pages-0074d9?logo=github)](https://angrymonkeycloud.github.io/CloudComponents/)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/UI-Blazor-5C2D91?logo=blazor)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)

> **[Live Demo](https://angrymonkeycloud.github.io/CloudComponents/)** — showcases every feature with interactive examples and light/dark mode.

---

## Packages

| Package | NuGet | Description |
|---------|-------|-------------|
| `AngryMonkey.CloudComponents` | [![NuGet](https://img.shields.io/nuget/v/AngryMonkey.CloudComponents?logo=nuget)](https://www.nuget.org/packages/AngryMonkey.CloudComponents) | Core UI primitives: Popup, Dialog, Switch, Tabs, ProgressBar, VolumeBar |
| `AngryMonkey.CloudComponents.Grid` | [![NuGet](https://img.shields.io/nuget/v/AngryMonkey.CloudComponents.Grid?logo=nuget)](https://www.nuget.org/packages/AngryMonkey.CloudComponents.Grid) | Feature-rich data grid with sorting, paging, selection, actions, reordering, and export |
| `AngryMonkey.CloudComponents.VideoPlayer` | [![NuGet](https://img.shields.io/nuget/v/AngryMonkey.CloudComponents.VideoPlayer?logo=nuget)](https://www.nuget.org/packages/AngryMonkey.CloudComponents.VideoPlayer) | HTML5 / HLS video player with full controls, casting, and settings |
| `AngryMonkey.CloudComponents.Maps` | [![NuGet](https://img.shields.io/nuget/v/AngryMonkey.CloudComponents.Maps?logo=nuget)](https://www.nuget.org/packages/AngryMonkey.CloudComponents.Maps) | Azure Maps wrapper with markers, regions, geocoding, search, and location-lock |
| `AngryMonkey.CloudComponents.Editor` | [![NuGet](https://img.shields.io/nuget/v/AngryMonkey.CloudComponents.Editor?logo=nuget)](https://www.nuget.org/packages/AngryMonkey.CloudComponents.Editor) | Rich text editor with formatting toolbar, media embeds, and HTML code view validation |
| `AngryMonkey.CloudComponents.Icons` | [![NuGet](https://img.shields.io/nuget/v/AngryMonkey.CloudComponents.Icons?logo=nuget)](https://www.nuget.org/packages/AngryMonkey.CloudComponents.Icons) | SVG icon and logo Razor components |

---

## Quick start

### 1. Install

```bash
dotnet add package AngryMonkey.CloudComponents
dotnet add package AngryMonkey.CloudComponents.Grid
dotnet add package AngryMonkey.CloudComponents.VideoPlayer
dotnet add package AngryMonkey.CloudComponents.Maps
dotnet add package AngryMonkey.CloudComponents.Editor
dotnet add package AngryMonkey.CloudComponents.Icons
```

### 2. Add namespaces to `_Imports.razor`

```razor
@using CloudComponents.Basic
@using CloudComponents.Grid.Components
@using CloudComponents.Grid.Models
@using CloudComponents.VideoPlayer
@using CloudComponents.Maps.Components
@using CloudComponents.Maps.Models
@using CloudComponents.Maps.Services
@using AngryMonkey.CloudComponents.Editor.Components
@using AngryMonkey.CloudComponents.Editor.Models
@using AngryMonkey.CloudComponents.Editor.Services
@using CloudIcons
@using CloudIcons.Icons
@using CloudIcons.Logos
```

### 3. Add required assets to `wwwroot/index.html`

```html
<!-- Core components CSS + JS -->
<link rel="stylesheet" href="_content/AngryMonkey.CloudComponents/css/amc-components.css" />
<script src="_content/AngryMonkey.CloudComponents/js/amc-components.js"></script>
<script src="_content/AngryMonkey.CloudComponents/js/dialog.js"></script>

<!-- VideoPlayer -->
<script src="_content/AngryMonkey.CloudComponents.VideoPlayer/hls.js"></script>
<script src="_content/AngryMonkey.CloudComponents.VideoPlayer/videoPlayer.js"></script>
<script src="_content/AngryMonkey.CloudComponents.VideoPlayer/progressbar.js"></script>
```

---

## Components at a glance

### Basics (`AngryMonkey.CloudComponents`)

| Component | Minimum usage |
|-----------|---------------|
| `PopupComp` | `<PopupComp @ref="_p" Title="Hello"><p>Body</p></PopupComp>` then `await _p.Open()` |
| `Dialog` | `<Dialog @ref="_d" Title="Confirm" Buttons="@_btns">Message</Dialog>` |
| `Switch` | `<Switch Value="@_val" ValueChanged="OnChanged" AllowNone="true" DisplayText="true" />` |
| `Tabs` | `<Tabs TabsList="@_tabs" />` where `_tabs` is a `List<TabItem>` |
| `ProgressBar` | `<ProgressBar Style="ProgressBarStyle.Flat" Value="@_v" Total="100" OnChanged="OnChanged" />` |
| `VolumeBar` | `<VolumeBar Value="@_vol" Extended="true" OnChanged="OnChanged" />` |

### CloudGrid (`AngryMonkey.CloudComponents.Grid`)

```razor
<CloudGrid TItem="MyModel" DataProvider="LoadData" Columns="@_columns" />
```

- `DataProvider` — async callback for server-driven load, sort, and page
- `CloudGridHeaderOptions` — adds search, refresh, export toolbar
- Paging modes: `None`, `LoadMore`, `Pager`
- Row actions, bulk selection, column reordering, drag-and-drop row reordering

See [CloudComponents.Grid/README.md](CloudComponents.Grid/README.md) for the full API.

### VideoPlayer (`AngryMonkey.CloudComponents.VideoPlayer`)

```razor
<VideoPlayer Metadata="@_metadata" />
```

Build `VideoPlayerMetadata` to configure source URL, type (MP4 / HLS), controls, volume, loop, aspect ratio, and captions.
See [CloudComponents.VideoPlayer/README.md](CloudComponents.VideoPlayer/README.md) for all options.

### AzureMap (`AngryMonkey.CloudComponents.Maps`)

```razor
<AzureMap @ref="_map" Options="@_options" OnMapReady="OnMapReady" />
```

Register your key in `Program.cs`:

```csharp
builder.Services.AddAzureMaps(options => options.SubscriptionKey = "YOUR_AZURE_MAPS_KEY");
```

Features: markers, regions, polygon boundaries, geocoding, reverse geocoding, place search, pin-my-location, location lock, camera and style updates at runtime.
See [CloudComponents.Maps/README.md](CloudComponents.Maps/README.md) for the full API.

### CloudEditor (`AngryMonkey.CloudComponents.Editor`)

```razor
<CloudEditor @bind-Value="_html" Placeholder="Start writing…" FirstHeadingLevel="2" />
```

Rich text editor with headings, bold/italic/strikethrough, colors, alignment, lists, links, image/video insertion, fullscreen mode, and HTML code view with validation.
See [CloudComponents.Editor/README.md](CloudComponents.Editor/README.md) for the full API.

### CloudIcons (`AngryMonkey.CloudComponents.Icons`)

```razor
<PlayIcon />  <PauseIcon />  <SearchIcon />  <GoogleLogo />  <MicrosoftLogo />
```

SVG icon and logo Razor components — no extra CSS class or import required.

---

## Repository structure

```
CloudComponents/
+-- CloudComponents.Basic/         # Core UI primitives package
+-- CloudComponents.Grid/          # Data grid library
+-- CloudComponents.VideoPlayer/   # Video player library
+-- CloudComponents.Maps/          # Azure Maps library
+-- CloudComponents.Maps.Web/      # Maps web helpers
+-- CloudComponents.Editor/        # Rich text editor library
+-- CloudComponents.Icons/         # SVG icon components
+-- CloudComponents.Demo/          # Unified demo app (deployed to GitHub Pages)
+-- archived/                      # Legacy standalone demo projects (reference only)
```

---

## Contributing

Pull requests are welcome. For larger changes please open an issue first to discuss the approach.

## License

MIT
