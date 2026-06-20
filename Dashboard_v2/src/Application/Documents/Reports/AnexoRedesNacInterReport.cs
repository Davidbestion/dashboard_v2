using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Application.Documents.Reports;

public sealed class AnexoRedesNacInterReport : IDocumentReport
{
    private readonly IApplicationDbContext _context;

    public AnexoRedesNacInterReport(IApplicationDbContext context)
    {
        _context = context;
    }

    public string ReportName => "anexo-redes-nac-inter";
    public string TemplateName => "AnexoRedesNacInter";

    public IReadOnlyCollection<string> AllowedRoles =>
        [nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Grupo_de_investigacion),
         nameof(RolesEnum.Vicedecano_de_investigacion), nameof(RolesEnum.Jefe_de_Redes)];

    public async Task<IReadOnlyDictionary<string, object>> GatherVariablesAsync(
        IReadOnlyDictionary<string, string>? parameters, CancellationToken ct)
    {
        var redes = await _context.Reds
            .AsNoTracking()
            .Include(r => r.Country)
            .Include(r => r.Coordinador).ThenInclude(u => u!.Area)
            .Where(r => r.Tipo == TipoRed.Nacional || r.Tipo == TipoRed.Internacional)
            .OrderBy(r => r.Nombre)
            .ToListAsync(ct);

        var nacionales = redes
            .Where(r => r.Tipo == TipoRed.Nacional)
            .Select(r => new AnexoRedNacionalRowDto
            {
                Area = r.Coordinador?.Area?.Nombre ?? string.Empty,
                Nombre = r.Nombre,
                CentroCoordina = string.Empty, // pendiente de modelar
                CantidadProfesores = r.CantidadProfesores,
            })
            .ToList();

        var internacionales = redes
            .Where(r => r.Tipo == TipoRed.Internacional)
            .Select(r => new AnexoRedInternacionalRowDto
            {
                Area = r.Coordinador?.Area?.Nombre ?? string.Empty,
                Nombre = r.Nombre,
                Pais = r.Country?.Name ?? string.Empty,
                Coordinacion = string.Empty, // pendiente de modelar
                CantidadProfesores = r.CantidadProfesores,
            })
            .ToList();

        return new Dictionary<string, object>
        {
            ["RedesNacionales"] = nacionales,
            ["RedesInternacionales"] = internacionales,
        };
    }
}

public sealed record AnexoRedNacionalRowDto
{
    public string Area { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public string CentroCoordina { get; init; } = string.Empty;
    public int CantidadProfesores { get; init; }
}

public sealed record AnexoRedInternacionalRowDto
{
    public string Area { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public string Pais { get; init; } = string.Empty;
    public string Coordinacion { get; init; } = string.Empty;
    public int CantidadProfesores { get; init; }
}
