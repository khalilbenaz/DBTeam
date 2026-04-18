using System;
using System.Linq;
using System.Windows;
using AvalonDock.Layout;
using DBTeam.Core.Events;
using DBTeam.Modules.QueryEditor.Formatting;
using DBTeam.Modules.QueryEditor.ViewModels;
using DBTeam.Modules.QueryEditor.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DBTeam.App.Shell;

public partial class MainWindow : Window
{
    private readonly IEventBus _bus;

    public MainWindow(IEventBus bus)
    {
        InitializeComponent();
        _bus = bus;
        _bus.Subscribe<OpenQueryEditorRequest>(OnOpenQueryEditor);
        _bus.Subscribe<OpenDocumentRequest>(OnOpenDocument);
        _bus.Subscribe<ConnectionOpenedEvent>(_ => Dispatcher.Invoke(() => { if (ObjectExplorerPane is not null) ObjectExplorerPane.IsActive = true; }));
        _bus.Subscribe<ShowPaneRequest>(e => Dispatcher.Invoke(() =>
        {
            if (e.PaneId == "OBJECT_EXPLORER" && ObjectExplorerPane is not null) ObjectExplorerPane.IsActive = true;
            else if (e.PaneId == "CONNECTIONS" && ConnectionsPane is not null) ConnectionsPane.IsActive = true;
        }));
        TryLoadIcon();
        Loaded += OnWindowLoaded;
        Closing += OnWindowClosing;
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var svc = App.Services.GetService(typeof(Services.SessionService)) as Services.SessionService;
            var state = svc?.Load();
            if (state is null) return;
            var conns = (App.Services.GetService(typeof(DBTeam.Core.Abstractions.IConnectionService)) as DBTeam.Core.Abstractions.IConnectionService);
            foreach (var d in state.Documents)
            {
                if (d.Kind != "Query" || string.IsNullOrEmpty(d.ConnectionId)) continue;
                if (!System.Guid.TryParse(d.ConnectionId, out var id)) continue;
                var conn = conns?.Saved.FirstOrDefault(c => c.Id == id);
                if (conn is null) continue;
                OnOpenQueryEditor(new OpenQueryEditorRequest { Connection = conn, Database = d.Database, InitialSql = d.Sql });
            }
        }
        catch { }
    }

    private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        try
        {
            var svc = App.Services.GetService(typeof(Services.SessionService)) as Services.SessionService;
            if (svc is null) return;
            var state = new Services.SessionState();
            foreach (var child in DocumentsPane.Children)
            {
                if (child is LayoutDocument doc && doc.Content is QueryEditorView qv && qv.DataContext is QueryEditorViewModel qvm && qvm.Connection is not null)
                {
                    state.Documents.Add(new Services.SessionDocument
                    {
                        Kind = "Query",
                        Title = doc.Title,
                        ConnectionId = qvm.Connection.Id.ToString(),
                        Database = qvm.Database,
                        Sql = qvm.Sql
                    });
                }
            }
            svc.Save(state);
        }
        catch { }
    }

    private void TryLoadIcon()
    {
        foreach (var pack in new[]
        {
            "pack://application:,,,/DBTeam.App;component/Resources/app-icon.png",
            "pack://application:,,,/DBTeam.App;component/Resources/app-icon.ico"
        })
        {
            try
            {
                Icon = new System.Windows.Media.Imaging.BitmapImage(new System.Uri(pack, System.UriKind.Absolute));
                return;
            }
            catch { }
        }
    }

    private void OnOpenQueryEditor(OpenQueryEditorRequest req)
    {
        Dispatcher.Invoke(() =>
        {
            var vm = App.Services.GetRequiredService<QueryEditorViewModel>();
            vm.Connection = req.Connection;
            vm.Database = req.Database;
            if (!string.IsNullOrEmpty(req.InitialSql))
            {
                try
                {
                    var formatted = TSqlFormatter.Format(req.InitialSql!, null, out var errors);
                    vm.Sql = (errors is { Count: > 0 }) ? req.InitialSql! : formatted;
                }
                catch { vm.Sql = req.InitialSql!; }
            }
            var view = new QueryEditorView { DataContext = vm };
            var doc = new LayoutDocument { Title = $"Query - {req.Connection.Name}" + (req.Database is null ? "" : $" [{req.Database}]"), Content = view };
            DocumentsPane.Children.Add(doc);
            doc.IsActive = true;
        });
    }

    private void OnOpenDocument(OpenDocumentRequest req)
    {
        Dispatcher.Invoke(() =>
        {
            var doc = new LayoutDocument { Title = req.Title, Content = req.Content };
            DocumentsPane.Children.Add(doc);
            doc.IsActive = true;
        });
    }
}
