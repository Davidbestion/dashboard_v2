using ClosedXML.Excel;
using System.Collections.Generic;

namespace TemplateGen.Core.Interfaces;

public interface ISheetTemplate
{
    string Name { get; }
    string Title { get; }
    string[] Headers { get; }
    string RangeName { get; }
    int StartRow { get; }      // fila donde empiezan los datos (para el named range)
    int StartCol { get; }
    int EndRowOffset { get; }  // cuántas filas abarca el named range (por defecto 1)
    
    // Expresiones de ClosedXML.Report en la fila de datos (fila StartRow)
    IEnumerable<(int Col, string Expression)> TemplateCells { get; }

    // Método que construye la hoja dentro del workbook
    void Generate(IXLWorkbook workbook);
}