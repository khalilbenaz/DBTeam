using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DBTeam.Core.Abstractions;
using DBTeam.Core.Events;
using DBTeam.Core.Models;
using DBTeam.Modules.DataGenerator.Engine;

namespace DBTeam.Modules.DataGenerator.ViewModels;

public partial class DataGeneratorViewModel : ObservableObject
{
    private readonly IConnectionService _connSvc;
    private readonly IDatabaseMetadataService _meta;
    private readonly IEventBus _bus;
    private readonly DataGeneratorEngine _engine;

    public DataGeneratorViewModel(IConnectionService connSvc, IDatabaseMetadataService meta, IEventBus bus)
    {
        _connSvc = connSvc; _meta = meta; _bus = bus;
        _engine = new DataGeneratorEngine(meta);
        Connections = new(); Databases = new(); Tables = new();
        _ = LoadAsync();
    }

    public ObservableCollection<SqlConnectionInfo> Connections { get; }
    public ObservableCollection<string> Databases { get; }
    public ObservableCollection<string> Tables { get; }

    [ObservableProperty] private SqlConnectionInfo? connection;
    [ObservableProperty] private string? database;
    [ObservableProperty] private string? selectedTable;
    [ObservableProperty] private int rowCount = 100;
    [ObservableProperty] private bool skipIdentity = true;
    [ObservableProperty] private string status = "Ready";
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private DataTable? previewTable;
    [ObservableProperty] private int progressValue;

    private async Task LoadAsync()
    {
        var all = await _connSvc.LoadAllAsync();
        Connections.Clear(); foreach (var c in all) Connections.Add(c);
    }

    partial void OnConnectionChanged(SqlConnectionInfo? value) => _ = LoadDbsAsync();
    partial void OnDatabaseChanged(string? value) => _ = LoadTablesAsync();

    private async Task LoadDbsAsync()
    {
        Databases.Clear();
        if (Connection is null) return;
        try { foreach (var d in await _meta.GetDatabasesAsync(Connection)) Databases.Add(d); } catch { }
    }

    private async Task LoadTablesAsync()
    {
        Tables.Clear();
        if (Connection is null || string.IsNullOrEmpty(Database)) return;
        try { foreach (var t in await _meta.GetTablesAsync(Connection, Database!)) Tables.Add($"{t.Schema}.{t.Name}"); } catch { }
    }

    private (string schema, string table)? ParseTable()
    {
        if (string.IsNullOrEmpty(SelectedTable)) return null;
        var parts = SelectedTable!.Split('.');
        return parts.Length == 2 ? (parts[0], parts[1]) : null;
    }

    [RelayCommand]
    private async Task PreviewAsync()
    {
        var t = ParseTable();
        if (Connection is null || Database is null || t is null) { Status = "Pick connection, database, table"; return; }
        IsBusy = true; Status = "Generating preview...";
        try
        {
            var rows = await _engine.PreviewAsync(Connection, Database!, t.Value.schema, t.Value.table,
                new GenerationOptions { RowCount = Math.Min(RowCount, 50), SkipIdentity = SkipIdentity });
            var dt = new DataTable();
            if (rows.Count > 0)
            {
                foreach (var k in rows[0].Keys) dt.Columns.Add(k, typeof(object));
                foreach (var r in rows) dt.Rows.Add(r.Values.Select(v => v ?? DBNull.Value).ToArray());
            }
            PreviewTable = dt;
            Status = $"Preview: {rows.Count} row(s)";
        }
        catch (Exception ex) { Status = ex.Message; }
        IsBusy = false;
    }

    [RelayCommand]
    private async Task InsertAsync()
    {
        var t = ParseTable();
        if (Connection is null || Database is null || t is null) { Status = "Pick connection, database, table"; return; }
        if (MessageBox.Show($"Insert {RowCount} row(s) into [{t.Value.schema}].[{t.Value.table}]?", "Confirm",
            MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        IsBusy = true; Status = "Inserting...";
        var progress = new Progress<int>(n => { ProgressValue = n; Status = $"Inserted {n}/{RowCount}..."; });
        try
        {
            var n = await _engine.InsertAsync(Connection, Database!, t.Value.schema, t.Value.table,
                new GenerationOptions { RowCount = RowCount, SkipIdentity = SkipIdentity }, progress);
            Status = $"Done · {n} row(s) inserted";
        }
        catch (Exception ex) { Status = ex.Message; }
        IsBusy = false;
        ProgressValue = 0;
    }

    [RelayCommand]
    private async Task ExportScriptAsync()
    {
        var t = ParseTable();
        if (Connection is null || Database is null || t is null) { Status = "Pick table"; return; }
        IsBusy = true;
        try
        {
            var rows = await _engine.PreviewAsync(Connection, Database!, t.Value.schema, t.Value.table,
                new GenerationOptions { RowCount = RowCount, SkipIdentity = SkipIdentity });
            var script = DataGeneratorEngine.RowsToSqlScript(t.Value.schema, t.Value.table, rows);
            _bus.Publish(new OpenQueryEditorRequest { Connection = Connection, Database = Database, InitialSql = script });
            Status = $"Script generated ({rows.Count} rows)";
        }
        catch (Exception ex) { Status = ex.Message; }
        IsBusy = false;
    }
}
