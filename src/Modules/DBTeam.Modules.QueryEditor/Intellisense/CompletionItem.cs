using System;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace DBTeam.Modules.QueryEditor.Intellisense;

public enum CompletionKind { Keyword, Table, View, Procedure, Function, Column, Schema }

public sealed class SqlCompletionItem : ICompletionData
{
    public SqlCompletionItem(string text, CompletionKind kind, string? description = null)
    {
        Text = text; Kind = kind; Description = description ?? $"{kind}: {text}";
    }

    public string Text { get; }
    public CompletionKind Kind { get; }
    public object Content => Text;
    public object Description { get; }
    public double Priority => Kind switch
    {
        CompletionKind.Column => 5,
        CompletionKind.Table => 4,
        CompletionKind.View => 3,
        CompletionKind.Procedure => 2,
        CompletionKind.Function => 2,
        CompletionKind.Schema => 1,
        _ => 0
    };
    public ImageSource? Image => null;

    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        textArea.Document.Replace(completionSegment, ToInsertionText());
    }

    /// <summary>
    /// Auto-brackets object identifiers so names colliding with T-SQL reserved
    /// keywords (e.g. <c>transaction</c>, <c>user</c>, <c>order</c>) parse correctly.
    /// Tables / views / procedures / functions are always qualified;
    /// columns are bracketed only when the name is a reserved keyword.
    /// </summary>
    private string ToInsertionText()
    {
        switch (Kind)
        {
            case CompletionKind.Table:
            case CompletionKind.View:
            case CompletionKind.Procedure:
            case CompletionKind.Function:
            {
                var parts = Text.Split('.');
                if (parts.Length == 2) return $"[{parts[0]}].[{parts[1]}]";
                return IsReserved(Text) || NeedsQuoting(Text) ? $"[{Text}]" : Text;
            }
            case CompletionKind.Column:
                return IsReserved(Text) || NeedsQuoting(Text) ? $"[{Text}]" : Text;
            default:
                return Text;
        }
    }

    private static bool NeedsQuoting(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        foreach (var c in s) if (!(char.IsLetterOrDigit(c) || c == '_')) return true;
        return char.IsDigit(s[0]);
    }

    private static readonly System.Collections.Generic.HashSet<string> Reserved =
        new(System.StringComparer.OrdinalIgnoreCase)
        {
            "ADD","ALL","ALTER","AND","ANY","AS","ASC","AUTHORIZATION","BACKUP","BEGIN","BETWEEN",
            "BREAK","BROWSE","BULK","BY","CASCADE","CASE","CHECK","CHECKPOINT","CLOSE","CLUSTERED",
            "COALESCE","COLLATE","COLUMN","COMMIT","COMPUTE","CONSTRAINT","CONTAINS","CONTAINSTABLE",
            "CONTINUE","CONVERT","CREATE","CROSS","CURRENT","CURRENT_DATE","CURRENT_TIME","CURRENT_TIMESTAMP",
            "CURRENT_USER","CURSOR","DATABASE","DBCC","DEALLOCATE","DECLARE","DEFAULT","DELETE","DENY","DESC",
            "DISK","DISTINCT","DISTRIBUTED","DOUBLE","DROP","DUMP","ELSE","END","ERRLVL","ESCAPE","EXCEPT",
            "EXEC","EXECUTE","EXISTS","EXIT","EXTERNAL","FETCH","FILE","FILLFACTOR","FOR","FOREIGN","FREETEXT",
            "FREETEXTTABLE","FROM","FULL","FUNCTION","GOTO","GRANT","GROUP","HAVING","HOLDLOCK","IDENTITY",
            "IDENTITYCOL","IDENTITY_INSERT","IF","IN","INDEX","INNER","INSERT","INTERSECT","INTO","IS","JOIN",
            "KEY","KILL","LEFT","LIKE","LINENO","LOAD","MERGE","NATIONAL","NOCHECK","NONCLUSTERED","NOT","NULL",
            "NULLIF","OF","OFF","OFFSETS","ON","OPEN","OPENDATASOURCE","OPENQUERY","OPENROWSET","OPENXML",
            "OPTION","OR","ORDER","OUTER","OVER","PERCENT","PIVOT","PLAN","PRECISION","PRIMARY","PRINT","PROC",
            "PROCEDURE","PUBLIC","RAISERROR","READ","READTEXT","RECONFIGURE","REFERENCES","REPLICATION","RESTORE",
            "RESTRICT","RETURN","REVERT","REVOKE","RIGHT","ROLLBACK","ROWCOUNT","ROWGUIDCOL","RULE","SAVE","SCHEMA",
            "SECURITYAUDIT","SELECT","SEMANTICKEYPHRASETABLE","SEMANTICSIMILARITYDETAILSTABLE","SEMANTICSIMILARITYTABLE",
            "SESSION_USER","SET","SETUSER","SHUTDOWN","SOME","STATISTICS","SYSTEM_USER","TABLE","TABLESAMPLE","TEXTSIZE",
            "THEN","TO","TOP","TRAN","TRANSACTION","TRIGGER","TRUNCATE","TRY_CONVERT","TSEQUAL","UNION","UNIQUE","UNPIVOT",
            "UPDATE","UPDATETEXT","USE","USER","VALUES","VARYING","VIEW","WAITFOR","WHEN","WHERE","WHILE","WITH","WITHIN GROUP",
            "WRITETEXT"
        };

    private static bool IsReserved(string s) => Reserved.Contains(s);
}
