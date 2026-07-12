using AngryMonkey.CloudComponents.DataGrid.Models;
using CloudIcons.Icons;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace AngryMonkey.CloudComponents.DataGrid.Components;

/// <summary>
/// Toolbar rendered above a <see cref="CloudDataGrid"/>: label, and a unified list of
/// <see cref="CloudDataGridAction"/> items covering every interactive element (links, buttons,
/// element toggles, and bulk operations).
///
/// Built-in actions (New, View, Search) are synthesised from the corresponding parameters
/// so that the markup iterates a single <see cref="AllHeaderActions"/> list without
/// any special-case branching.
/// </summary>
public partial class CloudDataGridHeader
{

    #region Parameters

    /// <summary>Title displayed on the left of the toolbar.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// All header actions in display order (consumer-supplied + built-in synthesised actions).
    /// </summary>
    [Parameter] public List<CloudDataGridAction> Actions { get; set; } = [];

    /// <summary>Raised when a <see cref="CloudDataGridActionType.Button"/> action is activated.</summary>
    [Parameter] public EventCallback<CloudDataGridActionEventArgs> OnActionClicked { get; set; }

    /// <summary>Ids of the currently selected rows — governs bulk action visibility.</summary>
    [Parameter] public List<Guid> SelectedRecords { get; set; } = [];

    // ── Built-in helpers (synthesised into Actions) ───────────────────────────

    /// <summary>Adds the built-in Search element action when true.</summary>
    [Parameter] public bool AllowSearch { get; set; }

    /// <summary>Debounce delay in ms before <see cref="OnSearchChanged"/> fires.</summary>
    [Parameter] public int SearchDebounceMilliseconds { get; set; } = 300;

    /// <summary>Raised when the debounced search query changes. Null = cleared.</summary>
    [Parameter] public EventCallback<string?> OnSearchChanged { get; set; }

    /// <summary>Adds the built-in Refresh button when true.</summary>
    [Parameter] public bool AllowRefresh { get; set; } = true;

    /// <summary>Raised when the built-in Refresh button is clicked.</summary>
    [Parameter] public EventCallback OnRefresh { get; set; }

    /// <summary>
    /// Optional extra content rendered at the end of the right-side action slot row.
    /// Use for custom buttons that don't fit the <see cref="CloudDataGridAction"/> model.
    /// </summary>
    [Parameter] public RenderFragment? ExtraActions { get; set; }

    #endregion

    #region Active-element state

    /// <summary>Key of the currently expanded Element action (null = none active).</summary>
    private string? _activeKey;

    /// <summary>Whether the More (⋯) dropdown is currently open.</summary>
    private bool _moreOpen;

    /// <summary>
    /// True only while an Element action is expanded into its focus panel. Opening the
    /// More dropdown does NOT count as a mode — it must not hide the other actions.
    /// </summary>
    private bool AnyModeActive => _activeKey != null;

    /// <summary>
    /// The currently active Element action, searched across both direct header actions
    /// and More-menu actions (null when nothing is active).
    /// </summary>
    private CloudDataGridAction? ActiveAction =>
        _activeKey == null
            ? null
            : DirectHeaderActions.Concat(MoreMenuActions).FirstOrDefault(a => a.Key == _activeKey);

    private async Task ActivateAsync(CloudDataGridAction action)
    {
        if (_activeKey == action.Key)
            return;

        await DeactivateCurrentAsync();
        _activeKey = action.Key;
    }

    private async Task DeactivateCurrentAsync()
    {
        if (_activeKey == null)
            return;

        CloudDataGridAction? current = ActiveAction;
        _activeKey = null;

        if (current?.OnDeactivated != null)
            await current.OnDeactivated();
    }

    private async Task CancelActiveAsync()
    {
        await DeactivateCurrentAsync();
    }

    private void ToggleMoreMenu()
    {
        _moreOpen = !_moreOpen;
    }

    private void CloseMoreMenu()
    {
        _moreOpen = false;
    }

    #endregion

    #region Search state (backing for synthesised Search action's ChildContent)

    private string _searchQuery = string.Empty;
    private bool _focusSearchInput;
    private ElementReference _searchInput;
    private CancellationTokenSource? _searchDebounceCts;

    private async Task OnSearchInputAsync(ChangeEventArgs e)
    {
        _searchQuery = e.Value?.ToString() ?? string.Empty;

        _searchDebounceCts?.Cancel();
        _searchDebounceCts = new CancellationTokenSource();
        CancellationToken token = _searchDebounceCts.Token;

        try { await Task.Delay(SearchDebounceMilliseconds, token); }
        catch (TaskCanceledException) { return; }

        await NotifySearchChangedAsync();
    }

