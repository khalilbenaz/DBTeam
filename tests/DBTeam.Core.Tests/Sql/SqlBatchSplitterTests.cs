using DBTeam.Core.Sql;

namespace DBTeam.Core.Tests.Sql;

public class SqlBatchSplitterTests
{
    private static string[] Texts(string sql) => SqlBatchSplitter.Split(sql).Select(b => b.Sql).ToArray();

    [Fact]
    public void Split_SansGo_RenvoieUnSeulBatch() =>
        Assert.Equal(new[] { "SELECT 1" }, Texts("SELECT 1"));

    [Fact]
    public void Split_GoSeulSurUneLigne_SepareLesBatchs() =>
        Assert.Equal(new[] { "SELECT 1", "SELECT 2" }, Texts("SELECT 1\nGO\nSELECT 2"));

    [Theory]
    [InlineData("go")]
    [InlineData("Go")]
    [InlineData("  GO  ")]
    [InlineData("\tgO\t")]
    public void Split_GoInsensibleALaCasseEtAuxEspaces(string go) =>
        Assert.Equal(new[] { "SELECT 1", "SELECT 2" }, Texts($"SELECT 1\r\n{go}\r\nSELECT 2"));

    [Fact]
    public void Split_GoAvecCompteur_RepeteLeBatch()
    {
        var batches = SqlBatchSplitter.Split("INSERT INTO t DEFAULT VALUES\nGO 5\nSELECT 1");
        Assert.Equal(2, batches.Count);
        Assert.Equal(5, batches[0].Repeat);
        Assert.Equal(1, batches[1].Repeat);
    }

    [Fact]
    public void Split_GoAvecCommentaireFinal_EstUnSeparateur() =>
        Assert.Equal(new[] { "A", "B" }, Texts("A\nGO -- fin\nB"));

    [Fact]
    public void Split_GoDansUneChaine_EstIgnore()
    {
        var sql = "SELECT 'a\nGO\nb'";
        Assert.Equal(new[] { sql }, Texts(sql));
    }

    [Fact]
    public void Split_ApostropheEchappeeDansChaine_RestePrudent()
    {
        var sql = "SELECT 'it''s\nGO\nstill string'";
        Assert.Equal(new[] { sql }, Texts(sql));
    }

    [Fact]
    public void Split_GoDansCommentaireBloc_EstIgnore()
    {
        var sql = "/*\nGO\n*/\nSELECT 1";
        Assert.Equal(new[] { sql }, Texts(sql));
    }

    [Fact]
    public void Split_CommentairesBlocImbriques_SontSuivis()
    {
        var sql = "/* a /* b\nGO\n*/ c\nGO\n*/ SELECT 1";
        Assert.Equal(new[] { sql }, Texts(sql));
    }

    [Fact]
    public void Split_GoDansCommentaireLigne_EstIgnore()
    {
        var sql = "-- GO\nSELECT 1";
        Assert.Equal(new[] { sql }, Texts(sql));
    }

    [Fact]
    public void Split_GoDansIdentifiantEntreCrochets_EstIgnore()
    {
        var sql = "SELECT [a\nGO\nb] FROM t";
        Assert.Equal(new[] { sql }, Texts(sql));
    }

    [Fact]
    public void Split_GoDansIdentifiantEntreGuillemets_EstIgnore()
    {
        var sql = "SELECT \"a\nGO\nb\" FROM t";
        Assert.Equal(new[] { sql }, Texts(sql));
    }

    [Fact]
    public void Split_MotCommencantParGo_NEstPasUnSeparateur()
    {
        Assert.Equal(new[] { "GOTO fin\nGOSUB" }, Texts("GOTO fin\nGOSUB"));
        Assert.Equal(new[] { "SELECT 1 GO" }, Texts("SELECT 1 GO"));
    }

    [Fact]
    public void Split_BatchsVides_SontIgnores() =>
        Assert.Equal(new[] { "SELECT 1" }, Texts("GO\n\nSELECT 1\nGO\nGO\n"));

    [Fact]
    public void Split_ConserveLeNumeroDeLigneDeDebut()
    {
        var b = SqlBatchSplitter.Split("SELECT 1\nGO\n\nSELECT 2");
        Assert.Equal(1, b[0].StartLine);
        Assert.Equal(4, b[1].StartLine);
    }

    [Fact]
    public void Split_GoZero_NEstPasUnCompteurValide_EtResteUnBatch()
    {
        // "GO 0" n'a pas de sens : traité comme du texte, SQL Server le rejettera.
        Assert.Single(SqlBatchSplitter.Split("SELECT 1\nGO 0\nSELECT 2"));
    }
}
