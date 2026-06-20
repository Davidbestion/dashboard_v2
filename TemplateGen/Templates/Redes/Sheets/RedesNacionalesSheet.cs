using TemplateGen.Core.Base;
using ClosedXML.Excel;

namespace TemplateGen.Templates;

public class RedesNacionalesSheet : SheetTemplateBase
{
    public override string Name => "Redes Nacionales";
    public override string Title => "REDES NACIONALES";
    public override string RangeName => "RedesNacionales";

    public override string[] Headers => new[]
    {
        "Área",
        "Nombre de la Red Nacional",
        "Centro que Coordina",
        "Cantidad de Profesores que Participan",
    };

    public override IEnumerable<(int Col, string Expression)> TemplateCells => new[]
    {
        (1, "{{item.Area}}"),
        (2, "{{item.Nombre}}"),
        (3, "{{item.CentroCoordina}}"),
        (4, "{{item.CantidadProfesores}}"),
    };

    protected override void PostGenerate(IXLWorksheet ws)
    {
        ws.Column(1).Width = 25;
        ws.Column(2).Width = 35;
        ws.Column(3).Width = 30;
        ws.Column(4).Width = 18;
        ws.SheetView.FreezeRows(4);
    }
}
