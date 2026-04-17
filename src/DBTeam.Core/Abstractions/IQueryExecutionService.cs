using System.Threading;
using System.Threading.Tasks;
using DBTeam.Core.Models;

namespace DBTeam.Core.Abstractions;

public interface IQueryExecutionService
{
    Task<QueryBatchResult> ExecuteAsync(SqlConnectionInfo c, QueryRequest request, CancellationToken ct = default);
    Task<string> GetEstimatedPlanXmlAsync(SqlConnectionInfo c, QueryRequest request, CancellationToken ct = default);
    Task<(QueryBatchResult Result, string PlanXml)> ExecuteWithActualPlanAsync(SqlConnectionInfo c, QueryRequest request, CancellationToken ct = default);
}
