# Copilot Instructions

## Architecture

Multi-project Blazor Razor Class Library solution targeting **net10.0**. Each component is a standalone NuGet package (`AngryMonkey.CloudComponents.*`) with a paired demo app.

| Library | Entry Component | Demo |
|---------|----------------|------|
| `CloudComponents.Grid` | `CloudGrid` | `CloudComponents.Grid.Demo` |
| `CloudComponents.VideoPlayer` | `VideoPlayer` | `CloudComponents.VideoPlayer.Demo` |
| `CloudComponents.Maps` | `AzureMap` | `CloudComponents.Maps.Demo` |

See per-project READMEs for API details: [Grid](../CloudComponents.Grid/README.md), [VideoPlayer](../CloudComponents.VideoPlayer/README.md), [Maps](../CloudComponents.Maps/README.md).

**Active vs. legacy**: `CloudComponents.Grid`, `CloudComponents.VideoPlayer`, `CloudComponents.Maps` are the current libraries. The `Components/` folder is a legacy multi-component library (GridView, Dialog, Tabs, etc.) — maintained separately, not the target for new work. `BlazorApp1/`, `Demo/`, `ServerDemo/` are sample/template projects.

## Build

- **Build**: `dotnet build CloudComponents.sln`
- **Pack**: `dotnet pack` → output in `NugetPackage/`
- **LESS compilation**: Handled by Visual Studio's `compilerconfig.json` — do not run manually. The `.css` and `.min.css` files are generated artifacts.

## Code Style

### CSS/LESS

- **Edit `.less` source files only** — never modify generated `.css`/`.min.css` files.
- Nested BEM-like class structure: `.cloudgrid { &-headcell { ... } &-body { ... } }`
- State modifiers as adjective classes: `._busy`, `._loading`, `._selected`, `._resizing`
- Use `::deep` for styles targeting child component content under Blazor CSS isolation.
- Theme via CSS variables: `--cloudgrid-row-height`, `--cloudgrid-color`, etc.
- Main class hard-coded in markup; additional runtime classes appended via C# methods.

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