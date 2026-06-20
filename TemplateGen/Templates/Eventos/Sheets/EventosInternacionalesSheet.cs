using ClosedXML.Excel;
using TemplateGen.Core.Interfaces;

namespace TemplateGen.Templates;

/// <summary>
/// Hoja 1 del anexo 3: eventos científicos internacionales (en el extranjero o en Cuba).
/// </summary>
public sealed class EventosInternacionalesSheet : ISheetTemplate
{
    public string Name => "1. Eventos Internacionales";
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
        ws.Column(1).Width = 50;
        ws.Column(2).Width = 30;
        ws.Column(3).Width = 18;

        EventosSheetHelper.WriteHeaderRow(
            ws, 1,
            ["Nombre del evento internacional", "País, si fue en el extranjero", "En Cuba"]);

        EventosSheetHelper.WriteTemplateRange(
            ws,
            "EventosInternacionales",
            2,
            3,
            [
                (1, "{{item.NombreEventoInternacional}}"),
                (2, "{{item.PaisSiFueEnElExtranjero}}"),
                (3, "{{item.EnCuba}}"),
            ]);
    }
}
