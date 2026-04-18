using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DBTeam.Core.Models;

namespace DBTeam.Core.Abstractions;

public interface IDatabaseMetadataService
{
    Task<IReadOnlyList<string>> GetDatabasesAsync(SqlConnectionInfo c, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetSchemasAsync(SqlConnectionInfo c, string database, CancellationToken ct = default);
    Task<IReadOnlyList<DbObjectNode>> GetTablesAsync(SqlConnectionInfo c, string database, CancellationToken ct = default);
    Task<IReadOnlyList<DbObjectNode>> GetViewsAsync(SqlConnectionInfo c, string database, CancellationToken ct = default);
    Task<IReadOnlyList<DbObjectNode>> GetProceduresAsync(SqlConnectionInfo c, string database, CancellationToken ct = default);
    Task<IReadOnlyList<DbObjectNode>> GetFunctionsAsync(SqlConnectionInfo c, string database, CancellationToken ct = default);
    Task<IReadOnlyList<ColumnInfo>> GetColumnsAsync(SqlConnectionInfo c, string database, string schema, string table, CancellationToken ct = default);
    Task<IReadOnlyList<IndexInfo>> GetIndexesAsync(SqlConnectionInfo c, string database, string schema, string table, CancellationToken ct = default);
    Task<IReadOnlyList<ForeignKeyInfo>> GetForeignKeysAsync(SqlConnectionInfo c, string database, string schema, string table, CancellationToken ct = default);
    Task<string> ScriptObjectAsync(SqlConnectionInfo c, string database, string schema, string name, DbObjectKind kind, CancellationToken ct = default);

    /// <summary>
    /// Signatures of all stored procedures and user-defined functions in the database
    /// (params with name/type/direction, return type for scalar UDFs).
    /// </summary>
    Task<IReadOnlyList<RoutineSignature>> GetRoutineSignaturesAsync(SqlConnectionInfo c, string database, CancellationToken ct = default);
}
