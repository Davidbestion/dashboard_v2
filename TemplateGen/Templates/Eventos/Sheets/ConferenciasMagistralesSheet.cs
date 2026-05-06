using ClosedXML.Excel;
using TemplateGen.Core.Interfaces;

namespace TemplateGen.Templates;

/// <summary>
/// Hoja 6 del anexo 3: conferencias magistrales impartidas.
/// El dominio actual no diferencia conferencias magistrales; queda para llenado manual.
/// </summary>
public sealed class ConferenciasMagistralesSheet : ISheetTemplate
{
    public string Name => "6. Conferencias Magistrales";
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
        ws.Column(2).Width = 20;

        EventosSheetHelper.WriteHeaderRow(ws, 1, ["", "Cantidad"]);

        var labels = new[]
        {
            (2, "En eventos internacionales en el extranjero"),
            (3, "En eventos internacionales en Cuba"),
            (4, "En eventos nacionales en Cuba"),
            (5, "En actividades científicas celebradas en la UH"),
            (6, "En instituciones extranjeras"),
            (7, "En instituciones cubanas"),
            (8, "TOTAL"),
        };

        foreach (var (row, label) in labels)
        {
            ws.Cell(row, 1).Value = label;
            EventosSheetHelper.ApplyTextStyle(ws.Cell(row, 1), bold: row == 8);

            for (int col = 1; col <= 2; col++)
            {
                EventosSheetHelper.ApplyThinBorder(ws.Cell(row, col));
            }
        }
    }
}
