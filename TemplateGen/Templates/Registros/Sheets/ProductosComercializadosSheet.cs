using ClosedXML.Excel;
using TemplateGen.Core.Interfaces;
using System.Collections.Generic;

namespace TemplateGen.Templates;

/// <summary>
/// Hoja agrupada por tipo de producto. Define dos rangos nombrados:
/// - "ProductosTipos" para la fila prototipo del tipo.
/// - "ProductosTipos.Productos" para la fila prototipo de cada producto dentro de un tipo.
/// Esto permite que la generación posterior (ClosedXML.Report o una rutina manual)
/// expanda tipos y sus productos según el modelo de datos.
/// </summary>
public sealed class ProductosComercializadosSheet : ISheetTemplate
{
    public string Name => "ProductosComercializados";
    public string Title => "5. Productos Comercializados";
    public string[] Headers => System.Array.Empty<string>();
    public string RangeName => string.Empty;
    public int StartRow => 0;
    public int StartCol => 1;
    public int EndRowOffset => 0;
    public IEnumerable<(int Col, string Expression)> TemplateCells => System.Linq.Enumerable.Empty<(int, string)>();

    public void Generate(IXLWorkbook workbook)
    {
        var ws = workbook.Worksheets.Add(Name);

        PublicationSheetHelper.WriteMergedTextRow(ws, 1, 1, 3, Title, bold: true);

        // Prototype for a product type (outer list)
        // Row 4: Tipo (in first column only)
        ws.Cell(4, 1).Value = "{{item.TipoProductoComercializadoNombre}}";
        PublicationSheetHelper.ApplyTextStyle(ws.Cell(4, 1), bold: true);
        PublicationSheetHelper.ApplyThinBorder(ws.Cell(4, 1));

        // Row 5: headers for products under the type (first column intentionally empty)
        PublicationSheetHelper.WriteHeaderRow(ws, 5, new[] { "", "Nombre", "Empresa o Institución" });

        // Row 6: product prototype (inner list)
        PublicationSheetHelper.WriteTemplateRange(
            ws,
            "ProductosTipos.Productos",
            6,
            3,
            new List<(int, string)>
            {
                (1, ""),
                (2, "{{item.Titulo}}"),
                (3, "{{item.InstitutionNombre}}"),
            });

        // Define outer range for types (type row + header row)
        var outerRange = ws.Range(4, 1, 5, 3);
        ws.Workbook.DefinedNames.Add("ProductosTipos", outerRange);

        // Layout
        ws.Column(1).Width = 30;   // type column
        ws.Column(2).Width = 60;
        ws.Column(3).Width = 40;
        ws.SheetView.FreezeRows(4);
    }
}
