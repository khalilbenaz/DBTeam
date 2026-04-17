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
        textArea.Document.Replace(completionSegment, Text);
    }
}
