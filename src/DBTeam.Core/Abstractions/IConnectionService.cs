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

    /// <summary>Anomalies du dernier chargement des connexions (secret illisible, fichier corrompu).</summary>
    IReadOnlyList<string> LastLoadWarnings { get; }
}

public interface IConnectionStore
{
    Task<IReadOnlyList<SqlConnectionInfo>> LoadAsync(CancellationToken ct = default);
    Task SaveAsync(IEnumerable<SqlConnectionInfo> all, CancellationToken ct = default);

    /// <summary>Anomalies rencontrées au dernier chargement (secret illisible, fichier corrompu...). À afficher à l'utilisateur.</summary>
    IReadOnlyList<string> LastLoadWarnings { get; }
}

public interface ISecretProtector
{
    string Protect(string plain);

    /// <summary>Déchiffre une valeur. Lève <see cref="SecretUnreadableException"/> si elle est illisible (autre profil/machine, données altérées).</summary>
    string Unprotect(string protectedValue);
}

public sealed class SecretUnreadableException : System.Exception
{
    public SecretUnreadableException(string message) : base(message) { }
    public SecretUnreadableException(string message, System.Exception inner) : base(message, inner) { }
}
