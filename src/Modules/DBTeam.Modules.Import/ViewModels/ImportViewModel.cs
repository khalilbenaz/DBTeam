using System;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DBTeam.Core.Abstractions;
using DBTeam.Core.Events;
using DBTeam.Core.Models;
using DBTeam.Modules.Import.Engine;
using Microsoft.Win32;

namespace DBTeam.Modules.Import.ViewModels;

public partial class ImportViewModel : ObservableObject
{
    private readonly IConnectionService _connSvc;
    private readonly IDatabaseMetadataService _meta;
    private readonly IEventBus _bus;

    public ImportViewModel(IConnectionService connSvc, IDatabaseMetadataService meta, IEventBus bus)
    {
        _connSvc = connSvc; _meta = meta; _bus = bus;
        Connections = new(); Databases = new();
        _ = LoadConnectionsAsync();
    }

    public ObservableCollection<SqlConnectionInfo> Connections { get; }
    public ObservableCollection<string> Databases { get; }

    [ObservableProperty] private SqlConnectionInfo? connection;
    [ObservableProperty] private string? database;
    [ObservableProperty] private string schema = "dbo";
    [ObservableProperty] private string tableName = "ImportedData";
    [ObservableProperty] private string? filePath;
    [ObservableProperty] private bool hasHeader = true;
    [ObservableProperty] private string delimiter = ",";
    [ObservableProperty] private DataTable? preview;
    [ObservableProperty] private string status = "Pick a file to import";
    [ObservableProperty] private int rowCount;

    private async Task LoadConnectionsAsync()
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
    public void PickFile()
    {
        var dlg = new OpenFileDialog { Filter = "CSV / TSV (*.csv;*.tsv;*.txt)|*.csv;*.tsv;*.txt|All files|*.*" };
        if (dlg.ShowDialog() != true) return;
        FilePath = dlg.FileName;
        Delimiter = Path.GetExtension(FilePath).Equals(".tsv", StringComparison.OrdinalIgnoreCase) ? "\\t" : ",";
        LoadPreview();
    }

    [RelayCommand]
    public void LoadPreview()
    {
        if (string.IsNullOrEmpty(FilePath) || !File.Exists(FilePath)) return;
        try
        {
            using var fs = File.OpenRead(FilePath);
            var delim = Delimiter == "\\t" ? '\t' : (Delimiter?.Length > 0 ? Delimiter[0] : ',');
            var dt = CsvImporter.ReadCsv(fs, HasHeader, delim);
            Preview = dt;
            RowCount = dt.Rows.Count;
            Status = $"Loaded {RowCount} row(s) · {dt.Columns.Count} column(s)";
            if (string.IsNullOrEmpty(TableName) || TableName == "ImportedData")
                TableName = Path.GetFileNameWithoutExtension(FilePath).Replace(" ", "_");
        }
        catch (Exception ex) { Status = ex.Message; }
    }

    [RelayCommand]
    public void GenerateCreateScript()
    {
        if (Preview is null || Connection is null) return;
        _bus.Publish(new OpenQueryEditorRequest
        {
            Connection = Connection,
            Database = Database,
            InitialSql = CsvImporter.GenerateCreateTableScript(Schema, TableName, Preview)
        });
    }

    [RelayCommand]
    public async Task ImportAsync()
    {
        if (Preview is null || Connection is null || string.IsNullOrEmpty(Database))
        { Status = "Pick connection, database, file"; return; }
        if (MessageBox.Show($"Bulk insert {Preview.Rows.Count} row(s) into [{Schema}].[{TableName}]?", "Confirm",
            MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        Status = "Importing...";
        try
        {
            var progress = new Progress<int>(n => Status = $"Inserted {n}...");
            var n = await CsvImporter.BulkInsertAsync(Connection, Database!, Schema, TableName, Preview, progress);
            Status = $"Done — {n} rows inserted";
        }
        catch (Exception ex) { Status = ex.Message; }
    }
}
