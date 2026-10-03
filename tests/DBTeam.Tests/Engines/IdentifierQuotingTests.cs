using System.Collections.Generic;
using System.Data;
using DBTeam.Modules.Admin.Engine;
using DBTeam.Modules.DataCompare.Engine;
using DBTeam.Modules.DataGenerator.Engine;
using DBTeam.Modules.Import.Engine;

namespace DBTeam.Tests.Engines;

/// <summary>Un identifiant contenant ']' doit être échappé (]]) dans tout script généré.</summary>
public class IdentifierQuotingTests
{
    [Fact]
    public void GenerateSyncScript_NomsAvecCrochet_SontEchappes()
    {
        var diff = new RowDiff { State = RowState.OnlyInSource, Source = new() { ["i]d"] = 1 } };
        var script = DataCompareEngine.GenerateSyncScript("s]x", "t]y", new[] { diff }, new[] { "i]d" }, new[] { "i]d" });
        Assert.Contains("INSERT INTO [s]]x].[t]]y] ([i]]d])", script);
    }

    [Fact]
    public void RowsToSqlScript_NomsAvecCrochet_SontEchappes()
    {
        var rows = new List<Dictionary<string, object?>> { new() { ["c]1"] = 1 } };
        var script = DataGeneratorEngine.RowsToSqlScript("s]x", "t]y", rows);
        Assert.Contains("INSERT INTO [s]]x].[t]]y] ([c]]1])", script);
    }

    [Fact]
    public void GenerateCreateTableScript_NomsAvecCrochet_SontEchappes()
    {
        var dt = new DataTable();
        dt.Columns.Add("c]1", typeof(int));
        var script = CsvImporter.GenerateCreateTableScript("s]x", "t]y", dt);
        Assert.Contains("CREATE TABLE [s]]x].[t]]y]", script);
        Assert.Contains("[c]]1]", script);
    }

    [Fact]
    public void GenerateIndexRebuildScript_NomsAvecCrochet_SontEchappes() =>
        Assert.Equal("ALTER INDEX [i]]x] ON [s]]x].[t]]y] REBUILD;",
            AdminQueries.GenerateIndexRebuildScript("s]x", "t]y", "i]x", "REBUILD"));

    [Fact]
    public void GenerateRestoreScript_BaseEtFichierHostiles_SontEchappes()
    {
        var script = AdminQueries.GenerateRestoreScript("d]b", "C:\\o'brien.bak");
        Assert.Contains("RESTORE DATABASE [d]]b]", script);
        Assert.Contains("N'C:\\o''brien.bak'", script);
    }
}
