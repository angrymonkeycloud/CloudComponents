using AngryMonkey.CloudComponents.Editor.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AngryMonkey.CloudComponents.Editor.Components;

/// <summary>
/// Rich text editor Blazor component. Wraps a contenteditable surface behind a
/// strongly-typed C# API with a configurable toolbar, media dialogs and an
/// HTML code view with validation.
/// </summary>
public partial class CloudEditor : ComponentBase, IAsyncDisposable
{
    private const string ModulePath = "./_content/AngryMonkey.CloudComponents.Editor/cloudeditor.js";

    private IJSObjectReference? _module;
    private IJSObjectReference? _controller;
    private DotNetObjectReference<CloudEditor>? _selfRef;
    private ElementReference _contentElement;

    private CloudEditorSelectionState _selectionState = new();
    private string? _lastSyncedValue;
    private bool _appliedReadOnly;
    private bool _isFullscreen;

    private EditorDialog _activeDialog = EditorDialog.None;

    private enum EditorDialog { None, Link, Image, Video }

    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>Editor content as an HTML string. Supports two-way binding.</summary>
    [Parameter] public string? Value { get; set; }

    [Parameter] public EventCallback<string?> ValueChanged { get; set; }

    /// <summary>Placeholder text shown while the editor is empty.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Disables all editing when set.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>Enables browser spell checking inside the editor. Default <c>true</c>.</summary>
    [Parameter] public bool Spellcheck { get; set; } = true;

    /// <summary>Minimum height of the editing surface. Default <c>250px</c>.</summary>
    [Parameter] public string MinHeight { get; set; } = "250px";

    /// <summary>Maximum height of the editing surface — the content scrolls beyond it.</summary>
    [Parameter] public string? MaxHeight { get; set; }

    /// <summary>Fixed height of the editing surface. Overrides min/max when set.</summary>
    [Parameter] public string? Height { get; set; }

    /// <summary>
    /// The HTML heading level produced by the first "Heading" entry in the toolbar.
    /// For example when set to <c>2</c>, "Heading 1" produces an <c>&lt;h2&gt;</c>,
    /// "Heading 2" an <c>&lt;h3&gt;</c>, and so on. Default <c>1</c>.
    /// </summary>
    [Parameter] public int FirstHeadingLevel { get; set; } = 1;

    /// <summary>
    /// Number of heading entries offered in the block format dropdown.
    /// Automatically capped so no heading maps beyond <c>&lt;h6&gt;</c>. Default <c>4</c>.
    /// </summary>
    [Parameter] public int HeadingCount { get; set; } = 4;

    /// <summary>Toolbar feature toggles. All features are enabled by default.</summary>
    [Parameter] public CloudEditorToolbarOptions Toolbar { get; set; } = new();

    /// <summary>Sanitizes HTML pasted from the clipboard. Default <c>true</c>.</summary>
    [Parameter] public bool SanitizePaste { get; set; } = true;

    /// <summary>Delay in milliseconds before content changes are pushed to <see cref="Value"/>. Default <c>250</c>.</summary>
    [Parameter] public int DebounceMilliseconds { get; set; } = 250;

    /// <summary>
    /// Optional handler that persists an uploaded image and returns its public URL.
    /// When omitted, uploaded images are embedded inline as data URLs.
    /// </summary>
    [Parameter] public Func<CloudEditorFileUpload, Task<string?>>? ImageUploadHandler { get; set; }

    /// <summary>Maximum accepted upload size in bytes. Default 5 MB.</summary>
    [Parameter] public long MaxUploadSize { get; set; } = 5 * 1024 * 1024;

    /// <summary>Raised after the content changed and <see cref="Value"/> was updated.</summary>
    [Parameter] public EventCallback<string?> OnChanged { get; set; }

    [Parameter] public EventCallback OnFocus { get; set; }

    [Parameter] public EventCallback OnBlur { get; set; }

    private string ContentStyle
    {
        get
        {
            List<string> styles = [];

            if (!string.IsNullOrEmpty(Height))
            {
                styles.Add($"height:{Height}");
                styles.Add("overflow-y:auto");
            }
            else
            {
                styles.Add($"min-height:{MinHeight}");

                if (!string.IsNullOrEmpty(MaxHeight))
                {
                    styles.Add($"max-height:{MaxHeight}");
                    styles.Add("overflow-y:auto");
                }
            }

            return string.Join(';', styles);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        _module = await JS.InvokeAsync<IJSObjectReference>("import", ModulePath);
        _selfRef = DotNetObjectReference.Create(this);

        _controller = await _module.InvokeAsync<IJSObjectReference>("create", _selfRef, _contentElement, new
        {
            debounceMs = Math.Max(0, DebounceMilliseconds),
            sanitizePaste = SanitizePaste,
            allowIframes = true
        });

        await _controller.InvokeVoidAsync("setHtml", Value ?? string.Empty);
        _lastSyncedValue = Value;
        _appliedReadOnly = ReadOnly;

        if (ReadOnly)
            await _controller.InvokeVoidAsync("setReadOnly", true);
    }

    protected override async Task OnParametersSetAsync()
    {
        if (_controller is null)
            return;

        if (!string.Equals(Value, _lastSyncedValue, StringComparison.Ordinal))
        {
            await _controller.InvokeVoidAsync("setHtml", Value ?? string.Empty);
            _lastSyncedValue = Value;
        }

        if (ReadOnly != _appliedReadOnly)
        {
            _appliedReadOnly = ReadOnly;
            await _controller.InvokeVoidAsync("setReadOnly", ReadOnly);
        }
    }

    /// <summary>Moves keyboard focus into the editing surface.</summary>
    public async Task FocusAsync()
    {
        if (_controller is not null)
            await _controller.InvokeVoidAsync("focus");
    }

    /// <summary>Inserts raw HTML at the current caret position.</summary>
    public async Task InsertHtmlAsync(string html)
    {
        if (_controller is not null && !string.IsNullOrEmpty(html))
            await _controller.InvokeVoidAsync("insertHtml", html);
    }

    [JSInvokable]
    public async Task HandleContentChangedAsync(string html)
    {
        string? value = string.IsNullOrWhiteSpace(html) ? null : html;

        _lastSyncedValue = value;
        Value = value;

        await ValueChanged.InvokeAsync(value);
        await OnChanged.InvokeAsync(value);
    }

    [JSInvokable]
    public async Task UpdateSelectionStateAsync(CloudEditorSelectionState state)
    {
        _selectionState = state;
        await InvokeAsync(StateHasChanged);
    }

    [JSInvokable]
    public async Task HandleFocusAsync() => await OnFocus.InvokeAsync();

    [JSInvokable]
    public async Task HandleBlurAsync() => await OnBlur.InvokeAsync();

    [JSInvokable]
    public async Task HandleLinkShortcutAsync()
    {
        if (Toolbar.Link && !_commandsDisabled)
        {
            await OpenLinkDialogAsync();
            await InvokeAsync(StateHasChanged);
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_controller is not null)
            {
                await _controller.InvokeVoidAsync("dispose");
                await _controller.DisposeAsync();
            }

            if (_module is not null)
                await _module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // Circuit/page torn down — nothing to clean up.
        }

        _selfRef?.Dispose();
    }
}
