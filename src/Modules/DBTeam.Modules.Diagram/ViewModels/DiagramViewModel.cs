using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DBTeam.Core.Abstractions;
using DBTeam.Core.Models;

namespace DBTeam.Modules.Diagram.ViewModels;

public partial class TableBoxViewModel : ObservableObject
{
    [ObservableProperty] private string schema = "dbo";
    [ObservableProperty] private string name = "";
    [ObservableProperty] private double x;
    [ObservableProperty] private double y;
    [ObservableProperty] private double width = 200;
    public string QualifiedName => $"{Schema}.{Name}";
    public ObservableCollection<string> Columns { get; } = new();
}

public sealed class FkLineViewModel
{
    public TableBoxViewModel From { get; set; } = default!;
    public TableBoxViewModel To { get; set; } = default!;
    public string Label { get; set; } = "";
}

public partial class DiagramViewModel : ObservableObject
{
    private readonly IConnectionService _connSvc;
    private readonly IDatabaseMetadataService _meta;

    public DiagramViewModel(IConnectionService connSvc, IDatabaseMetadataService meta)
    {
        _connSvc = connSvc; _meta = meta;
        Connections = new(); Databases = new(); Tables = new(); Lines = new();
        _ = LoadAsync();
    }

    public ObservableCollection<SqlConnectionInfo> Connections { get; }
    public ObservableCollection<string> Databases { get; }
    public ObservableCollection<TableBoxViewModel> Tables { get; }
    public ObservableCollection<FkLineViewModel> Lines { get; }

    [ObservableProperty] private SqlConnectionInfo? connection;
    [ObservableProperty] private string? database;
    [ObservableProperty] private string status = "Ready";
    [ObservableProperty] private bool isBusy;

    private async Task LoadAsync()
    {
        var all = await _connSvc.LoadAllAsync();
        Connections.Clear(); foreach (var c in all) Connections.Add(c);
    }

    partial void OnConnectionChanged(SqlConnectionInfo? value) => _ = LoadDbsAsync();
    private async Task LoadDbsAsync()
    {
        Databases.Clear();
        if (Connection is null) return;
        try { foreach (var d in await _meta.GetDatabasesAsync(Connection)) Databases.Add(d); } catch { }
    }

    [RelayCommand]
    private async Task LoadDiagramAsync()
    {
        if (Connection is null || string.IsNullOrEmpty(Database)) { Status = "Pick connection + database"; return; }
        IsBusy = true; Status = "Loading schema...";
        Tables.Clear(); Lines.Clear();
        try
        {
            var tables = await _meta.GetTablesAsync(Connection, Database!);
            var map = new Dictionary<string, TableBoxViewModel>(StringComparer.OrdinalIgnoreCase);
            int cols = (int)Math.Ceiling(Math.Sqrt(tables.Count));
            int ix = 0;
            foreach (var t in tables)
            {
                var box = new TableBoxViewModel
                {
                    Schema = t.Schema ?? "dbo",
                    Name = t.Name,
                    X = (ix % cols) * 240 + 20,
                    Y = (ix / cols) * 260 + 20
                };
                var columns = await _meta.GetColumnsAsync(Connection, Database!, box.Schema, box.Name);
                foreach (var c in columns.Take(12))
                    box.Columns.Add($"{(c.IsPrimaryKey ? "🔑 " : "")}{c.Name}  :  {c.DataType}");
                if (columns.Count > 12) box.Columns.Add($"… and {columns.Count - 12} more");
                Tables.Add(box);
                map[box.QualifiedName] = box;
                ix++;
            }

            foreach (var t in tables)
            {
                var fks = await _meta.GetForeignKeysAsync(Connection, Database!, t.Schema ?? "dbo", t.Name);
                foreach (var fk in fks)
                {
                    var fromKey = $"{t.Schema}.{t.Name}";
                    var toKey = $"{fk.ReferencedSchema}.{fk.ReferencedTable}";
                    if (map.TryGetValue(fromKey, out var a) && map.TryGetValue(toKey, out var b))
                        Lines.Add(new FkLineViewModel { From = a, To = b, Label = fk.Name });
                }
            }
            Status = $"{Tables.Count} table(s) · {Lines.Count} relation(s)";
        }
        catch (Exception ex) { Status = ex.Message; }
        IsBusy = false;
    }
}
