using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DBTeam.Core.Abstractions;
using DBTeam.Core.Events;
using DBTeam.Core.Models;

namespace DBTeam.Modules.TableDesigner.ViewModels;

public partial class ColumnRowViewModel : ObservableObject
{
    [ObservableProperty] private string name = "Column1";
    [ObservableProperty] private string dataType = "int";
    [ObservableProperty] private int? length;
    [ObservableProperty] private bool isNullable = true;
    [ObservableProperty] private bool isIdentity;
    [ObservableProperty] private bool isPrimaryKey;
    [ObservableProperty] private string? defaultExpression;
}

public partial class TableDesignerViewModel : ObservableObject
{
    private readonly IConnectionService _connSvc;
    private readonly IDatabaseMetadataService _meta;
    private readonly IEventBus _bus;

    public TableDesignerViewModel(IConnectionService connSvc, IDatabaseMetadataService meta, IEventBus bus)
    {
        _connSvc = connSvc; _meta = meta; _bus = bus;
        Connections = new();
        Databases = new();
        DataTypes = new() { "int","bigint","smallint","tinyint","bit","decimal","numeric","money","float","real",
            "char","varchar","nvarchar","nchar","text","ntext","date","datetime","datetime2","datetimeoffset","time",
            "uniqueidentifier","varbinary","binary","xml" };
        Columns = new() { new() { Name = "Id", DataType = "int", IsNullable = false, IsIdentity = true, IsPrimaryKey = true } };
        _ = LoadConnectionsAsync();
    }

    public ObservableCollection<SqlConnectionInfo> Connections { get; }
    public ObservableCollection<string> Databases { get; }
    public ObservableCollection<string> DataTypes { get; }
    public ObservableCollection<ColumnRowViewModel> Columns { get; }

    [ObservableProperty] private SqlConnectionInfo? connection;
    [ObservableProperty] private string? database;
    [ObservableProperty] private string schema = "dbo";
    [ObservableProperty] private string tableName = "NewTable";
    [ObservableProperty] private ColumnRowViewModel? selected;
    [ObservableProperty] private string status = "Ready";

    private async Task LoadConnectionsAsync()
    {
        var all = await _connSvc.LoadAllAsync();
        Connections.Clear();
        foreach (var c in all) Connections.Add(c);
    }

    partial void OnConnectionChanged(SqlConnectionInfo? value) => _ = LoadDbsAsync();

    private async Task LoadDbsAsync()
    {
        Databases.Clear();
        if (Connection is null) return;
        try { foreach (var d in await _meta.GetDatabasesAsync(Connection)) Databases.Add(d); } catch { }
    }

    [RelayCommand]
    public async Task LoadExistingAsync()
    {
        if (Connection is null || string.IsNullOrEmpty(Database) || string.IsNullOrWhiteSpace(Schema) || string.IsNullOrWhiteSpace(TableName))
        { Status = "Pick connection, database, schema, table name"; return; }
        try
        {
            var cols = await _meta.GetColumnsAsync(Connection, Database!, Schema, TableName);
            Columns.Clear();
            foreach (var c in cols)
            {
                Columns.Add(new ColumnRowViewModel
                {
                    Name = c.Name,
                    DataType = c.DataType,
                    Length = c.MaxLength,
                    IsNullable = c.IsNullable,
                    IsIdentity = c.IsIdentity,
                    IsPrimaryKey = c.IsPrimaryKey,
                    DefaultExpression = c.DefaultExpression
                });
            }
            Status = $"Loaded {cols.Count} column(s) from [{Schema}].[{TableName}]";
        }
        catch (System.Exception ex) { Status = ex.Message; }
    }

    [RelayCommand] private void AddColumn() => Columns.Add(new ColumnRowViewModel { Name = $"Column{Columns.Count + 1}" });
    [RelayCommand] private void RemoveColumn() { if (Selected is not null) Columns.Remove(Selected); }
    [RelayCommand] private void MoveUp() { if (Selected is null) return; var i = Columns.IndexOf(Selected); if (i > 0) Columns.Move(i, i - 1); }
    [RelayCommand] private void MoveDown() { if (Selected is null) return; var i = Columns.IndexOf(Selected); if (i < Columns.Count - 1 && i >= 0) Columns.Move(i, i + 1); }

    [RelayCommand]
    private void GenerateDdl()
    {
        var ddl = BuildDdl();
        if (Connection is null) return;
        _bus.Publish(new OpenQueryEditorRequest { Connection = Connection, Database = Database, InitialSql = ddl });
        Status = "DDL generated in new tab";
    }

    public string BuildDdl()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"CREATE TABLE [{Schema}].[{TableName}] (");
        for (int i = 0; i < Columns.Count; i++)
        {
            var c = Columns[i];
            sb.Append($"    [{c.Name}] {FormatType(c)}");
            if (c.IsIdentity) sb.Append(" IDENTITY(1,1)");
            sb.Append(c.IsNullable ? " NULL" : " NOT NULL");
            if (!string.IsNullOrWhiteSpace(c.DefaultExpression)) sb.Append($" DEFAULT {c.DefaultExpression}");
            if (i < Columns.Count - 1 || Columns.Any(x => x.IsPrimaryKey)) sb.Append(',');
            sb.AppendLine();
        }
        var pks = Columns.Where(x => x.IsPrimaryKey).Select(x => $"[{x.Name}]").ToList();
        if (pks.Count > 0)
            sb.AppendLine($"    CONSTRAINT [PK_{TableName}] PRIMARY KEY ({string.Join(",", pks)})");
        sb.AppendLine(");");
        return sb.ToString();
    }

    private static string FormatType(ColumnRowViewModel c)
    {
        var t = (c.DataType ?? "").ToLowerInvariant();
        return t switch
        {
            "varchar" or "nvarchar" or "char" or "nchar" or "varbinary" or "binary"
                => $"{c.DataType}({(c.Length.HasValue ? c.Length.ToString() : (t.StartsWith("n") ? "100" : "50"))})",
            "decimal" or "numeric" => $"{c.DataType}(18,2)",
            _ => c.DataType ?? "int"
        };
    }
}
