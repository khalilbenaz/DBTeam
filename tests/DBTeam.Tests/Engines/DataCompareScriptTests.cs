using System.Collections.Generic;
using DBTeam.Modules.DataCompare.Engine;

namespace DBTeam.Tests.Engines;

public class DataCompareScriptTests
{
    [Fact]
    public void GenerateSyncScript_Emits_Insert_For_OnlyInSource()
    {
        var diff = new RowDiff
        {
            State = RowState.OnlyInSource,
            Source = new() { ["Id"] = 1, ["Name"] = "Alice" }
        };
        var script = DataCompareEngine.GenerateSyncScript("dbo", "Users",
            new[] { diff }, new[] { "Id" }, new[] { "Id", "Name" });

        Assert.Contains("INSERT INTO [dbo].[Users]", script);
        Assert.Contains("N'Alice'", script);
        Assert.Contains("BEGIN TRAN", script);
        Assert.Contains("COMMIT", script);
    }

    [Fact]
    public void GenerateSyncScript_Emits_Delete_For_OnlyInTarget()
    {
        var diff = new RowDiff
        {
            State = RowState.OnlyInTarget,
            Target = new() { ["Id"] = 7, ["Name"] = "Bob" }
        };
        var script = DataCompareEngine.GenerateSyncScript("dbo", "Users",
            new[] { diff }, new[] { "Id" }, new[] { "Id", "Name" });

        Assert.Contains("DELETE FROM [dbo].[Users]", script);
        Assert.Contains("[Id]=7", script);
    }

    [Fact]
    public void GenerateSyncScript_Emits_Update_For_Different()
    {
        var diff = new RowDiff
        {
            State = RowState.Different,
            Source = new() { ["Id"] = 3, ["Name"] = "Alice2" },
            Target = new() { ["Id"] = 3, ["Name"] = "Alice" }
        };
        var script = DataCompareEngine.GenerateSyncScript("dbo", "Users",
            new[] { diff }, new[] { "Id" }, new[] { "Id", "Name" });

        Assert.Contains("UPDATE [dbo].[Users]", script);
        Assert.Contains("[Name]=N'Alice2'", script);
        Assert.Contains("[Id]=3", script);
    }

    [Fact]
    public void GenerateSyncScript_Escapes_SingleQuotes_In_Strings()
    {
        var diff = new RowDiff
        {
            State = RowState.OnlyInSource,
            Source = new() { ["Id"] = 1, ["Name"] = "O'Brien" }
        };
        var script = DataCompareEngine.GenerateSyncScript("dbo", "Users",
            new[] { diff }, new[] { "Id" }, new[] { "Id", "Name" });

        Assert.Contains("N'O''Brien'", script);
    }
}
