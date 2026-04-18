using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Windows;
using DBTeam.Core.Abstractions;
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
