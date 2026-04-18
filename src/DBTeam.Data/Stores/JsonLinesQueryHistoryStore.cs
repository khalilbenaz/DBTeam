using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DBTeam.Core.Abstractions;
using DBTeam.Core.Models;

namespace DBTeam.Data.Stores;

public sealed class JsonLinesQueryHistoryStore : IQueryHistoryStore
{
    private static readonly string AppDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DBTeam");
    private static readonly string FilePath = Path.Combine(AppDir, "history.jsonl");
    private static readonly JsonSerializerOptions Opts = new() { WriteIndented = false };
    private readonly SemaphoreSlim _lock = new(1, 1);
    private const int MaxEntries = 1000;

    public async Task AppendAsync(QueryHistoryEntry entry, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            Directory.CreateDirectory(AppDir);
            var json = JsonSerializer.Serialize(entry, Opts);
            await File.AppendAllLinesAsync(FilePath, new[] { json }, ct);
        }
        finally { _lock.Release(); }
    }

    public async Task<IReadOnlyList<QueryHistoryEntry>> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(FilePath)) return Array.Empty<QueryHistoryEntry>();
        await _lock.WaitAsync(ct);
        try
        {
            var lines = await File.ReadAllLinesAsync(FilePath, ct);
            var list = new List<QueryHistoryEntry>(lines.Length);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                try
                {
                    var e = JsonSerializer.Deserialize<QueryHistoryEntry>(line, Opts);
                    if (e is not null) list.Add(e);
                }
                catch { }
            }
            if (list.Count > MaxEntries)
            {
                var keep = list.Skip(list.Count - MaxEntries).ToList();
                await File.WriteAllLinesAsync(FilePath, keep.Select(e => JsonSerializer.Serialize(e, Opts)), ct);
                return keep;
            }
            return list;
        }
        finally { _lock.Release(); }
    }

    public async Task UpdateAsync(QueryHistoryEntry entry, CancellationToken ct = default)
    {
        var all = (await LoadAsync(ct)).ToList();
        var i = all.FindIndex(x => x.Id == entry.Id);
        if (i < 0) return;
        all[i] = entry;
        await _lock.WaitAsync(ct);
        try { await File.WriteAllLinesAsync(FilePath, all.Select(e => JsonSerializer.Serialize(e, Opts)), ct); }
        finally { _lock.Release(); }
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var all = (await LoadAsync(ct)).Where(x => x.Id != id).ToList();
        await _lock.WaitAsync(ct);
        try { await File.WriteAllLinesAsync(FilePath, all.Select(e => JsonSerializer.Serialize(e, Opts)), ct); }
        finally { _lock.Release(); }
    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try { if (File.Exists(FilePath)) File.Delete(FilePath); }
        finally { _lock.Release(); }
    }
}
