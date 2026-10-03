using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace DBTeam.Core.Sql;

/// <summary>Un batch T-SQL à envoyer tel quel au serveur, à exécuter <see cref="Repeat"/> fois (« GO n »).</summary>
public sealed record SqlBatch(string Sql, int Repeat, int StartLine);

/// <summary>
/// Découpe un script sur les séparateurs de batch <c>GO</c> comme le font SSMS/sqlcmd : <c>GO</c> est une
/// commande de l'outil client (pas du T-SQL), reconnue seulement quand elle est seule sur sa ligne, sans
/// distinction de casse, éventuellement suivie d'un compteur (<c>GO 5</c>) et d'un commentaire <c>--</c>.
/// Un <c>GO</c> à l'intérieur d'une chaîne, d'un identifiant entre [] ou "", ou d'un commentaire
/// (<c>--</c>, <c>/* */</c> imbriqué) n'est pas un séparateur.
/// Le découpage est fait par un petit analyseur lexical plutôt que par ScriptDom : ce dernier ne connaît
/// pas GO et refuse de parser un script qui en contient.
/// </summary>
public static class SqlBatchSplitter
{
    private static readonly Regex GoLine = new(
        @"^\s*GO(?:\s+(?<n>\d+))?\s*(?:--.*)?$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private enum State { Normal, String, Bracket, DoubleQuote, BlockComment }

    public static IReadOnlyList<SqlBatch> Split(string script)
    {
        ArgumentNullException.ThrowIfNull(script);
        var result = new List<SqlBatch>();
        var current = new StringBuilder();
        var state = State.Normal;
        var depth = 0;
        var currentStartLine = 1;
        var lineNo = 0;

        void Flush(int repeat)
        {
            var text = current.ToString();
            current.Clear();
            var trimmed = text.Trim();
            if (trimmed.Length == 0) return;
            var leadingNewlines = 0;
            foreach (var ch in text)
            {
                if (ch == '\n') leadingNewlines++;
                else if (!char.IsWhiteSpace(ch)) break;
            }
            result.Add(new SqlBatch(trimmed, repeat, currentStartLine + leadingNewlines));
        }

        foreach (var rawLine in script.Split('\n'))
        {
            lineNo++;
            var line = rawLine.EndsWith('\r') ? rawLine[..^1] : rawLine;

            if (state == State.Normal)
            {
                var m = GoLine.Match(line);
                if (m.Success)
                {
                    var repeat = 1;
                    var ok = true;
                    if (m.Groups["n"].Success)
                        ok = int.TryParse(m.Groups["n"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out repeat) && repeat > 0;
                    if (ok)
                    {
                        Flush(repeat);
                        currentStartLine = lineNo + 1;
                        continue;
                    }
                }
            }

            current.Append(rawLine).Append('\n');
            Scan(line, ref state, ref depth);
        }

        // le dernier '\n' ajouté n'existe pas dans le script d'origine : sans effet car Flush() fait Trim()
        Flush(1);
        return result;
    }

    private static void Scan(string line, ref State state, ref int depth)
    {
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            var next = i + 1 < line.Length ? line[i + 1] : '\0';
            switch (state)
            {
                case State.Normal:
                    if (c == '-' && next == '-') return; // commentaire de ligne : le reste de la ligne est ignoré
                    if (c == '/' && next == '*') { state = State.BlockComment; depth = 1; i++; }
                    else if (c == '\'') state = State.String;
                    else if (c == '[') state = State.Bracket;
                    else if (c == '"') state = State.DoubleQuote;
                    break;
                case State.String:
                    if (c == '\'') { if (next == '\'') i++; else state = State.Normal; }
                    break;
                case State.Bracket:
                    if (c == ']') { if (next == ']') i++; else state = State.Normal; }
                    break;
                case State.DoubleQuote:
                    if (c == '"') { if (next == '"') i++; else state = State.Normal; }
                    break;
                case State.BlockComment:
                    if (c == '/' && next == '*') { depth++; i++; }
                    else if (c == '*' && next == '/') { depth--; i++; if (depth == 0) state = State.Normal; }
                    break;
            }
        }
    }
}
