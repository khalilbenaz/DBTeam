using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bogus;
using DBTeam.Core.Abstractions;
using DBTeam.Core.Models;
using DBTeam.Data.Sql;
using Microsoft.Data.SqlClient;

namespace DBTeam.Modules.DataGenerator.Engine;

public sealed class GenerationOptions
{
    public int RowCount { get; set; } = 100;
    public int BatchSize { get; set; } = 500;
    public bool SkipIdentity { get; set; } = true;
}

public sealed class DataGeneratorEngine
{
    private readonly IDatabaseMetadataService _meta;
    public DataGeneratorEngine(IDatabaseMetadataService meta) { _meta = meta; }

    public async Task<List<Dictionary<string, object?>>> PreviewAsync(SqlConnectionInfo c, string db, string schema, string table, GenerationOptions opts, CancellationToken ct = default)
    {
        var cols = await _meta.GetColumnsAsync(c, db, schema, table, ct);
        var faker = new Faker();
        var rows = new List<Dictionary<string, object?>>();
        for (int i = 0; i < Math.Min(opts.RowCount, 50); i++)
        {
            var row = new Dictionary<string, object?>();
            foreach (var col in cols)
            {
                if (opts.SkipIdentity && col.IsIdentity) continue;
                row[col.Name] = GenerateValue(col, faker);
            }
            rows.Add(row);
        }
        return rows;
    }

    public async Task<int> InsertAsync(SqlConnectionInfo c, string db, string schema, string table, GenerationOptions opts, IProgress<int>? progress, CancellationToken ct = default)
    {
        var cols = (await _meta.GetColumnsAsync(c, db, schema, table, ct))
            .Where(col => !(opts.SkipIdentity && col.IsIdentity))
            .ToList();
        var faker = new Faker();
        var colList = string.Join(",", cols.Select(x => $"[{x.Name}]"));
        var paramList = string.Join(",", cols.Select(x => $"@{x.Name}"));
        var sql = $"INSERT INTO [{schema}].[{table}] ({colList}) VALUES ({paramList})";

        await using var conn = new SqlConnection(ConnectionStringFactory.Build(c, db));
        await conn.OpenAsync(ct);
        int inserted = 0;
        for (int i = 0; i < opts.RowCount; i++)
        {
            await using var cmd = new SqlCommand(sql, conn);
            foreach (var col in cols) cmd.Parameters.AddWithValue($"@{col.Name}", GenerateValue(col, faker) ?? DBNull.Value);
            inserted += await cmd.ExecuteNonQueryAsync(ct);
            if ((i + 1) % 50 == 0) progress?.Report(i + 1);
        }
        progress?.Report(opts.RowCount);
        return inserted;
    }

    public static object? GenerateValue(ColumnInfo col, Faker f)
    {
        if (col.IsNullable && f.Random.Double() < 0.05) return null;
        var t = col.DataType.ToLowerInvariant();
        var name = col.Name.ToLowerInvariant();
        return t switch
        {
            "bit" => f.Random.Bool(),
            "tinyint" => (byte)f.Random.Int(0, 255),
            "smallint" => (short)f.Random.Int(-32768, 32767),
            "int" => f.Random.Int(1, 1_000_000),
            "bigint" => f.Random.Long(1, long.MaxValue / 1000),
            "decimal" or "numeric" or "money" => (decimal)f.Random.Double(0, 10000),
            "float" or "real" => f.Random.Double(0, 10000),
            "date" => f.Date.Past(10).Date,
            "datetime" or "datetime2" or "datetimeoffset" or "smalldatetime" => f.Date.Past(5),
            "time" => f.Date.Past(1).TimeOfDay,
            "uniqueidentifier" => Guid.NewGuid(),
            "char" or "nchar" or "varchar" or "nvarchar" or "text" or "ntext" => GuessString(name, col, f),
            "varbinary" or "binary" => f.Random.Bytes(16),
            _ => GuessString(name, col, f)
        };
    }

    private static string GuessString(string name, ColumnInfo col, Faker f)
    {
        int max = col.MaxLength is null or -1 ? 200 : (col.MaxLength > 0 ? Math.Min(col.MaxLength.Value, 400) : 100);
        string s = name switch
        {
            var n when n.Contains("email") => f.Internet.Email(),
            var n when n.Contains("phone") || n.Contains("tel") => f.Phone.PhoneNumber(),
            var n when n.Contains("firstname") || n.Contains("first_name") => f.Name.FirstName(),
            var n when n.Contains("lastname") || n.Contains("last_name") => f.Name.LastName(),
            var n when n.Contains("fullname") || n == "name" => f.Name.FullName(),
            var n when n.Contains("city") => f.Address.City(),
            var n when n.Contains("country") => f.Address.Country(),
            var n when n.Contains("street") || n.Contains("address") => f.Address.StreetAddress(),
            var n when n.Contains("zip") || n.Contains("postal") => f.Address.ZipCode(),
            var n when n.Contains("company") => f.Company.CompanyName(),
            var n when n.Contains("url") || n.Contains("website") => f.Internet.Url(),
            var n when n.Contains("ip") => f.Internet.Ip(),
            var n when n.Contains("user") => f.Internet.UserName(),
            var n when n.Contains("password") => f.Internet.Password(12),
            var n when n.Contains("description") || n.Contains("comment") || n.Contains("notes") => f.Lorem.Sentence(8),
            _ => f.Lorem.Word()
        };
        if (s.Length > max) s = s[..max];
        return s;
    }

    public static string RowsToSqlScript(string schema, string table, List<Dictionary<string, object?>> rows)
    {
        if (rows.Count == 0) return "";
        var cols = rows[0].Keys.ToList();
        var sb = new StringBuilder();
        sb.AppendLine($"-- {rows.Count} row(s) for [{schema}].[{table}]");
        foreach (var r in rows)
        {
            sb.Append($"INSERT INTO [{schema}].[{table}] ({string.Join(",", cols.Select(x => $"[{x}]"))}) VALUES (");
            sb.Append(string.Join(",", cols.Select(c => Lit(r[c]))));
            sb.AppendLine(");");
        }
        return sb.ToString();
    }

    private static string Lit(object? v) => v switch
    {
        null => "NULL",
        string s => $"N'{s.Replace("'", "''")}'",
        bool b => b ? "1" : "0",
        DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss.fff}'",
        TimeSpan ts => $"'{ts:hh\\:mm\\:ss}'",
        Guid g => $"'{g}'",
        byte[] ba => "0x" + Convert.ToHexString(ba),
        _ => Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture) ?? "NULL"
    };
}
