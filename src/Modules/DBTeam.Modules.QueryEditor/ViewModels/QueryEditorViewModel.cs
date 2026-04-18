using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Windows;
using DBTeam.Core.Abstractions;
using DBTeam.Core.Events;
using DBTeam.Core.Infrastructure;
using DBTeam.Core.Models;
using DBTeam.Modules.QueryEditor.Formatting;
using DBTeam.Modules.ResultsGrid.Export;
using Microsoft.Win32;

namespace DBTeam.Modules.QueryEditor.ViewModels;

public partial class QueryEditorViewModel : ObservableObject
{
    private readonly IQueryExecutionService _exec;
    private readonly IDatabaseMetadataService _meta;
    private readonly IQueryHistoryStore? _history;

    public QueryEditorViewModel(IQueryExecutionService exec, IDatabaseMetadataService meta, IQueryHistoryStore? history = null)
    {
        _exec = exec; _meta = meta; _history = history;
        Databases = new ObservableCollection<string>();
    }

    [ObservableProperty] private SqlConnectionInfo? connection;
    [ObservableProperty] private string? database;
    [ObservableProperty] private string sql = "-- New query\nSELECT @@VERSION;";
    [ObservableProperty] private string statusText = "Ready";
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string messages = string.Empty;
    [ObservableProperty] private TimeSpan elapsed;
    [ObservableProperty] private int rowCount;

    public ObservableCollection<string> Databases { get; }
    public ObservableCollection<DataTable> Results { get; } = new();

    partial void OnConnectionChanged(SqlConnectionInfo? value)
    {
        if (value is not null) _ = LoadDatabasesAsync();
    }

    [RelayCommand]
    public Task RefreshDatabasesAsync() => LoadDatabasesAsync();

    private async Task LoadDatabasesAsync()
    {
        if (Connection is null) return;
        try
        {
            var dbs = await _meta.GetDatabasesAsync(Connection);
            Databases.Clear();
            foreach (var d in dbs) Databases.Add(d);
            if (string.IsNullOrEmpty(Database) && !string.IsNullOrEmpty(Connection.Database)) Database = Connection.Database;
        }
        catch { }
    }

    [RelayCommand]
    public async Task ExecuteAsync()
    {
        if (Connection is null) { StatusText = "No connection"; return; }
        IsBusy = true;
        Results.Clear();
        Messages = string.Empty;
        StatusText = "Executing...";
        var req = new QueryRequest { Sql = Sql, Database = Database };
        var r = await _exec.ExecuteAsync(Connection, req);
        foreach (var ds in r.ResultSets) Results.Add(ds);
        Messages = string.Join(Environment.NewLine, r.Messages);
        if (r.HasError) StatusText = "Error";
        else StatusText = $"Done · {r.ResultSets.Count} result set(s) · {r.RowsAffected} rows affected";
        Elapsed = r.Elapsed;
        RowCount = 0;
        foreach (var t in r.ResultSets) RowCount += t.Rows.Count;
        IsBusy = false;

        if (_history is not null)
        {
            try
            {
                await _history.AppendAsync(new QueryHistoryEntry
                {
                    Sql = Sql,
                    ConnectionName = Connection.Name,
                    Database = Database,
                    Elapsed = r.Elapsed,
                    RowsAffected = r.RowsAffected,
                    Success = !r.HasError
                });
            }
            catch { }
        }
    }

    [RelayCommand]
    public void FormatSql()
    {
        if (string.IsNullOrWhiteSpace(Sql)) return;
        var formatted = TSqlFormatter.Format(Sql, null, out var errors);
        if (errors is { Count: > 0 })
        {
            Messages = string.Join(Environment.NewLine, System.Linq.Enumerable.Select(errors, e => $"Parse error line {e.Line}:{e.Column} - {e.Message}"));
            StatusText = "Format: parse errors";
            return;
        }
        Sql = formatted;
        StatusText = "Formatted";
    }

