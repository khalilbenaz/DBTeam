using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DBTeam.Core.Models;
using DBTeam.Data.Sql;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DBTeam.Modules.Debugger.Engine;

public sealed class TSqlStatementInfo
{
    public int Index { get; set; }
    public int StartOffset { get; set; }
    public int Length { get; set; }
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public string Preview { get; set; } = "";
    public string Sql { get; set; } = "";
    public string Kind { get; set; } = "";
}

public sealed class StepResult
{
    public TSqlStatementInfo Statement { get; set; } = new();
    public DataTable? ResultSet { get; set; }
    public List<string> Messages { get; } = new();
    public int RowsAffected { get; set; }
    public Exception? Error { get; set; }
    public TimeSpan Elapsed { get; set; }
}

/// <summary>
/// Splits a T-SQL script into individual statements via ScriptDom and executes them
/// one-by-one on a persistent connection so DECLARE / SET / transaction state survives between steps.
/// </summary>
public sealed class TSqlStepExecutor : IDisposable
{
    private readonly SqlConnectionInfo _conn;
    private readonly string? _database;
    private SqlConnection? _connection;
    private readonly List<TSqlStatementInfo> _statements = new();

    public IReadOnlyList<TSqlStatementInfo> Statements => _statements;

    public TSqlStepExecutor(SqlConnectionInfo connection, string? database)
    {
        _conn = connection; _database = database;
    }

    public IList<ParseError> Parse(string sql)
    {
        _statements.Clear();
        if (string.IsNullOrWhiteSpace(sql)) return new List<ParseError>();

        var parser = new TSql160Parser(initialQuotedIdentifiers: true);
        using var reader = new StringReader(sql);
        var fragment = parser.Parse(reader, out var errors);
        if (fragment is not TSqlScript script) return errors;

        int idx = 0;
        foreach (var batch in script.Batches)
        {
            foreach (var statement in batch.Statements)
            {
                var stmtText = GetFragmentText(statement);
                _statements.Add(new TSqlStatementInfo
                {
                    Index = idx++,
                    StartOffset = statement.StartOffset,
                    Length = statement.FragmentLength,
                    StartLine = statement.StartLine,
                    EndLine = statement.StartLine + CountLines(stmtText) - 1,
                    Preview = Truncate(FirstLine(stmtText), 80),
                    Sql = stmtText,
                    Kind = statement.GetType().Name.Replace("Statement", "")
                });
            }
        }
        return errors;
    }

    public async Task OpenAsync(CancellationToken ct = default)
    {
        _connection?.Dispose();
        _connection = new SqlConnection(ConnectionStringFactory.Build(_conn, _database));
        await _connection.OpenAsync(ct);
    }

    public async Task<StepResult> ExecuteAsync(TSqlStatementInfo stmt, CancellationToken ct = default)
    {
        if (_connection is null) await OpenAsync(ct);
        var result = new StepResult { Statement = stmt };
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            void InfoMessage(object? _, SqlInfoMessageEventArgs e)
            { foreach (SqlError err in e.Errors) result.Messages.Add(err.Message); }
            _connection!.InfoMessage += InfoMessage;
            try
            {
                await using var cmd = new SqlCommand(stmt.Sql, _connection) { CommandTimeout = 60 };
                if (LooksLikeSelect(stmt.Sql))
                {
                    await using var r = await cmd.ExecuteReaderAsync(ct);
                    if (r.FieldCount > 0)
                    {
                        var dt = new DataTable();
                        dt.Load(r);
                        result.ResultSet = dt;
                    }
                }
                else
                {
                    result.RowsAffected = await cmd.ExecuteNonQueryAsync(ct);
                }
            }
            finally { _connection.InfoMessage -= InfoMessage; }
        }
        catch (Exception ex) { result.Error = ex; result.Messages.Add(ex.Message); }
        finally { sw.Stop(); result.Elapsed = sw.Elapsed; }
        return result;
    }

    /// <summary>
    /// Evaluates a boolean T-SQL expression on the live debug session so it can
    /// reference DECLARE'd variables and @@STATE. Returns true on any non-zero
    /// result, false on 0/NULL/error.
    /// </summary>
    public async Task<(bool value, string? error)> EvaluateConditionAsync(string expression, CancellationToken ct = default)
    {
        if (_connection is null) return (false, "not attached");
        if (string.IsNullOrWhiteSpace(expression)) return (true, null);
        try
        {
            await using var cmd = new SqlCommand(
                $"SELECT CASE WHEN ({expression}) THEN 1 ELSE 0 END", _connection) { CommandTimeout = 10 };
            var r = await cmd.ExecuteScalarAsync(ct);
            return (r is int i && i == 1, null);
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<DataTable?> GetSessionVariablesAsync(CancellationToken ct = default)
    {
        if (_connection is null) return null;
        try
        {
            await using var cmd = new SqlCommand(
                "SELECT @@ROWCOUNT AS RowCount, @@ERROR AS LastError, @@TRANCOUNT AS TranCount, CAST(@@SPID AS INT) AS Spid, DB_NAME() AS CurrentDb, ORIGINAL_LOGIN() AS Login",
                _connection);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            var dt = new DataTable(); dt.Load(r);
            return dt;
        }
        catch { return null; }
    }

    public void Dispose()
    {
        try { _connection?.Dispose(); } catch { }
    }

    private static string GetFragmentText(TSqlFragment f)
    {
        if (f.ScriptTokenStream is null) return "";
        var sb = new System.Text.StringBuilder();
        for (int i = f.FirstTokenIndex; i <= f.LastTokenIndex && i < f.ScriptTokenStream.Count; i++)
            sb.Append(f.ScriptTokenStream[i].Text);
        return sb.ToString();
    }

    private static string FirstLine(string s)
    {
        var i = s.IndexOfAny(new[] { '\r', '\n' });
        return i < 0 ? s.Trim() : s[..i].Trim();
    }

    private static int CountLines(string s)
    {
        if (string.IsNullOrEmpty(s)) return 1;
        return s.Count(c => c == '\n') + 1;
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "…";

    private static bool LooksLikeSelect(string sql)
    {
        var t = sql.TrimStart();
        return t.StartsWith("select", StringComparison.OrdinalIgnoreCase)
            || t.StartsWith("with", StringComparison.OrdinalIgnoreCase)
            || t.StartsWith("exec", StringComparison.OrdinalIgnoreCase)
            || t.StartsWith("execute", StringComparison.OrdinalIgnoreCase);
    }
}
