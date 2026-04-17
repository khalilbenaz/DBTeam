using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using DBTeam.Core.Abstractions;
using DBTeam.Core.Models;
using Microsoft.Data.SqlClient;

namespace DBTeam.Data.Sql;

public sealed class SqlServerMetadataService : IDatabaseMetadataService
{
    private static async Task<SqlConnection> OpenAsync(SqlConnectionInfo c, string? db, CancellationToken ct)
    {
        var conn = new SqlConnection(ConnectionStringFactory.Build(c, db));
        await conn.OpenAsync(ct);
        return conn;
    }

    public async Task<IReadOnlyList<string>> GetDatabasesAsync(SqlConnectionInfo c, CancellationToken ct = default)
    {
        await using var conn = await OpenAsync(c, "master", ct);
        await using var cmd = new SqlCommand("SELECT name FROM sys.databases WHERE database_id > 4 OR name IN ('master','msdb','model','tempdb') ORDER BY name", conn);
        var list = new List<string>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct)) list.Add(r.GetString(0));
        return list;
    }

    public async Task<IReadOnlyList<string>> GetSchemasAsync(SqlConnectionInfo c, string database, CancellationToken ct = default)
    {
        await using var conn = await OpenAsync(c, database, ct);
        await using var cmd = new SqlCommand("SELECT name FROM sys.schemas WHERE principal_id < 16384 ORDER BY name", conn);
        var list = new List<string>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct)) list.Add(r.GetString(0));
        return list;
    }

    public async Task<IReadOnlyList<DbObjectNode>> GetTablesAsync(SqlConnectionInfo c, string database, CancellationToken ct = default)
        => await ListObjectsAsync(c, database, "SELECT s.name,t.name FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id ORDER BY s.name,t.name", DbObjectKind.Table, ct);

    public async Task<IReadOnlyList<DbObjectNode>> GetViewsAsync(SqlConnectionInfo c, string database, CancellationToken ct = default)
        => await ListObjectsAsync(c, database, "SELECT s.name,v.name FROM sys.views v JOIN sys.schemas s ON s.schema_id=v.schema_id ORDER BY s.name,v.name", DbObjectKind.View, ct);

    public async Task<IReadOnlyList<DbObjectNode>> GetProceduresAsync(SqlConnectionInfo c, string database, CancellationToken ct = default)
        => await ListObjectsAsync(c, database, "SELECT s.name,p.name FROM sys.procedures p JOIN sys.schemas s ON s.schema_id=p.schema_id ORDER BY s.name,p.name", DbObjectKind.StoredProcedure, ct);

    public async Task<IReadOnlyList<DbObjectNode>> GetFunctionsAsync(SqlConnectionInfo c, string database, CancellationToken ct = default)
        => await ListObjectsAsync(c, database, "SELECT s.name,o.name FROM sys.objects o JOIN sys.schemas s ON s.schema_id=o.schema_id WHERE o.type IN ('FN','IF','TF','FS','FT') ORDER BY s.name,o.name", DbObjectKind.Function, ct);

    private static async Task<IReadOnlyList<DbObjectNode>> ListObjectsAsync(SqlConnectionInfo c, string db, string sql, DbObjectKind kind, CancellationToken ct)
    {
        await using var conn = await OpenAsync(c, db, ct);
        await using var cmd = new SqlCommand(sql, conn);
        var list = new List<DbObjectNode>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
            list.Add(new DbObjectNode { Schema = r.GetString(0), Name = r.GetString(1), Kind = kind, HasChildren = kind == DbObjectKind.Table });
        return list;
    }

    public async Task<IReadOnlyList<ColumnInfo>> GetColumnsAsync(SqlConnectionInfo c, string database, string schema, string table, CancellationToken ct = default)
    {
        const string sql = @"
SELECT c.name, ty.name AS DataType, c.max_length, c.precision, c.scale, c.is_nullable, c.is_identity, c.is_computed,
       OBJECT_DEFINITION(dc.object_id) AS DefaultExpr, c.collation_name, c.column_id,
       ISNULL(ic.is_primary_key, 0) AS IsPk
FROM sys.columns c
JOIN sys.types ty ON ty.user_type_id = c.user_type_id
LEFT JOIN sys.default_constraints dc ON dc.object_id = c.default_object_id
OUTER APPLY (
    SELECT TOP 1 1 AS is_primary_key FROM sys.index_columns kic
    JOIN sys.indexes i ON i.object_id=kic.object_id AND i.index_id=kic.index_id
    WHERE i.is_primary_key=1 AND kic.object_id=c.object_id AND kic.column_id=c.column_id
) ic
WHERE c.object_id = OBJECT_ID(@obj)
ORDER BY c.column_id";
        await using var conn = await OpenAsync(c, database, ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@obj", $"[{schema}].[{table}]");
        var list = new List<ColumnInfo>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            list.Add(new ColumnInfo
            {
                Name = r.GetString(0),
                DataType = r.GetString(1),
                MaxLength = r.GetInt16(2),
                Precision = r.GetByte(3),
                Scale = r.GetByte(4),
                IsNullable = r.GetBoolean(5),
                IsIdentity = r.GetBoolean(6),
                IsComputed = r.GetBoolean(7),
                DefaultExpression = r.IsDBNull(8) ? null : r.GetString(8),
                CollationName = r.IsDBNull(9) ? null : r.GetString(9),
                OrdinalPosition = r.GetInt32(10),
                IsPrimaryKey = r.GetInt32(11) == 1
            });
        }
        return list;
    }

    public async Task<IReadOnlyList<IndexInfo>> GetIndexesAsync(SqlConnectionInfo c, string database, string schema, string table, CancellationToken ct = default)
    {
        const string sql = @"
SELECT i.name, i.is_unique, i.type_desc, i.is_primary_key,
       STUFF((SELECT ','+col.name FROM sys.index_columns ic JOIN sys.columns col ON col.object_id=ic.object_id AND col.column_id=ic.column_id WHERE ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.is_included_column=0 ORDER BY ic.key_ordinal FOR XML PATH('')),1,1,''),
       STUFF((SELECT ','+col.name FROM sys.index_columns ic JOIN sys.columns col ON col.object_id=ic.object_id AND col.column_id=ic.column_id WHERE ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.is_included_column=1 FOR XML PATH('')),1,1,'')
FROM sys.indexes i
WHERE i.object_id = OBJECT_ID(@obj) AND i.type > 0
ORDER BY i.index_id";
        await using var conn = await OpenAsync(c, database, ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@obj", $"[{schema}].[{table}]");
        var list = new List<IndexInfo>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            var idx = new IndexInfo
            {
                Name = r.IsDBNull(0) ? "" : r.GetString(0),
                IsUnique = r.GetBoolean(1),
                IsClustered = !r.IsDBNull(2) && r.GetString(2) == "CLUSTERED",
                IsPrimaryKey = r.GetBoolean(3)
            };
            if (!r.IsDBNull(4)) idx.Columns.AddRange(r.GetString(4).Split(','));
            if (!r.IsDBNull(5)) idx.IncludedColumns.AddRange(r.GetString(5).Split(','));
            list.Add(idx);
        }
        return list;
    }

    public async Task<IReadOnlyList<ForeignKeyInfo>> GetForeignKeysAsync(SqlConnectionInfo c, string database, string schema, string table, CancellationToken ct = default)
    {
        const string sql = @"
SELECT fk.name, rs.name AS RefSchema, rt.name AS RefTable,
       cpa.name AS ColName, cpr.name AS RefColName, fk.delete_referential_action_desc, fk.update_referential_action_desc
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
JOIN sys.tables rt ON rt.object_id = fk.referenced_object_id
JOIN sys.schemas rs ON rs.schema_id = rt.schema_id
JOIN sys.columns cpa ON cpa.object_id = fkc.parent_object_id AND cpa.column_id = fkc.parent_column_id
JOIN sys.columns cpr ON cpr.object_id = fkc.referenced_object_id AND cpr.column_id = fkc.referenced_column_id
WHERE fk.parent_object_id = OBJECT_ID(@obj)
ORDER BY fk.name, fkc.constraint_column_id";
        await using var conn = await OpenAsync(c, database, ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@obj", $"[{schema}].[{table}]");
        var dict = new Dictionary<string, ForeignKeyInfo>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            var name = r.GetString(0);
            if (!dict.TryGetValue(name, out var fk))
            {
                fk = new ForeignKeyInfo
                {
                    Name = name,
                    ReferencedSchema = r.GetString(1),
                    ReferencedTable = r.GetString(2),
                    DeleteAction = r.GetString(5),
                    UpdateAction = r.GetString(6)
                };
                dict.Add(name, fk);
            }
            fk.Columns.Add((r.GetString(3), r.GetString(4)));
        }
        return new List<ForeignKeyInfo>(dict.Values);
    }

    public async Task<string> ScriptObjectAsync(SqlConnectionInfo c, string database, string schema, string name, DbObjectKind kind, CancellationToken ct = default)
    {
        await using var conn = await OpenAsync(c, database, ct);
        await using var cmd = new SqlCommand("SELECT OBJECT_DEFINITION(OBJECT_ID(@o))", conn);
        cmd.Parameters.AddWithValue("@o", $"[{schema}].[{name}]");
        var result = await cmd.ExecuteScalarAsync(ct);
        if (result is string s && !string.IsNullOrWhiteSpace(s)) return s;
        return await ScriptTableAsync(c, database, schema, name, ct);
    }

    private async Task<string> ScriptTableAsync(SqlConnectionInfo c, string database, string schema, string table, CancellationToken ct)
    {
        var cols = await GetColumnsAsync(c, database, schema, table, ct);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"CREATE TABLE [{schema}].[{table}] (");
        for (int i = 0; i < cols.Count; i++)
        {
            var col = cols[i];
            sb.Append($"    [{col.Name}] {FormatType(col)}");
            if (col.IsIdentity) sb.Append(" IDENTITY(1,1)");
            sb.Append(col.IsNullable ? " NULL" : " NOT NULL");
            if (col.DefaultExpression is not null) sb.Append($" DEFAULT {col.DefaultExpression}");
            if (i < cols.Count - 1) sb.Append(',');
            sb.AppendLine();
        }
        sb.AppendLine(");");
        return sb.ToString();
    }

    private static string FormatType(ColumnInfo c)
    {
        var t = c.DataType.ToLowerInvariant();
        return t switch
        {
            "varchar" or "char" or "varbinary" or "binary" => $"{c.DataType}({(c.MaxLength == -1 ? "MAX" : c.MaxLength?.ToString() ?? "50")})",
            "nvarchar" or "nchar" => $"{c.DataType}({(c.MaxLength == -1 ? "MAX" : ((c.MaxLength ?? 100) / 2).ToString())})",
            "decimal" or "numeric" => $"{c.DataType}({c.Precision},{c.Scale})",
            _ => c.DataType
        };
    }
}
