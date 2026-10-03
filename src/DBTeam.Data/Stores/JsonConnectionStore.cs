using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DBTeam.Core.Abstractions;
using DBTeam.Core.Infrastructure;
using DBTeam.Core.Models;

namespace DBTeam.Data.Stores;

public sealed class JsonConnectionStore : IConnectionStore
{
    private static readonly string DefaultFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DBTeam", "connections.json");
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    private readonly ISecretProtector _protector;
    private readonly string _filePath;
    private IReadOnlyList<string> _warnings = Array.Empty<string>();

    public JsonConnectionStore(ISecretProtector protector) : this(protector, DefaultFilePath) { }

    internal JsonConnectionStore(ISecretProtector protector, string filePath)
    {
        _protector = protector;
        _filePath = filePath;
    }

    public IReadOnlyList<string> LastLoadWarnings => _warnings;

    public async Task<IReadOnlyList<SqlConnectionInfo>> LoadAsync(CancellationToken ct = default)
    {
        var warnings = new List<string>();
        _warnings = warnings;
        if (!File.Exists(_filePath)) return Array.Empty<SqlConnectionInfo>();

        List<SqlConnectionInfo> list;
        try
        {
            await using var fs = File.OpenRead(_filePath);
            list = await JsonSerializer.DeserializeAsync<List<SqlConnectionInfo>>(fs, JsonOpts, ct) ?? new();
        }
        catch (JsonException ex)
        {
            // Fichier corrompu : on le met de côté (jamais écrasé en silence) et on repart d'une liste vide.
            var backup = _filePath + ".corrupt-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);
            File.Move(_filePath, backup, overwrite: true);
            warnings.Add($"connections.json est corrompu ({ex.Message}). Une copie a été conservée : {backup}");
            return Array.Empty<SqlConnectionInfo>();
        }

        foreach (var c in list)
        {
            if (string.IsNullOrEmpty(c.Password)) continue;
            var stored = c.Password;
            try { c.Password = _protector.Unprotect(stored); }
            catch (SecretUnreadableException)
            {
                c.Password = null;
                c.PasswordUnreadable = true;
                c.UnreadableProtectedPassword = stored;
                warnings.Add($"Le mot de passe de la connexion « {c.Name} » est illisible (chiffré par un autre profil Windows ou une autre machine). Ressaisissez-le ; l'ancienne valeur est conservée tant que vous ne le remplacez pas.");
            }
        }
        return list;
    }

    public async Task SaveAsync(IEnumerable<SqlConnectionInfo> all, CancellationToken ct = default)
    {
        var clone = all.Select(x => new SqlConnectionInfo
        {
            Id = x.Id, Name = x.Name, Server = x.Server, Database = x.Database, AuthMode = x.AuthMode,
            User = x.User,
            Password = !string.IsNullOrEmpty(x.Password) ? _protector.Protect(x.Password)
                     : x.PasswordUnreadable ? x.UnreadableProtectedPassword
                     : null,
            TrustServerCertificate = x.TrustServerCertificate, Encrypt = x.Encrypt,
            ConnectTimeoutSeconds = x.ConnectTimeoutSeconds, ApplicationName = x.ApplicationName,
            LastUsed = x.LastUsed
        }).ToList();
        await AtomicFile.WriteAsync(_filePath,
            (s, token) => JsonSerializer.SerializeAsync(s, clone, JsonOpts, token), ct);
    }
}
