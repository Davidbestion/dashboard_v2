using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Common.Models;
using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dashboard_v2.Application.LineasDeInvestigacion;

public sealed class LineaDeInvestigacionService : ILineaDeInvestigacionService
{
    private readonly IApplicationDbContext _context;

    public LineaDeInvestigacionService(IApplicationDbContext context)
    {
        _context = context;
    }

    public Task<List<LineaDeInvestigacionDto>> GetAllAsync(CancellationToken ct = default)
    {
        return _context.LineasDeInvestigacion
            .OrderBy(l => l.Nombre)
            .Select(l => new LineaDeInvestigacionDto
            {
                Id = l.Id,
                Nombre = l.Nombre,
                Descripcion = l.Descripcion,
                AreasDelConocimientoIds = l.AreasDelConocimiento.Select(a => a.Id).ToList()
            })
            .ToListAsync(ct);
    }

    public async Task<(Result Result, string? Id)> CreateAsync(CreateLineaDeInvestigacionRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            return (Result.Failure(new[] { "El nombre es obligatorio." }), null);

        var entity = new LineaDeInvestigacion
        {
            Id = System.Guid.NewGuid().ToString(),
            Nombre = request.Nombre.Trim(),
            Descripcion = request.Descripcion?.Trim(),
        };

        if (request.AreasDelConocimientoIds.Count > 0)
        {
            var areas = await _context.AreasDelConocimiento
                .Where(a => request.AreasDelConocimientoIds.Contains(a.Id))
                .ToListAsync(ct);
            entity.AreasDelConocimiento = areas;
        }

        _context.LineasDeInvestigacion.Add(entity);
        await _context.SaveChangesAsync(ct);

        return (Result.Success(), entity.Id);
    }

    public async Task<Result> UpdateAsync(string id, UpdateLineaDeInvestigacionRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            return Result.Failure(new[] { "El nombre es obligatorio." });

        var entity = await _context.LineasDeInvestigacion
            .Include(l => l.AreasDelConocimiento)
            .FirstOrDefaultAsync(l => l.Id == id, ct);

        if (entity is null)
            return Result.Failure(new[] { "Línea de investigación no encontrada." });

        entity.Nombre = request.Nombre.Trim();
        entity.Descripcion = request.Descripcion?.Trim();

        var newAreas = await _context.AreasDelConocimiento
            .Where(a => request.AreasDelConocimientoIds.Contains(a.Id))
            .ToListAsync(ct);
        entity.AreasDelConocimiento.Clear();
        foreach (var area in newAreas)
            entity.AreasDelConocimiento.Add(area);

        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(string id, CancellationToken ct = default)
    {
        var entity = await _context.LineasDeInvestigacion
            .FirstOrDefaultAsync(l => l.Id == id, ct);

        if (entity is null)
            return Result.Failure(new[] { "Línea de investigación no encontrada." });

        _context.LineasDeInvestigacion.Remove(entity);
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
