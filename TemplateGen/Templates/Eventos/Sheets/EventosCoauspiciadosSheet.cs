using ClosedXML.Excel;
using TemplateGen.Core.Interfaces;

namespace TemplateGen.Templates;

/// <summary>
/// Hoja 3 del anexo 3: eventos coauspiciados por el área.
/// Se rellena manualmente — el dominio actual no modela coauspicio.
/// </summary>
public sealed class EventosCoauspiciadosSheet : ISheetTemplate
{
    public string Name => "3. Eventos Coauspiciados";
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
        ws.Column(1).Width = 38;
        ws.Column(2).Width = 38;
        ws.Column(3).Width = 16;
        ws.Column(4).Width = 16;

        EventosSheetHelper.WriteHeaderRow(
            ws, 1,
            ["Evento coauspiciado", "Institución externa a la UH responsable del evento", "Internacional", "Nacional"]);

        EventosSheetHelper.WriteTemplateRange(
            ws,
            "EventosCoauspiciados",
            2,
            4,
            [
                (1, "{{item.EventoCoauspiciado}}"),
                (2, "{{item.InstitucionExternaResponsable}}"),
                (3, "{{item.Internacional}}"),
                (4, "{{item.Nacional}}"),
            ]);
    }
}
