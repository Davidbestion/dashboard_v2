using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Common.Models;
using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dashboard_v2.Application.Universidades;

public sealed class UniversidadService : IUniversidadService
{
    private readonly IApplicationDbContext _context;

    public UniversidadService(IApplicationDbContext context)
    {
        _context = context;
    }

    public Task<List<UniversidadDto>> GetAllAsync(CancellationToken ct = default)
    {
        return _context.Universidades
            .OrderBy(u => u.Nombre)
            .Select(u => new UniversidadDto { Id = u.Id, Nombre = u.Nombre })
            .ToListAsync(ct);
    }

    public async Task<(Result Result, string? Id)> CreateAsync(string nombre, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return (Result.Failure(new[] { "El nombre es obligatorio." }), null);

        var u = new Universidad { Nombre = nombre.Trim() };
        _context.Universidades.Add(u);
        await _context.SaveChangesAsync(ct);
        return (Result.Success(), u.Id);
    }

    public async Task<Result> UpdateAsync(string id, string nombre, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return Result.Failure(new[] { "El nombre es obligatorio." });

        var u = await _context.Universidades.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (u is null) return Result.Failure(new[] { "Universidad no encontrada." });

        u.Nombre = nombre.Trim();
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(string id, CancellationToken ct = default)
    {
        var u = await _context.Universidades.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (u is null) return Result.Failure(new[] { "Universidad no encontrada." });

        _context.Universidades.Remove(u);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
