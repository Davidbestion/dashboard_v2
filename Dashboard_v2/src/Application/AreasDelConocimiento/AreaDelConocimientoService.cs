using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Common.Models;
using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dashboard_v2.Application.AreasDelConocimiento;

public sealed class AreaDelConocimientoService : IAreaDelConocimientoService
{
    private readonly IApplicationDbContext _context;

    public AreaDelConocimientoService(IApplicationDbContext context)
    {
        _context = context;
    }

    public Task<List<AreaDelConocimientoDto>> GetAllAsync(CancellationToken ct = default)
    {
        return _context.AreasDelConocimiento
            .OrderBy(a => a.Nombre)
            .Select(a => new AreaDelConocimientoDto
            {
                Id = a.Id,
                Nombre = a.Nombre,
                Descripcion = a.Descripcion,
                LineasDeInvestigacionIds = a.LineasDeInvestigacion.Select(l => l.Id).ToList(),
            })
            .ToListAsync(ct);
    }

    public async Task<(Result Result, string? Id)> CreateAsync(CreateAreaDelConocimientoRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            return (Result.Failure(new[] { "El nombre es obligatorio." }), null);

        var entity = new AreaDelConocimiento
        {
            Nombre = request.Nombre.Trim(),
            Descripcion = request.Descripcion?.Trim(),
        };

        if (request.LineasDeInvestigacionIds != null && request.LineasDeInvestigacionIds.Count > 0)
        {
            var lineas = await _context.LineasDeInvestigacion
                .Where(l => request.LineasDeInvestigacionIds.Contains(l.Id))
                .ToListAsync(ct);
            entity.LineasDeInvestigacion = lineas;
        }

        _context.AreasDelConocimiento.Add(entity);
        await _context.SaveChangesAsync(ct);

        return (Result.Success(), entity.Id);
    }

    public async Task<Result> UpdateAsync(string id, UpdateAreaDelConocimientoRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            return Result.Failure(new[] { "El nombre es obligatorio." });

        var entity = await _context.AreasDelConocimiento
            .Include(a => a.LineasDeInvestigacion)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (entity is null)
            return Result.Failure(new[] { "Área del conocimiento no encontrada." });

        if (request.LineasDeInvestigacionIds != null)
        {
            var newLineas = await _context.LineasDeInvestigacion
                .Where(l => request.LineasDeInvestigacionIds.Contains(l.Id))
                .ToListAsync(ct);
            entity.LineasDeInvestigacion.Clear();
            foreach (var l in newLineas)
                entity.LineasDeInvestigacion.Add(l);
        }

        entity.Nombre = request.Nombre.Trim();
        entity.Descripcion = request.Descripcion?.Trim();

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(string id, CancellationToken ct = default)
    {
        var entity = await _context.AreasDelConocimiento.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (entity is null) return Result.Failure(new[] { "Área del conocimiento no encontrada." });

        _context.AreasDelConocimiento.Remove(entity);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
