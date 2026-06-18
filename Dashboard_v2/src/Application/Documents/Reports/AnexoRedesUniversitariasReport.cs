using System.IO.Compression;
using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Application.Documents.Reports;

public sealed class AnexoRedesUniversitariasReport : IZipDocumentReport
{
    private readonly IApplicationDbContext _context;

    public AnexoRedesUniversitariasReport(IApplicationDbContext context)
    {
        _context = context;
    }

    public string ReportName => "anexo-redes-universitarias";

    public IReadOnlyCollection<string> AllowedRoles =>
        [nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Grupo_de_investigacion),
         nameof(RolesEnum.Vicedecano_de_investigacion), nameof(RolesEnum.Jefe_de_Redes)];

    public async Task<byte[]> GenerateAsync(IDocumentRenderer renderer, CancellationToken ct = default)
    {
        var redes = await _context.Reds
            .AsNoTracking()
            .Where(r => r.Tipo == TipoRed.Universitaria)
            .Include(r => r.Participaciones)
                .ThenInclude(p => p.Author)
                    .ThenInclude(a => a.User)
                        .ThenInclude(u => u!.Area)
            .Include(r => r.Events)
                .ThenInclude(e => e.Organizadores)
                    .ThenInclude(o => o.User)
                        .ThenInclude(u => u.Area)
            .Include(r => r.Events)
                .ThenInclude(e => e.Country)
            .OrderBy(r => r.Nombre)
            .ToListAsync(ct);

        using var zipStream = new MemoryStream();
        using (var zip = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            int index = 1;
            foreach (var red in redes)
            {
                var variables = BuildVariables(red);
                var xlsxBytes = renderer.Render("AnexoRedUniversitaria", variables);

                var safeFileName = $"Anexo6_Red_{index:D2}.xlsx";
                var entry = zip.CreateEntry(safeFileName, CompressionLevel.Fastest);
                using var entryStream = entry.Open();
                await entryStream.WriteAsync(xlsxBytes, ct);
                index++;
            }
        }

        zipStream.Position = 0;
        return zipStream.ToArray();
    }

    private static IReadOnlyDictionary<string, object> BuildVariables(Domain.Entities.Red red)
    {
        var areasUH = red.Participaciones
            .Select(p => p.Author.User?.Area?.Nombre ?? string.Empty)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct()
            .OrderBy(a => a)
            .Select(a => new AnexoAreaParticipanteRowDto { AreaUH = a, AreaExterna = string.Empty })
            .ToList();

        if (areasUH.Count == 0)
            areasUH.Add(new AnexoAreaParticipanteRowDto());

        var eventos = red.Events
            .OrderBy(e => e.Name)
            .Select(e => new AnexoEventoRedRowDto
            {
                Nombre = e.Name,
                FechaLugar = e.Country?.Name ?? string.Empty,
                AreasParticipantes = string.Join(", ", e.Organizadores
                    .Select(o => o.User?.Area?.Nombre)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct()
                    .OrderBy(n => n)),
            })
            .ToList();

        if (eventos.Count == 0)
            eventos.Add(new AnexoEventoRedRowDto());

        // Proyectos, publicaciones, ponencias y premios vinculados a la red
        // aún no están modelados — se devuelven listas vacías con una fila de relleno
        var proyectos = new List<AnexoProyectoVinculadoRowDto> { new() };
        var publicaciones = new List<AnexoPublicacionRedRowDto> { new() };
        var ponencias = new List<AnexoPonenciaRedRowDto> { new() };
        var premios = new List<AnexoPremioRedRowDto> { new() };

        return new Dictionary<string, object>
        {
            ["NombreRed"]         = red.Nombre,
            ["AreasParticipantes"]  = areasUH,
            ["ProyectosVinculados"] = proyectos,
            ["EventosRed"]          = eventos,
            ["PublicacionesRed"]    = publicaciones,
            ["PonenciasRed"]        = ponencias,
            ["PremiosRed"]          = premios,
        };
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────────

public sealed record AnexoAreaParticipanteRowDto
{
    public string AreaUH { get; init; } = string.Empty;
    public string AreaExterna { get; init; } = string.Empty;
}

public sealed record AnexoProyectoVinculadoRowDto
{
    public string Titulo { get; init; } = string.Empty;
    public string UH { get; init; } = string.Empty;
    public string JefeProyectoUH { get; init; } = string.Empty;
    public string Externos { get; init; } = string.Empty;
    public string JefeProyectoExterno { get; init; } = string.Empty;
}

public sealed record AnexoEventoRedRowDto
{
    public string Nombre { get; init; } = string.Empty;
    public string FechaLugar { get; init; } = string.Empty;
    public string AreasParticipantes { get; init; } = string.Empty;
}

public sealed record AnexoPublicacionRedRowDto
{
    public string Titulo { get; init; } = string.Empty;
    public string Articulo { get; init; } = string.Empty;
    public string Libro { get; init; } = string.Empty;
    public string Autor { get; init; } = string.Empty;
}

public sealed record AnexoPonenciaRedRowDto
{
    public string Titulo { get; init; } = string.Empty;
    public string EventoNacional { get; init; } = string.Empty;
    public string EventoInternacional { get; init; } = string.Empty;
    public string Autor { get; init; } = string.Empty;
}

public sealed record AnexoPremioRedRowDto
{
    public string NombrePremio { get; init; } = string.Empty;
    public string OtorgadoA { get; init; } = string.Empty;
    public string Nacional { get; init; } = string.Empty;
    public string Internacional { get; init; } = string.Empty;
    public string Fecha { get; init; } = string.Empty;
}
