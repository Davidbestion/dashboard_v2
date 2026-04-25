using System.Collections.Generic;
using TemplateGen.Core.Base;
using TemplateGen.Core.Interfaces;

namespace TemplateGen.Templates;

/// <summary>
/// Genera la plantilla base del Anexo 5 de premios.
/// La plantilla contiene el encabezado visual y filas prototipo para que
/// Infrastructure pueda rellenar el cuerpo manualmente con formato estable.
/// </summary>
public sealed class AnexoPremiosTemplate : ExcelTemplateBase
{
    protected override string OutputPath =>
        "../Dashboard_v2/src/Infrastructure/Templates/AnexoPremios.xlsx";

    protected override IEnumerable<ISheetTemplate> GetSheets()
    {
        yield return new AnexoPremiosSheet();
    }
}
