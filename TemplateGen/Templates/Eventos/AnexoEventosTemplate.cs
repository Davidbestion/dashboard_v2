using System.Collections.Generic;
using TemplateGen.Core.Base;
using TemplateGen.Core.Interfaces;

namespace TemplateGen.Templates;

/// <summary>
/// Genera la plantilla Excel del anexo de eventos y actividades científicas.
/// El diseño replica una única hoja con varias tablas independientes, cada una
/// conectada a un rango nombrado o a variables escalares según corresponda.
/// </summary>
public sealed class AnexoEventosTemplate : ExcelTemplateBase
{
    // TODO(david): La plantilla del anexo de eventos sigue generándose con una sola hoja
    // compuesta y eso hoy causa corrimientos/formato roto al expandir rangos dinámicos.
    // Opciones para arreglarlo:
    // 1. Reemplazar este enfoque por generación manual de la hoja final.
    // 2. Mantener el .xlsx como referencia visual y escribir valores en posiciones fijas.
    // 3. Separar secciones en varias hojas si el modelo oficial lo permite.
    // 4. Rediseñar la plantilla para evitar merges y tablas dinámicas apiladas.
    /// <summary>
    /// Ruta donde se guarda la plantilla generada para ser embebida por Infrastructure.
    /// </summary>
    protected override string OutputPath =>
        "../Dashboard_v2/src/Infrastructure/Templates/AnexoEventosCientificos.xlsx";

    /// <summary>
    /// Devuelve la colección de hojas que conforman el anexo.
    /// En este caso el anexo completo vive en una sola hoja.
    /// </summary>
    protected override IEnumerable<ISheetTemplate> GetSheets()
    {
        yield return new EventosCientificosSheet();
    }
}
