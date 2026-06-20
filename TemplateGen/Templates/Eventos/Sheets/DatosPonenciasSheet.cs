using ClosedXML.Excel;
using TemplateGen.Core.Interfaces;

namespace TemplateGen.Templates;

/// <summary>
/// Hoja 7 del anexo 3: datos detallados de las ponencias presentadas.
/// </summary>
public sealed class DatosPonenciasSheet : ISheetTemplate
{
    public string Name => "7. Datos de Ponencias";
    public string Title => string.Empty;
    public string[] Headers => [];
    public string RangeName => string.Empty;
    public int StartRow => 0;
    public int StartCol => 1;
    public int EndRowOffset => 0;
    public IEnumerable<(int Col, string Expression)> TemplateCells => [];

    public void Generate(IXLWorkbook workbook)
    {
        var ws = workbook.Worksheets.Add(Name);
        ws.Column(1).Width = 38;
        ws.Column(2).Width = 32;
        ws.Column(3).Width = 32;
        ws.Column(4).Width = 22;

        EventosSheetHelper.WriteHeaderRow(
            ws, 1,
            ["Nombre de la ponencia", "Nombre de autores", "Nombre del evento o actividad científica", "País de celebración"]);

        EventosSheetHelper.WriteTemplateRange(
            ws,
            "DatosPonencias",
            2,
            4,
            [
                (1, "{{item.NombrePonencia}}"),
                (2, "{{item.NombreAutores}}"),
                (3, "{{item.NombreEventoOActividadCientifica}}"),
                (4, "{{item.PaisDeCelebracion}}"),
            ]);
    }
}
