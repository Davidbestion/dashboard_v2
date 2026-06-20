using ClosedXML.Excel;
using TemplateGen.Core.Interfaces;
using System.Collections.Generic;

namespace TemplateGen.Templates;

public class RegistrosSheet : ISheetTemplate
{
    public string Name => "Registros";
    public string Title => "2. Registros";
    public string[] Headers => System.Array.Empty<string>();
    public string RangeName => "Registros";
    public int StartRow => 0;
    public int StartCol => 1;
    public int EndRowOffset => 0;
    public IEnumerable<(int Col, string Expression)> TemplateCells => System.Linq.Enumerable.Empty<(int, string)>();

    public void Generate(IXLWorkbook workbook)
    {
        var ws = workbook.Worksheets.Add(Name);

        // Title
        PublicationSheetHelper.WriteMergedTextRow(ws, 1, 1, 4, Title, bold: true);

        // --- Registros Informáticos ---
        PublicationSheetHelper.WriteMergedTextRow(ws, 3, 1, 4, "2.1 Registros Informáticos", bold: true);
        PublicationSheetHelper.WriteHeaderRow(ws, 4, new[]
        {
            "Título del Registro",
            "Institución que lo otorga",
            "Número de certificado",
            "País",
        });
        PublicationSheetHelper.WriteTemplateRange(
            ws,
            "RegistrosInformaticos",
            5,
            4,
            new List<(int, string)>
            {
                (1, "{{item.Titulo}}"),
                (2, "{{item.InstitutionNombre}}"),
                (3, "{{item.NumeroCertificado}}"),
                (4, "{{item.CountryName}}"),
            });

        // --- Registros No Informáticos ---
        var header2Row = 8;
        PublicationSheetHelper.WriteMergedTextRow(ws, header2Row - 1, 1, 4, "2.2 Registros No Informáticos", bold: true);
        PublicationSheetHelper.WriteHeaderRow(ws, header2Row, new[]
        {
            "Título del Registro",
            "Institución que lo otorga",
            "Número de certificado",
            "País",
        });
        PublicationSheetHelper.WriteTemplateRange(
            ws,
            "RegistrosNoInformaticos",
            header2Row + 1,
            4,
            new List<(int, string)>
            {
                (1, "{{item.Titulo}}"),
                (2, "{{item.InstitutionNombre}}"),
                (3, "{{item.NumeroCertificado}}"),
                (4, "{{item.CountryName}}"),
            });

        // Layout
        for (int c = 1; c <= 4; c++) ws.Column(c).Width = 30;
        ws.SheetView.FreezeRows(4);
    }
}
