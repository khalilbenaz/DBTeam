using System;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DBTeam.Core.Abstractions;
using DBTeam.Core.Models;
using DBTeam.Core.Sql;
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
            int affected = 0;
            var batches = SqlBatchSplitter.Split(request.Sql);
            for (var bi = 0; bi < batches.Count; bi++)
            {
                for (var rep = 0; rep < batches[bi].Repeat; rep++)
                {
                    try
                    {
                        await using var cmd = new SqlCommand(batches[bi].Sql, conn) { CommandTimeout = request.CommandTimeoutSeconds };
                        foreach (var (k, v) in request.Parameters) cmd.Parameters.AddWithValue(k, v ?? DBNull.Value);

                        await using var reader = await cmd.ExecuteReaderAsync(ct);
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
                    }
                    catch (SqlException) when (batches.Count > 1)
                    {
                        // On s'arrête au premier batch en erreur et on dit lequel (ligne de début dans le script).
                        result.Messages.Add($"Batch {bi + 1}/{batches.Count} (ligne {batches[bi].StartLine}) en erreur : exécution interrompue.");
                        throw;
                    }
                }
            }
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
            // Plan du premier batch qui en produit un (GO n'est pas du T-SQL : il faut découper).
            foreach (var batch in SqlBatchSplitter.Split(request.Sql))
            {
                await using var cmd = new SqlCommand(batch.Sql, conn) { CommandTimeout = request.CommandTimeoutSeconds };
                await using var r = await cmd.ExecuteReaderAsync(ct);
                if (await r.ReadAsync(ct)) return r.GetString(0);
            }
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
            foreach (var batch in SqlBatchSplitter.Split(request.Sql))
            {
                for (var rep = 0; rep < batch.Repeat; rep++)
                {
                    await using var cmd = new SqlCommand(batch.Sql, conn) { CommandTimeout = request.CommandTimeoutSeconds };
                    foreach (var (k, v) in request.Parameters) cmd.Parameters.AddWithValue(k, v ?? DBNull.Value);
                    await using var reader = await cmd.ExecuteReaderAsync(ct);
                    do
                    {
                        if (reader.FieldCount == 1 && reader.GetName(0).Contains("ShowPlan", StringComparison.OrdinalIgnoreCase))
                        {
                            if (await reader.ReadAsync(ct) && planXml.Length == 0) planXml = reader.GetString(0);
                        }
                        else if (reader.FieldCount > 0)
                        {
                            var dt = new DataTable();
                            dt.Load(reader);
                            result.ResultSets.Add(dt);
                        }
                    } while (!reader.IsClosed && await reader.NextResultAsync(ct));
                }
            }
        }
        catch (Exception ex) { result.Error = ex; result.Messages.Add(ex.Message); }
        finally { sw.Stop(); result.Elapsed = sw.Elapsed; }
        return (result, planXml);
    }
}
