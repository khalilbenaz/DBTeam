using DBTeam.Core.Models;
using Microsoft.Data.SqlClient;

namespace DBTeam.Data.Sql;

public static class ConnectionStringFactory
{
    public static string Build(SqlConnectionInfo info, string? overrideDatabase = null)
    {
        var b = new SqlConnectionStringBuilder
        {
            DataSource = info.Server,
            InitialCatalog = overrideDatabase ?? (string.IsNullOrWhiteSpace(info.Database) ? "master" : info.Database),
            TrustServerCertificate = info.TrustServerCertificate,
            Encrypt = info.Encrypt,
            ConnectTimeout = info.ConnectTimeoutSeconds,
            ApplicationName = info.ApplicationName ?? "DBTeam"
        };
        switch (info.AuthMode)
        {
            case SqlAuthMode.Windows:
                b.IntegratedSecurity = true; break;
            case SqlAuthMode.SqlLogin:
                b.UserID = info.User ?? string.Empty;
                b.Password = info.Password ?? string.Empty;
                break;
            case SqlAuthMode.AzureActiveDirectoryIntegrated:
                b.Authentication = SqlAuthenticationMethod.ActiveDirectoryIntegrated; break;
            case SqlAuthMode.AzureActiveDirectoryPassword:
                b.Authentication = SqlAuthenticationMethod.ActiveDirectoryPassword;
                b.UserID = info.User ?? string.Empty;
                b.Password = info.Password ?? string.Empty;
                break;
        }
        return b.ConnectionString;
    }
}
