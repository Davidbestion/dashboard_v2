using System.Collections.Generic;
using TemplateGen.Core.Base;
using TemplateGen.Core.Interfaces;

namespace TemplateGen.Templates;

public sealed class AnexoRegistrosTemplate : ExcelTemplateBase
{
    protected override string OutputPath =>
        "../Dashboard_v2/src/Infrastructure/Templates/AnexoRegistros.xlsx";

    protected override IEnumerable<ISheetTemplate> GetSheets()
    {
        yield return new PatentesSheet();
        yield return new RegistrosSheet();
        yield return new NormasSheet();
        yield return new ProductosComercializadosSheet();
    }
}
