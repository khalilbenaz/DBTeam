using System.Collections.Generic;
using System.IO;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DBTeam.Modules.QueryEditor.Formatting;

public sealed class TSqlFormatOptions
{
    public bool AlignClauseBodies { get; set; } = true;
    public bool AsKeywordOnOwnLine { get; set; } = false;
    public bool IncludeSemicolons { get; set; } = true;
    public bool KeywordCasing { get; set; } = true;
    public int IndentationSize { get; set; } = 4;
    public bool NewLineBeforeFromClause { get; set; } = true;
    public bool NewLineBeforeWhereClause { get; set; } = true;
    public bool NewLineBeforeJoinClause { get; set; } = true;
    public bool NewLineBeforeGroupByClause { get; set; } = true;
    public bool NewLineBeforeOrderByClause { get; set; } = true;
    public bool NewLineBeforeOutputClause { get; set; } = true;
}

public static class TSqlFormatter
{
    public static string Format(string sql, TSqlFormatOptions? opts, out IList<ParseError> errors)
    {
        opts ??= new TSqlFormatOptions();
        var parser = new TSql160Parser(initialQuotedIdentifiers: true);
        using var reader = new StringReader(sql);
        var fragment = parser.Parse(reader, out errors);
        if (errors is { Count: > 0 } || fragment is null) return sql;

        var gen = new Sql160ScriptGenerator(new SqlScriptGeneratorOptions
        {
            AlignClauseBodies = opts.AlignClauseBodies,
            AsKeywordOnOwnLine = opts.AsKeywordOnOwnLine,
            IncludeSemicolons = opts.IncludeSemicolons,
            KeywordCasing = opts.KeywordCasing ? KeywordCasing.Uppercase : KeywordCasing.Lowercase,
            IndentationSize = opts.IndentationSize,
            NewLineBeforeFromClause = opts.NewLineBeforeFromClause,
            NewLineBeforeWhereClause = opts.NewLineBeforeWhereClause,
            NewLineBeforeJoinClause = opts.NewLineBeforeJoinClause,
            NewLineBeforeGroupByClause = opts.NewLineBeforeGroupByClause,
            NewLineBeforeOrderByClause = opts.NewLineBeforeOrderByClause,
            NewLineBeforeOutputClause = opts.NewLineBeforeOutputClause,
            SqlVersion = SqlVersion.Sql160
        });
        gen.GenerateScript(fragment, out string formatted);
        return formatted;
    }
}
