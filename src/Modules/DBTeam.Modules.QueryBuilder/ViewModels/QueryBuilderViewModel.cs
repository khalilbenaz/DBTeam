using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DBTeam.Core.Abstractions;
using DBTeam.Core.Events;
using DBTeam.Core.Models;

namespace DBTeam.Modules.QueryBuilder.ViewModels;

public partial class ColumnPickVm : ObservableObject
{
    [ObservableProperty] private bool isSelected;
    public string Name { get; set; } = "";
    public string DataType { get; set; } = "";
    public bool IsPrimaryKey { get; set; }
}

public partial class TablePickVm : ObservableObject
{
    [ObservableProperty] private bool isSelected;
    [ObservableProperty] private string? alias;
    public string Schema { get; set; } = "dbo";
    public string Name { get; set; } = "";
    public string QualifiedName => $"{Schema}.{Name}";
    public ObservableCollection<ColumnPickVm> Columns { get; } = new();
}

public partial class QueryBuilderViewModel : ObservableObject
{
    private readonly IConnectionService _connSvc;
    private readonly IDatabaseMetadataService _meta;
    private readonly IEventBus _bus;

    public QueryBuilderViewModel(IConnectionService connSvc, IDatabaseMetadataService meta, IEventBus bus)
    {
        _connSvc = connSvc; _meta = meta; _bus = bus;
        Connections = new(); Databases = new(); Tables = new();
        _ = LoadConnectionsAsync();
    }

    public ObservableCollection<SqlConnectionInfo> Connections { get; }
    public ObservableCollection<string> Databases { get; }
    public ObservableCollection<TablePickVm> Tables { get; }

    [ObservableProperty] private SqlConnectionInfo? connection;
    [ObservableProperty] private string? database;
    [ObservableProperty] private string generatedSql = "";
    [ObservableProperty] private string whereClause = "";
    [ObservableProperty] private string orderBy = "";
    [ObservableProperty] private int? topN;
    [ObservableProperty] private bool distinct;
    [ObservableProperty] private string filterTable = "";
    [ObservableProperty] private string status = "Pick a connection + database, then select tables/columns.";

    private async Task LoadConnectionsAsync()
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
        try
        {
            foreach (var t in await _meta.GetTablesAsync(Connection, Database!))
            {
                var vm = new TablePickVm { Schema = t.Schema ?? "dbo", Name = t.Name };
                Tables.Add(vm);
            }
            foreach (var v in await _meta.GetViewsAsync(Connection, Database!))
            {
                var vm = new TablePickVm { Schema = v.Schema ?? "dbo", Name = v.Name };
                Tables.Add(vm);
            }
            Status = $"{Tables.Count} table(s)/view(s) loaded";
        }
        catch (System.Exception ex) { Status = ex.Message; }
    }

    [RelayCommand]
    public async Task LoadColumnsAsync(TablePickVm? t)
    {
        if (t is null || Connection is null || string.IsNullOrEmpty(Database)) return;
        if (t.Columns.Count > 0) return;
        try
        {
            foreach (var c in await _meta.GetColumnsAsync(Connection, Database!, t.Schema, t.Name))
                t.Columns.Add(new ColumnPickVm { Name = c.Name, DataType = c.DataType, IsPrimaryKey = c.IsPrimaryKey });
        }
        catch { }
    }

    [RelayCommand]
    public void Build()
    {
        var selectedTables = Tables.Where(t => t.IsSelected).ToList();
        if (selectedTables.Count == 0) { Status = "Select at least one table"; return; }
        var sb = new StringBuilder();
        sb.Append("SELECT ");
        if (Distinct) sb.Append("DISTINCT ");
        if (TopN is > 0) sb.Append($"TOP {TopN} ");
        var cols = selectedTables.SelectMany(t =>
            t.Columns.Where(c => c.IsSelected)
                .Select(c => $"[{t.Alias ?? t.Name}].[{c.Name}]")).ToList();
        sb.Append(cols.Count == 0 ? "*" : string.Join(", ", cols));
        sb.AppendLine();
        sb.AppendLine("FROM " + string.Join(" CROSS JOIN ", selectedTables.Select(t =>
            $"[{t.Schema}].[{t.Name}]" + (string.IsNullOrEmpty(t.Alias) ? "" : $" AS [{t.Alias}]"))));
        if (!string.IsNullOrWhiteSpace(WhereClause)) sb.AppendLine("WHERE " + WhereClause);
        if (!string.IsNullOrWhiteSpace(OrderBy)) sb.AppendLine("ORDER BY " + OrderBy);
        sb.Append(";");
        GeneratedSql = sb.ToString();
        Status = $"Generated for {selectedTables.Count} table(s), {cols.Count} column(s)";
    }

    [RelayCommand]
    public void OpenInEditor()
    {
        if (Connection is null || string.IsNullOrWhiteSpace(GeneratedSql)) return;
        _bus.Publish(new OpenQueryEditorRequest { Connection = Connection, Database = Database, InitialSql = GeneratedSql });
    }
}
