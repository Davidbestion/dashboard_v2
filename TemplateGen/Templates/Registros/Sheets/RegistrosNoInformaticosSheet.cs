using TemplateGen.Core.Base;
using ClosedXML.Excel;

namespace TemplateGen.Templates;

public class RegistrosNoInformaticosSheet : SheetTemplateBase
{
    public override string Name => "RegistrosNoInformaticos";
    public override string Title => "3. Registros No Informáticos";
    public override string[] Headers => new[]
    {
        "Título del Registro",
        "Institución que lo otorga",
        "Número de certificado",
        "País",
    };
    public override string RangeName => "RegistrosNoInformaticos";

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
