using System;
using System.Data;
using System.IO;
using System.Text.Json;

namespace DBTeam.Modules.ResultsGrid.Export;

public sealed class JsonExporter : IResultExporter
{
    public ExportFormat Format => ExportFormat.Json;
    public string Extension => "json";
    public string Filter => "JSON (*.json)|*.json";

    public void Export(DataTable table, Stream output)
    {
        using var writer = new Utf8JsonWriter(output, new JsonWriterOptions { Indented = true });
        writer.WriteStartArray();
        foreach (DataRow row in table.Rows)
        {
            writer.WriteStartObject();
            foreach (DataColumn col in table.Columns)
            {
                var v = row[col];
                writer.WritePropertyName(col.ColumnName);
                WriteValue(writer, v);
            }
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    private static void WriteValue(Utf8JsonWriter w, object v)
    {
        switch (v)
        {
            case null:
            case DBNull:
                w.WriteNullValue(); break;
            case bool b:
                w.WriteBooleanValue(b); break;
            case byte or sbyte or short or ushort or int or uint or long or ulong:
                w.WriteNumberValue(Convert.ToInt64(v, System.Globalization.CultureInfo.InvariantCulture)); break;
            case float f:
                w.WriteNumberValue(f); break;
            case double d:
                w.WriteNumberValue(d); break;
            case decimal m:
                w.WriteNumberValue(m); break;
            case DateTime dt:
                w.WriteStringValue(dt.ToString("O", System.Globalization.CultureInfo.InvariantCulture)); break;
            case DateTimeOffset dto:
                w.WriteStringValue(dto.ToString("O", System.Globalization.CultureInfo.InvariantCulture)); break;
            case Guid g:
                w.WriteStringValue(g); break;
            case byte[] ba:
                w.WriteStringValue(Convert.ToBase64String(ba)); break;
            default:
                w.WriteStringValue(Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture) ?? ""); break;
        }
    }
}
