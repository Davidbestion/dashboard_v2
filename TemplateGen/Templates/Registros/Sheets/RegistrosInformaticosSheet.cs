using TemplateGen.Core.Base;
using ClosedXML.Excel;

namespace TemplateGen.Templates;

public class RegistrosInformaticosSheet : SheetTemplateBase
{
    public override string Name => "RegistrosInformaticos";
    public override string Title => "2. Registros Informáticos";
    public override string[] Headers => new[]
    {
        "Título del Registro",
        "Institución que lo otorga",
        "Número de certificado",
        "País",
    };
    public override string RangeName => "RegistrosInformaticos";

    public override IEnumerable<(int Col, string Expression)> TemplateCells => new[]
    {
        (1, "{{item.Titulo}}"),
        (2, "{{item.InstitutionNombre}}"),
        (3, "{{item.NumeroCertificado}}"),
        (4, "{{item.CountryName}}"),
    };

    protected override void PostGenerate(IXLWorksheet ws)
    {
        for (int c = 1; c <= 4; c++) ws.Column(c).Width = 30;
        ws.SheetView.FreezeRows(4);
    }
}
