using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Common.Models;
using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dashboard_v2.Application.Clasificaciones;

public sealed class ClasificacionService : IClasificacionService
{
    private readonly IApplicationDbContext _context;

    public ClasificacionService(IApplicationDbContext context)
    {
        _context = context;
    }

    public Task<List<ClasificacionDto>> GetAllAsync(CancellationToken ct = default)
    {
        return _context.Clasificaciones
            .OrderBy(c => c.Nombre)
            .Select(c => new ClasificacionDto { Id = c.Id, Nombre = c.Nombre })
            .ToListAsync(ct);
    }

    public async Task<(Result Result, string? Id)> CreateAsync(CreateClasificacionRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            return (Result.Failure(new[] { "El nombre es obligatorio." }), null);

        var clas = new Clasificacion
        {
            Nombre = request.Nombre.Trim()
        };

        _context.Clasificaciones.Add(clas);
        await _context.SaveChangesAsync(ct);

        return (Result.Success(), clas.Id);
    }

    public async Task<Result> UpdateAsync(string id, UpdateClasificacionRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            return Result.Failure(new[] { "El nombre es obligatorio." });

        var clas = await _context.Clasificaciones.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (clas is null) return Result.Failure(new[] { "Clasificación no encontrada." });

        clas.Nombre = request.Nombre.Trim();
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(string id, CancellationToken ct = default)
    {
        var clas = await _context.Clasificaciones.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (clas is null) return Result.Failure(new[] { "Clasificación no encontrada." });

        _context.Clasificaciones.Remove(clas);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
