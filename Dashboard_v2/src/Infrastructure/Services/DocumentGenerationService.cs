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
        if (string.Equals(templateName, "AnexoRegistros", StringComparison.OrdinalIgnoreCase))
        {
            return RenderRegistrosTemplate(templateStream, variables);
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

    private static byte[] RenderRegistrosTemplate(Stream templateStream, IReadOnlyDictionary<string, object> variables)
    {
        using var workbook = new XLWorkbook(templateStream);

        // --- Patentes ---
        var patentes = variables.TryGetValue("Patentes", out var rawPat) ? rawPat as IEnumerable<AnexoRegistrosPatenteRowDto> : Enumerable.Empty<AnexoRegistrosPatenteRowDto>();
        var wsPat = workbook.Worksheet("Patentes");
        const int patFirstBodyRow = 5;
        const int patDataPrototypeRow = 5;
        const int patLastColumn = 4;
        const int patExistingPrototypeRows = 1;

        var patTotalRowsNeeded = patentes?.Count() ?? 0;
        if (patTotalRowsNeeded > patExistingPrototypeRows)
            wsPat.Row(patDataPrototypeRow).InsertRowsBelow(patTotalRowsNeeded - patExistingPrototypeRows);

        var currentRowPat = patFirstBodyRow;
        if (patentes != null && patentes.Any())
        {
            foreach (var p in patentes)
            {
                CopyRowLayout(wsPat, patDataPrototypeRow, currentRowPat, patLastColumn);
                wsPat.Cell(currentRowPat, 1).Value = p.Titulo;
                wsPat.Cell(currentRowPat, 2).Value = p.NumeroSolicitudConcesion;
                wsPat.Cell(currentRowPat, 3).Value = p.EsNacional ? "X" : string.Empty;
                wsPat.Cell(currentRowPat, 4).Value = p.EsNacional ? string.Empty : "X";
                currentRowPat++;
            }
        }
        else
        {
            currentRowPat++;
        }

        ClearUnusedRows(wsPat, currentRowPat, patFirstBodyRow + Math.Max(patExistingPrototypeRows, patTotalRowsNeeded) - 1, patLastColumn);

        // --- Registros (two tables on same sheet) ---
        var registrosInfo = variables.TryGetValue("RegistrosInformaticos", out var rawRI) ? rawRI as IEnumerable<AnexoRegistroRowDto> : Enumerable.Empty<AnexoRegistroRowDto>();
        var registrosNoInfo = variables.TryGetValue("RegistrosNoInformaticos", out var rawRNI) ? rawRNI as IEnumerable<AnexoRegistroRowDto> : Enumerable.Empty<AnexoRegistroRowDto>();
        var wsReg = workbook.Worksheet("Registros");

        const int regPrototype1 = 5;
        const int regHeader2Base = 8;
        const int regDataOffset = 1; // data at header2 + 1
        const int regLastCol = 4;
        const int regExistingProto = 1;

        var reg1Count = registrosInfo?.Count() ?? 0;
        if (reg1Count > regExistingProto)
            wsReg.Row(regPrototype1).InsertRowsBelow(reg1Count - regExistingProto);

        var extraRowsFirst = Math.Max(0, reg1Count - regExistingProto);
        var header2Row = regHeader2Base + extraRowsFirst;
        var dataPrototype2 = header2Row + regDataOffset;

        var reg2Count = registrosNoInfo?.Count() ?? 0;
        if (reg2Count > regExistingProto)
            wsReg.Row(dataPrototype2).InsertRowsBelow(reg2Count - regExistingProto);

        // Fill first table
        var currentRow1 = regPrototype1;
        if (registrosInfo != null && registrosInfo.Any())
        {
            foreach (var r in registrosInfo)
            {
                CopyRowLayout(wsReg, regPrototype1, currentRow1, regLastCol);
                wsReg.Cell(currentRow1, 1).Value = r.Titulo;
                wsReg.Cell(currentRow1, 2).Value = r.InstitutionNombre;
                wsReg.Cell(currentRow1, 3).Value = r.NumeroCertificado;
                wsReg.Cell(currentRow1, 4).Value = r.CountryName;
                currentRow1++;
            }
        }
        else currentRow1++;

        var reg1BodyCount = Math.Max(regExistingProto, reg1Count);
        ClearUnusedRows(wsReg, currentRow1, regPrototype1 + reg1BodyCount - 1, regLastCol);

        // Fill second table
        var currentRow2 = dataPrototype2;
        if (registrosNoInfo != null && registrosNoInfo.Any())
        {
            foreach (var r in registrosNoInfo)
            {
                CopyRowLayout(wsReg, dataPrototype2, currentRow2, regLastCol);
                wsReg.Cell(currentRow2, 1).Value = r.Titulo;
                wsReg.Cell(currentRow2, 2).Value = r.InstitutionNombre;
                wsReg.Cell(currentRow2, 3).Value = r.NumeroCertificado;
                wsReg.Cell(currentRow2, 4).Value = r.CountryName;
                currentRow2++;
            }
        }
        else currentRow2++;

        var reg2BodyCount = Math.Max(regExistingProto, reg2Count);
        ClearUnusedRows(wsReg, currentRow2, dataPrototype2 + reg2BodyCount - 1, regLastCol);

        // --- Normas ---
        var normas = variables.TryGetValue("Normas", out var rawNormas) ? rawNormas as IEnumerable<AnexoNormaRowDto> : Enumerable.Empty<AnexoNormaRowDto>();
        var wsNormas = workbook.Worksheet("Normas");
        const int normasPrototypeRow = 5;
        const int normasLastCol = 3;
        const int normasExistingProto = 1;
        var normasCount = normas?.Count() ?? 0;
        if (normasCount > normasExistingProto) wsNormas.Row(normasPrototypeRow).InsertRowsBelow(normasCount - normasExistingProto);

        var curNormRow = normasPrototypeRow;
        if (normas != null && normas.Any())
        {
            foreach (var n in normas)
            {
                CopyRowLayout(wsNormas, normasPrototypeRow, curNormRow, normasLastCol);
                wsNormas.Cell(curNormRow, 1).Value = n.Titulo;
                wsNormas.Cell(curNormRow, 2).Value = n.Tipo;
                wsNormas.Cell(curNormRow, 3).Value = n.InstitutionNombre;
                curNormRow++;
            }
        }
        else curNormRow++;

        ClearUnusedRows(wsNormas, curNormRow, normasPrototypeRow + Math.Max(normasExistingProto, normasCount) - 1, normasLastCol);

        // --- Productos Comercializados (agrupado por tipo) ---
        var tiposProductos = variables.TryGetValue("ProductosTipos", out var rawTipos) ? rawTipos as IEnumerable<AnexoProductoTipoRowDto> : Enumerable.Empty<AnexoProductoTipoRowDto>();
        var wsProd = workbook.Worksheet("ProductosComercializados");
        const int prodFirstBodyRow = 4;
        const int prodTypePrototypeRow = 4;
        const int prodHeaderPrototypeRow = 5;
        const int prodProductPrototypeRow = 6;
        const int prodLastColumn = 3;
        const int prodExistingPrototypeRows = 3;

        var totalRowsNeededProd = tiposProductos?.Sum(t => 2 + Math.Max(1, t.Productos.Count)) ?? 0;
        var bodyRowCountProd = Math.Max(prodExistingPrototypeRows, totalRowsNeededProd);

        if (totalRowsNeededProd > prodExistingPrototypeRows)
            wsProd.Row(prodProductPrototypeRow).InsertRowsBelow(totalRowsNeededProd - prodExistingPrototypeRows);

        var currProdRow = prodFirstBodyRow;
        foreach (var tipo in tiposProductos ?? Enumerable.Empty<AnexoProductoTipoRowDto>())
        {
            CopyRowLayout(wsProd, prodTypePrototypeRow, currProdRow, prodLastColumn);
            wsProd.Cell(currProdRow, 1).Value = tipo.TipoProductoComercializadoNombre;
            wsProd.Cell(currProdRow, 2).Value = string.Empty;
            wsProd.Cell(currProdRow, 3).Value = string.Empty;
            currProdRow++;

            CopyRowLayout(wsProd, prodHeaderPrototypeRow, currProdRow, prodLastColumn);
            wsProd.Cell(currProdRow, 1).Value = string.Empty;
            wsProd.Cell(currProdRow, 2).Value = "Nombre";
            wsProd.Cell(currProdRow, 3).Value = "Empresa o Institución";
            currProdRow++;

            if (tipo.Productos.Count > 0)
            {
                foreach (var prod in tipo.Productos)
                {
                    CopyRowLayout(wsProd, prodProductPrototypeRow, currProdRow, prodLastColumn);
                    wsProd.Cell(currProdRow, 1).Value = string.Empty;
                    wsProd.Cell(currProdRow, 2).Value = prod.Titulo;
                    wsProd.Cell(currProdRow, 3).Value = prod.InstitutionNombre;
                    currProdRow++;
                }
            }
            else
            {
                CopyRowLayout(wsProd, prodProductPrototypeRow, currProdRow, prodLastColumn);
                currProdRow++;
            }
        }

        ClearUnusedRows(wsProd, currProdRow, prodFirstBodyRow + bodyRowCountProd - 1, prodLastColumn);

        // Remove any leftover template placeholders like {{item.X}} that may remain
        // in prototype rows when a table had zero items. This clears only cell contents
        // that contain the typical template marker and preserves styles.
        ClearTemplatePlaceholders(wsPat);
        ClearTemplatePlaceholders(wsReg);
        ClearTemplatePlaceholders(wsNormas);
        ClearTemplatePlaceholders(wsProd);

        using var output = new MemoryStream();
        workbook.SaveAs(output);
        return output.ToArray();
    }

    private static void ClearTemplatePlaceholders(IXLWorksheet ws)
    {
        var used = ws.RangeUsed();
        if (used == null) return;

        foreach (var cell in used.Cells())
        {
            if (cell.DataType == XLDataType.Text)
            {
                var s = cell.GetString();
                if (!string.IsNullOrEmpty(s) && s.Contains("{{") && s.Contains("}}"))
                {
                    cell.Clear(XLClearOptions.Contents);
                }
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
