using CloudComponents.Maps.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace CloudComponents.Maps.Components;

/// <summary>
/// Built-in place search box for <see cref="AzureMap"/>: debounced Azure Maps fuzzy search
/// with a keyboard-navigable results dropdown. Selecting a result flies the camera to it and,
/// when <see cref="AzureMap.LocationLock"/> is configured, is validated against the locked area.
/// </summary>
public partial class AzureMap
{
    private CancellationTokenSource? _searchDebounceCts;
    private List<MapSearchResult> _searchResults = [];
    private bool _searchDropdownOpen;
    private bool _isSearching;
    private int _searchHighlightIndex = -1;
    private string _searchQuery = string.Empty;
    private bool _searchInputReadOnly = true;

    /// <summary>Shows the built-in search box (top-left). Opt-in: defaults to <c>false</c>.</summary>
    [Parameter] public bool ShowSearchBox { get; set; }

    /// <summary>Placeholder text for the search input.</summary>
    [Parameter] public string SearchPlaceholder { get; set; } = "Search for a place…";

    /// <summary>Debounce delay, in milliseconds, before a typed query triggers a search request.</summary>
    [Parameter] public int SearchDebounceMs { get; set; } = 350;

    /// <summary>Maximum number of suggestions shown in the results dropdown.</summary>
    [Parameter] public int SearchResultLimit { get; set; } = 5;

    /// <summary>Zoom level used when a selected result has no viewport (a single point).</summary>
    [Parameter] public int SearchResultZoom { get; set; } = 14;

    /// <summary>Raised after the camera has moved to a search result the user selected.</summary>
    [Parameter] public EventCallback<MapSearchResult> OnSearchResultSelected { get; set; }

    /// <summary>Current suggestions for the active query, in relevance order.</summary>
    public IReadOnlyList<MapSearchResult> SearchResults => _searchResults;

    private string SearchQuery
    {
        get => _searchQuery;
        set
        {
            _searchQuery = value;
            _ = DebounceSearchAsync(value);
        }
    }

    private async Task DebounceSearchAsync(string query)
    {
        _searchDebounceCts?.Cancel();
        _searchDebounceCts?.Dispose();
        var cts = new CancellationTokenSource();
        _searchDebounceCts = cts;

        if (string.IsNullOrWhiteSpace(query))
        {
            _searchResults = [];
            _searchDropdownOpen = false;
            _isSearching = false;
            StateHasChanged();
            return;
        }

        try
        {
            await Task.Delay(SearchDebounceMs, cts.Token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        if (cts.IsCancellationRequested)
            return;

        await RunSearchAsync(query, cts.Token);
    }

    private async Task RunSearchAsync(string query, CancellationToken cancellationToken)
    {
        if (_controller is null)
            return;

        _isSearching = true;
        StateHasChanged();

        try
        {
            var results = await _controller.InvokeAsync<List<MapSearchResult>>(
                "search", cancellationToken, [query, SearchResultLimit]);

            if (cancellationToken.IsCancellationRequested)
                return;

            _searchResults = results ?? [];
            _searchHighlightIndex = _searchResults.Count > 0 ? 0 : -1;
            _searchDropdownOpen = _searchResults.Count > 0;
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer keystroke — ignore.
        }
        catch (Exception ex)
        {
            _searchResults = [];
            _searchDropdownOpen = false;
            await OnMapError.InvokeAsync($"Search failed: {ex.Message}");
        }
        finally
        {
            _isSearching = false;
            StateHasChanged();
        }
    }

    private async Task SelectSearchResultAsync(MapSearchResult result)
    {
        _searchDropdownOpen = false;
        _searchQuery = result.Address;
        _searchResults = [];

        if (!await IsPointAllowedByLocationLockAsync(result.Latitude, result.Longitude))
        {
            await RaiseLocationLockRejectedAsync(result.Latitude, result.Longitude);
            StateHasChanged();
            return;
        }

        var hasViewport = result.North > result.South && result.East > result.West;
        if (hasViewport)
            await SetBoundsAsync(result.South, result.West, result.North, result.East);
        else
            await SetCenterAsync(result.Latitude, result.Longitude, SearchResultZoom);

        await OnSearchResultSelected.InvokeAsync(result);
        StateHasChanged();
    }

    private void HandleSearchKeyDown(KeyboardEventArgs e)
    {
        switch (e.Key)
        {
            case "ArrowDown":
                if (_searchResults.Count > 0)
                    _searchHighlightIndex = Math.Min(_searchHighlightIndex + 1, _searchResults.Count - 1);
                break;

            case "ArrowUp":
                if (_searchResults.Count > 0)
                    _searchHighlightIndex = Math.Max(_searchHighlightIndex - 1, 0);
                break;

            case "Enter":
                if (_searchHighlightIndex >= 0 && _searchHighlightIndex < _searchResults.Count)
                    _ = SelectSearchResultAsync(_searchResults[_searchHighlightIndex]);
                break;

            case "Escape":
                _searchDropdownOpen = false;
                break;
        }
    }

    private async Task HandleSearchBlurAsync()
    {
        _searchInputReadOnly = true;

        // Delay so a pointer-down on a dropdown item registers before it unmounts.
        await Task.Delay(150);
        _searchDropdownOpen = false;
        StateHasChanged();
    }

    private void HandleSearchFocus()
    {
        _searchInputReadOnly = false;

        if (_searchResults.Count > 0)
            _searchDropdownOpen = true;
    }

    /// <summary>Clears the search box text and any pending/visible suggestions.</summary>
    public void ClearSearch()
    {
        _searchDebounceCts?.Cancel();
        _searchDebounceCts?.Dispose();
        _searchDebounceCts = null;

        _searchQuery = string.Empty;
        _searchResults = [];
        _searchDropdownOpen = false;
        _searchHighlightIndex = -1;
        StateHasChanged();
    }

    private void CancelSearchDebounce()
    {
        _searchDebounceCts?.Cancel();
        _searchDebounceCts?.Dispose();
        _searchDebounceCts = null;
    }
}
