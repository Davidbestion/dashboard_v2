using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Common.Models;
using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dashboard_v2.Application.GruposDeInvestigacion;

public sealed class GrupoDeInvestigacionService : IGrupoDeInvestigacionService
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public GrupoDeInvestigacionService(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public Task<List<GrupoDeInvestigacionDto>> GetAllAsync(CancellationToken ct = default)
    {
        return _context.GruposDeInvestigacion
            .OrderBy(g => g.Nombre)
            .Select(g => new GrupoDeInvestigacionDto
            {
                Id = g.Id,
                Nombre = g.Nombre,
                AreaId = g.AreaId,
                AreaNombre = g.Area.Nombre,
                LineasDeInvestigacionIds = g.LineasDeInvestigacion.Select(l => l.Id).ToList(),
                UsuariosIds = g.Usuarios.Select(u => u.Id).ToList(),
                CreadorId = g.CreadorId
            })
            .ToListAsync(ct);
    }

    public Task<List<GrupoDeInvestigacionDto>> GetMineAsync(CancellationToken ct = default)
    {
        var userId = _currentUser.Id;
        return _context.GruposDeInvestigacion
            .Where(g => g.Usuarios.Any(u => u.Id == userId))
            .OrderBy(g => g.Nombre)
            .Select(g => new GrupoDeInvestigacionDto
            {
                Id = g.Id,
                Nombre = g.Nombre,
                AreaId = g.AreaId,
                AreaNombre = g.Area.Nombre,
                LineasDeInvestigacionIds = g.LineasDeInvestigacion.Select(l => l.Id).ToList(),
                UsuariosIds = g.Usuarios.Select(u => u.Id).ToList(),
                CreadorId = g.CreadorId
            })
            .ToListAsync(ct);
    }

    public async Task<List<GrupoDeInvestigacionDto>> GetAreaAsync(CancellationToken ct = default)
    {
        var areaId = await _context.GetUserAreaIdAsync(_currentUser.Id, ct) ?? string.Empty;
        return await _context.GruposDeInvestigacion
            .Where(g => g.AreaId == areaId)
            .OrderBy(g => g.Nombre)
            .Select(g => new GrupoDeInvestigacionDto
            {
                Id = g.Id,
                Nombre = g.Nombre,
                AreaId = g.AreaId,
                AreaNombre = g.Area.Nombre,
                LineasDeInvestigacionIds = g.LineasDeInvestigacion.Select(l => l.Id).ToList(),
                UsuariosIds = g.Usuarios.Select(u => u.Id).ToList(),
                CreadorId = g.CreadorId
            })
            .ToListAsync(ct);
    }

    public async Task<(Result Result, string? Id)> CreateAsync(CreateGrupoDeInvestigacionRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            return (Result.Failure(new[] { "El nombre es obligatorio." }), null);

        if (!await _context.Areas.AnyAsync(a => a.Id == request.AreaId, ct))
            return (Result.Failure(new[] { "El área indicada no existe." }), null);

        var grupo = new GrupoDeInvestigacion
        {
            Nombre = request.Nombre.Trim(),
            AreaId = request.AreaId,
            CreadorId = _currentUser.Id
        };

        if (request.LineasDeInvestigacionIds.Count > 0)
        {
            var lineas = await _context.LineasDeInvestigacion
                .Where(l => request.LineasDeInvestigacionIds.Contains(l.Id))
                .ToListAsync(ct);
            grupo.LineasDeInvestigacion = lineas;
        }

        _context.GruposDeInvestigacion.Add(grupo);
        await _context.SaveChangesAsync(ct);

        return (Result.Success(), grupo.Id);
    }

    public async Task<Result> UpdateAsync(string id, UpdateGrupoDeInvestigacionRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            return Result.Failure(new[] { "El nombre es obligatorio." });

        var grupo = await _context.GruposDeInvestigacion
            .Include(g => g.LineasDeInvestigacion)
            .FirstOrDefaultAsync(g => g.Id == id, ct);

        if (grupo is null)
            return Result.Failure(new[] { "Grupo de investigación no encontrado." });

        var isSuperuser = _currentUser.Roles?.Contains("Superuser") == true;
        var isJefe = _currentUser.Roles?.Contains("Jefe_de_Grupo_de_investigacion") == true;
        if (!isSuperuser && !isJefe)
            return Result.Failure(new[] { "No tienes permisos para editar este grupo." });
        if (isJefe && !isSuperuser && grupo.CreadorId != _currentUser.Id)
            return Result.Failure(new[] { "Solo puedes editar tus propios grupos de investigación." });

        if (!await _context.Areas.AnyAsync(a => a.Id == request.AreaId, ct))
            return Result.Failure(new[] { "El área indicada no existe." });

        grupo.Nombre = request.Nombre.Trim();
        grupo.AreaId = request.AreaId;

        var newLineas = await _context.LineasDeInvestigacion
            .Where(l => request.LineasDeInvestigacionIds.Contains(l.Id))
            .ToListAsync(ct);
        grupo.LineasDeInvestigacion.Clear();
        foreach (var linea in newLineas)
            grupo.LineasDeInvestigacion.Add(linea);

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(string id, CancellationToken ct = default)
    {
        var grupo = await _context.GruposDeInvestigacion
            .FirstOrDefaultAsync(g => g.Id == id, ct);

        if (grupo is null)
            return Result.Failure(new[] { "Grupo de investigación no encontrado." });

        var isSuperuser = _currentUser.Roles?.Contains("Superuser") == true;
        var isJefe = _currentUser.Roles?.Contains("Jefe_de_Grupo_de_investigacion") == true;
        if (!isSuperuser && !isJefe)
            return Result.Failure(new[] { "No tienes permisos para eliminar este grupo." });
        if (isJefe && !isSuperuser && grupo.CreadorId != _currentUser.Id)
            return Result.Failure(new[] { "Solo puedes eliminar tus propios grupos de investigación." });

        _context.GruposDeInvestigacion.Remove(grupo);
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result> SetMiembrosAsync(string id, SetGrupoMiembrosRequest request, CancellationToken ct = default)
    {
        var grupo = await _context.GruposDeInvestigacion
            .Include(g => g.Usuarios)
            .FirstOrDefaultAsync(g => g.Id == id, ct);

        if (grupo is null)
            return Result.Failure(new[] { "Grupo de investigación no encontrado." });

        var isSuperuser = _currentUser.Roles?.Contains("Superuser") == true;
        var isJefe = _currentUser.Roles?.Contains("Jefe_de_Grupo_de_investigacion") == true;
        if (!isSuperuser && !isJefe)
            return Result.Failure(new[] { "No tienes permisos para gestionar los miembros de este grupo." });
        if (isJefe && !isSuperuser && grupo.CreadorId != _currentUser.Id)
            return Result.Failure(new[] { "Solo puedes gestionar los miembros de tus propios grupos de investigación." });

        var newUsuarios = await _context.Users
            .Where(u => request.UsuariosIds.Contains(u.Id))
            .ToListAsync(ct);

        grupo.Usuarios.Clear();
        foreach (var usuario in newUsuarios)
            grupo.Usuarios.Add(usuario);

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
