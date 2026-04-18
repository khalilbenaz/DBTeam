using DBTeam.Core.Models;
using DBTeam.Data.Sql;

namespace DBTeam.Tests.Infrastructure;

public class ConnectionStringTests
{
    [Fact]
    public void Build_Windows_Auth_Uses_Integrated_Security()
    {
        var info = new SqlConnectionInfo { Server = "localhost", Database = "db", AuthMode = SqlAuthMode.Windows };
        var cs = ConnectionStringFactory.Build(info);
        Assert.Contains("Integrated Security=True", cs, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_Sql_Auth_Includes_Credentials()
    {
        var info = new SqlConnectionInfo { Server = "s", Database = "d", AuthMode = SqlAuthMode.SqlLogin, User = "sa", Password = "p@ss" };
        var cs = ConnectionStringFactory.Build(info);
        Assert.Contains("User ID=sa", cs);
        Assert.Contains("Password=p@ss", cs);
    }

    [Fact]
    public void Build_Override_Database_Wins()
    {
        var info = new SqlConnectionInfo { Server = "s", Database = "d1", AuthMode = SqlAuthMode.Windows };
        var cs = ConnectionStringFactory.Build(info, "master");
        Assert.Contains("Initial Catalog=master", cs);
    }

    [Fact]
    public void Build_Empty_Database_Defaults_To_Master()
    {
        var info = new SqlConnectionInfo { Server = "s", Database = "", AuthMode = SqlAuthMode.Windows };
        var cs = ConnectionStringFactory.Build(info);
        Assert.Contains("Initial Catalog=master", cs);
    }
}
