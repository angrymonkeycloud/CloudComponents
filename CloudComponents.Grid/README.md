# CloudComponents.Grid

Generic Blazor data grid components (`CloudGrid`, `CloudGridHeader`) — purely presentational, with paging, column sorting, column resizing, row selection and debounced search. All interactions are implemented in C#/Blazor; **no JavaScript interop**.

Available as a project reference or as the `AngryMonkey.CloudComponents.Grid` NuGet package.

## Components

| Component | Purpose |
|---|---|
| `CloudGrid` | The grid itself: column headers (sort + resize), rows, status messages and a paging footer. |
| `CloudGridHeader` | Optional toolbar placed above a grid: title, "open view" link, "new record" link and a debounced search box. |

## Quick start

```razor
@using CloudComponents.Grid.Components
@using CloudComponents.Grid.Models

<CloudGridHeader Label="Contacts"
                 NewUrl="/contacts/new"
                 OnSearchChanged="query => ReloadAsync(query)" />

<CloudGrid Columns="_columns"
           Data="_data"
           IsLoading="_loading"
           OnPageChanged="OnPageChangedAsync"
           OnSortChanged="OnSortChangedAsync" />

@code {
    private readonly List<CloudGridColumn> _columns =
    [
        new() { Label = "Photo", IsImage = true, Sortable = false, Width = 80 },
        new() { Label = "Name", Key = "name", Width = 220 },
        new() { Label = "Email", Key = "email", Width = 260 },
        new() { Label = "Notes", Sortable = false }
    ];

    private CloudGridDataResult? _data;
    private bool _loading = true;
}
```

`CloudGridDataResult` carries one page of data:

```csharp
_data = new CloudGridDataResult
{
    Page = 1,
    PageSize = 25,
    Total = 134,
    Rows = records.Select(r => new CloudGridRow
    {
        Id = r.Id,
        Link = $"/contacts/{r.Id}",                   // optional: makes the row a link
        Cells = [r.PhotoUrl, r.Name, r.Email, r.Notes] // one value per column; null renders as "--"
    }).ToList()
};
```

## CloudGrid parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `Columns` | `List<CloudGridColumn>` | `[]` | Column definitions, in display order. |
| `Data` | `CloudGridDataResult?` | `null` | Current page of data. |
| `IsLoading` / `IsSearching` | `bool` | `false` | Show the loading / searching status instead of rows. |
| `Message` | `string?` | `null` | Message displayed instead of rows (e.g. "No records."). |
| `AllowSelection` | `bool` | `false` | Renders a checkbox column (incl. select-all). |
| `SelectedRecords` | `List<Guid>?` | `null` | Selected row ids. Supports `@bind-SelectedRecords`. |
| `CssClass` | `string?` | `null` | Extra class(es) on the root element. |
| `LinkTarget` | `string` | `_parent` | Target of row links. |
| `LoadingText` / `SearchingText` | `string` | — | Status texts. |
| `EmptyCellText` | `string` | `--` | Placeholder for null cells. |
| `RowHeight` | `double?` | `null` | Fixed row height in pixels; overrides `--cloudgrid-row-height` (default 32). |
| `RowsPerPage` | `int?` | `null` | Sizes the table to exactly header + N rows — no vertical scrollbar, no leftover pixels. |
| `PagingMode` | `CloudGridPagingMode` | `Pages` | `Pages`, `LoadMore` or `InfiniteScroll` (see below). |
| `LoadMoreText` | `string` | `load more` | Text of the load-more button. |
| `OnPageChanged` | `EventCallback<CloudGridPaginationType>` | — | Page navigation (footer arrows, load-more click, or infinite-scroll trigger). |
| `OnSortChanged` | `EventCallback<CloudGridSort>` | — | User sorted a column (reload sorted data server side). |

Unmatched attributes (e.g. `data-*`) are rendered on the root element. Rows render their own `CloudGridRow.Attributes` too.

### Exact sizing

