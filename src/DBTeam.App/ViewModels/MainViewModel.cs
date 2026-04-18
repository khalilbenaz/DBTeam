using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DBTeam.Core.Events;
using DBTeam.Core.Infrastructure;
using DBTeam.Core.Models;
using DBTeam.Modules.ConnectionManager.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DBTeam.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private string statusMessage = "Ready";
    [ObservableProperty] private string activeConnectionName = "No connection";
    [ObservableProperty] private SqlConnectionInfo? activeConnection;

    public MainViewModel()
    {
        var bus = ServiceLocator.TryGet<IEventBus>();
        bus?.Subscribe<ConnectionOpenedEvent>(OnConnectionOpened);
    }

    private void OnConnectionOpened(ConnectionOpenedEvent e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            ActiveConnection = e.Connection;
            ActiveConnectionName = $"{e.Connection.Name} ({e.Connection.Server})";
            StatusMessage = "Connected";
        });
    }

    [RelayCommand]
    private void NewConnection()
    {
        var dlg = App.Services.GetRequiredService<ConnectionDialog>();
        dlg.Owner = Application.Current.MainWindow;
        dlg.ShowDialog();
    }

    [RelayCommand]
    private void NewQuery()
    {
        if (ActiveConnection is null) { StatusMessage = "Select or create a connection first"; return; }
        var bus = App.Services.GetRequiredService<IEventBus>();
        bus.Publish(new OpenQueryEditorRequest { Connection = ActiveConnection });
    }

    [RelayCommand] private void Exit() => Application.Current.Shutdown();

    [RelayCommand]
    private void SchemaCompare()
    {
        var view = App.Services.GetRequiredService<DBTeam.Modules.SchemaCompare.Views.SchemaCompareView>();
        var bus = App.Services.GetRequiredService<IEventBus>();
        bus.Publish(new DBTeam.Core.Events.OpenDocumentRequest { Title = "Schema Compare", Content = view });
    }

    [RelayCommand]
    private void DataCompare()
    {
        var view = App.Services.GetRequiredService<DBTeam.Modules.DataCompare.Views.DataCompareView>();
        var bus = App.Services.GetRequiredService<IEventBus>();
        bus.Publish(new DBTeam.Core.Events.OpenDocumentRequest { Title = "Data Compare", Content = view });
    }

    [RelayCommand]
    private void NewTable()
    {
        var view = App.Services.GetRequiredService<DBTeam.Modules.TableDesigner.Views.TableDesignerView>();
        var bus = App.Services.GetRequiredService<IEventBus>();
        bus.Publish(new DBTeam.Core.Events.OpenDocumentRequest { Title = "New Table", Content = view });
    }

    [RelayCommand]
    private void DataGenerator()
    {
        var view = App.Services.GetRequiredService<DBTeam.Modules.DataGenerator.Views.DataGeneratorView>();
        var bus = App.Services.GetRequiredService<IEventBus>();
        bus.Publish(new DBTeam.Core.Events.OpenDocumentRequest { Title = "Data Generator", Content = view });
    }
    [RelayCommand]
    private void Documenter()
    {
        var view = App.Services.GetRequiredService<DBTeam.Modules.Documenter.Views.DocumenterView>();
        var bus = App.Services.GetRequiredService<IEventBus>();
        bus.Publish(new DBTeam.Core.Events.OpenDocumentRequest { Title = "Documenter", Content = view });
    }
    [RelayCommand]
    private void Profiler()
    {
        var view = App.Services.GetRequiredService<DBTeam.Modules.Profiler.Views.ProfilerView>();
        var bus = App.Services.GetRequiredService<IEventBus>();
        bus.Publish(new DBTeam.Core.Events.OpenDocumentRequest { Title = "Query Profiler", Content = view });
    }
    [RelayCommand]
    private void Administration()
    {
        var view = App.Services.GetRequiredService<DBTeam.Modules.Admin.Views.AdminView>();
        var bus = App.Services.GetRequiredService<IEventBus>();
        bus.Publish(new DBTeam.Core.Events.OpenDocumentRequest { Title = "Administration", Content = view });
    }
    [RelayCommand]
    private void Terminal()
    {
        var view = App.Services.GetRequiredService<DBTeam.Modules.Terminal.Views.TerminalView>();
        var bus = App.Services.GetRequiredService<IEventBus>();
        bus.Publish(new DBTeam.Core.Events.OpenDocumentRequest { Title = "Terminal", Content = view });
    }
    [RelayCommand]
    private void AiAssistant()
    {
        var view = App.Services.GetRequiredService<DBTeam.Modules.AiAssistant.Views.AiAssistantView>();
        var bus = App.Services.GetRequiredService<IEventBus>();
        bus.Publish(new DBTeam.Core.Events.OpenDocumentRequest { Title = "AI Assistant", Content = view });
    }
    [RelayCommand]
    private void Debugger()
    {
        var view = App.Services.GetRequiredService<DBTeam.Modules.Debugger.Views.DebuggerView>();
        var bus = App.Services.GetRequiredService<IEventBus>();
        bus.Publish(new DBTeam.Core.Events.OpenDocumentRequest { Title = "T-SQL Debugger", Content = view });
    }
    [RelayCommand]
    private void Diagram()
    {
        var view = App.Services.GetRequiredService<DBTeam.Modules.Diagram.Views.DiagramView>();
        var bus = App.Services.GetRequiredService<IEventBus>();
        bus.Publish(new DBTeam.Core.Events.OpenDocumentRequest { Title = "Database Diagram", Content = view });
    }
    [RelayCommand]
    private void ShowObjectExplorer() => App.Services.GetRequiredService<IEventBus>().Publish(new DBTeam.Core.Events.ShowPaneRequest { PaneId = "OBJECT_EXPLORER" });

    [RelayCommand]
    private void ShowConnections() => App.Services.GetRequiredService<IEventBus>().Publish(new DBTeam.Core.Events.ShowPaneRequest { PaneId = "CONNECTIONS" });
    [RelayCommand] private void ExecuteQuery() { }

    [RelayCommand]
    private void SetTheme(string? name)
    {
        if (!System.Enum.TryParse<DBTeam.App.Services.AppTheme>(name, true, out var t)) return;
        App.Services.GetRequiredService<DBTeam.App.Services.ThemeService>().Apply(t);
        StatusMessage = $"Theme: {t}";
    }

    [RelayCommand]
    private void About()
    {
        var dlg = new DBTeam.App.Shell.AboutDialog { Owner = Application.Current.MainWindow };
        dlg.ShowDialog();
    }

    [RelayCommand]
    private void Shortcuts()
    {
        MessageBox.Show(
            "F5 — Execute query\nCtrl+K — Format SQL\nCtrl+Space — Autocomplete\nCtrl+N — New connection\nCtrl+Q — New query",
            "Keyboard shortcuts", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void SetLanguage(string? code)
    {
        if (string.IsNullOrEmpty(code)) return;
        App.Services.GetRequiredService<DBTeam.App.Services.LocalizationService>().SetLanguage(code);
        StatusMessage = $"Language: {code}";
    }
}
