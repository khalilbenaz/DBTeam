using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DBTeam.Core.Models;
using DBTeam.Data.Sql;
using Microsoft.Data.SqlClient;

namespace DBTeam.Modules.Import.Engine;

public sealed class CsvImporter
{
    public static DataTable ReadCsv(Stream s, bool hasHeader, char delimiter = ',')
    {
        var dt = new DataTable();
        using var reader = new StreamReader(s, Encoding.UTF8, true);
        string? headerLine = reader.ReadLine();
        if (headerLine is null) return dt;
        var headers = ParseLine(headerLine, delimiter);
        if (hasHeader)
            foreach (var h in headers) dt.Columns.Add(SafeColumnName(h, dt.Columns));
        else
        {
            for (int i = 0; i < headers.Length; i++) dt.Columns.Add($"Col{i + 1}");
            dt.Rows.Add(headers.Cast<object>().ToArray());
        }
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = ParseLine(line, delimiter);
            var row = dt.NewRow();
            for (int i = 0; i < Math.Min(parts.Length, dt.Columns.Count); i++) row[i] = parts[i];
            dt.Rows.Add(row);
        }
        // Infer types
        for (int c = 0; c < dt.Columns.Count; c++) InferColumnType(dt, c);
        return dt;
    }

    private static string SafeColumnName(string n, DataColumnCollection existing)
    {
        var name = string.IsNullOrWhiteSpace(n) ? "Col" : n.Trim();
        var unique = name; int i = 1;
        while (existing.Contains(unique)) unique = $"{name}_{i++}";
        return unique;
    }

    public static async Task<int> BulkInsertAsync(SqlConnectionInfo c, string db, string schema, string table,
        DataTable data, IProgress<int>? progress = null, CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(ConnectionStringFactory.Build(c, db));
        await conn.OpenAsync(ct);
        using var bulk = new SqlBulkCopy(conn) { DestinationTableName = $"[{schema}].[{table}]", BatchSize = 500, BulkCopyTimeout = 300 };
        foreach (DataColumn col in data.Columns) bulk.ColumnMappings.Add(col.ColumnName, col.ColumnName);
        bulk.NotifyAfter = 500;
        bulk.SqlRowsCopied += (_, e) => progress?.Report((int)e.RowsCopied);
        await bulk.WriteToServerAsync(data, ct);
        return data.Rows.Count;
    }

    public static string GenerateCreateTableScript(string schema, string table, DataTable data)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"CREATE TABLE [{schema}].[{table}] (");
        for (int i = 0; i < data.Columns.Count; i++)
        {
            var col = data.Columns[i];
            sb.Append($"    [{col.ColumnName}] {GetSqlType(col.DataType)} NULL");
            if (i < data.Columns.Count - 1) sb.Append(',');
            sb.AppendLine();
        }
        sb.AppendLine(");");
        return sb.ToString();
    }

    private static string GetSqlType(Type t) => t switch
    {
        _ when t == typeof(int) => "INT",
        _ when t == typeof(long) => "BIGINT",
        _ when t == typeof(decimal) => "DECIMAL(18,6)",
        _ when t == typeof(double) => "FLOAT",
        _ when t == typeof(float) => "FLOAT",
        _ when t == typeof(bool) => "BIT",
        _ when t == typeof(DateTime) => "DATETIME2(3)",
        _ when t == typeof(Guid) => "UNIQUEIDENTIFIER",
        _ => "NVARCHAR(400)"
    };

    private static void InferColumnType(DataTable dt, int c)
    {
        var column = dt.Columns[c];
        bool allInt = true, allDecimal = true, allBool = true, allDate = true;
        foreach (DataRow r in dt.Rows)
        {
            var v = (r[c] as string)?.Trim();
            if (string.IsNullOrEmpty(v)) continue;
            if (allInt && !long.TryParse(v, out _)) allInt = false;
            if (allDecimal && !decimal.TryParse(v, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out _)) allDecimal = false;
            if (allBool && !(v.Equals("true", StringComparison.OrdinalIgnoreCase) || v.Equals("false", StringComparison.OrdinalIgnoreCase) || v == "0" || v == "1")) allBool = false;
            if (allDate && !DateTime.TryParse(v, out _)) allDate = false;
        }
        if (dt.Rows.Count == 0) return;
        var newType = allInt ? typeof(long) : allDecimal ? typeof(decimal) : allBool ? typeof(bool) : allDate ? typeof(DateTime) : typeof(string);
        if (newType == typeof(string)) return;
        var newCol = new DataColumn(column.ColumnName, newType);
        int idx = column.Ordinal;
        var list = new List<object?>();
        foreach (DataRow r in dt.Rows) list.Add(r[c]);
        dt.Columns.Remove(column);
        dt.Columns.Add(newCol);
        newCol.SetOrdinal(idx);
        for (int i = 0; i < list.Count; i++)
        {
            var s = list[i] as string;
            if (string.IsNullOrEmpty(s)) { dt.Rows[i][idx] = DBNull.Value; continue; }
            try { dt.Rows[i][idx] = newType switch
            {
                _ when newType == typeof(long) => long.Parse(s, System.Globalization.CultureInfo.InvariantCulture),
                _ when newType == typeof(decimal) => decimal.Parse(s, System.Globalization.CultureInfo.InvariantCulture),
                _ when newType == typeof(bool) => bool.TryParse(s, out var b) ? b : s == "1",
                _ when newType == typeof(DateTime) => DateTime.Parse(s),
                _ => (object)s
            };}
            catch { dt.Rows[i][idx] = DBNull.Value; }
        }
    }

    private static string[] ParseLine(string line, char delimiter)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        bool inQuote = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (inQuote)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                    else inQuote = false;
                }
                else sb.Append(c);
            }
            else
            {
                if (c == '"') inQuote = true;
                else if (c == delimiter) { result.Add(sb.ToString()); sb.Clear(); }
                else sb.Append(c);
            }
        }
        result.Add(sb.ToString());
        return result.ToArray();
    }
}
