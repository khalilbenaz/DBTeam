using System;
using System.Collections.Generic;
using System.Linq;

namespace DBTeam.Core.Sql;

/// <summary>
/// Équivalent de QUOTENAME(..., '[') : entoure un identifiant de crochets et double chaque ']'.
/// À utiliser pour TOUT identifiant (schéma, table, colonne, base, index...) interpolé dans du SQL
/// généré, sans quoi un objet nommé <c>a]b</c> casse le script et un nom hostile y injecte du SQL.
/// </summary>
public static class SqlIdentifier
{
    public static string Quote(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return "[" + name.Replace("]", "]]", StringComparison.Ordinal) + "]";
    }

    public static string Quote(string schema, string name) => Quote(schema) + "." + Quote(name);

    public static string QuoteList(IEnumerable<string> names, string separator = ",") =>
        string.Join(separator, names.Select(Quote));
}
