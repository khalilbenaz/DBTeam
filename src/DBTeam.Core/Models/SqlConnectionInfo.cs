using System;
using System.Text.Json.Serialization;

namespace DBTeam.Core.Models;

public enum SqlAuthMode { Windows, SqlLogin, AzureActiveDirectoryIntegrated, AzureActiveDirectoryPassword }

public sealed class SqlConnectionInfo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Server { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public SqlAuthMode AuthMode { get; set; } = SqlAuthMode.SqlLogin;
    public string? User { get; set; }
    public string? Password { get; set; }
    public bool TrustServerCertificate { get; set; } = false;
    public bool Encrypt { get; set; } = true;
    public int ConnectTimeoutSeconds { get; set; } = 15;
    public string? ApplicationName { get; set; } = "DBTeam";
    public DateTime? LastUsed { get; set; }

    /// <summary>Vrai si le mot de passe stocké n'a pas pu être déchiffré au chargement (autre profil Windows/machine).</summary>
    [JsonIgnore] public bool PasswordUnreadable { get; set; }

    /// <summary>Valeur chiffrée d'origine d'un mot de passe illisible, conservée pour ne pas l'écraser à la sauvegarde.</summary>
    [JsonIgnore] public string? UnreadableProtectedPassword { get; set; }
}
