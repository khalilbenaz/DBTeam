using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DBTeam.Core.Abstractions;
using DBTeam.Core.Events;
using DBTeam.Core.Infrastructure;
using DBTeam.Core.Models;
using DBTeam.Modules.QueryEditor.Intellisense;
using DBTeam.Modules.QueryEditor.ViewModels;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Rendering;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DBTeam.Modules.QueryEditor.Views;

public partial class QueryEditorView : UserControl
{
    private CompletionWindow? _completionWindow;
    private SqlCompletionProvider? _provider;
    private readonly DispatcherTimer _validationTimer = new() { Interval = TimeSpan.FromMilliseconds(700) };
    private ErrorRenderer? _errorRenderer;

    public QueryEditorView()
    {
        InitializeComponent();
        _provider = ServiceLocator.TryGet<SqlCompletionProvider>();
        DataContextChanged += OnDataContextChanged;
        Editor.TextChanged += OnEditorTextChanged;
        Editor.KeyDown += OnEditorKeyDown;
        Editor.TextArea.TextEntered += TextArea_TextEntered;
        Editor.TextArea.TextEntering += TextArea_TextEntering;

        _errorRenderer = new ErrorRenderer();
        Editor.TextArea.TextView.BackgroundRenderers.Add(_errorRenderer);
        _validationTimer.Tick += (_, _) => { _validationTimer.Stop(); Validate(); };
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
        _validationTimer.Stop(); _validationTimer.Start();
    }

    private void Validate()
    {
        if (_errorRenderer is null) return;
        _errorRenderer.Errors.Clear();
        try
        {
            var parser = new TSql160Parser(true);
            using var sr = new StringReader(Editor.Text);
            parser.Parse(sr, out var errors);
            foreach (var err in errors)
            {
                try
                {
                    var line = Editor.Document.GetLineByNumber(err.Line);
                    int start = line.Offset + Math.Max(0, err.Column - 1);
                    int end = Math.Min(start + 20, line.EndOffset);
                    _errorRenderer.Errors.Add((start, end, err.Message));
                }
                catch { }
            }
        }
        catch { }
        Editor.TextArea.TextView.InvalidateLayer(KnownLayer.Selection);
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
        else if (e.Key == Key.F12)
        {
            _ = GoToDefinitionAsync();
            e.Handled = true;
        }
        else if (e.Key == Key.Tab)
        {
            if (TryExpandSnippet()) e.Handled = true;
        }
    }

    private bool TryExpandSnippet()
    {
        int caret = Editor.CaretOffset;
        int start = FindWordStart(caret);
        if (start == caret) return false;
        var word = Editor.Document.GetText(start, caret - start);
        var snip = SqlSnippets.All.FirstOrDefault(s => string.Equals(s.Trigger, word, StringComparison.OrdinalIgnoreCase));
        if (snip is null) return false;
        Editor.Document.Replace(start, caret - start, string.Format(snip.Body, "Table", "Column", "Value", "Id", "1"));
        return true;
    }

    private async System.Threading.Tasks.Task GoToDefinitionAsync()
    {
        if (DataContext is not QueryEditorViewModel vm || vm.Connection is null || string.IsNullOrEmpty(vm.Database)) return;
        var ident = GetIdentifierUnderCaret();
        if (string.IsNullOrEmpty(ident)) return;

        string schema = "dbo", name = ident;
        var parts = ident.Split('.');
        if (parts.Length >= 2) { schema = parts[^2].Trim('[', ']'); name = parts[^1].Trim('[', ']'); }

        var meta = ServiceLocator.TryGet<IDatabaseMetadataService>();
        var bus = ServiceLocator.TryGet<IEventBus>();
        if (meta is null || bus is null) return;

        string sql = "";
        foreach (var kind in new[] { DbObjectKind.Table, DbObjectKind.View, DbObjectKind.StoredProcedure, DbObjectKind.Function })
        {
            try
            {
                sql = await meta.ScriptObjectAsync(vm.Connection, vm.Database!, schema, name, kind);
                if (!string.IsNullOrWhiteSpace(sql)) break;
            }
            catch { }
        }
        if (string.IsNullOrWhiteSpace(sql)) return;
        bus.Publish(new OpenQueryEditorRequest { Connection = vm.Connection, Database = vm.Database, InitialSql = sql });
    }

    private string? GetIdentifierUnderCaret()
    {
        var doc = Editor.Document;
        int caret = Editor.CaretOffset;
        int start = caret, end = caret;
        while (start > 0)
        {
            char ch = doc.GetCharAt(start - 1);
            if (char.IsLetterOrDigit(ch) || ch == '_' || ch == '.' || ch == '[' || ch == ']') start--;
            else break;
        }
        while (end < doc.TextLength)
        {
            char ch = doc.GetCharAt(end);
            if (char.IsLetterOrDigit(ch) || ch == '_' || ch == '.' || ch == '[' || ch == ']') end++;
            else break;
        }
        if (start == end) return null;
        return doc.GetText(start, end - start);
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
                // Alias resolution: if an alias matches, substitute the real schema+table
                var aliases = SqlCompletionProvider.ExtractAliases(Editor.Text);
                if (aliases.TryGetValue(table, out var resolved)) { schema = resolved.schema; table = resolved.table; }
                var cols = await _provider.GetColumnsForTableAsync(vm.Connection, vm.Database!, schema, table);
                foreach (var col in cols) data.Add(new SqlCompletionItem(col, CompletionKind.Column));
                if (data.Count == 0) { _completionWindow.Close(); return; }
                _completionWindow.Show();
                return;
            }
        }

        foreach (var kw in SqlCompletionProvider.Keywords)
            data.Add(new SqlCompletionItem(kw, CompletionKind.Keyword));

        if (vm.Connection is not null && !string.IsNullOrEmpty(vm.Database))
        {
            foreach (var t in await _provider.GetTablesAsync(vm.Connection, vm.Database!))
                data.Add(new SqlCompletionItem(t, CompletionKind.Table));
            foreach (var v in await _provider.GetViewsAsync(vm.Connection, vm.Database!))
                data.Add(new SqlCompletionItem(v, CompletionKind.View));
            foreach (var p in await _provider.GetProceduresAsync(vm.Connection, vm.Database!))
                data.Add(new SqlCompletionItem(p, CompletionKind.Procedure));
            foreach (var f in await _provider.GetFunctionsAsync(vm.Connection, vm.Database!))
                data.Add(new SqlCompletionItem(f, CompletionKind.Function));
        }

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
