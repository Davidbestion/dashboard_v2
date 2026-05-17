using System.Collections.Generic;
using TemplateGen.Core.Base;
using TemplateGen.Core.Interfaces;

namespace TemplateGen.Templates;

public sealed class AnexoRedesNacInterTemplate : ExcelTemplateBase
{
    protected override string OutputPath =>
        "../Dashboard_v2/src/Infrastructure/Templates/AnexoRedesNacInter.xlsx";

    protected override IEnumerable<ISheetTemplate> GetSheets()
    {
        yield return new RedesNacionalesSheet();
        yield return new RedesInternacionalesSheet();
    }
}
