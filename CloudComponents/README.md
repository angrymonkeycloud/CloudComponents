# CloudComponents Core

[![Website](https://img.shields.io/badge/Website-angrymonkeycloud.com-0B5FFF?style=flat-square&logo=googlechrome&logoColor=white)](https://angrymonkeycloud.com/cloudcomponents)
[![GitHub repository](https://img.shields.io/badge/GitHub-CloudComponents-181717?style=flat-square&logo=github)](https://github.com/angrymonkeycloud/CloudComponents)
[![NuGet](https://img.shields.io/nuget/v/AngryMonkey.CloudComponents?style=flat-square&logo=nuget)](https://www.nuget.org/packages/AngryMonkey.CloudComponents)
[![NuGet downloads](https://img.shields.io/nuget/dt/AngryMonkey.CloudComponents?style=flat-square&logo=nuget)](https://www.nuget.org/packages/AngryMonkey.CloudComponents)
[![.NET](https://img.shields.io/badge/.NET-10-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/UI-Blazor-5C2D91?style=flat-square&logo=blazor&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![License](https://img.shields.io/badge/License-MIT-2F855A?style=flat-square)](../LICENSE)

Core Blazor UI primitives for dialogs, popups, switches, tabs, progress indicators, volume controls, and safely rendered Markdown documentation.

## Installation

```bash
dotnet add package AngryMonkey.CloudComponents
```

## Components

| Component | Purpose |
| --- | --- |
| `PopupComp` | Programmatically controlled popup content |
| `Dialog` | Modal confirmation and action workflows |
| `Switch` | Boolean and optional tri-state input |
| `Tabs` | Structured tab navigation |
| `ProgressBar` | Interactive or display-only progress |
| `VolumeBar` | Compact audio volume input |
| `CloudMarkdown` | Remote or inline Markdown renderer with safe HTML handling, route-aware links, and fragment navigation |

## CloudMarkdown

`CloudMarkdown` renders Markdown from a URL or an in-memory string without inheriting surrounding page typography. It supports GitHub raw and blob URLs, rewrites relative GitHub README links through `DocumentRoutes`, and scrolls fragment links without retaining a hash in the address bar.

Register the HTTP client once in your host application:

```csharp
builder.Services.AddHttpClient();
```

Then add the namespace and use the component:

```razor
@using AngryMonkey.CloudComponents.Markdown

<CloudMarkdown SourceUrl="https://raw.githubusercontent.com/owner/repository/main/README.md"
               DocumentRoute="/documentation"
               DocumentRoutes="@_documentRoutes" />
```

Set `Content` for Markdown already loaded by your application. Supply `DocumentUrl` with `Content` when it contains relative links. `LoadingTemplate`, `ErrorTemplate`, `EmptyTemplate`, and `Class` provide presentation customization. The component resets inherited page styles and owns its complete Markdown design. Pass theme overrides through `Style` using component-local variables such as `--cloud-markdown-link`, `--cloud-markdown-surface`, and `--cloud-markdown-font-size`. Raw HTML is deliberately disabled; this keeps remotely hosted documentation safe by default.

## Angry Monkey Cloud

This package is part of the [Angry Monkey Cloud](https://angrymonkeycloud.com) open-source ecosystem. Follow the shared [AI development instructions](https://github.com/angrymonkeycloud/CloudDocs/blob/main/docs/ai/instructions.md) and browse the [GitHub organization](https://github.com/angrymonkeycloud) for related projects.
