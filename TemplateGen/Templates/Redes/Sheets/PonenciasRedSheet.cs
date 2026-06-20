using ClosedXML.Excel;

namespace TemplateGen.Templates;

public class PonenciasRedSheet : UniversitariaSheetBase
{
    public override string Name => "Ponencias";
    public override string Title => "PONENCIAS PRESENTADAS EN EVENTOS NACIONALES E INTERNACIONALES";
    public override string RangeName => "PonenciasRed";

    public override string[] Headers => new[]
    {
        "Título",
        "Evento Nacional",
        "Evento Internacional",
        "Autor",
    };

    public override IEnumerable<(int Col, string Expression)> TemplateCells => new[]
    {
        (1, "{{item.Titulo}}"),
        (2, "{{item.EventoNacional}}"),
        (3, "{{item.EventoInternacional}}"),
        (4, "{{item.Autor}}"),
    };

    protected override void PostGenerateSheet(IXLWorksheet ws)
    {
        ws.Column(1).Width = 40;
        ws.Column(2).Width = 30;
        ws.Column(3).Width = 30;
        ws.Column(4).Width = 30;
        ws.SheetView.FreezeRows(4);
    }
}
