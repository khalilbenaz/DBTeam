using System.Data;
using System.Threading;
using System.Threading.Tasks;
using DBTeam.Core.Models;
using DBTeam.Data.Sql;
using Microsoft.Data.SqlClient;

namespace DBTeam.Modules.Admin.Engine;

public static class AdminQueries
{
    public static Task<DataTable> LoginsAsync(SqlConnectionInfo c, CancellationToken ct = default) =>
        QueryAsync(c, "master", @"SELECT name, type_desc, is_disabled, create_date, modify_date, default_database_name, default_language_name FROM sys.server_principals WHERE type IN ('S','U','G','R') ORDER BY name", ct);

    public static Task<DataTable> UsersAsync(SqlConnectionInfo c, string db, CancellationToken ct = default) =>
        QueryAsync(c, db, @"SELECT name, type_desc, default_schema_name, create_date, modify_date FROM sys.database_principals WHERE type NOT IN ('R','A') AND name NOT LIKE '##%' ORDER BY name", ct);

    public static Task<DataTable> RolesAsync(SqlConnectionInfo c, string db, CancellationToken ct = default) =>
        QueryAsync(c, db, @"SELECT name, type_desc, create_date, modify_date FROM sys.database_principals WHERE type = 'R' ORDER BY name", ct);

    public static Task<DataTable> PermissionsAsync(SqlConnectionInfo c, string db, CancellationToken ct = default) =>
        QueryAsync(c, db, @"SELECT dp.name AS grantee, p.class_desc, p.permission_name, p.state_desc, OBJECT_NAME(p.major_id) AS object_name
FROM sys.database_permissions p
JOIN sys.database_principals dp ON dp.principal_id = p.grantee_principal_id
ORDER BY dp.name, p.class_desc", ct);

    public static Task<DataTable> IndexFragmentationAsync(SqlConnectionInfo c, string db, CancellationToken ct = default) =>
        QueryAsync(c, db, @"SELECT s.name AS schema_name, t.name AS table_name, i.name AS index_name, i.type_desc,
       ps.avg_fragmentation_in_percent, ps.page_count, ps.index_depth,
       CASE WHEN ps.avg_fragmentation_in_percent < 5  THEN 'OK'
            WHEN ps.avg_fragmentation_in_percent < 30 THEN 'REORGANIZE'
            ELSE 'REBUILD' END AS recommendation
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ps
JOIN sys.indexes i ON i.object_id = ps.object_id AND i.index_id = ps.index_id
JOIN sys.tables  t ON t.object_id = ps.object_id
JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE i.type > 0 AND ps.page_count > 10
ORDER BY ps.avg_fragmentation_in_percent DESC", ct);

    public static Task<DataTable> SlowQueriesAsync(SqlConnectionInfo c, string db, CancellationToken ct = default) =>
        QueryAsync(c, db, @"SELECT TOP 50 qs.execution_count, qs.total_worker_time/1000 AS total_cpu_ms,
       qs.total_logical_reads, qs.total_elapsed_time/1000 AS total_elapsed_ms,
       qs.last_execution_time,
       SUBSTRING(st.text, (qs.statement_start_offset/2)+1,
                ((CASE qs.statement_end_offset WHEN -1 THEN DATALENGTH(st.text) ELSE qs.statement_end_offset END - qs.statement_start_offset)/2) + 1) AS sql_text
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) st
ORDER BY qs.total_elapsed_time DESC", ct);

    public static Task<DataTable> ActiveSessionsAsync(SqlConnectionInfo c, CancellationToken ct = default) =>
        QueryAsync(c, "master", @"SELECT session_id, login_name, host_name, program_name, status, cpu_time, memory_usage, total_elapsed_time, last_request_start_time, reads, writes
FROM sys.dm_exec_sessions WHERE is_user_process = 1
ORDER BY last_request_start_time DESC", ct);

    public static Task<DataTable> DatabaseSizeAsync(SqlConnectionInfo c, CancellationToken ct = default) =>
        QueryAsync(c, "master", @"SELECT d.name AS database_name, d.state_desc, d.recovery_model_desc,
       CAST(SUM(mf.size)*8.0/1024 AS DECIMAL(18,2)) AS size_mb,
       CAST(SUM(CASE WHEN mf.type_desc='ROWS' THEN mf.size ELSE 0 END)*8.0/1024 AS DECIMAL(18,2)) AS data_mb,
       CAST(SUM(CASE WHEN mf.type_desc='LOG'  THEN mf.size ELSE 0 END)*8.0/1024 AS DECIMAL(18,2)) AS log_mb
FROM sys.databases d
JOIN sys.master_files mf ON mf.database_id = d.database_id
GROUP BY d.name, d.state_desc, d.recovery_model_desc
ORDER BY size_mb DESC", ct);

    public static string GenerateBackupScript(string db, string? path = null)
    {
        path ??= $@"C:\Backup\{db}_{{DATE}}.bak";
        return $@"-- Full backup of [{db}]
DECLARE @path NVARCHAR(500) = REPLACE(N'{path}', N'{{DATE}}', CONVERT(NVARCHAR(20), SYSUTCDATETIME(), 112) + '_' + REPLACE(CONVERT(NVARCHAR(20), SYSUTCDATETIME(), 108), ':', ''));
BACKUP DATABASE [{db}] TO DISK = @path
WITH FORMAT, INIT, COMPRESSION, STATS = 10, NAME = N'{db} Full backup';";
    }

    public static string GenerateRestoreScript(string db, string backupFile)
    {
        return $@"-- Restore [{db}] from a .bak (review file paths first)
USE master;
ALTER DATABASE [{db}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE [{db}]
    FROM DISK = N'{backupFile}'
    WITH REPLACE, STATS = 10;
ALTER DATABASE [{db}] SET MULTI_USER;";
    }

    public static string GenerateIndexRebuildScript(string schema, string table, string indexName, string recommendation)
    {
        var op = recommendation == "REBUILD" ? "REBUILD" : "REORGANIZE";
        return $"ALTER INDEX [{indexName}] ON [{schema}].[{table}] {op};";
    }

    private static async Task<DataTable> QueryAsync(SqlConnectionInfo c, string db, string sql, CancellationToken ct)
    {
        var dt = new DataTable();
        try
        {
            await using var conn = new SqlConnection(ConnectionStringFactory.Build(c, db));
            await conn.OpenAsync(ct);
            await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 60 };
            await using var r = await cmd.ExecuteReaderAsync(ct);
            dt.Load(r);
        }
        catch { }
        return dt;
    }
}
