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
    public ObservableCollection<Breakpoint> Breakpoints { get; } = new();
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
                if (NextIndex < Steps.Count)
                {
                    var nextLine = Steps[NextIndex].StartLine;
                    var bp = Breakpoints.FirstOrDefault(b => b.Line == nextLine);
                    if (bp is not null)
                    {
                        bool hit = true; string? evalErr = null;
                        if (bp.IsConditional)
                        {
                            var (val, err) = await _executor.EvaluateConditionAsync(bp.Condition!, ct);
                            hit = val; evalErr = err;
                        }
                        if (hit)
                        {
                            bp.HitCount++;
                            CurrentStep = Steps[NextIndex];
                            Status = bp.IsConditional
                                ? $"Conditional breakpoint hit at line {nextLine} (hits: {bp.HitCount})"
                                : $"Breakpoint hit at line {nextLine}";
                            if (evalErr is not null) Messages += Environment.NewLine + "[bp eval error] " + evalErr;
                            break;
                        }
                        else if (evalErr is not null)
                            Messages += Environment.NewLine + $"[bp line {nextLine}] condition error: {evalErr}";
                    }
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

    /// <summary>
    /// Step into: when the current step is <c>EXEC [schema].[proc] @p1 = …, @p2 = …</c>,
    /// fetch the proc body, parse it into its own statements and splice them
    /// into the Steps list right after the EXEC. The user can then step-over
    /// each inner statement. A parameter binding comment header is injected so
    /// the user sees which arguments were passed.
    /// </summary>
    [RelayCommand]
    public async Task StepIntoAsync()
    {
        if (_executor is null || CurrentStep is null) { Status = "Attach and select an EXEC statement first"; return; }
        var stmt = CurrentStep;
        var sql = stmt.Sql.Trim();
        if (!sql.StartsWith("EXEC", System.StringComparison.OrdinalIgnoreCase)
            && !sql.StartsWith("EXECUTE", System.StringComparison.OrdinalIgnoreCase))
        { Status = "Step into works only on EXEC statements"; return; }

        var afterExec = System.Text.RegularExpressions.Regex.Replace(sql, @"^\s*EXEC(UTE)?\s+", "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase).TrimEnd(';');
        var spaceIdx = afterExec.IndexOf(' ');
        string target = spaceIdx < 0 ? afterExec : afterExec[..spaceIdx].Trim();
        string args = spaceIdx < 0 ? "" : afterExec[spaceIdx..].Trim();
        var parts = target.Split('.');
        string schema = parts.Length >= 2 ? parts[^2].Trim('[', ']', ' ') : "dbo";
        string procName = parts[^1].Trim('[', ']', ' ');

        string body;
        try { body = await _meta.ScriptObjectAsync(Connection!, Database ?? "", schema, procName, DbObjectKind.StoredProcedure); }
        catch (System.Exception ex) { Status = "Cannot fetch proc body: " + ex.Message; return; }
        if (string.IsNullOrWhiteSpace(body)) { Status = $"[{schema}].[{procName}] has no readable body"; return; }

        var asMatch = System.Text.RegularExpressions.Regex.Match(body, @"\bAS\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var innerBody = asMatch.Success ? body[(asMatch.Index + 2)..].TrimStart('\r', '\n', ' ') : body;

        var injected = new System.Text.StringBuilder();
        injected.AppendLine($"-- [step-into] {schema}.{procName}");
        if (!string.IsNullOrEmpty(args))
        {
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(
                args, @"(?<name>@\w+)\s*=\s*(?<val>N?'[^']*'|[\d\.\-]+|NULL)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                injected.AppendLine($"-- bound {m.Groups["name"].Value} = {m.Groups["val"].Value}");
        }
        injected.AppendLine(innerBody);

        var sub = new Engine.TSqlStepExecutor(Connection!, Database);
        var errors = sub.Parse(injected.ToString());
        if (errors.Count > 0) { Status = $"Parse errors in proc body: {errors.Count}"; return; }

        int insertAt = NextIndex;
        foreach (var sbstmt in sub.Statements)
        {
            var copy = new Engine.TSqlStatementInfo
            {
                Sql = sbstmt.Sql,
                Kind = "↘ " + sbstmt.Kind,
                StartLine = sbstmt.StartLine,
                EndLine = sbstmt.EndLine,
                Preview = sbstmt.Preview
            };
            Steps.Insert(insertAt++, copy);
        }
        for (int i = 0; i < Steps.Count; i++) Steps[i].Index = i;
        Status = $"Stepped into [{schema}].[{procName}] — {sub.Statements.Count} inner statement(s) inserted";
    }

    [RelayCommand]
    public void ToggleBreakpoint(TSqlStatementInfo? s)
    {
        if (s is null) return;
        var existing = Breakpoints.FirstOrDefault(b => b.Line == s.StartLine);
        if (existing is not null) Breakpoints.Remove(existing);
        else Breakpoints.Add(new Breakpoint { Line = s.StartLine });
    }

    [RelayCommand]
    public void SetBreakpointCondition(TSqlStatementInfo? s)
    {
        if (s is null) return;
        var bp = Breakpoints.FirstOrDefault(b => b.Line == s.StartLine);
        if (bp is null) { bp = new Breakpoint { Line = s.StartLine }; Breakpoints.Add(bp); }
        var dlg = new Views.BreakpointConditionWindow(bp.Line, bp.Condition) { Owner = Application.Current?.MainWindow };
        if (dlg.ShowDialog() == true)
        {
            bp.Condition = string.IsNullOrWhiteSpace(dlg.Condition) ? null : dlg.Condition;
            Status = bp.IsConditional
                ? $"Condition set on line {bp.Line}"
                : $"Condition cleared on line {bp.Line}";
        }
    }

    [RelayCommand]
    public void ClearBreakpointCondition(TSqlStatementInfo? s)
    {
        if (s is null) return;
        var bp = Breakpoints.FirstOrDefault(b => b.Line == s.StartLine);
        if (bp is null) return;
        bp.Condition = null;
        Status = $"Condition cleared on line {bp.Line}";
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
