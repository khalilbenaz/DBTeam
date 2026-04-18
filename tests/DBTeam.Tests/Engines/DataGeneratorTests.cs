using System;
using System.Collections.Generic;
using System.Linq;
using Bogus;
using DBTeam.Core.Models;
using DBTeam.Modules.DataGenerator.Engine;

namespace DBTeam.Tests.Engines;

public class DataGeneratorTests
{
    private static readonly Faker _f = new();

    [Theory]
    [InlineData("int")]
    [InlineData("bigint")]
    [InlineData("smallint")]
    [InlineData("tinyint")]
    public void GenerateValue_Numeric_Returns_NonNull_For_NotNullable(string type)
    {
        var col = new ColumnInfo { Name = "Amount", DataType = type, IsNullable = false };
        var v = DataGeneratorEngine.GenerateValue(col, _f);
        Assert.NotNull(v);
    }

    [Fact]
    public void GenerateValue_Email_Column_Produces_EmailLike_String()
    {
        var col = new ColumnInfo { Name = "email", DataType = "varchar", IsNullable = false, MaxLength = 200 };
        var v = DataGeneratorEngine.GenerateValue(col, _f) as string;
        Assert.NotNull(v);
        Assert.Contains("@", v);
    }

    [Fact]
    public void GenerateValue_Bit_Returns_Bool()
    {
        var col = new ColumnInfo { Name = "active", DataType = "bit", IsNullable = false };
        var v = DataGeneratorEngine.GenerateValue(col, _f);
        Assert.IsType<bool>(v);
    }

    [Fact]
    public void GenerateValue_Uniqueidentifier_Returns_Guid()
    {
        var col = new ColumnInfo { Name = "id", DataType = "uniqueidentifier", IsNullable = false };
        var v = DataGeneratorEngine.GenerateValue(col, _f);
        Assert.IsType<Guid>(v);
    }

    [Fact]
    public void RowsToSqlScript_Emits_Insert_Statements()
    {
        var rows = new List<Dictionary<string, object?>>
        {
            new() { ["Id"] = 1, ["Name"] = "A" },
            new() { ["Id"] = 2, ["Name"] = "B" }
        };
        var script = DataGeneratorEngine.RowsToSqlScript("dbo", "T", rows);
        Assert.Equal(2, script.Split("INSERT INTO").Length - 1);
        Assert.Contains("N'A'", script);
        Assert.Contains("N'B'", script);
    }

    [Fact]
    public void RowsToSqlScript_EmptyRows_ReturnsEmpty()
    {
        var script = DataGeneratorEngine.RowsToSqlScript("dbo", "T", new List<Dictionary<string, object?>>());
        Assert.Equal(string.Empty, script);
    }
}
