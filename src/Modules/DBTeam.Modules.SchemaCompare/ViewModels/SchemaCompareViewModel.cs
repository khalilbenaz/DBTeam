using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DBTeam.Core.Abstractions;
using DBTeam.Core.Events;
using DBTeam.Core.Models;
using DBTeam.Modules.SchemaCompare.Engine;

namespace DBTeam.Modules.SchemaCompare.ViewModels;

public partial class SchemaCompareViewModel : ObservableObject
{
    private readonly IConnectionService _connSvc;
    private readonly IDatabaseMetadataService _meta;
    private readonly SchemaCompareEngine _engine;
    private readonly IEventBus _bus;

    public SchemaCompareViewModel(IConnectionService connSvc, IDatabaseMetadataService meta, IEventBus bus)
    {
        _connSvc = connSvc; _meta = meta; _bus = bus;
        _engine = new SchemaCompareEngine(meta);
        Connections = new ObservableCollection<SqlConnectionInfo>();
        SourceDatabases = new ObservableCollection<string>();
        TargetDatabases = new ObservableCollection<string>();
        Items = new ObservableCollection<SchemaDiffItem>();
        _ = LoadConnectionsAsync();
    }

    public ObservableCollection<SqlConnectionInfo> Connections { get; }
    public ObservableCollection<string> SourceDatabases { get; }
    public ObservableCollection<string> TargetDatabases { get; }
    public ObservableCollection<SchemaDiffItem> Items { get; }

    [ObservableProperty] private SqlConnectionInfo? sourceConnection;
    [ObservableProperty] private SqlConnectionInfo? targetConnection;
    [ObservableProperty] private string? sourceDatabase;
    [ObservableProperty] private string? targetDatabase;
    [ObservableProperty] private string status = "Ready";
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private SchemaDiffItem? selected;

    private async Task LoadConnectionsAsync()
    {
        var all = await _connSvc.LoadAllAsync();
        Connections.Clear();
        foreach (var c in all) Connections.Add(c);
    }

    partial void OnSourceConnectionChanged(SqlConnectionInfo? value) => _ = LoadDbsAsync(value, SourceDatabases);
    partial void OnTargetConnectionChanged(SqlConnectionInfo? value) => _ = LoadDbsAsync(value, TargetDatabases);

    private async Task LoadDbsAsync(SqlConnectionInfo? c, ObservableCollection<string> target)
    {
        target.Clear();
        if (c is null) return;
        try { foreach (var d in await _meta.GetDatabasesAsync(c)) target.Add(d); } catch { }
    }

    [RelayCommand]
    private async Task CompareAsync()
    {
        if (SourceConnection is null || TargetConnection is null || string.IsNullOrEmpty(SourceDatabase) || string.IsNullOrEmpty(TargetDatabase))
        { Status = "Pick source and target connection + database"; return; }
        IsBusy = true; Status = "Comparing...";
        try
        {
            var diffs = await _engine.CompareAsync(SourceConnection, SourceDatabase!, TargetConnection, TargetDatabase!);
            Items.Clear();
            foreach (var d in diffs) Items.Add(d);
            var diffCount = Items.Count(i => i.State != DiffState.Identical);
            Status = $"{Items.Count} objects · {diffCount} differences";
        }
        catch (System.Exception ex) { Status = ex.Message; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task GenerateScriptAsync()
    {
        if (Items.Count == 0 || TargetConnection is null || SourceConnection is null
            || string.IsNullOrEmpty(SourceDatabase) || string.IsNullOrEmpty(TargetDatabase)) return;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("-- Schema sync script (Source -> Target)");
        sb.AppendLine("SET XACT_ABORT ON;");
        sb.AppendLine("BEGIN TRAN;");
        foreach (var item in Items.Where(i => i.State != DiffState.Identical))
        {
            sb.AppendLine($"-- {item.KindLabel}: {item.QualifiedName}  [{item.State}]");
            switch (item.State)
            {
                case DiffState.OnlyInSource:
                    if (!string.IsNullOrWhiteSpace(item.SourceScript))
                    { sb.AppendLine(item.SourceScript); sb.AppendLine("GO"); }
                    break;
                case DiffState.OnlyInTarget:
                    sb.AppendLine($"DROP {item.KindLabel.ToUpper()} [{item.Schema}].[{item.Name}];");
                    sb.AppendLine("GO");
                    break;
                case DiffState.Different when item.Kind == DBTeam.Core.Models.DbObjectKind.Table:
                    sb.AppendLine(await TableAlterGenerator.BuildAsync(
                        _meta, SourceConnection!, SourceDatabase!, TargetConnection!, TargetDatabase!, item.Schema, item.Name));
                    break;
                case DiffState.Different:
                    sb.AppendLine($"DROP {item.KindLabel.ToUpper()} [{item.Schema}].[{item.Name}];");
                    sb.AppendLine("GO");
                    if (!string.IsNullOrWhiteSpace(item.SourceScript))
                    { sb.AppendLine(item.SourceScript); sb.AppendLine("GO"); }
                    break;
            }
            sb.AppendLine();
        }
        sb.AppendLine("COMMIT;");
        _bus.Publish(new OpenQueryEditorRequest { Connection = TargetConnection, Database = TargetDatabase, InitialSql = sb.ToString() });
        Status = "Script generated in new tab";
    }
}
