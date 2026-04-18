using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DBTeam.Core.Abstractions;
using DBTeam.Core.Events;
using DBTeam.Core.Models;

namespace DBTeam.Modules.QueryEditor.ViewModels;

public partial class QueryHistoryPanelViewModel : ObservableObject
{
    private readonly IQueryHistoryStore _store;
    private readonly IConnectionService _connSvc;
    private readonly IEventBus _bus;
    private readonly ObservableCollection<QueryHistoryEntry> _all = new();

    public QueryHistoryPanelViewModel(IQueryHistoryStore store, IConnectionService connSvc, IEventBus bus)
    {
        _store = store; _connSvc = connSvc; _bus = bus;
        Items = new ObservableCollection<QueryHistoryEntry>();
        _ = ReloadAsync();
    }

    public ObservableCollection<QueryHistoryEntry> Items { get; }

    [ObservableProperty] private QueryHistoryEntry? selected;
    [ObservableProperty] private string filter = string.Empty;
    [ObservableProperty] private bool favoritesOnly;

    partial void OnFilterChanged(string value) => ApplyFilter();
    partial void OnFavoritesOnlyChanged(bool value) => ApplyFilter();

    [RelayCommand]
    public async Task ReloadAsync()
    {
        var all = await _store.LoadAsync();
        _all.Clear();
        foreach (var e in all.OrderByDescending(x => x.ExecutedAt)) _all.Add(e);
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        Items.Clear();
        IEnumerable<QueryHistoryEntry> q = _all;
        if (FavoritesOnly) q = q.Where(x => x.IsFavorite);
        if (!string.IsNullOrWhiteSpace(Filter))
        {
            var f = Filter.Trim();
            q = q.Where(x => (x.Sql?.Contains(f, StringComparison.OrdinalIgnoreCase) ?? false)
                          || (x.Label?.Contains(f, StringComparison.OrdinalIgnoreCase) ?? false));
        }
        foreach (var e in q) Items.Add(e);
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync(QueryHistoryEntry? e)
    {
        var entry = e ?? Selected; if (entry is null) return;
        entry.IsFavorite = !entry.IsFavorite;
        await _store.UpdateAsync(entry);
    }

    [RelayCommand]
    private async Task DeleteAsync(QueryHistoryEntry? e)
    {
        var entry = e ?? Selected; if (entry is null) return;
        await _store.DeleteAsync(entry.Id);
        _all.Remove(entry);
        Items.Remove(entry);
    }

    [RelayCommand]
    private async Task ClearAsync()
    {
        if (MessageBox.Show("Clear all query history?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        await _store.ClearAsync();
        _all.Clear(); Items.Clear();
    }

    [RelayCommand]
    private void OpenInNewTab(QueryHistoryEntry? e)
    {
        var entry = e ?? Selected; if (entry is null) return;
        var conn = _connSvc.Saved.FirstOrDefault(c => c.Name == entry.ConnectionName);
        if (conn is null) return;
        _bus.Publish(new OpenQueryEditorRequest { Connection = conn, Database = entry.Database, InitialSql = entry.Sql });
    }
}
