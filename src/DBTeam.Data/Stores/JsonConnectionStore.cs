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

public sealed class JsonConnectionStore : IConnectionStore
{
    private static readonly string AppDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DBTeam");
    private static readonly string FilePath = Path.Combine(AppDir, "connections.json");
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    private readonly ISecretProtector _protector;
    public JsonConnectionStore(ISecretProtector protector) { _protector = protector; }

    public async Task<IReadOnlyList<SqlConnectionInfo>> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(FilePath)) return Array.Empty<SqlConnectionInfo>();
        await using var fs = File.OpenRead(FilePath);
        var list = await JsonSerializer.DeserializeAsync<List<SqlConnectionInfo>>(fs, JsonOpts, ct) ?? new();
        foreach (var c in list)
            if (!string.IsNullOrEmpty(c.Password)) c.Password = _protector.Unprotect(c.Password);
        return list;
    }

    public async Task SaveAsync(IEnumerable<SqlConnectionInfo> all, CancellationToken ct = default)
    {
        Directory.CreateDirectory(AppDir);
        var clone = all.Select(x => new SqlConnectionInfo
        {
            Id = x.Id, Name = x.Name, Server = x.Server, Database = x.Database, AuthMode = x.AuthMode,
            User = x.User,
            Password = string.IsNullOrEmpty(x.Password) ? null : _protector.Protect(x.Password),
            TrustServerCertificate = x.TrustServerCertificate, Encrypt = x.Encrypt,
            ConnectTimeoutSeconds = x.ConnectTimeoutSeconds, ApplicationName = x.ApplicationName,
            LastUsed = x.LastUsed
        }).ToList();
        await using var fs = File.Create(FilePath);
        await JsonSerializer.SerializeAsync(fs, clone, JsonOpts, ct);
    }
}
