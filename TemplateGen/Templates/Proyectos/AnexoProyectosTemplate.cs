using System;
using System.Collections.Generic;
using TemplateGen.Core.Base;
using TemplateGen.Core.Interfaces;

namespace TemplateGen.Templates;

public class AnexoProyectosTemplate : ExcelTemplateBase
{
    protected override string OutputPath =>
        "../Dashboard_v2/src/Infrastructure/Templates/AnexoProyectos.xlsx";

    protected override IEnumerable<ISheetTemplate> GetSheets()
    {
        // Use empty headers for now; these can be provided later when known
        yield return new PAPNSheet();
        yield return new PAPSSheet();
        yield return new PAPTSheet();
        yield return new PESheet();
        yield return new PNESheet();
        yield return new PDLSheet();
        yield return new PRCISheet();
        yield return new PNAPSheet();
        yield return new NuevasAplicacionesSheet();
    }
}