using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Common.Models;
using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dashboard_v2.Application.Areas;

/// <summary>
/// Application service implementing CRUD operations for academic areas.
/// </summary>
public sealed class AreaService : IAreaService
{
    private readonly IApplicationDbContext _context;

    public AreaService(IApplicationDbContext context)
    {
        _context = context;
    }

    public Task<List<AreaDto>> GetAllAsync(CancellationToken ct = default)
    {
        return _context.Areas
            .OrderBy(a => a.Nombre)
            .Select(a => new AreaDto
            {
                Id = a.Id,
                Nombre = a.Nombre,
                Descripcion = a.Descripcion,
                UniversidadId = a.UniversidadId,
                UniversidadNombre = a.Universidad != null ? a.Universidad.Nombre : null,
                AreasDelConocimientoIds = a.AreasDelConocimiento.Select(ac => ac.Id).ToList()
            })
            .ToListAsync(ct);
    }

    public async Task<(Result Result, string? Id)> CreateAsync(CreateAreaRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            return (Result.Failure(new[] { "El nombre es obligatorio." }), null);

        if (request.UniversidadId is not null &&
            !await _context.Universidades.AnyAsync(u => u.Id == request.UniversidadId, ct))
            return (Result.Failure(new[] { "La universidad indicada no existe." }), null);

        var area = new Area
        {
            Nombre = request.Nombre.Trim(),
            Descripcion = request.Descripcion?.Trim(),
            UniversidadId = request.UniversidadId
        };

        if (request.AreasDelConocimientoIds.Count > 0)
        {
            var areasConocimiento = await _context.AreasDelConocimiento
                .Where(a => request.AreasDelConocimientoIds.Contains(a.Id))
                .ToListAsync(ct);
            area.AreasDelConocimiento = areasConocimiento;
        }

        _context.Areas.Add(area);
        await _context.SaveChangesAsync(ct);

        return (Result.Success(), area.Id);
    }

    public async Task<Result> UpdateAsync(string id, UpdateAreaRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            return Result.Failure(new[] { "El nombre es obligatorio." });

        var area = await _context.Areas
            .Include(a => a.AreasDelConocimiento)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (area is null)
            return Result.Failure(new[] { "Área no encontrada." });

        if (request.UniversidadId is not null &&
            !await _context.Universidades.AnyAsync(u => u.Id == request.UniversidadId, ct))
            return Result.Failure(new[] { "La universidad indicada no existe." });

        area.Nombre = request.Nombre.Trim();
        area.Descripcion = request.Descripcion?.Trim();
        area.UniversidadId = request.UniversidadId;

        var newAreasConocimiento = await _context.AreasDelConocimiento
            .Where(a => request.AreasDelConocimientoIds.Contains(a.Id))
            .ToListAsync(ct);
        area.AreasDelConocimiento.Clear();
        foreach (var ac in newAreasConocimiento)
            area.AreasDelConocimiento.Add(ac);

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(string id, CancellationToken ct = default)
    {
        var area = await _context.Areas.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (area is null) return Result.Failure(new[] { "Área no encontrada." });

        _context.Areas.Remove(area);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
