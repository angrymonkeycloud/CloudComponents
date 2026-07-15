# CloudComponents TextEditor
[![Website](https://img.shields.io/badge/Website-angrymonkeycloud.com-0B5FFF?style=flat-square&logo=googlechrome&logoColor=white)](https://angrymonkeycloud.com/cloudcomponents)
[![GitHub repository](https://img.shields.io/badge/GitHub-CloudComponents-181717?style=flat-square&logo=github)](https://github.com/angrymonkeycloud/CloudComponents)
[![NuGet](https://img.shields.io/nuget/v/AngryMonkey.CloudComponents.TextEditor?style=flat-square&logo=nuget)](https://www.nuget.org/packages/AngryMonkey.CloudComponents.TextEditor)
[![NuGet downloads](https://img.shields.io/nuget/dt/AngryMonkey.CloudComponents.TextEditor?style=flat-square&logo=nuget)](https://www.nuget.org/packages/AngryMonkey.CloudComponents.TextEditor)
[![.NET](https://img.shields.io/badge/.NET-10-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/UI-Blazor-5C2D91?style=flat-square&logo=blazor&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![License](https://img.shields.io/badge/License-MIT-2F855A?style=flat-square)](../LICENSE)

Rich text editor for Blazor on .NET 10. Semantic HTML output, configurable toolbar, media embedding (images, YouTube/Vimeo/file video), and an HTML code view with built-in validation — all without JavaScript dependencies beyond the bundled module.

> `CloudTextEditor` is the single entry point. Bind `@bind-Value` to an HTML string and configure everything else through parameters.

---

## Table of contents

- [Features](#features)
- [Installation](#installation)
- [Quick start](#quick-start)
- [Component API](#component-api)
  - [`CloudTextEditor` parameters](#cloudeditor-parameters)
  - [Public methods](#public-methods)
  - [`CloudTextEditorToolbarOptions`](#cloudeditortoolbaroptions)
- [Heading level offset](#heading-level-offset)
- [Code view and validation](#code-view-and-validation)
- [Images and uploads](#images-and-uploads)
- [Video embedding](#video-embedding)
- [Styling and theming](#styling-and-theming)
- [Known limitations](#known-limitations)
- [Troubleshooting](#troubleshooting)
- [Changelog](#changelog)

---

## Features

- Inline formatting: bold, italic, underline, strikethrough, subscript, superscript, inline `<code>`
- Block formats: paragraph, headings (with configurable first heading level), blockquote, code block
- Text color and highlight color (palette + custom picker)
- Alignment (left / center / right / justify), bulleted and numbered lists, indentation
- Links (Ctrl+K shortcut, open-in-new-tab option), images (URL or file upload), video embeds (YouTube, Vimeo, direct files), horizontal rules
- Undo / redo, clear formatting, fullscreen mode
- HTML **code view** with live validation (tag matching, attribute quoting, void elements, implied end tags) — invalid HTML cannot be applied
- Paste sanitization (allowlist-based; strips scripts, event handlers and unknown tags)
- Placeholder, read-only mode, min/max/fixed height, debounced two-way binding
- Theme-friendly CSS variables — no font-family controls by design

## Installation

```shell
dotnet add package AngryMonkey.CloudComponents.TextEditor
```

## Quick start

Add the namespaces (once, in `_Imports.razor` for the whole project):

```razor
@using AngryMonkey.CloudComponents.TextEditor.Components
@using AngryMonkey.CloudComponents.TextEditor.Models
@using AngryMonkey.CloudComponents.TextEditor.Services
```

Then use the component:

```razor
<CloudTextEditor @bind-Value="_html" Placeholder="Start writing…" />

@code {
    private string? _html;
}
```

## Component API

### `CloudTextEditor` parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `Value` / `ValueChanged` | `string?` | `null` | Editor content as HTML. Supports `@bind-Value`. |
| `Placeholder` | `string?` | `null` | Shown while the editor is empty. |
| `ReadOnly` | `bool` | `false` | Disables all editing. |
| `Spellcheck` | `bool` | `true` | Browser spell checking inside the editor. |
| `MinHeight` | `string` | `"250px"` | Minimum height of the editing surface. |
| `MaxHeight` | `string?` | `null` | Content scrolls beyond this height. |
| `Height` | `string?` | `null` | Fixed height — overrides `MinHeight`/`MaxHeight`. |
| `FirstHeadingLevel` | `int` | `1` | HTML level produced by the first "Heading" entry (see [below](#heading-level-offset)). |
| `HeadingCount` | `int` | `4` | Number of heading entries in the dropdown (auto-capped at `h6`). |
| `Toolbar` | `CloudTextEditorToolbarOptions` | all `true` | Feature toggles per tool group. |
| `SanitizePaste` | `bool` | `true` | Sanitizes HTML pasted from the clipboard. |
| `DebounceMilliseconds` | `int` | `250` | Delay before `Value` updates while typing. Always flushed on blur. |
| `ImageUploadHandler` | `Func<CloudTextEditorFileUpload, Task<string?>>?` | `null` | Persists an uploaded image and returns its URL. Falls back to inline data URLs when `null`. |
| `MaxUploadSize` | `long` | `5242880` (5 MB) | Maximum accepted upload size in bytes. |
| `OnChanged` | `EventCallback<string?>` | — | Raised after content changes (after debounce). |
| `OnFocus` | `EventCallback` | — | Raised when the editing surface receives focus. |
| `OnBlur` | `EventCallback` | — | Raised when the editing surface loses focus. |

### Public methods

| Method | Description |
|---|---|
| `FocusAsync()` | Moves keyboard focus into the editing surface. |
| `InsertHtmlAsync(string html)` | Inserts raw HTML at the current caret position. |

### `CloudTextEditorToolbarOptions`

All flags default to `true`. Set any to `false` to hide that group:

| Flag | Controls |
|---|---|
| `Headings` | Block format dropdown (paragraph + headings) |
| `BasicFormatting` | Bold, italic, underline, strikethrough |
| `Script` | Subscript, superscript |
| `InlineCode` | Inline `<code>` toggle |
| `TextColor` | Text color picker |
| `HighlightColor` | Highlight / background color picker |
| `Alignment` | Left, center, right, justify |
| `Lists` | Unordered and ordered lists |
| `Indentation` | Indent / outdent |
| `Blockquote` | Blockquote toggle |
| `CodeBlock` | Code block (`<pre>`) toggle |
| `Link` | Link dialog and Ctrl+K shortcut |
| `Image` | Image dialog (URL + upload) |
| `Video` | Video embed dialog |
| `HorizontalRule` | Insert horizontal rule |
| `ClearFormatting` | Remove all inline formatting |
| `UndoRedo` | Undo and redo buttons |
| `CodeView` | HTML source view with validation |
| `Fullscreen` | Fullscreen toggle |

```razor
<CloudTextEditor @bind-Value="_html"
             Toolbar="new CloudTextEditorToolbarOptions { Video = false, Fullscreen = false }" />
```

## Heading level offset

`FirstHeadingLevel` remaps the heading dropdown so the document outline can start below `h1` — useful when the page already owns its `<h1>`:

```razor
<CloudTextEditor @bind-Value="_html" FirstHeadingLevel="2" HeadingCount="3" />
```

With this configuration the dropdown shows **Heading 1 → `<h2>`**, **Heading 2 → `<h3>`**, **Heading 3 → `<h4>`**. Entries never map beyond `<h6>`.

## Code view and validation

The `</>` toolbar button switches to an HTML source editor. Source is validated live by `CloudTextEditorHtmlValidator`:

- **Errors** (block Apply): unclosed/mismatched tags, unterminated comments and attribute quotes, stray closing tags
- **Warnings** (informational, do not block): `script`/`style`/`iframe` elements, inline `on*` event handlers, duplicate attributes, closing tags on void elements

**Apply** is enabled only while the source is error-free. **Cancel** discards source changes.

The validator is also available as a standalone static API:

```csharp
using AngryMonkey.CloudComponents.TextEditor.Services;

CloudTextEditorHtmlValidationResult result = CloudTextEditorHtmlValidator.Validate(html);

if (!result.IsValid)
{
    foreach (CloudTextEditorHtmlValidationMessage error in result.Errors)
        Console.WriteLine(error); // "Error (3,8): Unclosed tag <div>."
}

foreach (CloudTextEditorHtmlValidationMessage warning in result.Warnings)
    Console.WriteLine(warning); // "Warning (1,1): <script> tag is discouraged."
```

## Images and uploads

The image dialog supports URL insertion and file upload. Without an `ImageUploadHandler`, uploaded files are embedded inline as data URLs (not recommended for large images). With one, you control storage:

```razor
<CloudTextEditor @bind-Value="_html"
             ImageUploadHandler="UploadAsync"
             MaxUploadSize="10485760" />

@code {
    private async Task<string?> UploadAsync(CloudTextEditorFileUpload file)
    {
        await using Stream stream = file.OpenReadStream();
        // Persist to blob storage / an API and return the public URL.
        return await _storage.SaveAsync(file.FileName, file.ContentType, stream);
        // Return null to cancel the insertion.
    }
}
```

`CloudTextEditorFileUpload` exposes `FileName`, `ContentType`, `Size` (bytes), and `OpenReadStream()`.

## Video embedding

The video dialog detects the URL type automatically:

| URL pattern | Embed produced |
|---|---|
| YouTube watch / short / embed | Privacy-enhanced `youtube-nocookie.com` `<iframe>` |
| Vimeo player / video link | `player.vimeo.com` `<iframe>` |
| `.mp4`, `.webm`, `.ogg`, `.ogv`, `.mov` | Native `<video controls>` |

All embeds render responsive at 16 : 9 inside `<figure class="cloudeditor-embed">`.

## Styling and theming

All colors and metrics are CSS variables. Set them on any ancestor element to theme the editor:

```css
.my-container {
    --cloudeditor-accent:       #6b30c9;
    --cloudeditor-bg:           #1e1e24;
    --cloudeditor-color:        #eee;
    --cloudeditor-toolbar-bg:   #26262e;
    --cloudeditor-border-color: #3a3a44;
    --cloudeditor-code-bg:      #16161a;
    --cloudeditor-hover-bg:     rgba(255, 255, 255, 0.08);
    --cloudeditor-active-bg:    rgba(107, 48, 201, 0.25);
    --cloudeditor-muted:        #9a9aa5;
}
```

Additional variables: `--cloudeditor-font-size`, `--cloudeditor-radius`, `--cloudeditor-readonly-bg`, `--cloudeditor-quote-bg`, `--cloudeditor-error`, `--cloudeditor-warning`, `--cloudeditor-success`.

## Known limitations

- **`document.execCommand`** — the editor uses the browser's built-in `execCommand` API for most formatting operations. This API is marked as deprecated in the HTML spec but remains fully supported in all modern browsers and Chromium-based WebAssembly runtimes. A future release may migrate to a Range/Selection-based implementation.
- **Undo history** — undo/redo operates on the browser's native editing history, which can be reset when `Value` is set programmatically from outside the component.
- **Tables** — the allowed-tag allowlist includes table elements for paste sanitization, but there is no dedicated table-insertion UI in this release.

## Troubleshooting

- **`Value` doesn't update immediately while typing** — updates are debounced (`DebounceMilliseconds`, default 250 ms) and always flushed on blur.
- **Pasted markup loses styling** — paste sanitization keeps semantic tags only. Set `SanitizePaste="false"` to preserve the original clipboard HTML.
- **Apply is disabled in code view** — the HTML source contains validation errors; fix the reported lines or press Cancel.
- **Uploaded images inflate the document** — provide an `ImageUploadHandler` so images are stored externally instead of being inlined as data URLs.
- **Component doesn't render / JS error on load** — ensure the project references `AngryMonkey.CloudComponents.TextEditor` and the `@using` directives (or `_Imports.razor` entries) are in place.

## Changelog

### 4.1.3

- Initial release as a standalone NuGet package (`AngryMonkey.CloudComponents.TextEditor`).
- Full rich text toolbar: headings, inline formatting, color, alignment, lists, indentation, blockquote, code block, link, image, video, horizontal rule, undo/redo, clear formatting, fullscreen.
- Configurable first heading level (`FirstHeadingLevel`) and heading count (`HeadingCount`).
- HTML code view with `CloudTextEditorHtmlValidator` — errors block Apply, warnings are informational.
- Paste sanitization with allowlist.
- Image upload handler integration (`CloudTextEditorFileUpload`).
- Video embedding: YouTube (privacy-enhanced), Vimeo, direct file URLs.
- Theme-friendly CSS variables (`--cloudeditor-*`).
- No font-family controls by design.


Rich text editor for Blazor on .NET 10. Semantic HTML output, configurable toolbar, media embedding (images, YouTube/Vimeo/file video), and an HTML code view with built-in validation — all without JavaScript dependencies beyond the bundled module.

> `CloudTextEditor` is the single entry point. Bind `@bind-Value` to an HTML string and configure everything else through parameters.

---

## Table of contents

- [Features](#features)
- [Installation](#installation)
- [Quick start](#quick-start)
- [Component API](#component-api)
  - [`CloudTextEditor` parameters](#cloudeditor-parameters)
  - [`CloudTextEditorToolbarOptions`](#cloudeditortoolbaroptions)
- [Heading level offset](#heading-level-offset)
- [Code view and validation](#code-view-and-validation)
- [Images and uploads](#images-and-uploads)
- [Video embedding](#video-embedding)
- [Styling and theming](#styling-and-theming)
- [Troubleshooting](#troubleshooting)

---

## Features

- Inline formatting: bold, italic, underline, strikethrough, subscript, superscript, inline `<code>`
- Block formats: paragraph, headings (with configurable first heading level), blockquote, code block
- Text color and highlight color (palette + custom picker)
- Alignment (left / center / right / justify), bulleted and numbered lists, indentation
- Links (Ctrl+K, open-in-new-tab option), images (URL or upload), video embeds (YouTube, Vimeo, direct files), horizontal rules
- Undo / redo, clear formatting, fullscreen mode
- HTML **code view** with live validation (tag matching, attribute quoting, void elements, implied end tags) — invalid HTML cannot be applied
- Paste sanitization (allowlist based; strips scripts, event handlers and unknown tags)
- Placeholder, read-only mode, min/max/fixed height, debounced two-way binding
- Theme-friendly CSS variables, no font-family controls by design

## Installation

```shell
dotnet add package AngryMonkey.CloudComponents.TextEditor
```

## Quick start

```razor
@using AngryMonkey.CloudComponents.TextEditor.Components

<CloudTextEditor @bind-Value="_html" Placeholder="Start writing…" />

@code {
	private string? _html;
}
```

## Component API

### `CloudTextEditor` parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `Value` / `ValueChanged` | `string?` | `null` | Editor content as HTML. Supports `@bind-Value`. |
| `Placeholder` | `string?` | `null` | Shown while the editor is empty. |
| `ReadOnly` | `bool` | `false` | Disables all editing. |
| `Spellcheck` | `bool` | `true` | Browser spell checking. |
| `MinHeight` | `string` | `250px` | Minimum height of the editing surface. |
| `MaxHeight` | `string?` | `null` | Content scrolls beyond this height. |
| `Height` | `string?` | `null` | Fixed height (overrides min/max). |
| `FirstHeadingLevel` | `int` | `1` | HTML level produced by "Heading 1" (see below). |
| `HeadingCount` | `int` | `4` | Number of heading entries in the dropdown (capped at `h6`). |
| `Toolbar` | `CloudTextEditorToolbarOptions` | all on | Feature toggles per tool group. |
| `SanitizePaste` | `bool` | `true` | Sanitizes HTML pasted from the clipboard. |
| `DebounceMilliseconds` | `int` | `250` | Delay before `Value` updates while typing. |
| `ImageUploadHandler` | `Func<CloudTextEditorFileUpload, Task<string?>>?` | `null` | Persists uploads and returns a URL. Falls back to inline data URLs. |
| `MaxUploadSize` | `long` | 5 MB | Upload size limit in bytes. |
| `OnChanged` | `EventCallback<string?>` | — | Raised after content changes. |
| `OnFocus` / `OnBlur` | `EventCallback` | — | Focus events of the editing surface. |

Public methods: `FocusAsync()`, `InsertHtmlAsync(string html)`.

### `CloudTextEditorToolbarOptions`

Boolean flags, all `true` by default: `Headings`, `BasicFormatting`, `Script`, `InlineCode`, `TextColor`, `HighlightColor`, `Alignment`, `Lists`, `Indentation`, `Blockquote`, `CodeBlock`, `Link`, `Image`, `Video`, `HorizontalRule`, `ClearFormatting`, `UndoRedo`, `CodeView`, `Fullscreen`.

```razor
<CloudTextEditor @bind-Value="_html" Toolbar="new() { Video = false, Fullscreen = false }" />
```

## Heading level offset

`FirstHeadingLevel` remaps the heading dropdown so the document outline can start below `h1` (for example when the page already renders its own `h1`):

```razor
<CloudTextEditor @bind-Value="_html" FirstHeadingLevel="2" HeadingCount="3" />
```

With this configuration the dropdown shows **Heading 1 → `<h2>`**, **Heading 2 → `<h3>`**, **Heading 3 → `<h4>`**. Entries never map beyond `<h6>`.

## Code view and validation

The code view button (`</>`) switches to an HTML source editor. Source is validated live by `CloudTextEditorHtmlValidator`:

- **Errors** (block applying): unclosed/mismatched tags, unterminated comments and attribute quotes, stray closing tags
- **Warnings** (allowed): `script`/`style`/`iframe` content, inline `on*` event handlers, duplicate attributes, closing tags on void elements

**Apply** is enabled only while the source is error-free; **Cancel** discards source changes. The validator is also available directly:

```csharp
CloudTextEditorHtmlValidationResult result = CloudTextEditorHtmlValidator.Validate(html);
```

## Images and uploads

The image dialog supports URL insertion and file upload. Without an `ImageUploadHandler`, files are embedded as data URLs. With one, you control storage:

```razor
<CloudTextEditor @bind-Value="_html" ImageUploadHandler="UploadAsync" />

@code {
	private async Task<string?> UploadAsync(CloudTextEditorFileUpload file)
	{
		await using Stream stream = file.OpenReadStream();
		// Persist to blob storage / API and return the public URL.
		return await _storage.SaveAsync(file.FileName, file.ContentType, stream);
	}
}
```

## Video embedding

The video dialog accepts:

- **YouTube** watch/short/embed links → privacy-enhanced `youtube-nocookie.com` iframe
- **Vimeo** links → `player.vimeo.com` iframe
- **Direct files** (`.mp4`, `.webm`, `.ogg`, `.ogv`, `.mov`) → native `<video controls>`

Embeds render responsive at 16:9 inside `figure.cloudeditor-embed`.

## Styling and theming

All colors and metrics are driven by CSS variables set on any ancestor:

```css
.my-theme {
	--cloudeditor-accent: #6b30c9;
	--cloudeditor-bg: #1e1e24;
	--cloudeditor-color: #eee;
	--cloudeditor-toolbar-bg: #26262e;
	--cloudeditor-border-color: #3a3a44;
	--cloudeditor-code-bg: #16161a;
	--cloudeditor-hover-bg: rgba(255, 255, 255, 0.08);
	--cloudeditor-active-bg: rgba(107, 48, 201, 0.25);
	--cloudeditor-muted: #9a9aa5;
}
```

Additional variables: `--cloudeditor-font-size`, `--cloudeditor-radius`, `--cloudeditor-readonly-bg`, `--cloudeditor-quote-bg`, `--cloudeditor-error`, `--cloudeditor-warning`, `--cloudeditor-success`.

## Troubleshooting

- **`Value` doesn't update immediately while typing** — updates are debounced (`DebounceMilliseconds`, default 250 ms) and always flushed on blur.
- **Pasted markup loses styling** — paste sanitization keeps semantic tags only. Set `SanitizePaste="false"` to keep the original clipboard HTML.
- **Apply is disabled in code view** — the HTML source contains validation errors; fix the reported lines or press Cancel.
- **Uploaded images bloat the document** — provide an `ImageUploadHandler` so images are stored externally instead of inlined as data URLs.

---

## Angry Monkey Cloud

This project is part of the [Angry Monkey Cloud](https://angrymonkeycloud.com) open-source ecosystem. Follow the shared [AI development instructions](https://github.com/angrymonkeycloud/CloudDocs/blob/main/docs/ai/instructions.md) and browse the [project catalog](https://angrymonkeycloud.com) and [GitHub organization](https://github.com/angrymonkeycloud).
