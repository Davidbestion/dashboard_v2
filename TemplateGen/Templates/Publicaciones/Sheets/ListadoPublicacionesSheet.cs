using ClosedXML.Excel;
using TemplateGen.Core.Interfaces;

namespace TemplateGen.Templates;

/// <summary>
/// Primera hoja del anexo de publicaciones.
/// Contiene únicamente la tabla resumen de métricas para completado manual.
/// </summary>
public sealed class ListadoPublicacionesSheet : ISheetTemplate
{
    public string Name => "Listado de las publicaciones";
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
        WriteSummaryTable(ws);
    }

    private static void ApplyLayout(IXLWorksheet ws)
    {
        ws.Column(1).Width = 57.875;
        ws.Column(2).Width = 14.5;
        ws.Column(3).Width = 14.5;
        ws.Column(4).Width = 28.25;
    }

    private static void WriteSummaryTable(IXLWorksheet ws)
    {
        var headers = new[]
        {
            "Descripción",
            "Compromiso",
            "Publicados",
            "Total y porcentaje de cumplimiento.",
        };

        PublicationSheetHelper.WriteHeaderRow(ws, 1, headers);
        ws.Row(1).Height = 48;

        var descriptions = new Dictionary<int, string>
        {
            [2]  = "grupo 1",
            [3]  = "grupo 2",
            [4]  = "grupo 3",
            [5]  = "grupo 4",
            [6]  = "capitulo de libros",
            [7]  = "libros y monografias",
            [8]  = "Cantidad de publicaciones científicas en colaboración con autores extranjeros.",
            [9]  = "Cantidad de publicaciones científicas en colaboración donde el autor para correspondencia es de la UH.",
            [10] = "Cantidad de artículos de revista de divulgación.",
            [11] = "Cantidad de artículos en medios de prensa (digitales o impresos) y blogs que publican sobre resultados de la actividad científica",
            [12] = "Cantidad de publicaciones en coautoría con el sector productivo y de los servicios.",
            [13] = "Cantidad de ponencias de eventos nacionales e internacionales publicadas con ISSN o ISBN",
        };

        foreach (var (row, text) in descriptions)
        {
            ws.Cell(row, 1).Value = text;
            PublicationSheetHelper.ApplyTextStyle(ws.Cell(row, 1));

            for (int col = 1; col <= 4; col++)
                PublicationSheetHelper.ApplyThinBorder(ws.Cell(row, col));
        }

        ws.Row(11).Height = 32.25;
        ws.Row(13).Height = 32.25;

        // Columna "Publicados" (col 3): expresiones template que ClosedXML.Report
        // sustituirá con los conteos calculados por AnexoPublicacionesReport.
        var countExpressions = new Dictionary<int, string>
        {
            [2]  = "{{G1Count}}",
            [3]  = "{{G2Count}}",
            [4]  = "{{G3Count}}",
            [5]  = "{{G4Count}}",
            [6]  = "{{CapitulosCount}}",
            [7]  = "{{LibrosMonografiasCount}}",
            [10] = "{{ArticulosDivulgacionCount}}",
        };

        foreach (var (row, expr) in countExpressions)
        {
            var cell = ws.Cell(row, 3);
            cell.Value = expr;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
        }
    }
}
