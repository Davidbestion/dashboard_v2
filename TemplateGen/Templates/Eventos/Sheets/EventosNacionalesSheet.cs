using ClosedXML.Excel;
using TemplateGen.Core.Interfaces;

namespace TemplateGen.Templates;

/// <summary>
/// Hoja 2 del anexo 3: eventos nacionales en Cuba.
/// </summary>
public sealed class EventosNacionalesSheet : ISheetTemplate
{
    public string Name => "2. Eventos Nacionales";
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
        ws.Column(1).Width = 55;
        ws.Column(2).Width = 40;

        EventosSheetHelper.WriteHeaderRow(
            ws, 1,
            ["Nombre del evento nacional en Cuba", "Institución que lo organizó"]);

        EventosSheetHelper.WriteTemplateRange(
            ws,
            "EventosNacionales",
            2,
            2,
            [
                (1, "{{item.NombreEventoNacional}}"),
                (2, "{{item.InstitucionQueLoOrganizo}}"),
            ]);
    }
}
