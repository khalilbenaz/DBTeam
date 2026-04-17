using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DBTeam.Core.Abstractions;
using DBTeam.Core.Models;
using Microsoft.Data.SqlClient;

namespace DBTeam.Data.Sql;

public sealed class SqlServerConnectionService : IConnectionService
{
    private readonly IConnectionStore _store;
    private readonly List<SqlConnectionInfo> _saved = new();
    private bool _loaded;

    public SqlServerConnectionService(IConnectionStore store) { _store = store; }

    public IReadOnlyList<SqlConnectionInfo> Saved => _saved;

    public string BuildConnectionString(SqlConnectionInfo info) => ConnectionStringFactory.Build(info);

    public async Task<bool> TestAsync(SqlConnectionInfo info, CancellationToken ct = default)
    {
        try
        {
            await using var conn = new SqlConnection(ConnectionStringFactory.Build(info));
            await conn.OpenAsync(ct);
            return true;
        }
        catch { return false; }
    }

    public async Task<IReadOnlyList<SqlConnectionInfo>> LoadAllAsync(CancellationToken ct = default)
    {
        var list = await _store.LoadAsync(ct);
        _saved.Clear();
        _saved.AddRange(list);
        _loaded = true;
        return _saved;
    }

    public async Task SaveAsync(SqlConnectionInfo info, CancellationToken ct = default)
    {
        if (!_loaded) await LoadAllAsync(ct);
        var idx = _saved.FindIndex(x => x.Id == info.Id);
        if (idx >= 0) _saved[idx] = info; else _saved.Add(info);
        await _store.SaveAsync(_saved, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (!_loaded) await LoadAllAsync(ct);
        _saved.RemoveAll(x => x.Id == id);
        await _store.SaveAsync(_saved, ct);
    }
}
