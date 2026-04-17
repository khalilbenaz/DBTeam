using System;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DBTeam.Core.Abstractions;
using DBTeam.Core.Models;
using Microsoft.Data.SqlClient;

namespace DBTeam.Data.Sql;

public sealed class SqlServerQueryExecutionService : IQueryExecutionService
{
    public async Task<QueryBatchResult> ExecuteAsync(SqlConnectionInfo c, QueryRequest request, CancellationToken ct = default)
    {
        var result = new QueryBatchResult();
        var sw = Stopwatch.StartNew();
        try
        {
            await using var conn = new SqlConnection(ConnectionStringFactory.Build(c, request.Database));
            conn.InfoMessage += (_, e) => { foreach (SqlError err in e.Errors) result.Messages.Add(err.Message); };
            await conn.OpenAsync(ct);
            await using var cmd = new SqlCommand(request.Sql, conn) { CommandTimeout = request.CommandTimeoutSeconds };
            foreach (var (k, v) in request.Parameters) cmd.Parameters.AddWithValue(k, v ?? DBNull.Value);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            int affected = 0;
            do
            {
                if (reader.FieldCount > 0)
                {
                    var dt = new DataTable();
                    dt.Load(reader);
                    result.ResultSets.Add(dt);
                }
                else
                {
                    affected += reader.RecordsAffected > 0 ? reader.RecordsAffected : 0;
                }
            } while (!reader.IsClosed && await reader.NextResultAsync(ct));
            result.RowsAffected = affected;
        }
        catch (Exception ex)
        {
            result.Error = ex;
            result.Messages.Add(ex.Message);
        }
        finally
        {
            sw.Stop();
            result.Elapsed = sw.Elapsed;
        }
        return result;
    }

    public async Task<string> GetEstimatedPlanXmlAsync(SqlConnectionInfo c, QueryRequest request, CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(ConnectionStringFactory.Build(c, request.Database));
        await conn.OpenAsync(ct);
        await using (var on = new SqlCommand("SET SHOWPLAN_XML ON", conn)) await on.ExecuteNonQueryAsync(ct);
        try
        {
            await using var cmd = new SqlCommand(request.Sql, conn) { CommandTimeout = request.CommandTimeoutSeconds };
            await using var r = await cmd.ExecuteReaderAsync(ct);
            if (await r.ReadAsync(ct)) return r.GetString(0);
            return string.Empty;
        }
        finally
        {
            await using var off = new SqlCommand("SET SHOWPLAN_XML OFF", conn);
            await off.ExecuteNonQueryAsync(ct);
        }
    }

    public async Task<(QueryBatchResult Result, string PlanXml)> ExecuteWithActualPlanAsync(SqlConnectionInfo c, QueryRequest request, CancellationToken ct = default)
    {
        var result = new QueryBatchResult();
        var sw = Stopwatch.StartNew();
        string planXml = string.Empty;
        try
        {
            await using var conn = new SqlConnection(ConnectionStringFactory.Build(c, request.Database));
            conn.InfoMessage += (_, e) => { foreach (SqlError err in e.Errors) result.Messages.Add(err.Message); };
            await conn.OpenAsync(ct);
            await using (var on = new SqlCommand("SET STATISTICS XML ON", conn)) await on.ExecuteNonQueryAsync(ct);
            await using var cmd = new SqlCommand(request.Sql, conn) { CommandTimeout = request.CommandTimeoutSeconds };
            foreach (var (k, v) in request.Parameters) cmd.Parameters.AddWithValue(k, v ?? DBNull.Value);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            do
            {
                if (reader.FieldCount == 1 && reader.GetName(0).Contains("ShowPlan", StringComparison.OrdinalIgnoreCase))
                {
                    if (await reader.ReadAsync(ct)) planXml = reader.GetString(0);
                }
                else if (reader.FieldCount > 0)
                {
                    var dt = new DataTable();
                    dt.Load(reader);
                    result.ResultSets.Add(dt);
                }
            } while (!reader.IsClosed && await reader.NextResultAsync(ct));
        }
        catch (Exception ex) { result.Error = ex; result.Messages.Add(ex.Message); }
        finally { sw.Stop(); result.Elapsed = sw.Elapsed; }
        return (result, planXml);
    }
}
