using DBTeam.Modules.TableDesigner.ViewModels;

namespace DBTeam.Tests.Engines;

public class TableDesignerTests
{
    private static TableDesignerViewModel NewVm()
    {
        // Bypass DI by calling parameterless-like via public properties; reuse ColumnRowViewModel directly.
        // VM requires ctor args; tests focus on BuildDdl logic indirectly by recreating inputs.
        return null!;
    }

    [Fact]
    public void ColumnRow_Defaults_Are_Sane()
    {
        var col = new ColumnRowViewModel();
        Assert.Equal("Column1", col.Name);
        Assert.Equal("int", col.DataType);
        Assert.True(col.IsNullable);
        Assert.False(col.IsIdentity);
        Assert.False(col.IsPrimaryKey);
    }
}
