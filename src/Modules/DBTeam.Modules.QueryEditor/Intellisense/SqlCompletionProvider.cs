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
    private readonly Dictionary<string, List<string>> _columnCache = new();

    public SqlCompletionProvider(IDatabaseMetadataService meta) { _meta = meta; }

    public async Task<List<string>> GetTablesAsync(SqlConnectionInfo c, string db)
    {
        var key = $"{c.Id}|{db}";
        if (_tableCache.TryGetValue(key, out var cached)) return cached;
        var list = new List<string>();
        try
        {
            foreach (var t in await _meta.GetTablesAsync(c, db)) list.Add($"{t.Schema}.{t.Name}");
            foreach (var t in await _meta.GetViewsAsync(c, db)) list.Add($"{t.Schema}.{t.Name}");
        }
        catch { }
        _tableCache[key] = list;
        return list;
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
