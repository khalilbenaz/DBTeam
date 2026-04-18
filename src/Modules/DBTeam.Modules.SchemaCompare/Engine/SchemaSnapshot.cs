using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DBTeam.Core.Abstractions;
using DBTeam.Core.Models;

namespace DBTeam.Modules.SchemaCompare.Engine;

public sealed class SchemaSnapshotEntry
{
    public string Schema { get; set; } = "";
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Script { get; set; } = "";
}

public sealed class SchemaSnapshot
{
    public string Database { get; set; } = "";
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
    public List<SchemaSnapshotEntry> Objects { get; set; } = new();

    public static async Task<SchemaSnapshot> CaptureAsync(
        IDatabaseMetadataService meta, SqlConnectionInfo c, string db, CancellationToken ct = default)
    {
        var snap = new SchemaSnapshot { Database = db };
        foreach (var (kind, items) in new (DbObjectKind, System.Collections.Generic.IReadOnlyList<DbObjectNode>)[]
        {
            (DbObjectKind.Table, await meta.GetTablesAsync(c, db, ct)),
            (DbObjectKind.View, await meta.GetViewsAsync(c, db, ct)),
            (DbObjectKind.StoredProcedure, await meta.GetProceduresAsync(c, db, ct)),
            (DbObjectKind.Function, await meta.GetFunctionsAsync(c, db, ct))
        })
        {
            foreach (var o in items)
            {
                var script = await meta.ScriptObjectAsync(c, db, o.Schema ?? "dbo", o.Name, kind, ct);
                snap.Objects.Add(new SchemaSnapshotEntry { Schema = o.Schema ?? "dbo", Name = o.Name, Kind = kind.ToString(), Script = script });
            }
        }
        return snap;
    }

    public static string DefaultFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DBTeam", "snapshots");

    public string Save(string? folder = null)
    {
        var dir = folder ?? DefaultFolder;
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, $"{Database}-{CapturedAt:yyyyMMdd-HHmmss}.json");
        File.WriteAllText(file, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        return file;
    }

    public static SchemaSnapshot? Load(string file)
    {
        try { return JsonSerializer.Deserialize<SchemaSnapshot>(File.ReadAllText(file)); }
        catch { return null; }
    }

    public IReadOnlyList<SchemaDiffItem> Compare(SchemaSnapshot other)
    {
        var a = Objects.ToDictionary(x => $"{x.Kind}|{x.Schema}.{x.Name}");
        var b = other.Objects.ToDictionary(x => $"{x.Kind}|{x.Schema}.{x.Name}");
        var all = new HashSet<string>(a.Keys); all.UnionWith(b.Keys);
        var result = new List<SchemaDiffItem>();
        foreach (var k in all.OrderBy(x => x))
        {
            a.TryGetValue(k, out var s);
            b.TryGetValue(k, out var t);
            var item = new SchemaDiffItem
            {
                Schema = (s ?? t)!.Schema,
                Name = (s ?? t)!.Name,
                Kind = Enum.TryParse<DbObjectKind>((s ?? t)!.Kind, out var kk) ? kk : DbObjectKind.Table
            };
            if (s is null && t is not null) { item.State = DiffState.OnlyInTarget; item.TargetScript = t.Script; }
            else if (s is not null && t is null) { item.State = DiffState.OnlyInSource; item.SourceScript = s.Script; }
            else { item.SourceScript = s!.Script; item.TargetScript = t!.Script;
                   item.State = s.Script == t.Script ? DiffState.Identical : DiffState.Different; }
            result.Add(item);
        }
        return result;
    }
}
