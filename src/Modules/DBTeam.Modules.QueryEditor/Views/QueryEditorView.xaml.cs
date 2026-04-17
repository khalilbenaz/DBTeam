using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DBTeam.Core.Infrastructure;
using DBTeam.Modules.QueryEditor.Intellisense;
using DBTeam.Modules.QueryEditor.ViewModels;
using ICSharpCode.AvalonEdit.CodeCompletion;

namespace DBTeam.Modules.QueryEditor.Views;

public partial class QueryEditorView : UserControl
{
    private CompletionWindow? _completionWindow;
    private SqlCompletionProvider? _provider;

    public QueryEditorView()
    {
        InitializeComponent();
        _provider = ServiceLocator.TryGet<SqlCompletionProvider>();
        DataContextChanged += OnDataContextChanged;
        Editor.TextChanged += OnEditorTextChanged;
        Editor.KeyDown += OnEditorKeyDown;
        Editor.TextArea.TextEntered += TextArea_TextEntered;
        Editor.TextArea.TextEntering += TextArea_TextEntering;
    }

    private void OnDataContextChanged(object? sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is QueryEditorViewModel vm)
        {
            Editor.Text = vm.Sql ?? string.Empty;
            vm.PropertyChanged += (_, a) =>
            {
                if (a.PropertyName == nameof(QueryEditorViewModel.Sql) && Editor.Text != vm.Sql)
                    Editor.Text = vm.Sql ?? string.Empty;
            };
        }
    }

    private void OnEditorTextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is QueryEditorViewModel vm && vm.Sql != Editor.Text)
            vm.Sql = Editor.Text;
    }

    private void OnEditorKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not QueryEditorViewModel vm) return;
        if (e.Key == Key.F5)
        {
            vm.ExecuteCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.K && Keyboard.Modifiers == ModifierKeys.Control)
        {
            vm.FormatSqlCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Space && Keyboard.Modifiers == ModifierKeys.Control)
        {
            ShowCompletion(force: true);
            e.Handled = true;
        }
    }

    private void TextArea_TextEntered(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        if (e.Text == ".")
        {
            ShowCompletion(afterDot: true);
            return;
        }
        if (e.Text.Length == 1 && (char.IsLetter(e.Text[0]) || e.Text[0] == '_'))
        {
            if (_completionWindow is null) ShowCompletion(force: false);
        }
    }

    private void TextArea_TextEntering(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        if (e.Text.Length > 0 && _completionWindow is not null)
        {
            if (!char.IsLetterOrDigit(e.Text[0]) && e.Text[0] != '_')
                _completionWindow.CompletionList.RequestInsertion(e);
        }
    }

    private async void ShowCompletion(bool force = false, bool afterDot = false)
    {
        if (DataContext is not QueryEditorViewModel vm) return;
        if (_provider is null) return;
        if (_completionWindow is not null) return;

        int wordStart = FindWordStart(Editor.CaretOffset);

        _completionWindow = new CompletionWindow(Editor.TextArea)
        {
            MaxHeight = 280,
            Width = 320,
            StartOffset = afterDot ? Editor.CaretOffset : wordStart
        };
        _completionWindow.Closed += (_, _) => _completionWindow = null;
        var data = _completionWindow.CompletionList.CompletionData;

        if (afterDot)
        {
            var before = ExtractIdentifierBeforeDot();
            if (!string.IsNullOrEmpty(before) && vm.Connection is not null && !string.IsNullOrEmpty(vm.Database))
            {
                var parts = before!.Split('.');
                string schema = parts.Length >= 2 ? parts[^2].Trim('[', ']') : "dbo";
                string table = parts[^1].Trim('[', ']');
                var cols = await _provider.GetColumnsForTableAsync(vm.Connection, vm.Database!, schema, table);
                foreach (var col in cols) data.Add(new SqlCompletionItem(col, CompletionKind.Column));
                if (data.Count == 0) { _completionWindow.Close(); return; }
                _completionWindow.Show();
                return;
            }
        }

        foreach (var kw in SqlCompletionProvider.Keywords)
            data.Add(new SqlCompletionItem(kw, CompletionKind.Keyword));

        if (data.Count == 0) { _completionWindow.Close(); return; }
        _completionWindow.Show();
    }

    private int FindWordStart(int offset)
    {
        var doc = Editor.Document;
        int start = offset;
        while (start > 0)
        {
            char ch = doc.GetCharAt(start - 1);
            if (char.IsLetterOrDigit(ch) || ch == '_') start--;
            else break;
        }
        return start;
    }

    private string? ExtractIdentifierBeforeDot()
    {
        int offset = Editor.CaretOffset - 1;
        if (offset < 0) return null;
        var doc = Editor.Document;
        int start = offset;
        while (start > 0)
        {
            char ch = doc.GetCharAt(start - 1);
            if (char.IsLetterOrDigit(ch) || ch == '_' || ch == '.' || ch == '[' || ch == ']') start--;
            else break;
        }
        var ident = doc.GetText(start, offset - start);
        return string.IsNullOrWhiteSpace(ident) ? null : ident;
    }
}
