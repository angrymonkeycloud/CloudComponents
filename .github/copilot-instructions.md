# Copilot Instructions

## General AI-Assisted Development

For general AI-assisted development guidance, C# style, static assets, and documentation standards that apply to this repository, see:

- [AI Instructions](https://github.com/angrymonkeycloud/CloudDocs/blob/main/docs/ai/instructions.md)

**Note**: Project-specific instructions below take precedence when conflicts exist.

## Architecture

Multi-project Blazor Razor Class Library solution targeting **net10.0**. Each component is a standalone NuGet package (`AngryMonkey.CloudComponents.*`) with a paired demo app.

| Library | Entry Component | Demo |
|---------|----------------|------|
| `CloudComponents.DataGrid` | `CloudGrid` | `CloudComponents.DataGrid.Demo` |
| `CloudComponents.VideoPlayer` | `VideoPlayer` | `CloudComponents.VideoPlayer.Demo` |
| `CloudComponents.Maps` | `AzureMap` | `CloudComponents.Maps.Demo` |

See per-project READMEs for API details: [Grid](../CloudComponents.DataGrid/README.md), [VideoPlayer](../CloudComponents.VideoPlayer/README.md), [Maps](../CloudComponents.Maps/README.md).

**Active vs. legacy**: `CloudComponents.DataGrid`, `CloudComponents.VideoPlayer`, `CloudComponents.Maps` are the current libraries. The `Components/` folder is a legacy multi-component library (GridView, Dialog, Tabs, etc.) — maintained separately, not the target for new work. `BlazorApp1/`, `Demo/`, `ServerDemo/` are sample/template projects.

## Build

- **Build**: `dotnet build CloudComponents.sln`
- **Pack**: `dotnet pack` → output in `NugetPackage/`
- **LESS compilation**: Handled by CloudMate. Always create or update `.less` sources and never hand-author `.css` or `.min.css` files. Agents must leave generated CSS unchanged and may let the developer run CloudMate later.

## Code Style

### Component Structure

- **Grid pattern**: Parent component (`CloudGrid`) + child components (`CloudGridHeader`, `CloudGridBody`, `CloudGridFooter`), each with `.razor`, `.razor.cs`, `.razor.less`.
- **VideoPlayer pattern**: Single Razor file + partial classes split by concern (`VideoPlayer.Playback.cs`, `VideoPlayer.Volume.cs`, `VideoPlayer.FullScreen.cs`, etc.).
- **Maps pattern**: When adding substantial new feature areas to a Blazor component, split the logic into partial classes (e.g., `AzureMap.Search.cs`, `AzureMap.LocationLock.cs`) rather than growing the main component file.
- Models in `Models/` folder, services in `Services/`, extensions in `Extensions/`.

### JavaScript Interop

- Use module isolation: `IJSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PackageName/script.js")`
- JS modules live in `wwwroot/` of each component library.
- Service classes for interop should be `sealed`.

### Icons

- `CloudComponents.Icons` provides `.razor` icon components (Play, Pause, FullScreen, Sort, Search, etc.) and brand logos.
- Consumed by importing the component: `<PlayIcon />`, `<GoogleLogo />` etc. — no additional CSS import needed for icons.

## Conventions

- Data flow uses async callbacks — Grid's `DataProvider`: `Func<CloudGridDataRequest, Task<CloudGridDataResult?>` returns paginated/filtered results.
- Event callbacks follow the pattern: `OnActionClicked`, `OnSearchChanged`, `OnSelectionChanged`.
- Demo pages are standalone Blazor WebAssembly or Server apps in separate projects — each page showcases one feature.
- NuGet package IDs use prefix `AngryMonkey.CloudComponents.*`.
- No CI/CD workflows — packaging is done locally via `dotnet pack`.
- .NET upgrade plan tracked in [.github/upgrades/dotnet-upgrade-plan.md](upgrades/dotnet-upgrade-plan.md).
