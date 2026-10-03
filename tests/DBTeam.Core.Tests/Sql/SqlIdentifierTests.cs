using DBTeam.Core.Sql;

namespace DBTeam.Core.Tests.Sql;

public class SqlIdentifierTests
{
    [Fact]
    public void Quote_NomSimple_EntoureDeCrochets() =>
        Assert.Equal("[Orders]", SqlIdentifier.Quote("Orders"));

    [Fact]
    public void Quote_CrochetFermant_EstDouble() =>
        Assert.Equal("[a]]b]", SqlIdentifier.Quote("a]b"));

    [Fact]
    public void Quote_NomHostile_NePeutPasSortirDuCrochet()
    {
        var q = SqlIdentifier.Quote("x]; DROP TABLE t;--");
        Assert.Equal("[x]]; DROP TABLE t;--]", q);
    }

    [Fact]
    public void Quote_CrochetOuvrant_RestePresentTelQuel() =>
        Assert.Equal("[a[b]", SqlIdentifier.Quote("a[b"));

    [Fact]
    public void Quote_Null_Leve() =>
        Assert.Throws<ArgumentNullException>(() => SqlIdentifier.Quote(null!));

    [Fact]
    public void Quote_SchemaEtNom_EstQualifie() =>
        Assert.Equal("[dbo].[T]]x]", SqlIdentifier.Quote("dbo", "T]x"));

    [Fact]
    public void QuoteList_Colonnes_SontSeparees() =>
        Assert.Equal("[a],[b]]]", SqlIdentifier.QuoteList(new[] { "a", "b]" }));
}
