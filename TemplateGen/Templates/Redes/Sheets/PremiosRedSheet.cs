using ClosedXML.Excel;

namespace TemplateGen.Templates;

public class PremiosRedSheet : UniversitariaSheetBase
{
    public override string Name => "Premios y Reconocimientos";
    public override string Title => "PREMIOS Y RECONOCIMIENTOS";
    public override string RangeName => "PremiosRed";

    public override string[] Headers => new[]
    {
        "Nombre del Premio",
        "Otorgado a",
        "Nacional",
        "Internacional",
        "Fecha",
    };

    public override IEnumerable<(int Col, string Expression)> TemplateCells => new[]
    {
        (1, "{{item.NombrePremio}}"),
        (2, "{{item.OtorgadoA}}"),
        (3, "{{item.Nacional}}"),
        (4, "{{item.Internacional}}"),
        (5, "{{item.Fecha}}"),
    };

    protected override void PostGenerateSheet(IXLWorksheet ws)
    {
        ws.Column(1).Width = 35;
        ws.Column(2).Width = 30;
        ws.Column(3).Width = 12;
        ws.Column(4).Width = 14;
        ws.Column(5).Width = 15;
        ws.SheetView.FreezeRows(4);
    }
}
