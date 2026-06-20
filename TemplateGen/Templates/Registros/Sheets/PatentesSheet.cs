using TemplateGen.Core.Base;
using ClosedXML.Excel;

namespace TemplateGen.Templates;

public class PatentesSheet : SheetTemplateBase
{
    public override string Name => "Patentes";
    public override string Title => "1. Patentes";
    public override string[] Headers => new[]
    {
        "Título de Patentes de invención",
        "Número de solicitud o concesión",
        "Nacional",
        "Extranjero",
    };
    public override string RangeName => "Patentes";

    public override IEnumerable<(int Col, string Expression)> TemplateCells => new[]
    {
        (1, "{{item.Titulo}}"),
        (2, "{{item.NumeroSolicitudConcesion}}"),
        (3, "{{item.EsNacional ? \"X\" : \"\"}}"),
        (4, "{{item.EsNacional ? \"\" : \"X\"}}"),
    };

    protected override void PostGenerate(IXLWorksheet ws)
    {
        ws.Column(1).Width = 60;
        ws.Column(2).Width = 30;
        ws.Column(3).Width = 12;
        ws.Column(4).Width = 12;
        ws.SheetView.FreezeRows(4);
    }
}
