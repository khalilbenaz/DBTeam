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

public partial class ConnectionDialogViewModel : ObservableObject
{
    private readonly IConnectionService _svc;
    private readonly IDatabaseMetadataService _meta;
    private readonly IEventBus _bus;

    public ConnectionDialogViewModel(IConnectionService svc, IDatabaseMetadataService meta, IEventBus bus)
    {
        _svc = svc; _meta = meta; _bus = bus;
        Model = new SqlConnectionInfo { Name = "New connection", Server = "localhost" };
        AuthModes = new ObservableCollection<SqlAuthMode>(Enum.GetValues<SqlAuthMode>());
    }

    [ObservableProperty] private SqlConnectionInfo model;
    [ObservableProperty] private string statusText = string.Empty;
    [ObservableProperty] private bool isBusy;
    public ObservableCollection<SqlAuthMode> AuthModes { get; }
    public ObservableCollection<string> Databases { get; } = new();

    public Action<bool>? CloseAction { get; set; }

    [RelayCommand]
    private async Task TestAsync()
    {
        IsBusy = true; StatusText = "Testing...";
        var ok = await _svc.TestAsync(Model);
        StatusText = ok ? "Connection OK" : "Connection failed";
        IsBusy = false;
    }

    [RelayCommand]
    private async Task LoadDatabasesAsync()
    {
        IsBusy = true; StatusText = "Loading databases...";
        try
        {
            var dbs = await _meta.GetDatabasesAsync(Model);
            Databases.Clear();
            foreach (var d in dbs) Databases.Add(d);
            StatusText = $"{dbs.Count} database(s)";
        }
        catch (Exception ex) { StatusText = ex.Message; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task SaveAndConnectAsync()
    {
        IsBusy = true;
        try
        {
            Model.LastUsed = DateTime.UtcNow;
            await _svc.SaveAsync(Model);
            _bus.Publish(new ConnectionsChangedEvent());
            _bus.Publish(new ConnectionOpenedEvent { Connection = Model });
            CloseAction?.Invoke(true);
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        finally { IsBusy = false; }
    }

    [RelayCommand] private void Cancel() => CloseAction?.Invoke(false);
}
