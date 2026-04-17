using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DBTeam.Core.Abstractions;
using DBTeam.Core.Models;

namespace DBTeam.Modules.SchemaCompare.Engine;

public enum DiffState { Identical, OnlyInSource, OnlyInTarget, Different }

public sealed class SchemaDiffItem
{
    public DbObjectKind Kind { get; set; }
    public string Schema { get; set; } = "";
    public string Name { get; set; } = "";
    public DiffState State { get; set; }
    public string? SourceScript { get; set; }
    public string? TargetScript { get; set; }
    public string QualifiedName => $"{Schema}.{Name}";
    public string KindLabel => Kind.ToString();
}

public sealed class SchemaCompareEngine
{
    private readonly IDatabaseMetadataService _meta;
    public SchemaCompareEngine(IDatabaseMetadataService meta) { _meta = meta; }

    public async Task<IReadOnlyList<SchemaDiffItem>> CompareAsync(
        SqlConnectionInfo sourceConn, string sourceDb,
        SqlConnectionInfo targetConn, string targetDb,
        CancellationToken ct = default)
    {
        var items = new List<SchemaDiffItem>();
        await CompareKindAsync(sourceConn, sourceDb, targetConn, targetDb, DbObjectKind.Table, items, ct);
        await CompareKindAsync(sourceConn, sourceDb, targetConn, targetDb, DbObjectKind.View, items, ct);
        await CompareKindAsync(sourceConn, sourceDb, targetConn, targetDb, DbObjectKind.StoredProcedure, items, ct);
        await CompareKindAsync(sourceConn, sourceDb, targetConn, targetDb, DbObjectKind.Function, items, ct);
        return items;
    }

    private async Task CompareKindAsync(
        SqlConnectionInfo sConn, string sDb, SqlConnectionInfo tConn, string tDb,
        DbObjectKind kind, List<SchemaDiffItem> outList, CancellationToken ct)
    {
        var src = await FetchAsync(_meta, sConn, sDb, kind, ct);
        var tgt = await FetchAsync(_meta, tConn, tDb, kind, ct);
        var sMap = src.ToDictionary(n => $"{n.Schema}.{n.Name}", n => n);
        var tMap = tgt.ToDictionary(n => $"{n.Schema}.{n.Name}", n => n);
        var allKeys = new HashSet<string>(sMap.Keys);
        allKeys.UnionWith(tMap.Keys);

        foreach (var key in allKeys.OrderBy(k => k))
        {
            sMap.TryGetValue(key, out var s);
            tMap.TryGetValue(key, out var t);
            var item = new SchemaDiffItem { Kind = kind, Schema = (s ?? t)!.Schema ?? "dbo", Name = (s ?? t)!.Name };
            if (s is not null && t is null) { item.State = DiffState.OnlyInSource; item.SourceScript = await _meta.ScriptObjectAsync(sConn, sDb, item.Schema, item.Name, kind, ct); }
            else if (s is null && t is not null) { item.State = DiffState.OnlyInTarget; item.TargetScript = await _meta.ScriptObjectAsync(tConn, tDb, item.Schema, item.Name, kind, ct); }
            else
            {
                item.SourceScript = await _meta.ScriptObjectAsync(sConn, sDb, item.Schema, item.Name, kind, ct);
                item.TargetScript = await _meta.ScriptObjectAsync(tConn, tDb, item.Schema, item.Name, kind, ct);
                item.State = Normalize(item.SourceScript) == Normalize(item.TargetScript) ? DiffState.Identical : DiffState.Different;
            }
            outList.Add(item);
        }
    }

    private static async Task<IReadOnlyList<DbObjectNode>> FetchAsync(IDatabaseMetadataService m, SqlConnectionInfo c, string db, DbObjectKind kind, CancellationToken ct)
        => kind switch
        {
            DbObjectKind.Table => await m.GetTablesAsync(c, db, ct),
            DbObjectKind.View => await m.GetViewsAsync(c, db, ct),
            DbObjectKind.StoredProcedure => await m.GetProceduresAsync(c, db, ct),
            DbObjectKind.Function => await m.GetFunctionsAsync(c, db, ct),
            _ => new List<DbObjectNode>()
        };

    private static string Normalize(string? s) => (s ?? "").Replace("\r", "").Replace("\t", " ").Replace("  ", " ").Trim();

    public static string GenerateSyncScript(IEnumerable<SchemaDiffItem> selected, bool sourceToTarget = true)
    {
        var sb = new StringBuilder();
        sb.AppendLine("-- Schema sync script");
        sb.AppendLine("-- Direction: " + (sourceToTarget ? "Source -> Target" : "Target -> Source"));
        sb.AppendLine();
        foreach (var item in selected)
        {
            sb.AppendLine($"-- {item.KindLabel}: {item.QualifiedName}  [{item.State}]");
            switch (item.State)
            {
                case DiffState.OnlyInSource when sourceToTarget:
                    sb.AppendLine(item.SourceScript);
                    sb.AppendLine("GO");
                    break;
                case DiffState.OnlyInTarget when sourceToTarget:
                    sb.AppendLine($"DROP {item.KindLabel.ToUpper()} [{item.Schema}].[{item.Name}];");
                    sb.AppendLine("GO");
                    break;
                case DiffState.Different when sourceToTarget:
                    if (item.Kind == DbObjectKind.Table)
                        sb.AppendLine($"-- TODO: manual ALTER TABLE [{item.Schema}].[{item.Name}]");
                    else
                    {
                        sb.AppendLine($"DROP {item.KindLabel.ToUpper()} [{item.Schema}].[{item.Name}];");
                        sb.AppendLine("GO");
                        sb.AppendLine(item.SourceScript);
                        sb.AppendLine("GO");
                    }
                    break;
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
