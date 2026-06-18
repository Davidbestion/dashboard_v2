using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Application.Documents.Reports;

public sealed class AnexoRegistrosReport : IDocumentReport
{
    private readonly IApplicationDbContext _context;

    public AnexoRegistrosReport(IApplicationDbContext context)
    {
        _context = context;
    }

    public string ReportName => "anexo-registros";

    public string TemplateName => "AnexoRegistros";

    public IReadOnlyCollection<string> AllowedRoles =>
        [nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Grupo_de_investigacion), nameof(RolesEnum.Vicedecano_de_investigacion)];

    public async Task<IReadOnlyDictionary<string, object>> GatherVariablesAsync(IReadOnlyDictionary<string, string>? parameters, CancellationToken ct)
    {
        var patentes = await _context.Patentes
            .AsNoTracking()
            .OrderBy(p => p.Titulo)
            .Select(p => new AnexoRegistrosPatenteRowDto
            {
                Titulo = p.Titulo,
                NumeroSolicitudConcesion = p.NumeroSolicitudConcesion,
                EsNacional = p.EsNacional,
            })
            .ToListAsync(ct);

        var registros = await _context.Registros
            .AsNoTracking()
            .Include(r => r.Institution)
            .Include(r => r.Country)
            .OrderBy(r => r.Titulo)
            .Select(r => new AnexoRegistroRowDto
            {
                Titulo = r.Titulo,
                InstitutionNombre = r.Institution.Nombre,
                NumeroCertificado = r.NumeroCertificado,
                CountryName = r.Country.Name,
                EsInformatico = r.EsInformatico,
            })
            .ToListAsync(ct);

        var registrosInformaticos = registros.Where(r => r.EsInformatico).ToList();
        var registrosNoInformaticos = registros.Where(r => !r.EsInformatico).ToList();

        var normas = await _context.Normas
            .AsNoTracking()
            .Include(n => n.TipoNorma)
            .Include(n => n.Institution)
            .OrderBy(n => n.Titulo)
            .Select(n => new AnexoNormaRowDto
            {
                Titulo = n.Titulo,
                Tipo = n.TipoNorma != null ? n.TipoNorma.Nombre : string.Empty,
                InstitutionNombre = n.Institution.Nombre,
            })
            .ToListAsync(ct);

        var tipos = await _context.TipoProductosComercializados
            .AsNoTracking()
            .Include(t => t.Productos)
                .ThenInclude(p => p.Institution)
            .OrderBy(t => t.Nombre)
            .ToListAsync(ct);

        var tiposDto = tipos
            .Select(t => new AnexoProductoTipoRowDto
            {
                TipoProductoComercializadoNombre = t.Nombre,
                Productos = t.Productos
                    .OrderBy(p => p.Titulo)
                    .Select(p => new AnexoProductoRowDto
                    {
                        Titulo = p.Titulo,
                        InstitutionNombre = p.Institution?.Nombre ?? string.Empty,
                    })
                    .ToList()
            })
            .ToList();

        return new Dictionary<string, object>
        {
            ["Patentes"] = patentes,
            ["RegistrosInformaticos"] = registrosInformaticos,
            ["RegistrosNoInformaticos"] = registrosNoInformaticos,
            ["Normas"] = normas,
            ["ProductosTipos"] = tiposDto,
        };
    }
}

public sealed record AnexoRegistrosPatenteRowDto
{
    public string Titulo { get; init; } = string.Empty;
    public string NumeroSolicitudConcesion { get; init; } = string.Empty;
    public bool EsNacional { get; init; }
}

public sealed record AnexoRegistroRowDto
{
    public string Titulo { get; init; } = string.Empty;
    public string InstitutionNombre { get; init; } = string.Empty;
    public string NumeroCertificado { get; init; } = string.Empty;
    public string CountryName { get; init; } = string.Empty;
    public bool EsInformatico { get; init; }
}

public sealed record AnexoNormaRowDto
{
    public string Titulo { get; init; } = string.Empty;
    public string Tipo { get; init; } = string.Empty;
    public string InstitutionNombre { get; init; } = string.Empty;
}

public sealed record AnexoProductoTipoRowDto
{
    public string TipoProductoComercializadoNombre { get; init; } = string.Empty;
    public IReadOnlyList<AnexoProductoRowDto> Productos { get; init; } = [];
}

public sealed record AnexoProductoRowDto
{
    public string Titulo { get; init; } = string.Empty;
    public string InstitutionNombre { get; init; } = string.Empty;
}
