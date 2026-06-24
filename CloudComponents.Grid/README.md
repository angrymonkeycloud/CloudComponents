# CloudComponents.Grid

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/UI-Blazor-5C2D91?logo=blazor)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![NuGet](https://img.shields.io/nuget/v/AngryMonkey.CloudComponents.Grid?logo=nuget)](https://www.nuget.org/packages/AngryMonkey.CloudComponents.Grid)
[![NuGet Downloads](https://img.shields.io/nuget/dt/AngryMonkey.CloudComponents.Grid?logo=nuget)](https://www.nuget.org/packages/AngryMonkey.CloudComponents.Grid)

Production-ready Blazor data grid for .NET 10 with strongly typed models, server-driven data loading, paging modes, sorting, selection, row actions, reordering, export, and theme-friendly CSS variables.

> `CloudGrid` is the single entry point. It composes header, body, and footer internally and is driven by one required `DataProvider` callback.

---

## Table of contents

- [Features](#features)
- [Installation](#installation)
- [Quick start](#quick-start)
- [Data flow (`DataProvider`)](#data-flow-dataprovider)
- [Component API](#component-api)
  - [`CloudGrid` parameters](#cloudgrid-parameters)
  - [`CloudGridHeaderOptions`](#cloudgridheaderoptions)
- [Models reference](#models-reference)
- [Paging modes](#paging-modes)
- [Sorting behavior](#sorting-behavior)
- [Actions (header, row, bulk, more)](#actions-header-row-bulk-more)
- [Selection](#selection)
- [Row reordering](#row-reordering)
- [Export](#export)
- [Styling and theming](#styling-and-theming)
- [Troubleshooting](#troubleshooting)

---

## Features

- Pure Blazor interaction model for grid UX (sorting, resizing, selection, reordering)
- Required async `DataProvider` pattern for consistent load/search/sort/page orchestration
- Optional toolbar (`CloudGridHeaderOptions`) with built-in Search, Refresh, Export
- Header actions + row actions + bulk actions + more-menu actions
- Paging modes:
  - `Pages`
  - `LoadMore`
  - `InfiniteScroll` (via built-in Blazor virtualization)
- Runtime CSV export for:
  - current page
  - all records (respecting active search/sort)
  - selected records
- Row link support + row-level arbitrary HTML attributes
- CSS-variable theming + predictable class naming

---

## Installation

### NuGet

```powershell
dotnet add package AngryMonkey.CloudComponents.Grid
```

### Project reference

Reference `CloudComponents.Grid` from your Blazor app.

### Namespace import

Add where needed:

```razor
@using CloudComponents.Grid.Components
@using CloudComponents.Grid.Models
```

---

## Quick start

```razor
@using CloudComponents.Grid.Components
@using CloudComponents.Grid.Models

<CloudGrid Columns="_columns"
           DataProvider="LoadAsync"
           Header="_header"
           AllowSelection="true"
           @bind-SelectedRecords="_selected"
           RowsPerPage="25"
           PagingMode="CloudGridPagingMode.Pages" />

@code {
    private readonly List<Guid> _selected = [];

    private readonly List<CloudGridColumn> _columns =
    [
        new() { Label = "Photo", Key = "photo", IsImage = true, Sortable = false, Width = 84 },
        new() { Label = "Name", Key = "name", Width = 220 },
        new() { Label = "Email", Key = "email", Width = 280 },
        new() { Label = "Status", Key = "status", Width = 120 }
    ];

    private readonly CloudGridHeaderOptions _header = new()
    {
        Label = "Contacts",
        AllowSearch = true,
        AllowRefresh = true,
        AllowExport = true
    };

    private async Task<CloudGridDataResult?> LoadAsync(CloudGridDataRequest request)
    {
        // Query your backend with request.Page, request.PageSize, request.Search, request.Sort
        await Task.Delay(100);

        return new CloudGridDataResult
        {
            Page = request.Page,
            PageSize = request.PageSize,
            Total = 2,
            Rows =
            [
                new()
                {
                    Id = Guid.NewGuid(),
                    Link = "/contacts/1",
                    Cells = ["/images/p1.jpg", "Mia Carter", "mia@domain.com", "Active"]
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Link = "/contacts/2",
                    Cells = ["/images/p2.jpg", "Noah Brooks", "noah@domain.com", "Inactive"]
                }
            ]
        };
    }
}
```

---

## Data flow (`DataProvider`)

`CloudGrid` always calls a required callback:

```csharp
Func<CloudGridDataRequest, Task<CloudGridDataResult?>> DataProvider
```

Called on:
- initial load
- search query changes
- sort changes
- page changes
- reload/refresh

`CloudGridDataRequest` includes:
- `Page`
- `PageSize`
- `Search`
- `Sort`
- `IsAppend`
- `Total`

Return a `CloudGridDataResult` with `Rows`, `Page`, `PageSize`, `Total`, and optional `ErrorMessage`.

---

## Component API

### `CloudGrid` parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `Header` | `CloudGridHeaderOptions?` | `null` | Shows toolbar when provided. |
| `Columns` | `List<CloudGridColumn>` | `[]` | Column definitions in render order. |
| `DataProvider` | `Func<CloudGridDataRequest, Task<CloudGridDataResult?>>` | **required** | Data source callback for all grid state transitions. |
| `AllowSelection` | `bool` | `false` | Enables checkbox column + select-all behavior. |
| `SelectedRecords` | `List<Guid>?` | `null` | Selected row IDs (supports `@bind-SelectedRecords`). |
| `SelectedRecordsChanged` | `EventCallback<List<Guid>>` | — | Raised on selection updates. |
| `AllowReordering` | `bool` | `false` | Enables row drag handle and drop reordering. |
| `OnRowsReordered` | `EventCallback<CloudGridRowReorder>` | — | Fired after a row is dropped in a new position. |
| `OnActionClicked` | `EventCallback<CloudGridActionEventArgs>` | — | Fired for button actions (row/header/bulk). |
| `RowActions` | `List<CloudGridAction>?` | `null` | Explicit row actions (merged with `Header.Actions` that set `ShowOnRow`). |
| `ActionFilter` | `Func<CloudGridRow, CloudGridAction, bool>?` | `null` | Per-row action visibility filter. |
| `CssClass` | `string?` | `null` | Additional classes appended to `.cloudgrid`. |
| `LinkTarget` | `string` | `_parent` | Target for row links (`CloudGridRow.Link`). |
| `LoadingText` | `string` | `retrieving data...` | Loading status text. |
| `SearchingText` | `string` | `searching...` | Searching status text. |
| `EmptyCellText` | `string` | `--` | Placeholder for null/empty cells. |
| `RowHeight` | `double?` | `null` | Overrides `--cloudgrid-row-height` in px. |
| `RowsPerPage` | `int?` | `null` | Desired body rows in viewport and page size sent to provider. |
| `PagingMode` | `CloudGridPagingMode` | `Pages` | `Pages`, `LoadMore`, or `InfiniteScroll`. |
| `LoadMoreText` | `string` | `load more` | Label for load-more button mode. |
| `AdditionalAttributes` | `Dictionary<string, object>?` | `null` | Captures unmatched attributes on root element. |

### `CloudGridHeaderOptions`

| Property | Type | Default | Description |
|---|---|---|---|
| `Label` | `string?` | `null` | Header title. |
| `Actions` | `List<CloudGridAction>` | `[]` | Unified action list for header/bulk/row/more menu placement. |
| `OnActionClicked` | `EventCallback<CloudGridActionEventArgs>` | — | Callback for button action clicks. |
| `AllowSearch` | `bool` | `false` | Enables built-in search element action. |
| `SearchDebounceMilliseconds` | `int` | `300` | Debounce delay for search callback. |
| `OnSearchChanged` | `EventCallback<string?>` | — | Debounced search query callback. |
| `AllowRefresh` | `bool` | `true` | Enables built-in refresh button action. |
| `OnRefresh` | `EventCallback` | — | Callback for built-in refresh click. |
| `AllowExport` | `bool` | `true` | Injects built-in export action in More menu. |
| `ExtraActions` | `RenderFragment?` | `null` | Optional custom trailing content in header action area. |

---

## Models reference

### Core data models

- `CloudGridColumn`
  - `Label`, `Key`, `Width`, `MinWidth`, `Sortable`, `Resizable`, `IsImage`
- `CloudGridRow`
  - `Id`, `Link`, `Cells`, `Attributes`
- `CloudGridDataRequest`
  - `Page`, `PageSize`, `Search`, `Sort`, `IsAppend`, `Total`
- `CloudGridDataResult`
  - `Rows`, `Page`, `PageSize`, `Total`, `ErrorMessage`
- `CloudGridSort`
  - `Column`, `ColumnIndex`, `Direction`, `Key`

### Action models

- `CloudGridAction`
  - Supports `Link`, `Button`, `Element` modes via `CloudGridActionType`
  - Placement flags: `ShowOnHeader`, `ShowInMore`, `ShowOnBulkHeader`, `ShowOnRow`
  - Optional icon/text/tooltips and deactivation hooks for element actions
- `CloudGridActionEventArgs`
  - `Action`, `RecordIds`

### Enums

- `CloudGridPagingMode`: `Pages`, `LoadMore`, `InfiniteScroll`
- `CloudGridPaginationType`: `LeftArrow`, `RightArrow`
- `CloudGridSortDirection`: `Ascending`, `Descending`
- `CloudGridActionType`: `Link`, `Button`, `Element`

---

## Paging modes

### `Pages` (default)

- Footer shows previous/next paging controls
- New page replaces current rows
- Best for classic server paging

### `LoadMore`

- Renders a load-more button at list end
- Emits next-page request and expects rows to be appended

### `InfiniteScroll`

- Uses Blazor virtualization to fetch next page near list end
- Also expects append behavior

In append modes, ensure your `DataProvider` returns incremented `Page` and updated `Total`.

---

## Sorting behavior

If `OnSortChanged` is handled:
- grid updates visual sort indicator
- consumer reloads data server-side using `CloudGridSort.Key` + `Direction`

If no handler is attached:
- grid sorts currently loaded rows locally
- nulls are pushed last
- mixed values are compared as case-insensitive text

---

## Actions (header, row, bulk, more)

You can define one `CloudGridAction` list and control placement using flags.

```csharp
new CloudGridAction
{
    Key = "delete-selected",
    Text = "Delete",
    Type = CloudGridActionType.Button,
    ShowOnBulkHeader = true,
    ShowOnHeader = false
}
```

Placement guidance:
- `ShowOnHeader = true`: direct header action area
- `ShowInMore = true`: appears in More (⋯) menu
- `ShowOnBulkHeader = true`: appears only when rows are selected
- `ShowOnRow = true`: appears per row

`Element` actions can expand inline content and are cancellable.

---

## Selection

Enable row selection:

```razor
<CloudGrid ... AllowSelection="true" @bind-SelectedRecords="_selected" />
```

Details:
- select-all operates on currently loaded rows
- selected IDs are exposed through two-way binding
- hidden input `#SelectedRecords` is rendered for legacy integrations

---

## Row reordering

Enable and listen:

```razor
<CloudGrid ... AllowReordering="true" OnRowsReordered="HandleReorder" />
```

`CloudGridRowReorder` provides:
- moved `RecordId`
- `OldIndex`, `NewIndex`
- full `OrderedRecordIds` after drop

---

## Export

When `Header.AllowExport = true` (default), grid injects an export action in the More menu.

Built-in export options:
- Current page
- All records (using current search/sort)
- Selection (only shown when rows are selected)

Files are downloaded as CSV using the package JS module.

---

## Styling and theming

The component uses scoped styles authored in `*.razor.less` and compiled to `*.razor.css`.

### Core classes

- `.cloudgrid`
- `.cloudgrid-head`, `.cloudgrid-headcell`
- `.cloudgrid-row`, `.cloudgrid-cell`
- `.cloudgrid-status`
- `.cloudgridheader`, `.cloudgridheader-action`

### State classes

- `._busy`
- `._resizing`
- `._rowdragging`
- `._selectable`
- sorted state adjectives on head cells

### CSS variables

| Variable | Default |
|---|---|
| `--cloudgrid-font-size` | `14px` |
| `--cloudgrid-row-height` | `32px` |
| `--cloudgrid-color` | `#505050` |
| `--cloudgrid-background` | `#fff` |
| `--cloudgrid-accent-color` | `#000` |
| `--cloudgrid-border-color` | `rgba(0,0,0,0.15)` |
| `--cloudgrid-head-background` | `#f8f8f8` |
| `--cloudgrid-hover-background` | `#e8e8e8` |
| `--cloudgrid-selected-background` | `#f8f8f8` |

Header-specific variables (`--cloudgridheader-*`) fall back to matching `--cloudgrid-*` values.

Example:

```css
:root {
    --cloudgrid-accent-color: #0a5dc2;
    --cloudgrid-row-height: 40px;
}
```

---

## Troubleshooting

### “Grid never loads data”
- Confirm `DataProvider` is assigned.
- Verify callback returns non-null `CloudGridDataResult`.

### “Sorting doesn’t affect backend data”
- Implement `OnSortChanged` and apply `request.Sort` in your server query.

### “LoadMore/InfiniteScroll keeps requesting”
- In append mode, stop adding rows once end is reached; keep `Total` accurate.

### “No export action visible”
- Ensure `Header` is set and `AllowExport` is true.

---

## License

See repository licensing terms.
