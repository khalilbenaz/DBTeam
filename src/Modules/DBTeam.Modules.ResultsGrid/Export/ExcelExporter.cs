using System.Data;
using System.IO;
using OfficeOpenXml;

namespace DBTeam.Modules.ResultsGrid.Export;

public sealed class ExcelExporter : IResultExporter
{
    static ExcelExporter()
    {
        ExcelPackage.License.SetNonCommercialPersonal("DB TEAM");
    }

    public ExportFormat Format => ExportFormat.Excel;
    public string Extension => "xlsx";
    public string Filter => "Excel (*.xlsx)|*.xlsx";

    public void Export(DataTable table, Stream output)
    {
        using var pkg = new ExcelPackage();
        var ws = pkg.Workbook.Worksheets.Add(string.IsNullOrEmpty(table.TableName) ? "Results" : table.TableName);
        ws.Cells["A1"].LoadFromDataTable(table, PrintHeaders: true);
        using (var header = ws.Cells[1, 1, 1, table.Columns.Count])
        {
            header.Style.Font.Bold = true;
            header.AutoFilter = true;
        }
        ws.View.FreezePanes(2, 1);
        ws.Cells.AutoFitColumns(10, 80);
        pkg.SaveAs(output);
    }
}
