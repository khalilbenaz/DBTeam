using System.Data;
using System.IO;

namespace DBTeam.Modules.ResultsGrid.Export;

public enum ExportFormat { Csv, Excel, Json, Xml }

public interface IResultExporter
{
    ExportFormat Format { get; }
    string Extension { get; }
    string Filter { get; }
    void Export(DataTable table, Stream output);
}
