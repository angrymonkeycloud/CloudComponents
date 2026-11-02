# CloudComponents.Tests

One test project for the whole CloudComponents library: the core components, DataGrid, TextEditor, VideoPlayer, Icons and Maps.

```
dotnet test CloudComponents.Tests
```

Everything that needs no key runs by default. Tests that call a real service are **live tests**: they are skipped, naming the
missing setting, until its key is configured.

## What is tested

| Area | Folder | Kind |
| --- | --- | --- |
| Core components (Dialog, Popup, Tabs, ProgressBar, CopyButton, Switch, VolumeBar, Markdown, helpers) | `Core/Interactive` | bUnit |
| DateTimePicker, GridView | `Core/DateTimePicker`, `Core/GridView` | bUnit |
| DataGrid | `DataGrid` | bUnit |
| VideoPlayer, TextEditor | `VideoPlayer`, `TextEditor` | bUnit |
| Icons | `Icons` | bUnit (every icon, by reflection) |
| Maps: the `AzureMap` component and its models | `Maps/Component`, `Maps/Models` | bUnit with a fake JS controller |
| Maps: the JavaScript controller (`mapInterop.js`) | `js/maps` | Node's test runner, against a fake browser and fake SDK; run by `JavaScript/JavaScriptSuiteTests` |
| Azure Maps web services | `Maps/Azure/AzureLiveTests.cs` | live |
| Google Maps Platform web services | `Maps/Google/GoogleLiveTests.cs` | live |
| Google Maps JavaScript API (browser key) | `Maps/Google/GoogleMapsJsCheck.html` | manual page, see below |

The JavaScript tests need Node.js 22 or later on the `PATH`; without it they are skipped. They can also be run directly:

```
cd CloudComponents.Tests/js
node --test maps/*.test.mjs
```

A test marked `todo` in the JavaScript files is a known defect or a hardening item: it does not fail the run, but the C# runner
prints it so it is never hidden. Today: the SDK loader remembers a failed load for the life of the page, and two popups write a
URL or a colour into HTML without limiting it.

## Keys for the live tests

Keys are read from this project's **user secrets**, or from **environment variables**. They are never written to the repository,
and failure messages have every configured key replaced by `***`.

| Setting | Environment variable | For |
| --- | --- | --- |
| `Maps:Azure:SubscriptionKey` | `Maps__Azure__SubscriptionKey` | Azure Maps (Search, Polygon, tiles, SDK) |
| `Maps:Google:ServerKey` | `Maps__Google__ServerKey` | Google Geocoding API and Places API (New) |
| `Maps:Google:BrowserKey` | `Maps__Google__BrowserKey` | Maps JavaScript API; only used by the manual page |

```
cd CloudComponents.Tests
dotnet user-secrets set "Maps:Azure:SubscriptionKey" "<key>"
dotnet user-secrets set "Maps:Google:ServerKey" "<key>"
dotnet user-secrets set "Maps:Google:BrowserKey" "<key>"
```

The **server key** is used from the test process, which sends no HTTP referrer, so it must not be restricted by referrer. Restrict it
by API (Geocoding API, Places API (New)) and, where you can, by IP. The **browser key** is restricted by HTTP referrer and by API
(Maps JavaScript API, plus Geocoding API and Places API (New) if the page-side Geocoder and Places are used).

To make a missing key a failure instead of a skip, for example in a build that must prove the keys work:

```
set CLOUDCOMPONENTS_REQUIRE_LIVE=1
```

Run only the live tests, or leave them out:

```
dotnet test CloudComponents.Tests --filter "Category=Live"
dotnet test CloudComponents.Tests --filter "Category!=Live"
dotnet test CloudComponents.Tests --filter "Provider=Google"
```

## The Google browser-key page

A referrer-restricted key cannot be exercised from a test, because the web services reject such keys and the JavaScript API only
validates them inside a browser. `Maps/Google/GoogleMapsJsCheck.html` loads the map the way the app will and checks: the script, the
road map, satellite (hybrid) imagery, advanced markers (when a Map ID is given), the Geocoder and Places (New) text search and
autocomplete. Serve it from an origin the key allows, open it, paste the key and (optionally) the Map ID, and read the PASS / FAIL
list. For the MAUI app the page origin is `https://0.0.0.1`.

## Adding tests

- Name tests as sentences: `A_closed_dialog_renders_nothing`. One behaviour per test.
- Components: `class XTests : BunitContext`, `JSInterop.Mode = JSRuntimeMode.Loose` or set the modules up explicitly.
- A test that needs a key uses `[LiveFact(TestSecrets.GoogleServerKey)]`; read the key with `TestSecrets.Require(...)` and never put a
  URL with a key in an assertion message without `TestSecrets.Redact(...)`.
- JavaScript tests live in `js/<area>/*.test.mjs` and are picked up automatically; `js/support/harness.mjs` is the fake browser and SDK.
