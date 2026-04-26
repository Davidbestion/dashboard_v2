using ClosedXML.Report;
using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Documents.Reports;
using ClosedXML.Excel;

namespace Dashboard_v2.Infrastructure.Services;

/// <summary>
/// Implementación del renderizador de documentos Excel usando ClosedXML.Report.
/// Carga la plantilla .xlsx embebida, inyecta las variables en los Named Ranges
/// y devuelve el archivo generado como bytes.
///
/// Esta clase NO contiene lógica de negocio ni conoce la estructura de ningún
/// reporte concreto. Cada reporte define sus propias variables en su clase
/// <see cref="IDocumentReport"/> correspondiente.
/// </summary>
public sealed class DocumentRenderer : IDocumentRenderer
{
    // TODO(david): 'anexo-eventos' no es estable con este renderer genérico basado en
    // ClosedXML.Report porque su hoja mezcla varias tablas dinámicas, merges y bloques fijos.
    // Opciones para arreglarlo:
    // 1. Sacar 'anexo-eventos' de este renderer y generar el workbook manualmente.
    // 2. Mantener el .xlsx solo como referencia visual y rellenarlo celda por celda.
    // 3. Reestructurar el anexo en varias hojas si el formato final lo permite.
    // 4. Rediseñar la plantilla para evitar desplazamientos automáticos de filas.
    // Prefijo del recurso embebido: [AssemblyName].[ruta relativa con puntos]
    private const string ResourcePrefix = "Dashboard_v2.Infrastructure.Templates.";

    /// <inheritdoc />
    public byte[] Render(string templateName, IReadOnlyDictionary<string, object> variables)
    {
        using var templateStream = LoadEmbeddedTemplate(templateName);

        if (string.Equals(templateName, "AnexoPremios", StringComparison.OrdinalIgnoreCase))
        {
            return RenderAwardsTemplate(templateStream, variables);
        }

        var template = new XLTemplate(templateStream);

        foreach (var (name, value) in variables)
            template.AddVariable(name, value);

        template.Generate();

        using var output = new MemoryStream();
        template.SaveAs(output);
        return output.ToArray();
    }

    private static byte[] RenderAwardsTemplate(Stream templateStream, IReadOnlyDictionary<string, object> variables)
    {
        using var workbook = new XLWorkbook(templateStream);
        var ws = workbook.Worksheet(1);

        var tiposPremio = variables.TryGetValue("TiposPremio", out var rawValue)
            ? rawValue as IEnumerable<AnexoPremiosTipoRowDto>
            : Enumerable.Empty<AnexoPremiosTipoRowDto>();

        const int firstBodyRow = 4;
        const int typePrototypeRow = 4;
        const int headerPrototypeRow = 5;
        const int awardPrototypeRow = 6;
        const int lastColumn = 3;
        const int existingPrototypeRows = 3;

        var totalRowsNeeded = tiposPremio?.Sum(type => 2 + Math.Max(1, type.Premios.Count)) ?? 0;
        var bodyRowCount = Math.Max(existingPrototypeRows, totalRowsNeeded);

        if (totalRowsNeeded > existingPrototypeRows)
        {
            ws.Row(awardPrototypeRow).InsertRowsBelow(totalRowsNeeded - existingPrototypeRows);
        }

        var currentRow = firstBodyRow;
        foreach (var tipo in tiposPremio ?? Enumerable.Empty<AnexoPremiosTipoRowDto>())
        {
            CopyRowLayout(ws, typePrototypeRow, currentRow, lastColumn);
            ws.Cell(currentRow, 1).Value = tipo.Numero;
            ws.Cell(currentRow, 2).Value = tipo.TipoPremio;
            ws.Cell(currentRow, 3).Value = string.Empty;
            currentRow++;

            CopyRowLayout(ws, headerPrototypeRow, currentRow, lastColumn);
            ws.Cell(currentRow, 1).Value = string.Empty;
            ws.Cell(currentRow, 2).Value = "Titulo";
            ws.Cell(currentRow, 3).Value = "Autores";
            currentRow++;

            if (tipo.Premios.Count > 0)
            {
                foreach (var premio in tipo.Premios)
                {
                    CopyRowLayout(ws, awardPrototypeRow, currentRow, lastColumn);
                    ws.Cell(currentRow, 1).Value = string.Empty;
                    ws.Cell(currentRow, 2).Value = premio.Titulo;
                    ws.Cell(currentRow, 3).Value = premio.Autores;
                    currentRow++;
                }
            }
            else
            {
                // If there are no awards under this type, insert a blank row
                CopyRowLayout(ws, awardPrototypeRow, currentRow, lastColumn);
                // leave cells empty
                currentRow++;
            }
        }

        ClearUnusedRows(ws, currentRow, firstBodyRow + bodyRowCount - 1, lastColumn);

        using var output = new MemoryStream();
        workbook.SaveAs(output);
        return output.ToArray();
    }

    private static void CopyRowLayout(IXLWorksheet ws, int prototypeRow, int targetRow, int lastColumn)
    {
        ws.Row(targetRow).Height = ws.Row(prototypeRow).Height;

        for (int col = 1; col <= lastColumn; col++)
        {
            var targetCell = ws.Cell(targetRow, col);
            var prototypeCell = ws.Cell(prototypeRow, col);

            targetCell.Clear(XLClearOptions.Contents);
            targetCell.Style = prototypeCell.Style;
        }
    }

    private static void ClearUnusedRows(IXLWorksheet ws, int startRow, int endRow, int lastColumn)
    {
        if (startRow > endRow)
            return;

        for (int row = startRow; row <= endRow; row++)
        {
            for (int col = 1; col <= lastColumn; col++)
            {
                ws.Cell(row, col).Clear(XLClearOptions.Contents);
            }
        }
    }

    private static Stream LoadEmbeddedTemplate(string templateName)
    {
        var resourceName = $"{ResourcePrefix}{templateName}.xlsx";
        var assembly = typeof(DocumentRenderer).Assembly;
        return assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Plantilla embebida '{resourceName}' no encontrada. " +
                $"Verifica que Infrastructure/Templates/{templateName}.xlsx exista " +
                "y esté marcada como EmbeddedResource en Infrastructure.csproj.");
    }
}
