using TemplateGen.Core.Base;

namespace TemplateGen.Templates;

public class ProyectosSheetBase : SheetTemplateBase
{
    private readonly string _name;
    private readonly string _title;
    private readonly string[] _headers;

    public ProyectosSheetBase(string name, string title, string[] headers)
    {
        _name = name;
        _title = title;
        _headers = headers;
    }

    public override string Name => _name;
    public override string Title => _title;
    public override string[] Headers => _headers;
    public override string RangeName => _name;

    // TemplateCells puedes definirlos según el tipo de hoja
}