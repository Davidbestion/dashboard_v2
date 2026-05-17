using ClosedXML.Excel;

namespace TemplateGen.Templates;

public class EventosRedSheet : UniversitariaSheetBase
{
    public override string Name => "Eventos Científicos";
    public override string Title => "EVENTOS CIENTÍFICOS Y TALLERES COORDINADOS POR LA RED";
    public override string RangeName => "EventosRed";

    public override string[] Headers => new[]
    {
        "Nombre del Evento",
        "Fecha / Lugar",
        "Áreas Participantes",
    };

    public override IEnumerable<(int Col, string Expression)> TemplateCells => new[]
    {
        (1, "{{item.Nombre}}"),
        (2, "{{item.FechaLugar}}"),
        (3, "{{item.AreasParticipantes}}"),
    };

    protected override void PostGenerateSheet(IXLWorksheet ws)
    {
        ws.Column(1).Width = 40;
        ws.Column(2).Width = 25;
        ws.Column(3).Width = 35;
        ws.SheetView.FreezeRows(4);
    }
}
