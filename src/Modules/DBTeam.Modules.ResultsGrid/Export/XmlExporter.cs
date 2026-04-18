using System;
using System.Data;
using System.IO;
using System.Xml;

namespace DBTeam.Modules.ResultsGrid.Export;

public sealed class XmlExporter : IResultExporter
{
    public ExportFormat Format => ExportFormat.Xml;
    public string Extension => "xml";
    public string Filter => "XML (*.xml)|*.xml";

    public void Export(DataTable table, Stream output)
    {
        var settings = new XmlWriterSettings { Indent = true, Encoding = System.Text.Encoding.UTF8 };
        using var w = XmlWriter.Create(output, settings);
        w.WriteStartDocument();
        w.WriteStartElement("rows");
        foreach (DataRow row in table.Rows)
        {
            w.WriteStartElement("row");
            foreach (DataColumn col in table.Columns)
            {
                w.WriteStartElement(SanitizeName(col.ColumnName));
                var v = row[col];
                if (v is null or DBNull) w.WriteAttributeString("null", "true");
                else w.WriteString(FormatValue(v));
                w.WriteEndElement();
            }
            w.WriteEndElement();
        }
        w.WriteEndElement();
        w.WriteEndDocument();
    }

    private static string SanitizeName(string n)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var c in n)
        {
            if (char.IsLetterOrDigit(c) || c == '_') sb.Append(c);
            else sb.Append('_');
        }
        var s = sb.ToString();
        if (string.IsNullOrEmpty(s) || char.IsDigit(s[0])) s = "_" + s;
        return s;
    }

    private static string FormatValue(object v) => v switch
    {
        DateTime dt => dt.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
        DateTimeOffset dto => dto.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
        byte[] ba => Convert.ToBase64String(ba),
        _ => Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture) ?? ""
    };
}
