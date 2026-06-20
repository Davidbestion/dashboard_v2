using ClosedXML.Excel;
using TemplateGen.Core.Interfaces;

namespace TemplateGen.Templates;

/// <summary>
/// Hoja 5 del anexo 3: resumen de ponencias presentadas (conteos por categoría).
/// Usa variables escalares en lugar de rangos dinámicos.
/// </summary>
public sealed class PonenciasResumenSheet : ISheetTemplate
{
    public string Name => "5. Ponencias - Resumen";
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
        ws.Column(1).Width = 52;
        ws.Column(2).Width = 22;
        ws.Column(3).Width = 28;
        ws.Column(4).Width = 32;

        EventosSheetHelper.WriteHeaderRow(
            ws, 1,
            ["", "Cantidad de ponencias", "Cantidad donde 1er autor es del área", "Cantidad con autores de otras áreas de la UH"]);

        var rows = new[]
        {
            (2,  "En eventos internacionales en el extranjero",       "{{PonenciasInternacionalesExtranjero}}", false),
            (3,  "En eventos internacionales en Cuba",                "{{PonenciasInternacionalesCuba}}",      false),
            (4,  "En eventos nacionales en Cuba",                     "{{PonenciasNacionalesCuba}}",           false),
            (5,  "En actividades científicas celebradas en la UH",    "{{PonenciasActividadesUH}}",            false),
            (6,  "TOTAL",                                             "{{PonenciasTotal}}",                    true),
        };

        foreach (var (row, label, expr, bold) in rows)
        {
            ws.Cell(row, 1).Value = label;
            EventosSheetHelper.ApplyTextStyle(ws.Cell(row, 1), bold: bold);

            ws.Cell(row, 2).Value = expr;
            EventosSheetHelper.ApplyTextStyle(ws.Cell(row, 2), bold: bold, center: true);

            for (int col = 1; col <= 4; col++)
            {
                EventosSheetHelper.ApplyThinBorder(ws.Cell(row, col));
            }
        }
    }
}
