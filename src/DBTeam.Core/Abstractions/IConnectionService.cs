using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DBTeam.Core.Models;

namespace DBTeam.Core.Abstractions;

public interface IConnectionService
{
    IReadOnlyList<SqlConnectionInfo> Saved { get; }
    Task<bool> TestAsync(SqlConnectionInfo info, CancellationToken ct = default);
    Task SaveAsync(SqlConnectionInfo info, CancellationToken ct = default);
    Task DeleteAsync(System.Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<SqlConnectionInfo>> LoadAllAsync(CancellationToken ct = default);
    string BuildConnectionString(SqlConnectionInfo info);
}

public interface IConnectionStore
{
    Task<IReadOnlyList<SqlConnectionInfo>> LoadAsync(CancellationToken ct = default);
    Task SaveAsync(IEnumerable<SqlConnectionInfo> all, CancellationToken ct = default);
}

public interface ISecretProtector
{
    string Protect(string plain);
    string Unprotect(string protectedValue);
}