    private async Task OnSearchKeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Key != "Escape")
            return;

        if (string.IsNullOrEmpty(_searchQuery))
            await CancelActiveAsync();
        else
        {
            _searchQuery = string.Empty;
            await NotifySearchChangedAsync();
        }
    }

    private async Task OnSearchBlurAsync(FocusEventArgs _)
    {
        if (!string.IsNullOrEmpty(_searchQuery))
            return;

        await CancelActiveAsync();
    }

    private Task NotifySearchChangedAsync()
    {
        string? query = string.IsNullOrWhiteSpace(_searchQuery) ? null : _searchQuery.Trim();
        return OnSearchChanged.InvokeAsync(query);
    }

    private async Task ClearSearchAndDeactivateAsync()
    {
        bool hadQuery = !string.IsNullOrEmpty(_searchQuery);
        _searchQuery = string.Empty;
        _activeKey = null;
        await InvokeAsync(StateHasChanged);

        if (hadQuery)
            await NotifySearchChangedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_focusSearchInput)
            return;

        _focusSearchInput = false;

        try { await _searchInput.FocusAsync(); }
        catch { /* element not yet available */ }
    }

    #endregion

    #region Built-in action synthesis

    // ── Built-in action keys ─────────────────────────────────────────────────
    private const string KeySearch = "__search";
    private const string KeyRefresh = "__refresh";
    private const string KeyMore = "__more";

    /// <summary>
    /// Consumer-supplied and built-in actions rendered as direct icon buttons
    /// (i.e. <see cref="CloudDataGridAction.ShowInMore"/> is false).
    /// </summary>
    private IEnumerable<CloudDataGridAction> DirectHeaderActions
    {
        get
        {
            foreach (CloudDataGridAction action in Actions.Where(a => a.ShowOnHeader && !a.ShowInMore))
                yield return action;

            if (AllowRefresh)
                yield return new CloudDataGridAction
                {
                    Key = KeyRefresh,
                    Type = CloudDataGridActionType.Button,
                    Tooltip = "Refresh",
                    IconOnly = true,
                    Icon = builder => { builder.OpenComponent<RefreshIcon>(0); builder.CloseComponent(); },
                    ShowOnHeader = true
                };

            if (AllowSearch)
                yield return new CloudDataGridAction
                {
                    Key = KeySearch,
                    Type = CloudDataGridActionType.Element,
                    Tooltip = "Search",
                    IconOnly = true,
                    Icon = builder => { builder.OpenComponent<SearchIcon>(0); builder.CloseComponent(); },
                    ChildContent = SearchContent,
                    OnDeactivated = ClearSearchAndDeactivateAsync,
                    ShowOnHeader = true
                };
        }
    }

    /// <summary>
    /// Actions that appear inside the More (⋯) dropdown menu
    /// (i.e. <see cref="CloudDataGridAction.ShowInMore"/> is true).
    /// </summary>
    private IEnumerable<CloudDataGridAction> MoreMenuActions =>
        Actions.Where(a => a.ShowInMore);

    private bool HasMoreMenuItems => MoreMenuActions.Any();

    private IEnumerable<CloudDataGridAction> BulkHeaderActions =>
        Actions.Where(a => a.ShowOnBulkHeader);

    private bool HasVisibleBulkActions =>
        !AnyModeActive && SelectedRecords.Count > 0 && BulkHeaderActions.Any();

    #endregion

    #region CSS helpers

    private string ActionsClasses => AnyModeActive ? "_mode-active" : string.Empty;

    private static string HeaderActionClasses(CloudDataGridAction action, bool isBulk = false)
    {
        List<string> classes = [];

        if (isBulk)
            classes.Add("_bulk");

        if (!string.IsNullOrWhiteSpace(action.CssClass))
            classes.Add(action.CssClass);

        return string.Join(' ', classes);
    }

    #endregion

    #region Action interaction

    private async Task FireActionClickedAsync(CloudDataGridAction action, List<Guid>? recordIds = null)
    {
        if (action.Key == KeyRefresh)
        {
            await OnRefresh.InvokeAsync();
            return;
        }

        await OnActionClicked.InvokeAsync(new CloudDataGridActionEventArgs
        {
            Action = action,
            RecordIds = recordIds ?? []
        });
    }

    private async Task FireMoreMenuActionAsync(CloudDataGridAction action)
    {
        CloseMoreMenu();

        // Element actions hosted in the More menu open their inline focus panel
        // (e.g. Export options) instead of firing a one-shot click.
        if (action.Type == CloudDataGridActionType.Element)
        {
            await HandleActivateAsync(action);
            return;
        }

        await FireActionClickedAsync(action);
    }

    private async Task HandleActivateAsync(CloudDataGridAction action)
    {
        if (_activeKey == action.Key)
        {
            await CancelActiveAsync();
        }
        else
        {
            _moreOpen = false;
            await DeactivateCurrentAsync();
            _activeKey = action.Key;

            if (action.Key == KeySearch)
                _focusSearchInput = true;

            // Give the action's ChildContent a handle to programmatically close the panel.
            action.OnActivated?.Invoke(CancelActiveAsync);
        }
    }

    #endregion
}

