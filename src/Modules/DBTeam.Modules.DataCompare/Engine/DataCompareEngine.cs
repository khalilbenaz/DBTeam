using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DBTeam.Core.Abstractions;
using DBTeam.Core.Models;
using DBTeam.Data.Sql;
using Microsoft.Data.SqlClient;

namespace DBTeam.Modules.DataCompare.Engine;

public enum RowState { OnlyInSource, OnlyInTarget, Different, Identical }

public sealed class RowDiff
{
    public RowState State { get; set; }
    public Dictionary<string, object?> Source { get; set; } = new();
    public Dictionary<string, object?> Target { get; set; } = new();
    public string KeyDisplay { get; set; } = "";
}

public sealed class DataCompareEngine
{
    private readonly IDatabaseMetadataService _meta;
    public DataCompareEngine(IDatabaseMetadataService meta) { _meta = meta; }

    /// <summary>
    /// CHECKSUM mode — no PK required. Produces a per-row hash comparison via BINARY_CHECKSUM(*).
    /// Slower (full scan + hash) but handles tables without primary keys.
    /// </summary>
    public async Task<(int sourceRows, int targetRows, int matching, int onlySource, int onlyTarget)> CompareByChecksumAsync(
        SqlConnectionInfo sConn, string sDb, string schema, string table,
        SqlConnectionInfo tConn, string tDb, CancellationToken ct = default)
    {
        var sSums = await HashesAsync(sConn, sDb, schema, table, ct);
        var tSums = await HashesAsync(tConn, tDb, schema, table, ct);
        var inter = new HashSet<int>(sSums.Keys); inter.IntersectWith(tSums.Keys);
        int matching = inter.Sum(k => Math.Min(sSums[k], tSums[k]));
        int onlySource = sSums.Where(kv => !inter.Contains(kv.Key)).Sum(kv => kv.Value)
                       + inter.Sum(k => Math.Max(0, sSums[k] - tSums[k]));
        int onlyTarget = tSums.Where(kv => !inter.Contains(kv.Key)).Sum(kv => kv.Value)
                       + inter.Sum(k => Math.Max(0, tSums[k] - sSums[k]));
        return (sSums.Values.Sum(), tSums.Values.Sum(), matching, onlySource, onlyTarget);
    }

    private static async Task<Dictionary<int, int>> HashesAsync(SqlConnectionInfo c, string db, string schema, string table, CancellationToken ct)
    {
        var d = new Dictionary<int, int>();
        await using var conn = new SqlConnection(ConnectionStringFactory.Build(c, db));
        await conn.OpenAsync(ct);
        await using var cmd = new SqlCommand($"SELECT BINARY_CHECKSUM(*) AS h, COUNT(*) AS c FROM [{schema}].[{table}] GROUP BY BINARY_CHECKSUM(*)", conn) { CommandTimeout = 180 };
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct)) d[r.GetInt32(0)] = r.GetInt32(1);
        return d;
    }

    public async Task<(IReadOnlyList<RowDiff> Diffs, IReadOnlyList<string> KeyCols, IReadOnlyList<string> AllCols)> CompareAsync(
        SqlConnectionInfo sConn, string sDb, string schema, string table,
        SqlConnectionInfo tConn, string tDb, CancellationToken ct = default)
    {
        var cols = await _meta.GetColumnsAsync(sConn, sDb, schema, table, ct);
        var keyCols = cols.Where(c => c.IsPrimaryKey).Select(c => c.Name).ToList();
        if (keyCols.Count == 0) throw new InvalidOperationException($"Table {schema}.{table} has no primary key; data compare requires one.");
        var allCols = cols.Select(c => c.Name).ToList();

        var src = await LoadAsync(sConn, sDb, schema, table, allCols, ct);
        var tgt = await LoadAsync(tConn, tDb, schema, table, allCols, ct);
        string keyOf(DataRow r) => string.Join("|", keyCols.Select(k => r[k]?.ToString() ?? "NULL"));
        var sMap = src.Rows.Cast<DataRow>().ToDictionary(keyOf);
        var tMap = tgt.Rows.Cast<DataRow>().ToDictionary(keyOf);
        var keys = new HashSet<string>(sMap.Keys); keys.UnionWith(tMap.Keys);

        var diffs = new List<RowDiff>();
        foreach (var k in keys)
        {
            sMap.TryGetValue(k, out var sr);
            tMap.TryGetValue(k, out var tr);
            var d = new RowDiff { KeyDisplay = k };
            if (sr != null) foreach (var c in allCols) d.Source[c] = sr[c] is DBNull ? null : sr[c];
            if (tr != null) foreach (var c in allCols) d.Target[c] = tr[c] is DBNull ? null : tr[c];
            if (sr == null) d.State = RowState.OnlyInTarget;
            else if (tr == null) d.State = RowState.OnlyInSource;
            else d.State = allCols.All(c => Equals(d.Source[c], d.Target[c])) ? RowState.Identical : RowState.Different;
            diffs.Add(d);
        }
        return (diffs, keyCols, allCols);
    }

    private static async Task<DataTable> LoadAsync(SqlConnectionInfo c, string db, string schema, string table, IReadOnlyList<string> cols, CancellationToken ct)
    {
        var colList = string.Join(",", cols.Select(n => $"[{n}]"));
        await using var conn = new SqlConnection(ConnectionStringFactory.Build(c, db));
        await conn.OpenAsync(ct);
        await using var cmd = new SqlCommand($"SELECT {colList} FROM [{schema}].[{table}]", conn) { CommandTimeout = 120 };
        await using var r = await cmd.ExecuteReaderAsync(ct);
        var dt = new DataTable();
        dt.Load(r);
        return dt;
    }

    public static string GenerateSyncScript(string schema, string table, IEnumerable<RowDiff> diffs, IReadOnlyList<string> keyCols, IReadOnlyList<string> allCols)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"-- Data sync for [{schema}].[{table}]");
        sb.AppendLine("SET XACT_ABORT ON;");
        sb.AppendLine("BEGIN TRAN;");
        foreach (var d in diffs)
        {
            switch (d.State)
            {
                case RowState.OnlyInSource:
                    sb.AppendLine($"INSERT INTO [{schema}].[{table}] ({string.Join(",", allCols.Select(c => $"[{c}]"))}) VALUES ({string.Join(",", allCols.Select(c => Lit(d.Source[c])))});");
                    break;
                case RowState.OnlyInTarget:
                    sb.AppendLine($"DELETE FROM [{schema}].[{table}] WHERE {string.Join(" AND ", keyCols.Select(k => $"[{k}]={Lit(d.Target[k])}"))};");
                    break;
                case RowState.Different:
                    var nonKey = allCols.Where(c => !keyCols.Contains(c)).ToList();
                    sb.AppendLine($"UPDATE [{schema}].[{table}] SET {string.Join(",", nonKey.Select(c => $"[{c}]={Lit(d.Source[c])}"))} WHERE {string.Join(" AND ", keyCols.Select(k => $"[{k}]={Lit(d.Source[k])}"))};");
                    break;
            }
        }
        sb.AppendLine("COMMIT;");
        return sb.ToString();
    }

    private static string Lit(object? v) => v switch
    {
        null => "NULL",
        string s => $"N'{s.Replace("'", "''")}'",
        bool b => b ? "1" : "0",
        DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss.fff}'",
        Guid g => $"'{g}'",
        byte[] ba => "0x" + Convert.ToHexString(ba),
        _ => Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture) ?? "NULL"
    };
}
