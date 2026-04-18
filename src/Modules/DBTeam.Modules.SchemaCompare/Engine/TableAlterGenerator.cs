using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DBTeam.Core.Abstractions;
using DBTeam.Core.Models;

namespace DBTeam.Modules.SchemaCompare.Engine;

public static class TableAlterGenerator
{
    public static async Task<string> BuildAsync(
        IDatabaseMetadataService meta,
        SqlConnectionInfo sConn, string sDb,
        SqlConnectionInfo tConn, string tDb,
        string schema, string table,
        CancellationToken ct = default)
    {
        var sCols = await meta.GetColumnsAsync(sConn, sDb, schema, table, ct);
        var tCols = await meta.GetColumnsAsync(tConn, tDb, schema, table, ct);

        var sMap = sCols.ToDictionary(c => c.Name, System.StringComparer.OrdinalIgnoreCase);
        var tMap = tCols.ToDictionary(c => c.Name, System.StringComparer.OrdinalIgnoreCase);

        var sb = new StringBuilder();
        sb.AppendLine($"-- Column diff for [{schema}].[{table}]");

        // Columns only in source → ADD
        foreach (var col in sCols)
        {
            if (!tMap.ContainsKey(col.Name))
                sb.AppendLine($"ALTER TABLE [{schema}].[{table}] ADD [{col.Name}] {FormatType(col)}{(col.IsNullable ? " NULL" : " NOT NULL")}{DefaultClause(col)};");
        }

        // Columns only in target → DROP
        foreach (var col in tCols)
        {
            if (!sMap.ContainsKey(col.Name))
                sb.AppendLine($"ALTER TABLE [{schema}].[{table}] DROP COLUMN [{col.Name}];");
        }

        // Columns in both but different → ALTER
        foreach (var s in sCols)
        {
            if (!tMap.TryGetValue(s.Name, out var t)) continue;
            if (ColumnEquals(s, t)) continue;
            sb.AppendLine($"ALTER TABLE [{schema}].[{table}] ALTER COLUMN [{s.Name}] {FormatType(s)}{(s.IsNullable ? " NULL" : " NOT NULL")};");
        }

        // Foreign keys diff
        var sFks = await meta.GetForeignKeysAsync(sConn, sDb, schema, table, ct);
        var tFks = await meta.GetForeignKeysAsync(tConn, tDb, schema, table, ct);
        var sFkMap = sFks.ToDictionary(f => f.Name, System.StringComparer.OrdinalIgnoreCase);
        var tFkMap = tFks.ToDictionary(f => f.Name, System.StringComparer.OrdinalIgnoreCase);

        foreach (var fk in tFks)
            if (!sFkMap.ContainsKey(fk.Name))
                sb.AppendLine($"ALTER TABLE [{schema}].[{table}] DROP CONSTRAINT [{fk.Name}];");

        foreach (var fk in sFks)
        {
            if (tFkMap.ContainsKey(fk.Name)) continue;
            var cols = string.Join(",", fk.Columns.Select(c => $"[{c.Column}]"));
            var refCols = string.Join(",", fk.Columns.Select(c => $"[{c.ReferencedColumn}]"));
            sb.AppendLine($"ALTER TABLE [{schema}].[{table}] ADD CONSTRAINT [{fk.Name}] FOREIGN KEY ({cols}) REFERENCES [{fk.ReferencedSchema}].[{fk.ReferencedTable}] ({refCols});");
        }

        // Indexes diff
        var sIdx = await meta.GetIndexesAsync(sConn, sDb, schema, table, ct);
        var tIdx = await meta.GetIndexesAsync(tConn, tDb, schema, table, ct);
        var sIdxMap = sIdx.ToDictionary(i => i.Name, System.StringComparer.OrdinalIgnoreCase);
        var tIdxMap = tIdx.ToDictionary(i => i.Name, System.StringComparer.OrdinalIgnoreCase);

        foreach (var ix in tIdx)
            if (!sIdxMap.ContainsKey(ix.Name) && !ix.IsPrimaryKey)
                sb.AppendLine($"DROP INDEX [{ix.Name}] ON [{schema}].[{table}];");

        foreach (var ix in sIdx)
        {
            if (tIdxMap.ContainsKey(ix.Name) || ix.IsPrimaryKey) continue;
            var cols = string.Join(",", ix.Columns.Select(c => $"[{c}]"));
            var unique = ix.IsUnique ? "UNIQUE " : "";
            var clustered = ix.IsClustered ? "CLUSTERED" : "NONCLUSTERED";
            var included = ix.IncludedColumns.Count > 0 ? $" INCLUDE ({string.Join(",", ix.IncludedColumns.Select(c => $"[{c}]"))})" : "";
            sb.AppendLine($"CREATE {unique}{clustered} INDEX [{ix.Name}] ON [{schema}].[{table}] ({cols}){included};");
        }

        return sb.ToString();
    }

    private static bool ColumnEquals(ColumnInfo a, ColumnInfo b)
        => string.Equals(a.DataType, b.DataType, System.StringComparison.OrdinalIgnoreCase)
        && a.MaxLength == b.MaxLength
        && a.Precision == b.Precision
        && a.Scale == b.Scale
        && a.IsNullable == b.IsNullable
        && a.IsIdentity == b.IsIdentity
        && string.Equals(a.DefaultExpression, b.DefaultExpression, System.StringComparison.OrdinalIgnoreCase);

    private static string FormatType(ColumnInfo c)
    {
        var t = (c.DataType ?? "").ToLowerInvariant();
        return t switch
        {
            "varchar" or "char" or "varbinary" or "binary" =>
                $"{c.DataType}({(c.MaxLength == -1 ? "MAX" : (c.MaxLength?.ToString() ?? "50"))})",
            "nvarchar" or "nchar" =>
                $"{c.DataType}({(c.MaxLength == -1 ? "MAX" : (((c.MaxLength ?? 100) / 2).ToString()))})",
            "decimal" or "numeric" => $"{c.DataType}({c.Precision},{c.Scale})",
            _ => c.DataType ?? "int"
        };
    }

    private static string DefaultClause(ColumnInfo c)
        => string.IsNullOrWhiteSpace(c.DefaultExpression) ? "" : $" DEFAULT {c.DefaultExpression}";
}
