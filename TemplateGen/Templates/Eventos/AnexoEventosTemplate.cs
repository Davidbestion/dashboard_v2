using System.Collections.Generic;
using TemplateGen.Core.Base;
using TemplateGen.Core.Interfaces;

namespace TemplateGen.Templates;

/// <summary>
/// Genera la plantilla Excel del anexo de eventos y actividades científicas.
/// Cada sección del anexo ocupa una hoja independiente para que ClosedXML.Report
/// pueda expandir cada rango dinámico sin interferir con las demás tablas.
/// </summary>
public sealed class AnexoEventosTemplate : ExcelTemplateBase
{
    /// <summary>
    /// Ruta donde se guarda la plantilla generada para ser embebida por Infrastructure.
    /// </summary>
    protected override string OutputPath =>
        "../Dashboard_v2/src/Infrastructure/Templates/AnexoEventosCientificos.xlsx";

    /// <summary>
    /// Devuelve la colección de hojas que conforman el anexo, una por sección.
    /// </summary>
    protected override IEnumerable<ISheetTemplate> GetSheets()
    {
        yield return new EventosInternacionalesSheet();
        yield return new EventosNacionalesSheet();
        yield return new EventosCoauspiciadosSheet();
        yield return new ActividadesCientificasUHSheet();
        yield return new PonenciasResumenSheet();
        yield return new ConferenciasMagistralesSheet();
        yield return new DatosPonenciasSheet();
    }
}
