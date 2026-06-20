using System.Collections.Generic;
using TemplateGen.Core.Base;
using TemplateGen.Core.Interfaces;

namespace TemplateGen.Templates;

/// <summary>
/// Genera la plantilla física del anexo de grupos científicos estudiantiles.
/// El archivo resultante se embebe luego en Infrastructure para la generación
/// de documentos Excel rellenos desde la aplicación.
/// </summary>
public sealed class AnexoGruposEstudiantilesTemplate : ExcelTemplateBase
{
    /// <summary>
    /// Ruta de salida de la plantilla generada dentro del proyecto Infrastructure.
    /// </summary>
    protected override string OutputPath =>
        "../Dashboard_v2/src/Infrastructure/Templates/AnexoGruposEstudiantiles.xlsx";

    /// <summary>
    /// Devuelve las hojas que componen el anexo.
    /// </summary>
    protected override IEnumerable<ISheetTemplate> GetSheets()
    {
        yield return new GruposEstudiantilesSheet();
    }
}
