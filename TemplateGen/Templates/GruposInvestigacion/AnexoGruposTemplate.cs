using System.Collections.Generic;
using TemplateGen.Core.Base;
using TemplateGen.Core.Interfaces;

namespace TemplateGen.Templates;

public class AnexoGruposTemplate : ExcelTemplateBase
{
    protected override string OutputPath =>
        "../Dashboard_v2/src/Infrastructure/Templates/AnexoGrupos.xlsx";

    protected override IEnumerable<ISheetTemplate> GetSheets()
    {
        yield return new GruposSheet();
    }
}