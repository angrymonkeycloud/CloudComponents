# CloudComponents.DataGrid
[![Website](https://img.shields.io/badge/Website-angrymonkeycloud.com-0B5FFF?style=flat-square&logo=googlechrome&logoColor=white)](https://angrymonkeycloud.com/cloudcomponents)
[![GitHub repository](https://img.shields.io/badge/GitHub-CloudComponents-181717?style=flat-square&logo=github)](https://github.com/angrymonkeycloud/CloudComponents)
[![NuGet](https://img.shields.io/nuget/v/AngryMonkey.CloudComponents.DataGrid?style=flat-square&logo=nuget)](https://www.nuget.org/packages/AngryMonkey.CloudComponents.DataGrid)
[![NuGet downloads](https://img.shields.io/nuget/dt/AngryMonkey.CloudComponents.DataGrid?style=flat-square&logo=nuget)](https://www.nuget.org/packages/AngryMonkey.CloudComponents.DataGrid)
[![.NET](https://img.shields.io/badge/.NET-10-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/UI-Blazor-5C2D91?style=flat-square&logo=blazor&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![License](https://img.shields.io/badge/License-MIT-2F855A?style=flat-square)](../LICENSE)

Production-ready Blazor data grid for .NET 10 with strongly typed models, server-driven data loading, paging modes, sorting, selection, row actions, reordering, export, and theme-friendly CSS variables.

> `CloudDataGrid` is the single entry point. It composes header, body, and footer internally and is driven by one required `DataProvider` callback.

---

## Table of contents

- [Features](#features)
- [Installation](#installation)
- [Quick start](#quick-start)
- [Data flow (`DataProvider`)](#data-flow-dataprovider)
- [Component API](#component-api)
  - [`CloudDataGrid` parameters](#cloudgrid-parameters)
  - [`CloudDataGridHeaderOptions`](#cloudgridheaderoptions)
- [Models reference](#models-reference)
- [Paging modes](#paging-modes)
- [Body height modes](#body-height-modes)
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
- Optional toolbar (`CloudDataGridHeaderOptions`) with built-in Search, Refresh, Export
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
dotnet add package AngryMonkey.CloudComponents.DataGrid
```

### Project reference

Reference `CloudComponents.DataGrid` from your Blazor app.

### Namespace import

Add where needed:

```razor
@using CloudComponents.DataGrid.Components
@using CloudComponents.DataGrid.Models
```

---

## Quick start

```razor
@using CloudComponents.DataGrid.Components
@using CloudComponents.DataGrid.Models

<CloudDataGrid Columns="_columns"
           DataProvider="LoadAsync"
           Header="_header"
           AllowSelection="true"
           @bind-SelectedRecords="_selected"
           RowsPerPage="25"
           PagingMode="CloudDataGridPagingMode.Pages" />

@code {
    private readonly List<Guid> _selected = [];

    private readonly List<CloudDataGridColumn> _columns =
    [
        new() { Label = "Photo", Key = "photo", IsImage = true, Sortable = false, Width = 84 },
        new() { Label = "Name", Key = "name", Width = 220 },
        new() { Label = "Email", Key = "email", Width = 280 },
        new() { Label = "Status", Key = "status", Width = 120 }
    ];

    private readonly CloudDataGridHeaderOptions _header = new()
    {
        Label = "Contacts",
        AllowSearch = true,
        AllowRefresh = true,
        AllowExport = true
    };

    private async Task<CloudDataGridDataResult?> LoadAsync(CloudDataGridDataRequest request)
    {
        // Query your backend with request.Page, request.PageSize, request.Search, request.Sort
        await Task.Delay(100);

        return new CloudDataGridDataResult
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

`CloudDataGrid` always calls a required callback:

```csharp
Func<CloudDataGridDataRequest, Task<CloudDataGridDataResult?>> DataProvider
```

Called on:
- initial load
- search query changes
- sort changes
- page changes
- reload/refresh

`CloudDataGridDataRequest` includes:
- `Page`
- `PageSize`
- `Search`
- `Sort`
- `IsAppend`
- `Total`

Return a `CloudDataGridDataResult` with `Rows`, `Page`, `PageSize`, `Total`, and optional `ErrorMessage`.

---

## Component API

### `CloudDataGrid` parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `Header` | `CloudDataGridHeaderOptions?` | `null` | Shows toolbar when provided. |
| `Columns` | `List<CloudDataGridColumn>` | `[]` | Column definitions in render order. |
| `DataProvider` | `Func<CloudDataGridDataRequest, Task<CloudDataGridDataResult?>>` | **required** | Data source callback for all grid state transitions. |
| `AllowSelection` | `bool` | `false` | Enables checkbox column + select-all behavior. |
| `SelectedRecords` | `List<Guid>?` | `null` | Selected row IDs (supports `@bind-SelectedRecords`). |
| `SelectedRecordsChanged` | `EventCallback<List<Guid>>` | — | Raised on selection updates. |
| `AllowReordering` | `bool` | `false` | Enables row drag handle and drop reordering. |
| `OnRowsReordered` | `EventCallback<CloudDataGridRowReorder>` | — | Fired after a row is dropped in a new position. |
| `OnActionClicked` | `EventCallback<CloudDataGridActionEventArgs>` | — | Fired for button actions (row/header/bulk). |
| `RowActions` | `List<CloudDataGridAction>?` | `null` | Explicit row actions (merged with `Header.Actions` that set `ShowOnRow`). |
| `ActionFilter` | `Func<CloudDataGridRow, CloudDataGridAction, bool>?` | `null` | Per-row action visibility filter. |
| `CssClass` | `string?` | `null` | Additional classes appended to `.cloudgrid`. |
| `LinkTarget` | `string` | `_parent` | Target for row links (`CloudDataGridRow.Link`). |
| `LoadingText` | `string` | `retrieving data...` | Loading status text. |
| `SearchingText` | `string` | `searching...` | Searching status text. |
| `EmptyCellText` | `string` | `--` | Placeholder for null/empty cells. |
| `RowHeight` | `double?` | `null` | Overrides `--cloudgrid-row-height` in px. |
| `RowsPerPage` | `int?` | `null` | Desired body rows in viewport and page size sent to provider. |
| `HeightMode` | `CloudDataGridHeightMode` | `FullHeight` | How the grid is sized vertically: `FullHeight` (default, fills parent), `RowHeight` (fixed to `RowsPerPage` rows), or `Auto` (grows with content, optionally capped by `MaxHeight`). |
| `MaxHeight` | `double?` | `null` | Maximum height in pixels when `HeightMode` is `Auto`. Ignored for other modes. |
| `PagingMode` | `CloudDataGridPagingMode` | `Pages` | `Pages`, `LoadMore`, or `InfiniteScroll`. |
| `LoadMoreText` | `string` | `load more` | Label for load-more button mode. |
| `AdditionalAttributes` | `Dictionary<string, object>?` | `null` | Captures unmatched attributes on root element. |

### `CloudDataGridHeaderOptions`

| Property | Type | Default | Description |
|---|---|---|---|
| `Label` | `string?` | `null` | Header title. |
| `Actions` | `List<CloudDataGridAction>` | `[]` | Unified action list for header/bulk/row/more menu placement. |
| `OnActionClicked` | `EventCallback<CloudDataGridActionEventArgs>` | — | Callback for button action clicks. |
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

- `CloudDataGridColumn`
  - `Label`, `Key`, `Width`, `MinWidth`, `Sortable`, `Resizable`, `IsImage`
- `CloudDataGridRow`
  - `Id`, `Link`, `Cells`, `Attributes`
- `CloudDataGridDataRequest`
  - `Page`, `PageSize`, `Search`, `Sort`, `IsAppend`, `Total`
- `CloudDataGridDataResult`
  - `Rows`, `Page`, `PageSize`, `Total`, `ErrorMessage`
- `CloudDataGridSort`
  - `Column`, `ColumnIndex`, `Direction`, `Key`

### Action models

- `CloudDataGridAction`
  - Supports `Link`, `Button`, `Element` modes via `CloudDataGridActionType`
  - Placement flags: `ShowOnHeader`, `ShowInMore`, `ShowOnBulkHeader`, `ShowOnRow`
  - Optional icon/text/tooltips and deactivation hooks for element actions
- `CloudDataGridActionEventArgs`
  - `Action`, `RecordIds`

### Enums

- `CloudDataGridPagingMode`: `Pages`, `LoadMore`, `InfiniteScroll`
- `CloudDataGridPaginationType`: `LeftArrow`, `RightArrow`
- `CloudDataGridSortDirection`: `Ascending`, `Descending`
- `CloudDataGridActionType`: `Link`, `Button`, `Element`

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

## Body height modes

### `FullHeight` (default)

- Grid fills the full height of its parent container
- Header and footer stay fixed, only the row area between them scrolls
- **Requires a parent with a definite height** (e.g., a fixed-height wrapper or a flex/grid item that stretches)
- Best for admin dashboards, split-pane layouts, or full-page grids

```razor
<div style="height: 600px;">
    <CloudDataGrid HeightMode="CloudDataGridHeightMode.FullHeight" ... />
</div>
```

### `RowHeight`

- Grid is exactly tall enough to show `RowsPerPage` rows (no more, no less)
- Calculated as `RowsPerPage × var(--cloudgrid-row-height)`
- The body scrolls internally if accumulating paging (`LoadMore`/`InfiniteScroll`) appends beyond that height
- Best for embedding a grid in a content flow without forcing a container height

```razor
<CloudDataGrid HeightMode="CloudDataGridHeightMode.RowHeight" RowsPerPage="10" ... />
```

### `Auto`

- Grid grows vertically to fit its content (all rows visible, no internal scroll by default)
- Optional `MaxHeight` parameter caps the grid's height — once content exceeds the cap, it behaves like `FullHeight`
- Best for small record sets or dynamic-height scenarios where you want to avoid an artificial viewport

```razor
<!-- No scroll until content grows past 500px -->
<CloudDataGrid HeightMode="CloudDataGridHeightMode.Auto" MaxHeight="500" ... />
```

---

## Sorting behavior

If `OnSortChanged` is handled:
- grid updates visual sort indicator
- consumer reloads data server-side using `CloudDataGridSort.Key` + `Direction`

If no handler is attached:
- grid sorts currently loaded rows locally
- nulls are pushed last
- mixed values are compared as case-insensitive text

---

## Actions (header, row, bulk, more)

You can define one `CloudDataGridAction` list and control placement using flags.

```csharp
new CloudDataGridAction
{
    Key = "delete-selected",
    Text = "Delete",
    Type = CloudDataGridActionType.Button,
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
<CloudDataGrid ... AllowSelection="true" @bind-SelectedRecords="_selected" />
```

Details:
- select-all operates on currently loaded rows
- selected IDs are exposed through two-way binding
- hidden input `#SelectedRecords` is rendered for legacy integrations

---

## Row reordering

Enable and listen:

```razor
<CloudDataGrid ... AllowReordering="true" OnRowsReordered="HandleReorder" />
```

`CloudDataGridRowReorder` provides:
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
- Verify callback returns non-null `CloudDataGridDataResult`.

### “Sorting doesn’t affect backend data”
- Implement `OnSortChanged` and apply `request.Sort` in your server query.

### “LoadMore/InfiniteScroll keeps requesting”
- In append mode, stop adding rows once end is reached; keep `Total` accurate.

### “No export action visible”
- Ensure `Header` is set and `AllowExport` is true.

---

## License

See repository licensing terms.

---

## Angry Monkey Cloud

This project is part of the [Angry Monkey Cloud](https://angrymonkeycloud.com) open-source ecosystem. Follow the shared [AI development instructions](https://github.com/angrymonkeycloud/CloudDocs/blob/main/docs/ai/instructions.md) and browse the [project catalog](https://angrymonkeycloud.com) and [GitHub organization](https://github.com/angrymonkeycloud).
