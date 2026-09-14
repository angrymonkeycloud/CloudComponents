using System.Text.Json;
using AngryMonkey.CloudComponents.DataGrid.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AngryMonkey.CloudComponents.DataGrid.Components;

public partial class CloudDataGrid
{
    [Parameter] public string? PreferenceKey { get; set; }
    [Parameter] public List<CloudDataGridColumn>? SortColumns { get; set; }
    private Dictionary<string, double> _savedWidths = [];
    private string StorageKey => "cloudgrid.view.v1:" + PreferenceKey;

    private sealed class Preferences
    {
        public string? SortKey { get; set; }
        public CloudDataGridSortDirection Direction { get; set; }
        public Dictionary<string, double> Widths { get; set; } = [];
    }

    private async Task RestorePreferencesAsync()
    {
        if (string.IsNullOrEmpty(PreferenceKey)) return;
        try
        {
            string? json = await JS.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            Preferences? saved = json is null ? null : JsonSerializer.Deserialize<Preferences>(json);
            if (saved is null) return;
            _savedWidths = (saved.Widths ?? []).Where(item => double.IsFinite(item.Value) && item.Value >= 40 && item.Value <= 3000).ToDictionary();
            List<CloudDataGridColumn> allowed = SortColumns ?? Columns;
            CloudDataGridColumn? column = allowed.FirstOrDefault(item => (item.Key ?? item.Label) == saved.SortKey && item.Sortable);
            if (column is not null && Enum.IsDefined(saved.Direction))
                _sort = new() { Column = column, ColumnIndex = allowed.IndexOf(column), Direction = saved.Direction };
        }
        catch { }
    }

    private async Task SavePreferencesAsync()
    {
        if (string.IsNullOrEmpty(PreferenceKey)) return;
        try
        {
            await JS.InvokeVoidAsync("localStorage.setItem", StorageKey, JsonSerializer.Serialize(new Preferences { SortKey = _sort?.Key, Direction = _sort?.Direction ?? CloudDataGridSortDirection.Ascending, Widths = _savedWidths }));
        }
        catch { }
    }

    private async Task OnColumnWidthsChanged(Dictionary<string, double> widths)
    {
        _savedWidths = new(widths);
        await SavePreferencesAsync();
    }

    public Task RefreshAsync() => ExecuteAsync(page: 1, isAppend: false);
}
