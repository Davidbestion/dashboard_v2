using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Common.Models;
using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dashboard_v2.Application.GruposEstudiantiles;

public sealed class GrupoEstudiantilService : IGrupoEstudiantilService
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public GrupoEstudiantilService(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public Task<List<GrupoEstudiantilDto>> GetAllAsync(CancellationToken ct = default)
    {
        return _context.GruposEstudiantiles
            .OrderBy(g => g.Nombre)
            .Select(g => new GrupoEstudiantilDto
            {
                Id = g.Id,
                Nombre = g.Nombre,
                AreaId = g.AreaId,
                AreaNombre = g.Area.Nombre,
                LineasDeInvestigacionIds = g.LineasDeInvestigacion.Select(l => l.Id).ToList()
            })
            .ToListAsync(ct);
    }

    public async Task<List<GrupoEstudiantilDto>> GetAreaAsync(CancellationToken ct = default)
    {
        var areaId = await _context.GetUserAreaIdAsync(_currentUser.Id, ct) ?? string.Empty;
        return await _context.GruposEstudiantiles
            .Where(g => g.AreaId == areaId)
            .OrderBy(g => g.Nombre)
            .Select(g => new GrupoEstudiantilDto
            {
                Id = g.Id,
                Nombre = g.Nombre,
                AreaId = g.AreaId,
                AreaNombre = g.Area.Nombre,
                LineasDeInvestigacionIds = g.LineasDeInvestigacion.Select(l => l.Id).ToList()
            })
            .ToListAsync(ct);
    }

    public async Task<(Result Result, string? Id)> CreateAsync(CreateGrupoEstudiantilRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            return (Result.Failure(["El nombre es obligatorio."]), null);

        var isSuperuser = _currentUser.Roles?.Contains("Superuser") == true;
        string areaId;

        if (isSuperuser)
        {
            if (!await _context.Areas.AnyAsync(a => a.Id == request.AreaId, ct))
                return (Result.Failure(["El área indicada no existe."]), null);
            areaId = request.AreaId;
        }
        else
        {
            // Vicedecano: the group is always created in their own area.
            var vicedecanoArea = await _context.GetUserAreaIdAsync(_currentUser.Id, ct);
            if (string.IsNullOrEmpty(vicedecanoArea))
                return (Result.Failure(["No tienes un área asignada."]), null);
            areaId = vicedecanoArea;
        }

        var grupo = new GrupoEstudiantil
        {
            Nombre = request.Nombre.Trim(),
            AreaId = areaId,
        };

        if (request.LineasDeInvestigacionIds.Count > 0)
        {
            var lineas = await _context.LineasDeInvestigacion
                .Where(l => request.LineasDeInvestigacionIds.Contains(l.Id))
                .ToListAsync(ct);
            grupo.LineasDeInvestigacion = lineas;
        }

        _context.GruposEstudiantiles.Add(grupo);
        await _context.SaveChangesAsync(ct);

        return (Result.Success(), grupo.Id);
    }

    public async Task<Result> UpdateAsync(string id, UpdateGrupoEstudiantilRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            return Result.Failure(["El nombre es obligatorio."]);

        var grupo = await _context.GruposEstudiantiles
            .Include(g => g.LineasDeInvestigacion)
            .FirstOrDefaultAsync(g => g.Id == id, ct);

        if (grupo is null)
            return Result.Failure(["Grupo estudiantil no encontrado."]);

        var isSuperuser = _currentUser.Roles?.Contains("Superuser") == true;
        string newAreaId;

        if (isSuperuser)
        {
            if (!await _context.Areas.AnyAsync(a => a.Id == request.AreaId, ct))
                return Result.Failure(["El área indicada no existe."]);
            newAreaId = request.AreaId;
        }
        else
        {
            // Vicedecano: can only edit groups in their own area.
            var vicedecanoArea = await _context.GetUserAreaIdAsync(_currentUser.Id, ct);
            if (string.IsNullOrEmpty(vicedecanoArea))
                return Result.Failure(["No tienes un área asignada."]);
            if (grupo.AreaId != vicedecanoArea)
                return Result.Failure(["No tienes permisos para editar grupos de otra área."]);
            newAreaId = vicedecanoArea;
        }

        grupo.Nombre = request.Nombre.Trim();
        grupo.AreaId = newAreaId;

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
        var grupo = await _context.GruposEstudiantiles
            .FirstOrDefaultAsync(g => g.Id == id, ct);

        if (grupo is null)
            return Result.Failure(["Grupo estudiantil no encontrado."]);

        var isSuperuser = _currentUser.Roles?.Contains("Superuser") == true;
        if (!isSuperuser)
        {
            var vicedecanoArea = await _context.GetUserAreaIdAsync(_currentUser.Id, ct);
            if (string.IsNullOrEmpty(vicedecanoArea))
                return Result.Failure(["No tienes un área asignada."]);
            if (grupo.AreaId != vicedecanoArea)
                return Result.Failure(["No tienes permisos para eliminar grupos de otra área."]);
        }

        _context.GruposEstudiantiles.Remove(grupo);
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
