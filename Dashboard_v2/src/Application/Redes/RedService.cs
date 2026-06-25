using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Common.Models;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Domain.Enums;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Application.Redes;

/// <summary>
/// Application service for managing research networks (redes): listings, coordinator assignment, participants, events, and CRUD.
/// </summary>
public sealed class RedService : IRedService
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public RedService(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    private bool IsInRole(string role) => _currentUser.Roles?.Contains(role) == true;

    public async Task<List<RedDto>> GetRedesAsync(CancellationToken ct = default)
    {
        IQueryable<Red> query = _context.Reds.AsNoTracking();

        if (IsInRole(nameof(RolesEnum.Vicedecano_de_investigacion)))
        {
            var areaId = await _context.GetUserAreaIdAsync(_currentUser.Id, ct);
            if (!string.IsNullOrEmpty(areaId))
            {
                query = query.Where(r =>
                    (r.CoordinadorId != null && r.Coordinador!.AreaId == areaId) ||
                    r.Participaciones.Any(p => p.Author.User != null && p.Author.User.AreaId == areaId));
            }
        }

        return await query
            .Select(r => new RedDto(r.Id, r.Nombre, r.CountryId, r.Country != null ? r.Country.Name : null, r.CantidadProfesores, (int)r.Tipo))
            .ToListAsync(ct);
    }

    public async Task<List<RedConCoordinadorDto>> GetMisRedesAsync(CancellationToken ct = default)
    {
        IQueryable<Red> query;

        if (IsInRole(nameof(RolesEnum.Superuser)))
        {
            query = _context.Reds.AsNoTracking();
        }
        else if (IsInRole(nameof(RolesEnum.Jefe_de_Redes)))
        {
            var jefeAreaId = await _context.GetUserAreaIdAsync(_currentUser.Id, ct);
            if (jefeAreaId == null)
                return [];

            query = _context.Reds.AsNoTracking()
                .Where(r =>
                    (r.Coordinador != null && r.Coordinador.AreaId == jefeAreaId) ||
                    r.Participaciones.Any(p => p.Author.User != null && p.Author.User.AreaId == jefeAreaId));
        }
        else
        {
            // Profesor: redes que coordina o en las que participa.
            query = _context.Reds.AsNoTracking().Where(r =>
                r.CoordinadorId == _currentUser.Id ||
                r.Participaciones.Any(p => p.Author.UserId == _currentUser.Id));
        }

        var reds = await query
            .Include(r => r.Coordinador)
            .Include(r => r.Participaciones).ThenInclude(p => p.Author)
            .Include(r => r.Country)
            .OrderBy(r => r.Nombre)
            .ToListAsync(ct);

        return reds.Select(r => new RedConCoordinadorDto(
            r.Id,
            r.Nombre,
            (int)r.Tipo,
            r.Country?.Name,
            r.CantidadProfesores,
            r.CoordinadorId,
            r.Coordinador != null ? $"{r.Coordinador.UserName} {r.Coordinador.UserLastName1}" : null,
            r.Coordinador?.Email,
            r.Participaciones.Select(p => new ParticipanteRedDto(p.AuthorId, p.Author.Name)).ToList()
        )).ToList();
    }

    public async Task<Result> SetCoordinadorAsync(string redId, string? coordinadorId, CancellationToken ct = default)
    {
        var red = await _context.Reds.FindAsync(redId, ct);
        if (red == null)
            return Result.Failure(["Red no encontrada."]);

        if (coordinadorId != null)
        {
            var existe = await _context.Users.AnyAsync(u => u.Id == coordinadorId, ct);
            if (!existe)
                return Result.Failure(["Usuario coordinador no encontrado."]);
        }

        red.CoordinadorId = coordinadorId;
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<(bool Found, List<ParticipanteRedDto> Participantes)> GetParticipantesAsync(string redId, CancellationToken ct = default)
    {
        if (!await _context.Reds.AnyAsync(r => r.Id == redId, ct))
            return (false, []);

        var list = await _context.ParticipacionesEnRed
            .AsNoTracking()
            .Where(p => p.RedId == redId)
            .Include(p => p.Author)
            .Select(p => new ParticipanteRedDto(p.AuthorId, p.Author.Name))
            .ToListAsync(ct);

        return (true, list);
    }

    public async Task<Result> AddParticipanteAsync(string redId, string authorId, CancellationToken ct = default)
    {
        if (!await _context.Reds.AnyAsync(r => r.Id == redId, ct))
            return Result.Failure(["Red no encontrada."]);
        if (!await _context.Authors.AnyAsync(a => a.Id == authorId, ct))
            return Result.Failure(["Autor no encontrado."]);
        if (await _context.ParticipacionesEnRed.AnyAsync(p => p.RedId == redId && p.AuthorId == authorId, ct))
            return Result.Failure(["El autor ya es participante de esta red."]);

        _context.ParticipacionesEnRed.Add(new ParticipacionEnRed { RedId = redId, AuthorId = authorId });
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> RemoveParticipanteAsync(string redId, string authorId, CancellationToken ct = default)
    {
        var p = await _context.ParticipacionesEnRed
            .FirstOrDefaultAsync(x => x.RedId == redId && x.AuthorId == authorId, ct);
        if (p == null)
            return Result.Failure(["El autor no es participante de esta red."]);

        _context.ParticipacionesEnRed.Remove(p);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<(Result Result, string? Id)> CreateRedAsync(CreateRedBody body, CancellationToken ct = default)
    {
        if (!Enum.IsDefined(typeof(TipoRed), body.Tipo))
            return (Result.Failure(["Tipo de red no válido."]), null);

        var entity = new Red
        {
            Nombre = body.Nombre,
            CountryId = body.CountryId,
            CantidadProfesores = body.CantidadProfesores,
            Tipo = (TipoRed)body.Tipo,
        };

        _context.Reds.Add(entity);
        await _context.SaveChangesAsync(ct);

        return (Result.Success(), entity.Id);
    }

    public async Task<Result> UpdateRedAsync(string id, UpdateRedBody body, CancellationToken ct = default)
    {
        if (!Enum.IsDefined(typeof(TipoRed), body.Tipo))
            return Result.Failure(["Tipo de red no válido."]);

        var red = await _context.Reds.FindAsync(id, ct);
        if (red == null)
            return Result.Failure(["Red no encontrada."]);

        red.Nombre = body.Nombre;
        red.CountryId = body.CountryId;
        red.CantidadProfesores = body.CantidadProfesores;
        red.Tipo = (TipoRed)body.Tipo;

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteRedAsync(string id, CancellationToken ct = default)
    {
        var red = await _context.Reds.FindAsync(id, ct);
        if (red == null)
            return Result.Failure(["Red no encontrada."]);

        _context.Reds.Remove(red);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    public Task<List<EventForRedDto>> GetEventsForRedAsync(string redId, CancellationToken ct = default)
        => _context.Events
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .Select(e => new EventForRedDto(e.Id, e.Name, e.RedId == redId))
            .ToListAsync(ct);

    public async Task<Result> SetEventsForRedAsync(string redId, List<int> eventIds, CancellationToken ct = default)
    {
        var red = await _context.Reds.FindAsync(redId, ct);
        if (red == null)
            return Result.Failure(["Red no encontrada."]);

        var distinctIds = eventIds.Distinct().ToList();

        var events = await _context.Events.Where(e => distinctIds.Contains(e.Id)).ToListAsync(ct);
        if (events.Count != distinctIds.Count)
            return Result.Failure(["Uno o más eventos no existen."]);

        var currentlyAssigned = await _context.Events.Where(e => e.RedId == redId).ToListAsync(ct);
        var toUnassign = currentlyAssigned.Where(e => !distinctIds.Contains(e.Id)).ToList();
        foreach (var e in toUnassign) e.RedId = null;
        foreach (var e in events) e.RedId = redId;

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
