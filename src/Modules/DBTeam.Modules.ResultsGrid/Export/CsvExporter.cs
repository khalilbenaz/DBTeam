using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;

namespace DBTeam.Modules.ResultsGrid.Export;

public sealed class CsvExporter : IResultExporter
{
    public ExportFormat Format => ExportFormat.Csv;
    public string Extension => "csv";
    public string Filter => "CSV (*.csv)|*.csv";

    public void Export(DataTable table, Stream output)
    {
        using var writer = new StreamWriter(output, new UTF8Encoding(true));
        for (int i = 0; i < table.Columns.Count; i++)
        {
            if (i > 0) writer.Write(',');
            writer.Write(Quote(table.Columns[i].ColumnName));
        }
        writer.WriteLine();

        foreach (DataRow row in table.Rows)
        {
            for (int i = 0; i < table.Columns.Count; i++)
            {
                if (i > 0) writer.Write(',');
                writer.Write(FormatValue(row[i]));
            }
            writer.WriteLine();
        }
    }

    private static string FormatValue(object? v) => v switch
    {
        null or DBNull => "",
        string s => Quote(s),
        DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
        DateTimeOffset dto => dto.ToString("O", CultureInfo.InvariantCulture),
        TimeSpan ts => ts.ToString("c", CultureInfo.InvariantCulture),
        IFormattable f => Quote(f.ToString(null, CultureInfo.InvariantCulture) ?? ""),
        _ => Quote(Convert.ToString(v, CultureInfo.InvariantCulture) ?? "")
    };

    private static string Quote(string s)
    {
        if (s.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0) return s;
        return "\"" + s.Replace("\"", "\"\"") + "\"";
    }
}
