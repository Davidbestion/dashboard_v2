using TemplateGen.Core.Base;
using ClosedXML.Excel;

namespace TemplateGen.Templates;

public class RedesInternacionalesSheet : SheetTemplateBase
{
    public override string Name => "Redes Internacionales";
    public override string Title => "REDES INTERNACIONALES";
    public override string RangeName => "RedesInternacionales";

    public override string[] Headers => new[]
    {
        "Área",
        "Nombre de la Red Internacional",
        "País",
        "Coordinación",
        "Cantidad de Profesores que Participan",
    };

    public override IEnumerable<(int Col, string Expression)> TemplateCells => new[]
    {
        (1, "{{item.Area}}"),
        (2, "{{item.Nombre}}"),
        (3, "{{item.Pais}}"),
        (4, "{{item.Coordinacion}}"),
        (5, "{{item.CantidadProfesores}}"),
    };

    protected override void PostGenerate(IXLWorksheet ws)
    {
        ws.Column(1).Width = 25;
        ws.Column(2).Width = 35;
        ws.Column(3).Width = 20;
        ws.Column(4).Width = 25;
        ws.Column(5).Width = 18;
        ws.SheetView.FreezeRows(4);
    }
}
