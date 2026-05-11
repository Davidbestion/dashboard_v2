using ClosedXML.Excel;

namespace TemplateGen.Templates;

public class ProyectosVinculadosSheet : UniversitariaSheetBase
{
    public override string Name => "Proyectos Vinculados";
    public override string Title => "PROYECTOS VINCULADOS";
    public override string RangeName => "ProyectosVinculados";

    public override string[] Headers => new[]
    {
        "Título del Proyecto",
        "UH",
        "Jefe de Proyecto (UH)",
        "Externos",
        "Jefe de Proyecto (Externo)",
    };

    public override IEnumerable<(int Col, string Expression)> TemplateCells => new[]
    {
        (1, "{{item.Titulo}}"),
        (2, "{{item.UH}}"),
        (3, "{{item.JefeProyectoUH}}"),
        (4, "{{item.Externos}}"),
        (5, "{{item.JefeProyectoExterno}}"),
    };

    protected override void PostGenerateSheet(IXLWorksheet ws)
    {
        ws.Column(1).Width = 40;
        ws.Column(2).Width = 20;
        ws.Column(3).Width = 25;
        ws.Column(4).Width = 20;
        ws.Column(5).Width = 25;
        ws.SheetView.FreezeRows(4);
    }
}
