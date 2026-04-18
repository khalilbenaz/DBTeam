using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DBTeam.Core.Abstractions;
using DBTeam.Core.Models;
using DBTeam.Modules.Debugger.Engine;

namespace DBTeam.Modules.Debugger.ViewModels;

public partial class DebuggerViewModel : ObservableObject, IDisposable
{
    private readonly IConnectionService _connSvc;
    private readonly IDatabaseMetadataService _meta;
    private TSqlStepExecutor? _executor;
    private CancellationTokenSource? _cts;

    public DebuggerViewModel(IConnectionService connSvc, IDatabaseMetadataService meta)
    {
        _connSvc = connSvc; _meta = meta;
        Connections = new ObservableCollection<SqlConnectionInfo>();
        Databases = new ObservableCollection<string>();
        _ = LoadConnectionsAsync();
    }

    public ObservableCollection<SqlConnectionInfo> Connections { get; }
    public ObservableCollection<string> Databases { get; }
    public ObservableCollection<TSqlStatementInfo> Steps { get; } = new();
    public ObservableCollection<int> Breakpoints { get; } = new();
    public ObservableCollection<DataTable> Results { get; } = new();

    [ObservableProperty] private SqlConnectionInfo? connection;
    [ObservableProperty] private string? database;
    [ObservableProperty] private string sql =
        "-- Set breakpoints by clicking a statement in the Steps list\n" +
        "DECLARE @greeting nvarchar(50) = 'Hello from DB TEAM Debugger';\n" +
        "SELECT @greeting AS Message;\n" +
        "SELECT name, create_date FROM sys.databases ORDER BY database_id;";
    [ObservableProperty] private TSqlStatementInfo? currentStep;
    [ObservableProperty] private string status = "Idle";
    [ObservableProperty] private string messages = "";
    [ObservableProperty] private DataTable? sessionInfo;
    [ObservableProperty] private bool isRunning;
    [ObservableProperty] private bool isAttached;
    [ObservableProperty] private int nextIndex;

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
    public async Task AttachAsync()
    {
        if (Connection is null) { Status = "Pick a connection"; return; }
        Detach();
        _executor = new TSqlStepExecutor(Connection, Database);
        var errors = _executor.Parse(Sql);
        Steps.Clear();
        foreach (var s in _executor.Statements) Steps.Add(s);
        if (errors.Count > 0)
        {
            Messages = string.Join(Environment.NewLine, errors.Select(e => $"[parse] Line {e.Line}:{e.Column} — {e.Message}"));
            Status = $"Parse errors: {errors.Count}";
            return;
        }
        await _executor.OpenAsync();
        IsAttached = true;
        NextIndex = 0;
        CurrentStep = Steps.FirstOrDefault();
        Results.Clear(); Messages = "";
        Status = $"Attached · {Steps.Count} step(s) parsed";
        SessionInfo = await _executor.GetSessionVariablesAsync();
    }

    [RelayCommand]
    public void Detach()
    {
        _cts?.Cancel(); _cts?.Dispose(); _cts = null;
        _executor?.Dispose(); _executor = null;
        IsAttached = false; IsRunning = false;
        CurrentStep = null;
        Status = "Detached";
    }

    [RelayCommand]
    public async Task StepOverAsync()
    {
        if (_executor is null || NextIndex >= Steps.Count) return;
        IsRunning = true;
        try
        {
            var step = Steps[NextIndex];
            CurrentStep = step;
            Status = $"Executing step {step.Index + 1}/{Steps.Count}: {step.Kind}";
            var r = await _executor.ExecuteAsync(step);
            AppendResult(r);
            NextIndex++;
            if (NextIndex >= Steps.Count)
            {
                Status = "Finished — detach or re-attach to re-run";
                IsRunning = false;
                return;
            }
            CurrentStep = Steps[NextIndex];
            Status = $"Paused before step {NextIndex + 1}/{Steps.Count} ({CurrentStep.Kind})";
            SessionInfo = await _executor.GetSessionVariablesAsync();
        }
        finally { IsRunning = false; }
    }

    [RelayCommand]
    public async Task ContinueAsync()
    {
        if (_executor is null) return;
        IsRunning = true;
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;
        try
        {
            while (NextIndex < Steps.Count && !ct.IsCancellationRequested)
            {
                var step = Steps[NextIndex];
                CurrentStep = step;
                Status = $"Executing step {step.Index + 1}/{Steps.Count}";
                var r = await _executor.ExecuteAsync(step, ct);
                AppendResult(r);
                if (r.Error is not null) { Status = "Paused on error"; break; }
                NextIndex++;
                // Breakpoint check: pause BEFORE the next step if its start line ∈ breakpoints
                if (NextIndex < Steps.Count && Breakpoints.Contains(Steps[NextIndex].StartLine))
                {
                    CurrentStep = Steps[NextIndex];
                    Status = $"Breakpoint hit at line {CurrentStep.StartLine}";
                    break;
                }
            }
            if (NextIndex >= Steps.Count) Status = "Finished";
            SessionInfo = await _executor.GetSessionVariablesAsync();
        }
        finally { IsRunning = false; }
    }

    [RelayCommand]
    public void Stop()
    {
        _cts?.Cancel();
        Status = "Stopped";
    }

    [RelayCommand]
    public void ToggleBreakpoint(TSqlStatementInfo? s)
    {
        if (s is null) return;
        if (Breakpoints.Contains(s.StartLine)) Breakpoints.Remove(s.StartLine);
        else Breakpoints.Add(s.StartLine);
    }

    [RelayCommand]
    public async Task RestartAsync() => await AttachAsync();

    private void AppendResult(StepResult r)
    {
        if (r.ResultSet is not null) Results.Add(r.ResultSet);
        var lines = new System.Collections.Generic.List<string>
        {
            $"[step {r.Statement.Index + 1}] {r.Statement.Kind} · {r.Elapsed.TotalMilliseconds:F0} ms · {r.RowsAffected} row(s)"
        };
        lines.AddRange(r.Messages);
        if (r.Error is not null) lines.Add("ERROR: " + r.Error.Message);
        Messages = (string.IsNullOrEmpty(Messages) ? "" : Messages + Environment.NewLine) + string.Join(Environment.NewLine, lines);
    }

    public void Dispose() => Detach();
}
