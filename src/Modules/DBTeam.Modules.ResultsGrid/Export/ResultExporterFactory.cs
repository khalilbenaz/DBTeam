namespace DBTeam.Modules.ResultsGrid.Export;

public static class ResultExporterFactory
{
    public static IResultExporter Create(ExportFormat format) => format switch
    {
        ExportFormat.Csv => new CsvExporter(),
        ExportFormat.Excel => new ExcelExporter(),
        ExportFormat.Json => new JsonExporter(),
        ExportFormat.Xml => new XmlExporter(),
        _ => new CsvExporter()
    };
}