    [RelayCommand]
    public void Export(string? format)
    {
        if (Results.Count == 0) { StatusText = "Nothing to export"; return; }
        var fmt = (format ?? "Csv").ToLowerInvariant() switch
        {
            "excel" or "xlsx" => ExportFormat.Excel,
            "json" => ExportFormat.Json,
            "xml" => ExportFormat.Xml,
            _ => ExportFormat.Csv
        };
        var exporter = ResultExporterFactory.Create(fmt);
        var suggested = $"query-{DateTime.Now:yyyyMMdd-HHmmss}.{exporter.Extension}";
        var dlg = new SaveFileDialog { FileName = suggested, Filter = exporter.Filter };
        if (dlg.ShowDialog() != true) return;

        try
        {
            using var fs = File.Create(dlg.FileName);
            exporter.Export(Results[0], fs);
            StatusText = $"Exported: {dlg.FileName}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Export failed", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText = "Export failed";
        }
    }

    /// <summary>
    /// Generate a pivot script from the first result set, using column names as hints.
    /// User picks row/column/value axes afterwards in the generated SQL.
    /// </summary>
    [RelayCommand]
    public void GeneratePivot()
    {
        if (Results.Count == 0 || Connection is null) { StatusText = "Execute a query first"; return; }
        var t = Results[0];
        if (t.Columns.Count < 3) { StatusText = "Need at least 3 columns (row, column, value)"; return; }
        var rowCol = t.Columns[0].ColumnName;
        var colCol = t.Columns[1].ColumnName;
        var valCol = t.Columns[t.Columns.Count - 1].ColumnName;
        var distinctVals = new System.Collections.Generic.HashSet<string>();
        foreach (System.Data.DataRow r in t.Rows)
        {
            var v = r[colCol]?.ToString();
            if (!string.IsNullOrEmpty(v) && distinctVals.Count < 20) distinctVals.Add(v);
        }
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("-- PIVOT skeleton. Adjust source, axis columns, aggregator, and value list.");
        sb.AppendLine("SELECT *");
        sb.AppendLine("FROM (");
        sb.AppendLine($"    SELECT [{rowCol}], [{colCol}], [{valCol}]");
        sb.AppendLine("    FROM (<your-source-query>)");
        sb.AppendLine(") src");
        sb.AppendLine("PIVOT (");
        sb.AppendLine($"    SUM([{valCol}])");
        sb.AppendLine($"    FOR [{colCol}] IN ({string.Join(", ", System.Linq.Enumerable.Select(distinctVals, v => $"[{v}]"))})");
        sb.AppendLine(") p;");
        var bus = ServiceLocator.TryGet<IEventBus>();
        bus?.Publish(new OpenQueryEditorRequest { Connection = Connection, Database = Database, InitialSql = sb.ToString() });
        StatusText = "Pivot skeleton generated";
    }

    /// <summary>
    /// Master-detail: given a column name and its value, build a SELECT against the target table
    /// (deduced from the column name or a naming convention like FK_Table_Id → Table).
    /// </summary>
    [RelayCommand]
    public void FollowRelation(System.Tuple<string, object?>? arg)
    {
        if (arg is null || Connection is null) return;
        var column = arg.Item1;
        var value = arg.Item2;
        if (string.IsNullOrWhiteSpace(column) || value is null) return;
        // Heuristic: "CustomerId" → table "Customer"; "FK_Order_Customer" → "Customer"
        string target;
        if (column.EndsWith("Id", System.StringComparison.OrdinalIgnoreCase) && column.Length > 2)
            target = column[..^2];
        else if (column.StartsWith("FK_", System.StringComparison.OrdinalIgnoreCase))
            target = column.Split('_').LastOrDefault() ?? column;
        else
            target = column;
        var lit = value switch
        {
            string s => $"N'{s.Replace("'", "''")}'",
            System.Guid g => $"'{g}'",
            System.DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss.fff}'",
            _ => System.Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "NULL"
        };
        var sql = $"SELECT TOP 100 *\nFROM [dbo].[{target}]\nWHERE [{column}] = {lit}\n   OR [Id] = {lit};";
        var bus = ServiceLocator.TryGet<IEventBus>();
        bus?.Publish(new OpenQueryEditorRequest { Connection = Connection, Database = Database, InitialSql = sql });
    }

    [RelayCommand]
    public async Task EstimatedPlanAsync()
    {
        if (Connection is null) return;
        IsBusy = true;
        try
        {
            var xml = await _exec.GetEstimatedPlanXmlAsync(Connection, new QueryRequest { Sql = Sql, Database = Database });
            Messages = xml;
            StatusText = "Estimated plan loaded";
        }
        catch (Exception ex) { Messages = ex.Message; StatusText = "Error"; }
        IsBusy = false;
    }
}
