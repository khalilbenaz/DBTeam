using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DBTeam.Core.Abstractions;
using DBTeam.Core.Models;

namespace DBTeam.Modules.Profiler.ViewModels;

public sealed class OperatorStat
{
    public string Name { get; set; } = "";
    public string Node { get; set; } = "";
    public double EstimatedCost { get; set; }
    public double EstimatedRows { get; set; }
    public double ActualRows { get; set; }
    public double LogicalReads { get; set; }
}

public partial class ProfilerViewModel : ObservableObject
{
    private readonly IConnectionService _connSvc;
    private readonly IDatabaseMetadataService _meta;
    private readonly IQueryExecutionService _exec;

    public ProfilerViewModel(IConnectionService connSvc, IDatabaseMetadataService meta, IQueryExecutionService exec)
    {
        _connSvc = connSvc; _meta = meta; _exec = exec;
        Connections = new(); Databases = new(); Operators = new();
        _ = LoadAsync();
    }

    public ObservableCollection<SqlConnectionInfo> Connections { get; }
    public ObservableCollection<string> Databases { get; }
    public ObservableCollection<OperatorStat> Operators { get; }

    [ObservableProperty] private SqlConnectionInfo? connection;
    [ObservableProperty] private string? database;
    [ObservableProperty] private string sql = "SELECT TOP 100 * FROM sys.objects;";
    [ObservableProperty] private string planXml = string.Empty;
    [ObservableProperty] private string status = "Ready";
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private TimeSpan elapsed;
    [ObservableProperty] private int rowCount;

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
    private async Task EstimatedAsync()
    {
        if (Connection is null) { Status = "Pick a connection"; return; }
        IsBusy = true; Status = "Getting estimated plan...";
        try
        {
            PlanXml = await _exec.GetEstimatedPlanXmlAsync(Connection, new QueryRequest { Sql = Sql, Database = Database });
            ParsePlan(PlanXml, actual: false);
            Status = $"Estimated plan · {Operators.Count} operator(s)";
        }
        catch (Exception ex) { Status = ex.Message; }
        IsBusy = false;
    }

    [RelayCommand]
    private async Task ActualAsync()
    {
        if (Connection is null) { Status = "Pick a connection"; return; }
        IsBusy = true; Status = "Running with actual plan...";
        try
        {
            var (result, plan) = await _exec.ExecuteWithActualPlanAsync(Connection, new QueryRequest { Sql = Sql, Database = Database });
            PlanXml = plan;
            Elapsed = result.Elapsed;
            RowCount = result.ResultSets.Sum(r => r.Rows.Count);
            ParsePlan(plan, actual: true);
            Status = $"Actual plan · {Operators.Count} operator(s) · {result.Elapsed.TotalMilliseconds:F0}ms · {RowCount} rows";
        }
        catch (Exception ex) { Status = ex.Message; }
        IsBusy = false;
    }

    private void ParsePlan(string xml, bool actual)
    {
        Operators.Clear();
        if (string.IsNullOrWhiteSpace(xml)) return;
        try
        {
            var doc = XDocument.Parse(xml);
            var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;
            foreach (var rel in doc.Descendants(ns + "RelOp"))
            {
                var op = new OperatorStat
                {
                    Name = rel.Attribute("PhysicalOp")?.Value ?? rel.Attribute("LogicalOp")?.Value ?? "?",
                    Node = rel.Attribute("NodeId")?.Value ?? "",
                    EstimatedCost = ParseDouble(rel.Attribute("EstimatedTotalSubtreeCost")?.Value),
                    EstimatedRows = ParseDouble(rel.Attribute("EstimateRows")?.Value)
                };
                if (actual)
                {
                    var run = rel.Descendants(ns + "RunTimeInformation").Descendants(ns + "RunTimeCountersPerThread").FirstOrDefault();
                    if (run is not null)
                    {
                        op.ActualRows = ParseDouble(run.Attribute("ActualRows")?.Value);
                        op.LogicalReads = ParseDouble(run.Attribute("ActualLogicalReads")?.Value);
                    }
                }
                Operators.Add(op);
            }
        }
        catch { }
    }

    private static double ParseDouble(string? s) => double.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0;
}
