using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DBTeam.Core.Abstractions;
using DBTeam.Core.Events;
using DBTeam.Core.Models;
using DBTeam.Modules.DataCompare.Engine;

namespace DBTeam.Modules.DataCompare.ViewModels;

public partial class DataCompareViewModel : ObservableObject
{
    private readonly IConnectionService _connSvc;
    private readonly IDatabaseMetadataService _meta;
    private readonly DataCompareEngine _engine;
    private readonly IEventBus _bus;

    public DataCompareViewModel(IConnectionService connSvc, IDatabaseMetadataService meta, IEventBus bus)
    {
        _connSvc = connSvc; _meta = meta; _bus = bus;
        _engine = new DataCompareEngine(meta);
        Connections = new ObservableCollection<SqlConnectionInfo>();
        SourceDatabases = new(); TargetDatabases = new();
        Tables = new(); Rows = new();
        _ = LoadConnectionsAsync();
    }

    public ObservableCollection<SqlConnectionInfo> Connections { get; }
    public ObservableCollection<string> SourceDatabases { get; }
    public ObservableCollection<string> TargetDatabases { get; }
    public ObservableCollection<string> Tables { get; }
    public ObservableCollection<RowDiff> Rows { get; }

    [ObservableProperty] private SqlConnectionInfo? sourceConnection;
    [ObservableProperty] private SqlConnectionInfo? targetConnection;
    [ObservableProperty] private string? sourceDatabase;
    [ObservableProperty] private string? targetDatabase;
    [ObservableProperty] private string? selectedTable;
    [ObservableProperty] private string status = "Ready";
    [ObservableProperty] private bool isBusy;

    private IReadOnlyList<string> _keyCols = new List<string>();
    private IReadOnlyList<string> _allCols = new List<string>();

    private async Task LoadConnectionsAsync()
    {
        var all = await _connSvc.LoadAllAsync();
        Connections.Clear();
        foreach (var c in all) Connections.Add(c);
    }

    partial void OnSourceConnectionChanged(SqlConnectionInfo? value) => _ = LoadDbsAsync(value, SourceDatabases);
    partial void OnTargetConnectionChanged(SqlConnectionInfo? value) => _ = LoadDbsAsync(value, TargetDatabases);
    partial void OnSourceDatabaseChanged(string? value) => _ = LoadTablesAsync();

    private async Task LoadDbsAsync(SqlConnectionInfo? c, ObservableCollection<string> to)
    {
        to.Clear();
        if (c is null) return;
        try { foreach (var d in await _meta.GetDatabasesAsync(c)) to.Add(d); } catch { }
    }

    private async Task LoadTablesAsync()
    {
        Tables.Clear();
        if (SourceConnection is null || string.IsNullOrEmpty(SourceDatabase)) return;
        var items = await _meta.GetTablesAsync(SourceConnection, SourceDatabase!);
        foreach (var t in items) Tables.Add($"{t.Schema}.{t.Name}");
    }

    [RelayCommand]
    private async Task CompareAsync()
    {
        if (SourceConnection is null || TargetConnection is null
            || string.IsNullOrEmpty(SourceDatabase) || string.IsNullOrEmpty(TargetDatabase)
            || string.IsNullOrEmpty(SelectedTable))
        { Status = "Pick source, target, databases and a table"; return; }
        var parts = SelectedTable!.Split('.');
        var schema = parts[0]; var table = parts[1];
        IsBusy = true; Status = "Comparing rows...";
        try
        {
            var (diffs, keys, all) = await _engine.CompareAsync(SourceConnection!, SourceDatabase!, schema, table, TargetConnection!, TargetDatabase!);
            _keyCols = keys; _allCols = all;
            Rows.Clear();
            foreach (var d in diffs.Where(x => x.State != RowState.Identical)) Rows.Add(d);
            Status = $"{diffs.Count} rows · {Rows.Count} differences · PK: {string.Join(",", keys)}";
        }
        catch (System.Exception ex) { Status = ex.Message; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void GenerateScript()
    {
        if (Rows.Count == 0 || string.IsNullOrEmpty(SelectedTable) || TargetConnection is null) return;
        var parts = SelectedTable!.Split('.');
        var script = DataCompareEngine.GenerateSyncScript(parts[0], parts[1], Rows, _keyCols, _allCols);
        _bus.Publish(new OpenQueryEditorRequest { Connection = TargetConnection, Database = TargetDatabase, InitialSql = script });
        Status = "Script generated";
    }
}
