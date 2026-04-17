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

namespace DBTeam.Modules.ConnectionManager.ViewModels;

public partial class ConnectionsPanelViewModel : ObservableObject
{
    private readonly IConnectionService _svc;
    private readonly IEventBus _bus;

    public ConnectionsPanelViewModel(IConnectionService svc, IEventBus bus)
    {
        _svc = svc; _bus = bus;
        Connections = new ObservableCollection<SqlConnectionInfo>();
        _ = ReloadAsync();
        _bus.Subscribe<ConnectionsChangedEvent>(_ => Application.Current.Dispatcher.Invoke(async () => await ReloadAsync()));
    }

    public ObservableCollection<SqlConnectionInfo> Connections { get; }

    [ObservableProperty] private SqlConnectionInfo? selected;

    [RelayCommand]
    public async Task ReloadAsync()
    {
        var all = await _svc.LoadAllAsync();
        Connections.Clear();
        foreach (var c in all.OrderByDescending(x => x.LastUsed ?? DateTime.MinValue)) Connections.Add(c);
    }

    [RelayCommand]
    private void Connect(SqlConnectionInfo? c)
    {
        var info = c ?? Selected;
        if (info is null) return;
        _bus.Publish(new ConnectionOpenedEvent { Connection = info });
    }

    [RelayCommand]
    private async Task DeleteAsync(SqlConnectionInfo? c)
    {
        var info = c ?? Selected;
        if (info is null) return;
        if (MessageBox.Show($"Delete connection '{info.Name}'?", "Confirm", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        await _svc.DeleteAsync(info.Id);
        await ReloadAsync();
    }
}
