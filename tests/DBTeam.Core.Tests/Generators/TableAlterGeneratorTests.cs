using DBTeam.Core.Abstractions;
using DBTeam.Core.Models;
using DBTeam.Modules.SchemaCompare.Engine;

namespace DBTeam.Core.Tests.Generators;

public class TableAlterGeneratorTests
{
    private sealed class FakeMeta : IDatabaseMetadataService
    {
        public List<ColumnInfo> Source = new(), Target = new();
        public List<ForeignKeyInfo> SourceFks = new(), TargetFks = new();
        public List<IndexInfo> SourceIdx = new(), TargetIdx = new();

        public Task<IReadOnlyList<ColumnInfo>> GetColumnsAsync(SqlConnectionInfo c, string database, string schema, string table, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ColumnInfo>>(database == "S" ? Source : Target);
        public Task<IReadOnlyList<IndexInfo>> GetIndexesAsync(SqlConnectionInfo c, string database, string schema, string table, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<IndexInfo>>(database == "S" ? SourceIdx : TargetIdx);
        public Task<IReadOnlyList<ForeignKeyInfo>> GetForeignKeysAsync(SqlConnectionInfo c, string database, string schema, string table, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ForeignKeyInfo>>(database == "S" ? SourceFks : TargetFks);

        public Task<IReadOnlyList<string>> GetDatabasesAsync(SqlConnectionInfo c, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<string>> GetSchemasAsync(SqlConnectionInfo c, string database, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<DbObjectNode>> GetTablesAsync(SqlConnectionInfo c, string database, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<DbObjectNode>> GetViewsAsync(SqlConnectionInfo c, string database, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<DbObjectNode>> GetProceduresAsync(SqlConnectionInfo c, string database, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<DbObjectNode>> GetFunctionsAsync(SqlConnectionInfo c, string database, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<string> ScriptObjectAsync(SqlConnectionInfo c, string database, string schema, string name, DbObjectKind kind, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<RoutineSignature>> GetRoutineSignaturesAsync(SqlConnectionInfo c, string database, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private static Task<string> Build(FakeMeta m, string schema, string table) =>
        TableAlterGenerator.BuildAsync(m, new SqlConnectionInfo(), "S", new SqlConnectionInfo(), "T", schema, table);

    [Fact]
    public async Task Build_ColonneAjoutee_GenereAddAvecNomsEchappes()
    {
        var m = new FakeMeta();
        m.Source.Add(new ColumnInfo { Name = "c]ol", DataType = "int", IsNullable = true });
        var sql = await Build(m, "s]x", "t]y");
        Assert.Contains("ALTER TABLE [s]]x].[t]]y] ADD [c]]ol] int NULL;", sql);
    }

    [Fact]
    public async Task Build_ColonneSupprimee_GenereDropColumnEchappe()
    {
        var m = new FakeMeta();
        m.Target.Add(new ColumnInfo { Name = "a]b", DataType = "int" });
        var sql = await Build(m, "dbo", "T");
        Assert.Contains("DROP COLUMN [a]]b];", sql);
    }

    [Fact]
    public async Task Build_ForeignKeyEtIndex_EchappentTousLesIdentifiants()
    {
        var m = new FakeMeta();
        var fk = new ForeignKeyInfo { Name = "fk]1", ReferencedSchema = "r]s", ReferencedTable = "r]t" };
        fk.Columns.Add(("c]1", "rc]1"));
        m.SourceFks.Add(fk);
        var ix = new IndexInfo { Name = "ix]1" };
        ix.Columns.Add("k]1");
        ix.IncludedColumns.Add("i]1");
        m.SourceIdx.Add(ix);
        m.TargetIdx.Add(new IndexInfo { Name = "old]ix" });
        m.TargetFks.Add(new ForeignKeyInfo { Name = "old]fk" });

        var sql = await Build(m, "dbo", "T");

        Assert.Contains("ADD CONSTRAINT [fk]]1] FOREIGN KEY ([c]]1]) REFERENCES [r]]s].[r]]t] ([rc]]1]);", sql);
        Assert.Contains("DROP CONSTRAINT [old]]fk];", sql);
        Assert.Contains("CREATE NONCLUSTERED INDEX [ix]]1] ON [dbo].[T] ([k]]1]) INCLUDE ([i]]1]);", sql);
        Assert.Contains("DROP INDEX [old]]ix] ON [dbo].[T];", sql);
    }
}