Set `RowHeight` (C#) to fix the row height — it is emitted as the `--cloudgrid-row-height` CSS variable on the root, so header, rows and image thumbnails all follow it. Set `RowsPerPage` to give the table an exact height of `(RowsPerPage + 1) × row height` (header + N body rows, all border-box) so a full page fits with no vertical scrollbar and no extra pixels:

```razor
<CloudGrid Columns="_columns" Data="_data" RowHeight="36" RowsPerPage="10" />
```

### Paging modes

- **`Pages`** (default) — footer pager with previous/next arrows; each page replaces the rows. With `RowsPerPage` set, the page fits exactly.
- **`LoadMore`** — a "load more" button after the last row raises `OnPageChanged(RightArrow)`; append the next page's rows to `Data.Rows` (and keep `Data.Total` accurate). With `RowsPerPage` set, rows scroll inside the fixed viewport.
- **`InfiniteScroll`** — rows are virtualized (built-in Blazor `Virtualize`, no JavaScript); when the user scrolls near the end of the loaded rows the grid raises `OnPageChanged(RightArrow)` automatically. Append rows exactly like `LoadMore`.

In the accumulating modes the grid stops requesting when a callback completes without adding rows, so a misbehaving source can't cause a request loop.

```csharp
private async Task OnPageChangedAsync(CloudGridPaginationType type)
{
    CloudGridDataResult next = await LoadPageAsync(_data!.Page + 1);

    _data = new CloudGridDataResult
    {
        Page = next.Page,
        PageSize = next.PageSize,
        Total = next.Total,
        Rows = [.. _data.Rows, .. next.Rows] // append for LoadMore / InfiniteScroll
    };
}
```

### Sorting

Click a sortable column header to sort ascending; click again to flip direction.

- **With** an `OnSortChanged` handler the grid only renders the indicator — reload `Data` yourself using `CloudGridSort.Key` and `Direction`. Sort the *entire* record set (re-query or re-fetch everything), not just the loaded page, so the result is correct across pages.
- **Without** a handler the grid sorts the rows it was given locally: same-typed values natively (numbers, dates), everything else as case-insensitive text, nulls last. This is only correct when `Data` contains the full record set.

Disable per column with `Sortable = false`.

### Image columns

Set `CloudGridColumn.IsImage = true` to render that column's cell values as image thumbnails. Cell values must be image URLs (strings); null or empty values fall back to `EmptyCellText`. Thumbnails are sized to the row height (`--cloudgrid-row-height`) and lazy-loaded by the browser.

```csharp
new CloudGridColumn { Label = "Photo", IsImage = true, Sortable = false, Width = 80 }
```

### Column resizing

Drag the right edge of a column header. Implemented with Blazor pointer events plus a transparent capture overlay — no JavaScript. `CloudGridColumn.MinWidth` limits shrinking; disable per column with `Resizable = false`.

### Selection

Set `AllowSelection="true"`; bind with `@bind-SelectedRecords`. A hidden `#SelectedRecords` input with the comma-joined ids is rendered for legacy integrations.

## CloudGridHeader parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `Label` | `string?` | `null` | Title text. |
| `ViewUrl` | `string?` | `null` | "Open view" link; hidden when empty. |
| `NewUrl` | `string?` | `null` | "New record" link; hidden when empty. |
| `NewButtonText` | `string` | `new` | Text of the new button. |
| `AllowSearch` | `bool` | `true` | Show the search button/box. |
| `SearchDebounceMilliseconds` | `int` | `300` | Typing debounce before the callback fires. |
| `OnSearchChanged` | `EventCallback<string?>` | — | Debounced query; `null` when cleared. |

## Styling

CSS classes follow the [Angry Monkey CSS naming convention](https://github.com/angrymonkeydocs/css-naming-convention): one block class per component (`.cloudgrid`, `.cloudgridheader`), dash-prefixed children (`.cloudgrid-row`, `.cloudgrid-cell`), underscore adjectives (`._busy`, `._sorted-ascending`, `._selected`).

Styles are authored in `*.razor.less` (compiled to scoped `*.razor.css` by Web Compiler — never edit the `.css` directly).

### Theming with CSS custom properties

All design tokens are modern CSS variables with built-in fallback defaults. Override them from any ancestor (e.g. `:root`) — no recompilation needed:

| Variable | Default | Used for |
|---|---|---|
| `--cloudgrid-font-size` | `14px` | Base font size. |
| `--cloudgrid-row-height` | `32px` | Header/body row height and thumbnail size. |
| `--cloudgrid-color` | `#505050` | Text color. |
| `--cloudgrid-background` | `#fff` | Grid background. |
| `--cloudgrid-accent-color` | `#000` | Sort chevron, pager arrows, resize handle, icons. |
| `--cloudgrid-border-color` | `rgba(0,0,0,0.15)` | Header/footer borders, search box border. |
| `--cloudgrid-head-background` | `#f8f8f8` | Sticky header background. |
| `--cloudgrid-hover-background` | `#e8e8e8` | Row/header/action hover. |
| `--cloudgrid-selected-background` | `#f8f8f8` | Selected row background. |

`CloudGridHeader` reads `--cloudgridheader-*` equivalents (`-color`, `-accent-color`, `-border-color`, `-hover-background`, `-font-size`), each falling back to the matching `--cloudgrid-*` variable — so a single `--cloudgrid-*` override themes the whole family, while `--cloudgridheader-*` allows targeting the header alone.

```css
:root {
    --cloudgrid-accent-color: #0a5dc2;
    --cloudgrid-row-height: 40px;
}
```

Key hooks for consumers (use `::deep` from a parent scoped stylesheet):

- `.cloudgrid` — root; `._busy`, `._resizing`, `._selectable` state adjectives.
- `.cloudgrid-head` / `.cloudgrid-headcell` — sticky header row; `._sortable`, `._sorted-ascending`, `._sorted-descending`.
- `.cloudgrid-row` — `._selected` when checked.
- `.cloudgrid-status` — `._loading`, `._searching`, `._message`.
