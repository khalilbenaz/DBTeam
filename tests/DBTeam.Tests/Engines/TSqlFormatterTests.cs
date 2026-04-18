using DBTeam.Modules.QueryEditor.Formatting;

namespace DBTeam.Tests.Engines;

public class TSqlFormatterTests
{
    [Fact]
    public void Format_Uppercases_Keywords()
    {
        var sql = "select id from dbo.customers where active = 1;";
        var formatted = TSqlFormatter.Format(sql, null, out var errors);

        Assert.Empty(errors);
        Assert.Contains("SELECT", formatted);
        Assert.Contains("FROM", formatted);
        Assert.Contains("WHERE", formatted);
    }

    [Fact]
    public void Format_Preserves_Identifiers_Case()
    {
        var sql = "SELECT FirstName FROM dbo.Customers;";
        var formatted = TSqlFormatter.Format(sql, null, out var errors);

        Assert.Empty(errors);
        Assert.Contains("FirstName", formatted);
        Assert.Contains("Customers", formatted);
    }

    [Fact]
    public void Format_Returns_Original_On_ParseError()
    {
        var sql = "SELCT bad FROM;";
        var formatted = TSqlFormatter.Format(sql, null, out var errors);

        Assert.NotEmpty(errors);
        Assert.Equal(sql, formatted);
    }

    [Fact]
    public void Format_Puts_FromClause_On_NewLine()
    {
        var sql = "SELECT a, b FROM t WHERE a = 1;";
        var formatted = TSqlFormatter.Format(sql, null, out _);

        var lines = formatted.Split('\n');
        Assert.Contains(lines, l => l.TrimStart().StartsWith("FROM"));
        Assert.Contains(lines, l => l.TrimStart().StartsWith("WHERE"));
    }
}
