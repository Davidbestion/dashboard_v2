using ClosedXML.Excel;

namespace TemplateGen.Templates;

public class PublicacionesRedSheet : UniversitariaSheetBase
{
    public override string Name => "Publicaciones";
    public override string Title => "PUBLICACIONES";
    public override string RangeName => "PublicacionesRed";

    public override string[] Headers => new[]
    {
        "Título",
        "Artículo",
        "Libro",
        "Autor",
    };

    public override IEnumerable<(int Col, string Expression)> TemplateCells => new[]
    {
        (1, "{{item.Titulo}}"),
        (2, "{{item.Articulo}}"),
        (3, "{{item.Libro}}"),
        (4, "{{item.Autor}}"),
    };

    protected override void PostGenerateSheet(IXLWorksheet ws)
    {
        ws.Column(1).Width = 40;
        ws.Column(2).Width = 20;
        ws.Column(3).Width = 20;
        ws.Column(4).Width = 30;
        ws.SheetView.FreezeRows(4);
    }
}
