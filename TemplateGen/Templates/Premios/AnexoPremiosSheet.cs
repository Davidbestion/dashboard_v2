using ClosedXML.Excel;
using TemplateGen.Core.Interfaces;

namespace TemplateGen.Templates;

/// <summary>
/// Hoja única del Anexo 5 de premios.
/// Se genera como shell visual con filas prototipo para que el backend
/// complete el cuerpo manualmente sin depender de rangos dinámicos anidados.
/// </summary>
public sealed class AnexoPremiosSheet : ISheetTemplate
{
    public string Name => "Premios";
    public string Title => string.Empty;
    public string[] Headers => [];
    public string RangeName => string.Empty;
    public int StartRow => 0;
    public int StartCol => 1;
    public int EndRowOffset => 0;
    public IEnumerable<(int Col, string Expression)> TemplateCells => [];

    public void Generate(IXLWorkbook workbook)
    {
        var ws = workbook.Worksheets.Add(Name);

        ApplyLayout(ws);
        WriteHeader(ws);
        WritePrototypeRows(ws);
    }

    private static void ApplyLayout(IXLWorksheet ws)
    {
        ws.Column(1).Width = 11.57;
        ws.Column(2).Width = 57.29;
        ws.Column(3).Width = 17.86;
    }

    private static void WriteHeader(IXLWorksheet ws)
    {
        ws.Cell(1, 1).Value = "Anexo5: Premios nacionales o internacionales";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 12;

        ws.Cell(2, 1).Value = "Incluir listado de los premios.";
        ws.Cell(2, 1).Style.Alignment.WrapText = true;
    }

    private static void WritePrototypeRows(IXLWorksheet ws)
    {
        WriteTypeRow(ws, 4, "1", "Tipo de premio");
        WriteAwardsHeaderRow(ws, 5);
        WriteAwardRow(ws, 6, "Título del premio", "Autores separados por coma");
    }

    private static void WriteTypeRow(IXLWorksheet ws, int row, string number, string typeName)
    {
        ws.Row(row).Height = 15.75;

        ws.Cell(row, 1).Value = number;
        ws.Cell(row, 2).Value = typeName;
        ws.Cell(row, 3).Value = string.Empty;

        ApplyBaseCellStyle(ws.Cell(row, 1), bold: false);
        ApplyBaseCellStyle(ws.Cell(row, 2), bold: true);
        ApplyBaseCellStyle(ws.Cell(row, 3), bold: false);
    }

    private static void WriteAwardsHeaderRow(IXLWorksheet ws, int row)
    {
        ws.Row(row).Height = 15.75;

        ws.Cell(row, 1).Value = string.Empty;
        ws.Cell(row, 2).Value = "Titulo";
        ws.Cell(row, 3).Value = "Autores";

        ApplyBaseCellStyle(ws.Cell(row, 1), bold: false);
        ApplyBaseCellStyle(ws.Cell(row, 2), bold: true);
        ApplyBaseCellStyle(ws.Cell(row, 3), bold: true);
    }

    private static void WriteAwardRow(IXLWorksheet ws, int row, string title, string authors)
    {
        ws.Row(row).Height = 15.75;

        ws.Cell(row, 1).Value = string.Empty;
        ws.Cell(row, 2).Value = title;
        ws.Cell(row, 3).Value = authors;

        ApplyBaseCellStyle(ws.Cell(row, 1), bold: false);
        ApplyBaseCellStyle(ws.Cell(row, 2), bold: false);
        ApplyBaseCellStyle(ws.Cell(row, 3), bold: false);
    }

    private static void ApplyBaseCellStyle(IXLCell cell, bool bold)
    {
        cell.Style.Font.FontName = "Times New Roman";
        cell.Style.Font.FontSize = 10;
        cell.Style.Font.Bold = bold;
        cell.Style.Alignment.WrapText = true;
        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
    }
}
