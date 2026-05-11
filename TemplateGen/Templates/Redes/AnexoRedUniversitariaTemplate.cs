using System.Collections.Generic;
using TemplateGen.Core.Base;
using TemplateGen.Core.Interfaces;

namespace TemplateGen.Templates;

/// <summary>
/// Plantilla base para el archivo Excel de una red universitaria.
/// Contiene 6 hojas, una por cada tabla del Anexo 6.
/// Este template se renderiza N veces en tiempo de ejecución (una por cada red universitaria),
/// sustituyendo la variable escalar {{NombreRed}} en cada pasada.
/// </summary>
public sealed class AnexoRedUniversitariaTemplate : ExcelTemplateBase
{
    protected override string OutputPath =>
        "../Dashboard_v2/src/Infrastructure/Templates/AnexoRedUniversitaria.xlsx";

    protected override IEnumerable<ISheetTemplate> GetSheets()
    {
        yield return new AreasParticipantesSheet();
        yield return new ProyectosVinculadosSheet();
        yield return new EventosRedSheet();
        yield return new PublicacionesRedSheet();
        yield return new PonenciasRedSheet();
        yield return new PremiosRedSheet();
    }
}
