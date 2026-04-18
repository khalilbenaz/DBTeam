using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DBTeam.Core.Models;

namespace DBTeam.Core.Abstractions;

public interface IQueryHistoryStore
{
    Task AppendAsync(QueryHistoryEntry entry, CancellationToken ct = default);
    Task<IReadOnlyList<QueryHistoryEntry>> LoadAsync(CancellationToken ct = default);
    Task UpdateAsync(QueryHistoryEntry entry, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task ClearAsync(CancellationToken ct = default);
}
