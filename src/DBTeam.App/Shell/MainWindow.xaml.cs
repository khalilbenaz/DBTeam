using System;
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
