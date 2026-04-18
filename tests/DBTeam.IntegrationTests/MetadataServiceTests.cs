using System.Threading.Tasks;
using DBTeam.Core.Abstractions;
using DBTeam.Data.Sql;
using DBTeam.Data.Security;
using DBTeam.Data.Stores;
using DBTeam.Data.Sql;
using Microsoft.Data.SqlClient;

namespace DBTeam.IntegrationTests;

public class MetadataServiceTests : IClassFixture<LocalDbFixture>
{
    private readonly LocalDbFixture _fx;
    public MetadataServiceTests(LocalDbFixture fx) { _fx = fx; }

    [SkipIfLocalDbUnavailable]
    public async Task GetDatabases_Contains_Master_And_OurDatabase()
    {
        if (!_fx.IsAvailable) return;
        var svc = new SqlServerMetadataService();
        var dbs = await svc.GetDatabasesAsync(_fx.Connection);
        Assert.Contains("master", dbs);
        Assert.Contains(_fx.DatabaseName, dbs);
    }

    [SkipIfLocalDbUnavailable]
    public async Task GetTables_After_Create_Returns_Created_Table()
    {
        if (!_fx.IsAvailable) return;
        await using var c = new SqlConnection(ConnectionStringFactory.Build(_fx.Connection, _fx.DatabaseName));
        await c.OpenAsync();
        await using var cmd = new SqlCommand(@"IF OBJECT_ID('dbo.TestT','U') IS NULL
            CREATE TABLE dbo.TestT (Id int IDENTITY(1,1) PRIMARY KEY, Name nvarchar(50) NOT NULL);", c);
        await cmd.ExecuteNonQueryAsync();

        var svc = new SqlServerMetadataService();
        var tables = await svc.GetTablesAsync(_fx.Connection, _fx.DatabaseName);
        Assert.Contains(tables, t => t.Schema == "dbo" && t.Name == "TestT");

        var cols = await svc.GetColumnsAsync(_fx.Connection, _fx.DatabaseName, "dbo", "TestT");
        Assert.Equal(2, cols.Count);
        Assert.Contains(cols, x => x.Name == "Id" && x.IsPrimaryKey && x.IsIdentity);
        Assert.Contains(cols, x => x.Name == "Name" && !x.IsNullable);
    }

    [SkipIfLocalDbUnavailable]
    public async Task QueryExecution_Scalar()
    {
        if (!_fx.IsAvailable) return;
        var exec = new SqlServerQueryExecutionService();
        var r = await exec.ExecuteAsync(_fx.Connection, new DBTeam.Core.Models.QueryRequest
        {
            Sql = "SELECT 42 AS X;",
            Database = "master"
        });
        Assert.False(r.HasError, r.Messages.Count > 0 ? r.Messages[0] : "unknown error");
        Assert.Single(r.ResultSets);
        Assert.Equal(42, System.Convert.ToInt32(r.ResultSets[0].Rows[0]["X"]));
    }
}
