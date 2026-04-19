using TemplateGen.Core.Interfaces;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;

namespace TemplateGen.Core.Base;

public abstract class ExcelTemplateBase : IExcelTemplate
{
    protected abstract string OutputPath { get; }
    protected abstract IEnumerable<ISheetTemplate> GetSheets();

    public void Generate()
    {
        using var workbook = new XLWorkbook();
        foreach (var sheet in GetSheets())
        {
            sheet.Generate(workbook);
        }
        workbook.SaveAs(OutputPath);
        Console.WriteLine($"  → {OutputPath}");
    }
}