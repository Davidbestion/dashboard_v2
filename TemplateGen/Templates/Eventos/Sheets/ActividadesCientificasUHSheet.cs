using ClosedXML.Excel;
using TemplateGen.Core.Interfaces;

namespace TemplateGen.Templates;

/// <summary>
/// Hoja 4 del anexo 3: actividades científicas organizadas por el área en la UH.
/// Se rellena manualmente — el dominio actual no modela actividades internas.
/// </summary>
public sealed class ActividadesCientificasUHSheet : ISheetTemplate
{
    public string Name => "4. Actividades en la UH";
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
        ws.Column(1).Width = 70;

        EventosSheetHelper.WriteHeaderRow(ws, 1, ["Actividad científica"]);

        EventosSheetHelper.WriteTemplateRange(
            ws,
            "ActividadesCientificasUH",
            2,
            1,
            [
                (1, "{{item.ActividadCientifica}}"),
            ]);
    }
}
