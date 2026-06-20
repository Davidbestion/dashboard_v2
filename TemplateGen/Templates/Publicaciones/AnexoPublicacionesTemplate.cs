using System.Collections.Generic;
using TemplateGen.Core.Base;
using TemplateGen.Core.Interfaces;

namespace TemplateGen.Templates;

/// <summary>
/// Genera la plantilla Excel del anexo de publicaciones.
/// Incluye una primera hoja informativa sin relleno automático y el resto de
/// hojas con rangos nombrados que ClosedXML.Report expande al generar el documento.
/// </summary>
public sealed class AnexoPublicacionesTemplate : ExcelTemplateBase
{
    /// <summary>
    /// Ruta donde se guardará la plantilla generada para ser embebida por Infrastructure.
    /// </summary>
    protected override string OutputPath =>
        "../Dashboard_v2/src/Infrastructure/Templates/AnexoPublicaciones.xlsx";

    /// <summary>
    /// Devuelve todas las hojas que componen el anexo.
    /// </summary>
    protected override IEnumerable<ISheetTemplate> GetSheets()
    {
        yield return new ListadoPublicacionesSheet();
        yield return new GrupoRevistasSheet(
            name: "G1",
            rangeName: "G1",
            headerRow: 1,
            dataRow: 2,
            hasQuartileColumn: true,
            staticRows: [],
            columnWidths: new Dictionary<int, double>
            {
                [1] = 8,
                [2] = 35.125,
                [3] = 32.375,
                [4] = 30,
                [5] = 22,
                [6] = 12,
            });
        yield return new GrupoRevistasSheet(
            name: "G2",
            rangeName: "G2",
            headerRow: 1,
            dataRow: 2,
            hasQuartileColumn: false,
            staticRows: [],
            columnWidths: new Dictionary<int, double>
            {
                [1] = 8,
                [2] = 26,
                [3] = 28.875,
                [4] = 36.125,
                [5] = 25.125,
            });
        yield return new GrupoRevistasSheet(
            name: "G3",
            rangeName: "G3",
            headerRow: 1,
            dataRow: 2,
            hasQuartileColumn: false,
            staticRows: [],
            columnWidths: new Dictionary<int, double>
            {
                [1] = 8,
                [2] = 43.25,
                [3] = 30.375,
                [4] = 31,
                [5] = 25.75,
            });
        yield return new GrupoRevistasSheet(
            name: "G4",
            rangeName: "G4",
            headerRow: 1,
            dataRow: 2,
            hasQuartileColumn: false,
            staticRows: [],
            columnWidths: new Dictionary<int, double>
            {
                [1] = 3.125,
                [2] = 26.25,
                [3] = 32.375,
                [4] = 41.5,
                [5] = 22,
            });
        yield return new LibrosMonografiasCapitulosSheet();
        yield return new ArticulosDivulgacionSheet();
    }
}
