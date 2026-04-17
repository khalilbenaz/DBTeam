using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DBTeam.Core.Abstractions;
using DBTeam.Core.Events;
using DBTeam.Core.Models;

namespace DBTeam.Modules.ObjectExplorer.ViewModels;

public partial class ObjectExplorerViewModel : ObservableObject
{
    private readonly IDatabaseMetadataService _meta;
    private readonly IEventBus _bus;

    public ObjectExplorerViewModel(IDatabaseMetadataService meta, IEventBus bus)
    {
        _meta = meta; _bus = bus;
        _bus.Subscribe<ConnectionOpenedEvent>(OnConnectionOpened);
    }

    public ObservableCollection<TreeNodeViewModel> Roots { get; } = new();

    [ObservableProperty] private TreeNodeViewModel? selected;

    private void OnConnectionOpened(ConnectionOpenedEvent e)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() => AddServer(e.Connection));
    }

    private void AddServer(SqlConnectionInfo c)
    {
        foreach (var existing in Roots)
        {
            if (existing.Connection?.Id == c.Id)
            {
                existing.IsSelected = true;
                existing.IsExpanded = true;
                return;
            }
        }
        var root = new TreeNodeViewModel
        {
            Title = $"{c.Name} ({c.Server})",
            Kind = DbObjectKind.Server,
            Connection = c,
            Loader = LoadDatabasesAsync
        };
        root.Children.Add(new TreeNodeViewModel { Title = "Loading..." });
        Roots.Add(root);
    }

    private async Task LoadDatabasesAsync(TreeNodeViewModel server)
    {
        server.Children.Clear();
        var dbs = await _meta.GetDatabasesAsync(server.Connection!);
        foreach (var db in dbs)
        {
            var dbNode = new TreeNodeViewModel
            {
                Title = db,
                Kind = DbObjectKind.Database,
                Connection = server.Connection,
                Database = db,
                Loader = LoadDbFoldersAsync
            };
            dbNode.Children.Add(new TreeNodeViewModel { Title = "..." });
            server.Children.Add(dbNode);
        }
    }

    private Task LoadDbFoldersAsync(TreeNodeViewModel db)
    {
        db.Children.Clear();
        db.Children.Add(MakeFolder(db, "Tables", DbObjectKind.TableFolder, LoadTablesAsync));
        db.Children.Add(MakeFolder(db, "Views", DbObjectKind.ViewFolder, LoadViewsAsync));
        db.Children.Add(MakeFolder(db, "Stored Procedures", DbObjectKind.StoredProcedureFolder, LoadProceduresAsync));
        db.Children.Add(MakeFolder(db, "Functions", DbObjectKind.FunctionFolder, LoadFunctionsAsync));
        return Task.CompletedTask;
    }

    private static TreeNodeViewModel MakeFolder(TreeNodeViewModel parent, string title, DbObjectKind kind, System.Func<TreeNodeViewModel, Task> loader)
    {
        var n = new TreeNodeViewModel { Title = title, Kind = kind, Connection = parent.Connection, Database = parent.Database, Loader = loader };
        n.Children.Add(new TreeNodeViewModel { Title = "..." });
        return n;
    }

    private async Task LoadTablesAsync(TreeNodeViewModel folder)
    {
        folder.Children.Clear();
        var items = await _meta.GetTablesAsync(folder.Connection!, folder.Database!);
        foreach (var t in items)
            folder.Children.Add(new TreeNodeViewModel { Title = $"{t.Schema}.{t.Name}", Kind = DbObjectKind.Table, Connection = folder.Connection, Database = folder.Database, Schema = t.Schema, ObjectName = t.Name });
    }

    private async Task LoadViewsAsync(TreeNodeViewModel folder)
    {
        folder.Children.Clear();
        var items = await _meta.GetViewsAsync(folder.Connection!, folder.Database!);
        foreach (var t in items)
            folder.Children.Add(new TreeNodeViewModel { Title = $"{t.Schema}.{t.Name}", Kind = DbObjectKind.View, Connection = folder.Connection, Database = folder.Database, Schema = t.Schema, ObjectName = t.Name });
    }

    private async Task LoadProceduresAsync(TreeNodeViewModel folder)
    {
        folder.Children.Clear();
        var items = await _meta.GetProceduresAsync(folder.Connection!, folder.Database!);
        foreach (var t in items)
            folder.Children.Add(new TreeNodeViewModel { Title = $"{t.Schema}.{t.Name}", Kind = DbObjectKind.StoredProcedure, Connection = folder.Connection, Database = folder.Database, Schema = t.Schema, ObjectName = t.Name });
    }

    private async Task LoadFunctionsAsync(TreeNodeViewModel folder)
    {
        folder.Children.Clear();
        var items = await _meta.GetFunctionsAsync(folder.Connection!, folder.Database!);
        foreach (var t in items)
            folder.Children.Add(new TreeNodeViewModel { Title = $"{t.Schema}.{t.Name}", Kind = DbObjectKind.Function, Connection = folder.Connection, Database = folder.Database, Schema = t.Schema, ObjectName = t.Name });
    }

    [RelayCommand]
    private void OpenNewQuery(TreeNodeViewModel? node)
    {
        var n = node ?? Selected;
        if (n?.Connection is null) return;
        string? sql = n.Kind switch
        {
            DbObjectKind.Table => $"SELECT TOP 100 * FROM [{n.Schema}].[{n.ObjectName}];",
            DbObjectKind.View => $"SELECT TOP 100 * FROM [{n.Schema}].[{n.ObjectName}];",
            _ => null
        };
        _bus.Publish(new OpenQueryEditorRequest { Connection = n.Connection, Database = n.Database, InitialSql = sql });
    }

    [RelayCommand]
    private void DisconnectServer(TreeNodeViewModel? node)
    {
        var n = node ?? Selected;
        if (n is null) return;
        var root = n;
        while (root.Kind != DbObjectKind.Server)
        {
            var parent = Roots.FirstOrDefault(r => Contains(r, root));
            if (parent is null) break;
            root = parent;
            if (root == n) break;
        }
        if (root.Kind == DbObjectKind.Server) Roots.Remove(root);
    }

    private static bool Contains(TreeNodeViewModel parent, TreeNodeViewModel target)
    {
        if (parent == target) return true;
        foreach (var c in parent.Children) if (Contains(c, target)) return true;
        return false;
    }

    [RelayCommand]
    private async Task ScriptObjectAsync(TreeNodeViewModel? node)
    {
        var n = node ?? Selected;
        if (n?.Connection is null || n.ObjectName is null || n.Schema is null || n.Database is null) return;
        var script = await _meta.ScriptObjectAsync(n.Connection, n.Database, n.Schema, n.ObjectName, n.Kind);
        _bus.Publish(new OpenQueryEditorRequest { Connection = n.Connection, Database = n.Database, InitialSql = script });
    }
}
