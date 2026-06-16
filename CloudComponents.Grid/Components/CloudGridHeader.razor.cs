using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace CloudComponents.Grid.Components;

/// <summary>
/// Toolbar usually placed above a <see cref="CloudGrid"/>: title, optional
/// "open view" / "new record" links and a debounced search box.
/// Search is implemented entirely in C# (no JavaScript interop).
/// </summary>
public partial class CloudGridHeader
{
    #region Parameters

    /// <summary>Title displayed on the left.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Optional link to open the full view. Hidden when null or empty.</summary>
    [Parameter] public string? ViewUrl { get; set; }

    /// <summary>Optional link for the "new" button. Hidden when null or empty.</summary>
    [Parameter] public string? NewUrl { get; set; }

    [Parameter] public string NewButtonText { get; set; } = "new";

    /// <summary>Whether to render the search button/box.</summary>
    [Parameter] public bool AllowSearch { get; set; } = true;

    /// <summary>Delay applied while typing before <see cref="OnSearchChanged"/> is raised.</summary>
    [Parameter] public int SearchDebounceMilliseconds { get; set; } = 300;

    /// <summary>
    /// Raised when the (debounced) search query changes. Null means the search was cleared.
    /// </summary>
    [Parameter] public EventCallback<string?> OnSearchChanged { get; set; }

    /// <summary>Optional extra action buttons rendered after the built-in search button.</summary>
    [Parameter] public RenderFragment? ExtraActions { get; set; }

    #endregion

    #region Search

    private bool _isSearchOpen;
    private string _searchQuery = string.Empty;
    private bool _focusSearchInput;
    private ElementReference _searchInput;
    private CancellationTokenSource? _searchDebounceCts;

    /// <summary>
    /// True when any exclusive mode (search, reorder, …) is active.
    /// Used to hide all non-active slots so the toolbar stays flat.
    /// </summary>
    private bool AnyModeActive => _isSearchOpen;

    private void OpenSearch()
    {
        _isSearchOpen = true;
        _focusSearchInput = true;
    }

    private async Task OnSearchInputAsync(ChangeEventArgs e)
    {
        _searchQuery = e.Value?.ToString() ?? string.Empty;

        _searchDebounceCts?.Cancel();
        _searchDebounceCts = new CancellationTokenSource();
        CancellationToken token = _searchDebounceCts.Token;

        try
        {
            await Task.Delay(SearchDebounceMilliseconds, token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        await NotifySearchChangedAsync();
    }

    private async Task OnSearchKeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Key == "Escape")
        {
            if (string.IsNullOrEmpty(_searchQuery))
                await ClearSearchAsync();
            else
                _searchQuery = string.Empty;
        }
    }

    private async Task OnSearchBlurAsync(FocusEventArgs e)
    {
        if (!string.IsNullOrEmpty(_searchQuery))
            return;

        await ClearSearchAsync();
    }

    private async Task ClearSearchAsync()
    {
        _isSearchOpen = false;

        bool searchWasCleared = !string.IsNullOrEmpty(_searchQuery);

        _searchQuery = string.Empty;

        if (searchWasCleared)
            await NotifySearchChangedAsync();
    }

    private Task NotifySearchChangedAsync()
    {
        string? query = string.IsNullOrWhiteSpace(_searchQuery) ? null : _searchQuery.Trim();
        return OnSearchChanged.InvokeAsync(query);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_focusSearchInput)
            return;

        _focusSearchInput = false;

        try
        {
            await _searchInput.FocusAsync();
        }
        catch
        {
            // Element may not be available yet; ignore.
        }
    }

    #endregion
}
