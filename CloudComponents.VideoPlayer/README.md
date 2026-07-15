# CloudComponents.VideoPlayer
[![Website](https://img.shields.io/badge/Website-angrymonkeycloud.com-0B5FFF?style=flat-square&logo=googlechrome&logoColor=white)](https://angrymonkeycloud.com/cloudcomponents)
[![GitHub repository](https://img.shields.io/badge/GitHub-CloudComponents-181717?style=flat-square&logo=github)](https://github.com/angrymonkeycloud/CloudComponents)
[![NuGet](https://img.shields.io/nuget/v/AngryMonkey.CloudComponents.VideoPlayer?style=flat-square&logo=nuget)](https://www.nuget.org/packages/AngryMonkey.CloudComponents.VideoPlayer)
[![NuGet downloads](https://img.shields.io/nuget/dt/AngryMonkey.CloudComponents.VideoPlayer?style=flat-square&logo=nuget)](https://www.nuget.org/packages/AngryMonkey.CloudComponents.VideoPlayer)
[![.NET](https://img.shields.io/badge/.NET-10-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/UI-Blazor-5C2D91?style=flat-square&logo=blazor&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![License](https://img.shields.io/badge/License-MIT-2F855A?style=flat-square)](../LICENSE)
A standalone Blazor component library providing a fully-featured HTML5 video player with HLS live-stream support, casting, and a rich controls UI.

## Features

- **MP4 & HLS playback** — native video for standard files; HLS.js for adaptive live streams.
- **Full controls** — play/pause/stop, seek, volume, mute, playback speed, loop.
- **Fullscreen** — enter/exit fullscreen with keyboard shortcut and button.
- **Settings menu** — playback speed selector, loop toggle, video info panel.
- **Casting** — Chromecast support via the built-in cast bridge.
- **Progress bar** — interactive seek with buffered-range display.
- **Volume bar** — slider with mute shortcut.
- **Aspect ratio** — optional reserved-aspect-ratio mode.
- **Events** — exposes `TimeUpdate`, `VideoEvents`, and full `VideoPlayerMetadata` state.

## Installation

```bash
dotnet add package AngryMonkey.CloudComponents.VideoPlayer
```

## Setup

1. Add the script references to your `wwwroot/index.html` (Blazor WASM) or `_Host.cshtml` (Blazor Server):

```html
<!-- HLS.js (only needed for live/HLS streams) -->
<script src="_content/CloudComponents.VideoPlayer/hls.js"></script>
```

2. Register the namespace in `_Imports.razor`:

```razor
@using CloudComponents.VideoPlayer
```

## Usage

### Basic MP4 player

```razor
@code {
	private VideoPlayerMetadata Metadata = new()
	{
		Title = "My Video",
		VideoUrl = "https://example.com/video.mp4",
		EnableControls = true,
		ShowProgressBar = true,
		ShowSettings = true,
		Volume = 1.0
	};
}

<VideoPlayer Metadata="Metadata" />
```

### HLS live stream

```razor
@code {
	private VideoPlayerMetadata Metadata = new()
	{
		Title = "Live Channel",
		VideoUrl = "https://example.com/stream.m3u8",
		IsLive = true,
		EnableControls = true
	};
}

<VideoPlayer Metadata="Metadata" />
```

### With child content (header overlay)

```razor
<VideoPlayer Metadata="Metadata">
	<div class="my-overlay">Episode 1 — Introduction</div>
</VideoPlayer>
```

## `VideoPlayerMetadata` properties

| Property | Type | Default | Description |
|---|---|---|---|
| `VideoUrl` | `string?` | `null` | URL of the video or HLS `.m3u8` playlist |
| `Title` | `string?` | `null` | Display title shown in the settings panel |
| `IsLive` | `bool` | `false` | Treat the source as a live stream (hides progress bar) |
| `EnableControls` | `bool` | `true` | Show the control bar |
| `ShowProgressBar` | `bool` | `true` | Show the seek/progress bar |
| `ShowSettings` | `bool` | `true` | Show the settings menu button |
| `ShowStopButton` | `bool` | `false` | Show a stop button alongside play/pause |
| `Volume` | `double` | `1.0` | Initial volume (0.0 – 1.0) |
| `Autoplay` | `bool` | `false` | Start playback automatically |
| `Loop` | `bool` | `false` | Loop the video when it ends |
| `ReserveAspectRatio` | `bool` | `false` | Maintain 16:9 aspect ratio when controls are hidden |

## Customization

Override CSS variables for accent color and border radius:

```css
:root {
	--amc-videoplayer-accentColor: #4c9aff !important;
	--amc-videoplayer-roundedCorder: 4px !important;
}
```

## Demo

Run the `CloudComponents.VideoPlayer.Demo` project:

```bash
dotnet run --project CloudComponents.VideoPlayer.Demo
```

---

## Angry Monkey Cloud

This project is part of the [Angry Monkey Cloud](https://angrymonkeycloud.com) open-source ecosystem. Follow the shared [AI development instructions](https://github.com/angrymonkeycloud/CloudDocs/blob/main/docs/ai/instructions.md) and browse the [project catalog](https://angrymonkeycloud.com) and [GitHub organization](https://github.com/angrymonkeycloud).
