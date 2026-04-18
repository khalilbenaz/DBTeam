using System;
using System.Threading.Tasks;
using DBTeam.Core.Models;
using DBTeam.Data.Sql;
using Microsoft.Data.SqlClient;

namespace DBTeam.IntegrationTests;

/// <summary>
/// xUnit async fixture that creates a throwaway database on the shared
/// LocalDB instance. Each test class sharing this fixture gets its own DB
/// to isolate schema changes.
/// </summary>
public sealed class LocalDbFixture : IAsyncLifetime
{
    public SqlConnectionInfo Connection { get; private set; } = null!;
    public string DatabaseName { get; } = "DBTeam_IT_" + Guid.NewGuid().ToString("N");
    public bool IsAvailable { get; private set; }

    public async Task InitializeAsync()
    {
        try
        {
            Connection = new SqlConnectionInfo
            {
                Name = "LocalDB",
                Server = @"(localdb)\MSSQLLocalDB",
                AuthMode = SqlAuthMode.Windows,
                TrustServerCertificate = true,
                Encrypt = false,
                ApplicationName = "DBTeam.IT"
            };
            await using var master = new SqlConnection(ConnectionStringFactory.Build(Connection, "master"));
            await master.OpenAsync();
            await using var cmd = new SqlCommand($"CREATE DATABASE [{DatabaseName}];", master);
            await cmd.ExecuteNonQueryAsync();
            IsAvailable = true;
        }
        catch
        {
            // LocalDB not present or not startable — tests should skip themselves.
            IsAvailable = false;
        }
    }

    public async Task DisposeAsync()
    {
        if (!IsAvailable) return;
        try
        {
            await using var master = new SqlConnection(ConnectionStringFactory.Build(Connection, "master"));
            await master.OpenAsync();
            await using var kill = new SqlCommand($"ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;", master);
            try { await kill.ExecuteNonQueryAsync(); } catch { }
            await using var drop = new SqlCommand($"DROP DATABASE [{DatabaseName}];", master);
            try { await drop.ExecuteNonQueryAsync(); } catch { }
        }
        catch { }
    }
}

public sealed class SkipIfLocalDbUnavailableAttribute : FactAttribute
{
    public SkipIfLocalDbUnavailableAttribute()
    {
        try
        {
            using var c = new SqlConnection(@"Data Source=(localdb)\MSSQLLocalDB;Integrated Security=true;Connect Timeout=3");
            c.Open();
        }
        catch
        {
            Skip = "SQL Server LocalDB is not available on this machine.";
        }
    }
}
