using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DBTeam.Core.Abstractions;
using DBTeam.Core.Models;

namespace DBTeam.Modules.QueryEditor.Intellisense;

public sealed class SqlCompletionProvider
{
    public static readonly string[] Keywords =
    {
        "SELECT","FROM","WHERE","JOIN","INNER","LEFT","RIGHT","FULL","OUTER","CROSS","APPLY",
        "ON","AND","OR","NOT","IN","EXISTS","BETWEEN","LIKE","IS","NULL","AS","ORDER","BY",
        "GROUP","HAVING","DISTINCT","TOP","OFFSET","FETCH","NEXT","ROWS","ONLY","UNION","ALL",
        "INSERT","INTO","VALUES","UPDATE","SET","DELETE","MERGE","OUTPUT","TRUNCATE",
        "CREATE","ALTER","DROP","TABLE","VIEW","PROCEDURE","FUNCTION","INDEX","TRIGGER","SCHEMA",
        "DATABASE","CONSTRAINT","PRIMARY","KEY","FOREIGN","REFERENCES","UNIQUE","CHECK","DEFAULT",
        "IDENTITY","NULL","NOT","DECLARE","SET","EXEC","EXECUTE","RETURN","BEGIN","END","IF","ELSE",
        "WHILE","TRY","CATCH","THROW","RAISERROR","CASE","WHEN","THEN","ELSE","END","WITH","CTE",
        "OVER","PARTITION","ROW_NUMBER","RANK","DENSE_RANK","LAG","LEAD","SUM","COUNT","AVG","MAX","MIN",
        "CAST","CONVERT","GETDATE","GETUTCDATE","ISNULL","COALESCE","NULLIF","LEN","SUBSTRING","REPLACE",
        "UPPER","LOWER","LTRIM","RTRIM","TRIM","CHARINDEX","FORMAT","TRY_CAST","TRY_CONVERT","TRY_PARSE"
    };

    private readonly IDatabaseMetadataService _meta;
    private readonly Dictionary<string, List<string>> _tableCache = new();
    private readonly Dictionary<string, List<string>> _viewCache = new();
    private readonly Dictionary<string, List<string>> _procCache = new();
    private readonly Dictionary<string, List<string>> _funcCache = new();
    private readonly Dictionary<string, List<string>> _columnCache = new();

    public SqlCompletionProvider(IDatabaseMetadataService meta) { _meta = meta; }

    public async Task<List<string>> GetTablesAsync(SqlConnectionInfo c, string db)
    {
        var key = $"{c.Id}|{db}";
        if (_tableCache.TryGetValue(key, out var cached)) return cached;
        var list = new List<string>();
        try { foreach (var t in await _meta.GetTablesAsync(c, db)) list.Add($"{t.Schema}.{t.Name}"); } catch { }
        _tableCache[key] = list;
        return list;
    }

    public async Task<List<string>> GetViewsAsync(SqlConnectionInfo c, string db)
    {
        var key = $"{c.Id}|{db}";
        if (_viewCache.TryGetValue(key, out var cached)) return cached;
        var list = new List<string>();
        try { foreach (var v in await _meta.GetViewsAsync(c, db)) list.Add($"{v.Schema}.{v.Name}"); } catch { }
        _viewCache[key] = list;
        return list;
    }

    public async Task<List<string>> GetProceduresAsync(SqlConnectionInfo c, string db)
    {
        var key = $"{c.Id}|{db}";
        if (_procCache.TryGetValue(key, out var cached)) return cached;
        var list = new List<string>();
        try { foreach (var p in await _meta.GetProceduresAsync(c, db)) list.Add($"{p.Schema}.{p.Name}"); } catch { }
        _procCache[key] = list;
        return list;
    }

    public async Task<List<string>> GetFunctionsAsync(SqlConnectionInfo c, string db)
    {
        var key = $"{c.Id}|{db}";
        if (_funcCache.TryGetValue(key, out var cached)) return cached;
        var list = new List<string>();
        try { foreach (var f in await _meta.GetFunctionsAsync(c, db)) list.Add($"{f.Schema}.{f.Name}"); } catch { }
        _funcCache[key] = list;
        return list;
    }

    /// <summary>
    /// Parses the current SQL and extracts a map of alias → (schema, table).
    /// Handles FROM/JOIN with optional AS + schema qualifier.
    /// </summary>
    public static Dictionary<string, (string schema, string table)> ExtractAliases(string sql)
    {
        var map = new Dictionary<string, (string, string)>(System.StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(sql)) return map;
        // Match:  FROM/JOIN  [schema].[table]  [AS]  alias
        var pattern = @"\b(?:FROM|JOIN)\s+(?:\[?(?<schema>\w+)\]?\.)?\[?(?<table>\w+)\]?\s+(?:AS\s+)?\[?(?<alias>\w+)\]?\b";
        foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(sql, pattern,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Multiline))
        {
            var alias = m.Groups["alias"].Value;
            var table = m.Groups["table"].Value;
            var schema = m.Groups["schema"].Success ? m.Groups["schema"].Value : "dbo";
            if (string.IsNullOrWhiteSpace(alias) || alias.Equals(table, System.StringComparison.OrdinalIgnoreCase)
                || alias.Equals("ON", System.StringComparison.OrdinalIgnoreCase)
                || alias.Equals("WHERE", System.StringComparison.OrdinalIgnoreCase)
                || alias.Equals("GROUP", System.StringComparison.OrdinalIgnoreCase)
                || alias.Equals("ORDER", System.StringComparison.OrdinalIgnoreCase)
                || alias.Equals("INNER", System.StringComparison.OrdinalIgnoreCase)
                || alias.Equals("LEFT", System.StringComparison.OrdinalIgnoreCase)
                || alias.Equals("RIGHT", System.StringComparison.OrdinalIgnoreCase)
                || alias.Equals("FULL", System.StringComparison.OrdinalIgnoreCase)
                || alias.Equals("CROSS", System.StringComparison.OrdinalIgnoreCase)) continue;
            map[alias] = (schema, table);
        }
        return map;
    }

    /// <summary>
    /// CTE names declared via WITH foo AS (...), bar AS (...).
    /// </summary>
    public static HashSet<string> ExtractCteNames(string sql)
    {
        var set = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(sql)) return set;
        var pattern = @"\b(?:WITH|,)\s+(?<cte>\w+)\s+AS\s*\(";
        foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(sql, pattern,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Multiline))
            set.Add(m.Groups["cte"].Value);
        return set;
    }

    public async Task<List<string>> GetColumnsForTableAsync(SqlConnectionInfo c, string db, string schema, string table)
    {
        var key = $"{c.Id}|{db}|{schema}|{table}";
        if (_columnCache.TryGetValue(key, out var cached)) return cached;
        var list = new List<string>();
        try
        {
            var cols = await _meta.GetColumnsAsync(c, db, schema, table);
            list.AddRange(cols.Select(x => x.Name));
        }
        catch { }
        _columnCache[key] = list;
        return list;
    }

    public void Invalidate() { _tableCache.Clear(); _columnCache.Clear(); }
}
