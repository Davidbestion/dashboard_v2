using TemplateGen.Core.Base;
using ClosedXML.Excel;

namespace TemplateGen.Templates;

public class NormasSheet : SheetTemplateBase
{
    public override string Name => "Normas";
    public override string Title => "4. Normas";
    public override string[] Headers => new[]
    {
        "Título de la norma",
        "Tipo de norma",
        "Institución que la emite",
    };
    public override string RangeName => "Normas";

    public override IEnumerable<(int Col, string Expression)> TemplateCells => new[]
    {
        (1, "{{item.Titulo}}"),
        (2, "{{item.Tipo}}"),
        (3, "{{item.InstitutionNombre}}"),
    };

    protected override void PostGenerate(IXLWorksheet ws)
    {
        ws.Column(1).Width = 60;
        ws.Column(2).Width = 30;
        ws.Column(3).Width = 40;
        ws.SheetView.FreezeRows(4);
    }
}
