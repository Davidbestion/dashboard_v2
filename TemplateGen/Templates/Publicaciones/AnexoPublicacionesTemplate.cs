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
            headerRow: 4,
            dataRow: 5,
            hasQuartileColumn: true,
            staticRows:
            [
                new PublicationStaticRow(2, 1, 4, "1)     Publicaciones en Web de la Ciencia (Science Citation Index Expanded (SCIE), Social Sciences Citation Index (SSCI), Arts and Humanities citation index (AHCI) y Scopus ."),
                new PublicationStaticRow(3, 1, 5, "para buscar el cuartil de scopus revisar en scimago  https://www.scimagojr.com/"),
            ],
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
            headerRow: 8,
            dataRow: 9,
            hasQuartileColumn: false,
            staticRows:
            [
                new PublicationStaticRow(2, 1, 4, "2)     Publicaciones en bases de datos especializadas reconocidas "),
                new PublicationStaticRow(3, 1, 4, "emerging citation index"),
                new PublicationStaticRow(4, 1, 4, "scielo (www.scielo.com), EMERGING CITATION INDEX,  CHEMICAL ABSTRACT CA (http://info.cas. org), biological abstract BA (http://www.biosis.org)"),
                new PublicationStaticRow(5, 1, 4, "COMPENDEX, MEDLINE, CAB International, Pascal, INSPEC"),
            ],
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
            headerRow: 8,
            dataRow: 9,
            hasQuartileColumn: false,
            staticRows:
            [
                new PublicationStaticRow(2, 1, 4, "clasificacion."),
                new PublicationStaticRow(3, 1, 4, "ICYT, PERIODICA, CLASE, LILACS, REDALYC"),
                new PublicationStaticRow(4, 1, 4, "LATINDEX CATALOGO 2,0"),
                new PublicationStaticRow(5, 1, 4, "DOAJ, IME,"),
            ],
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
            headerRow: 4,
            dataRow: 5,
            hasQuartileColumn: false,
            staticRows:
            [
                new PublicationStaticRow(2, 1, 4, "Revistas nacionales certificadas por CITMA"),
                new PublicationStaticRow(3, 1, 4, "Revistas extranjeras arbitradas"),
            ],
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
