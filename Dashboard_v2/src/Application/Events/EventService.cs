using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Common.Models;
using Dashboard_v2.Application.FileStorage;
using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Application.Events;

/// <summary>
/// Application service implementing event and presentation (ponencia) management.
/// Implements both <see cref="IEventService"/> and <see cref="IPresentationService"/> since
/// presentations are always scoped to an event.
/// </summary>
public sealed class EventService : IEventService, IPresentationService
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    /// <summary>
    /// Servicio de cola de borrado diferido. Puede ser null si MinIO no está configurado;
    /// usar con el operador <c>?.</c>.
    /// </summary>
    private readonly IFileDeletionQueueService? _deletionQueue;

    public EventService(
        IApplicationDbContext context,
        IUser currentUser,
        IFileDeletionQueueService? deletionQueue = null)
    {
        _context       = context;
        _currentUser   = currentUser;
        _deletionQueue = deletionQueue;
    }

    private bool IsSuperuser => _currentUser.Roles?.Contains(nameof(RolesEnum.Superuser)) == true;
    private bool IsVicedecano => _currentUser.Roles?.Contains(nameof(RolesEnum.Vicedecano_de_investigacion)) == true;

    public async Task<List<EventDto>> GetMyEventsAsync(CancellationToken ct = default)
    {
        if (IsSuperuser) return await GetAllEventsAsync(ct);
        if (IsVicedecano) return await GetAreaEventsAsync(ct);
        return await QueryEventsAsync(EventScope.ForUser(_currentUser.Id ?? string.Empty), ct);
    }

    public Task<List<EventDto>> GetAllEventsAsync(CancellationToken ct = default) =>
        IsVicedecano ? GetAreaEventsAsync(ct) : QueryEventsAsync(EventScope.All, ct);

    public async Task<List<EventDto>> GetAreaEventsAsync(CancellationToken ct = default)
    {
        var areaId = await _context.GetUserAreaIdAsync(_currentUser.Id, ct) ?? string.Empty;
        return await QueryEventsAsync(EventScope.ForArea(areaId), ct);
    }

    /// <summary>Aplica el alcance dado, proyecta a <see cref="EventDto"/> y completa el conteo de ponencias.</summary>
    private async Task<List<EventDto>> QueryEventsAsync(EventScope scope, CancellationToken ct)
    {
        var rawEvents = await scope.Apply(_context.Events.AsNoTracking())
            .OrderBy(e => e.Name)
            .Select(e => new
            {
                e.Id, e.Name, e.CountryId,
                CountryName = e.Country.Name,
                e.EventTypeId,
                EventTypeName = e.EventType.Name,
                Institutions = e.Institutions.Select(i => i.Nombre).ToList(),
                e.RedId,
                RedName = e.Red != null ? e.Red.Nombre : null,
                OrganizadorIds = e.Organizadores.Select(o => o.UserId).ToList(),
                e.EvidenceFileId,
                e.FechaInicio,
                e.FechaFin,
            })
            .ToListAsync(ct);

        var counts = await GetPresentationCountsAsync(rawEvents.Select(e => e.Id), ct);

        return rawEvents.Select(e => new EventDto
        {
            Id = e.Id,
            Name = e.Name,
            CountryId = e.CountryId,
            CountryName = e.CountryName,
            EventTypeId = e.EventTypeId,
            EventTypeName = e.EventTypeName,
            Institutions = e.Institutions,
            PresentationCount = counts.GetValueOrDefault(e.Id, 0),
            RedId = e.RedId,
            RedName = e.RedName,
            OrganizadorIds = e.OrganizadorIds,
            EvidenceFileId = e.EvidenceFileId,
            FechaInicio = e.FechaInicio,
            FechaFin = e.FechaFin,
        }).ToList();
    }

    /// <summary>
    /// Alcance de visibilidad para el listado de eventos: todos, por usuario
    /// (participante u organizador) o por área académica.
    /// </summary>
    private sealed class EventScope
    {
        private readonly string? _userId;
        private readonly string? _areaId;

        private EventScope(string? userId, string? areaId)
        {
            _userId = userId;
            _areaId = areaId;
        }

        public static EventScope All { get; } = new(null, null);
        public static EventScope ForUser(string userId) => new(userId, null);
        public static EventScope ForArea(string areaId) => new(null, areaId);

        public IQueryable<Event> Apply(IQueryable<Event> source)
        {
            if (_userId != null)
                return source.Where(e => e.Participaciones.Any(p => p.UserId == _userId)
                                       || e.Organizadores.Any(o => o.UserId == _userId));
            if (_areaId != null)
                return source.Where(e => e.Organizadores.Any(o => o.User != null && o.User.AreaId == _areaId)
                                       || e.Participaciones.Any(p => p.User != null && p.User.AreaId == _areaId));
            return source;
        }
    }

    public Task<List<CountryDto>> GetCountriesAsync(CancellationToken ct = default)
        => _context.Countries
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CountryDto(c.Id, c.Name))
            .ToListAsync(ct);

    public async Task<(Result Result, CountryDto? Country)> CreateCountryAsync(CreateCountryRequest request, CancellationToken ct = default)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return (Result.Failure(new[] { "El nombre del país es obligatorio." }), null);

        if (await _context.Countries.AnyAsync(c => c.Name.ToLower() == name.ToLower(), ct))
            return (Result.Failure(new[] { $"El país '{name}' ya existe en el sistema." }), null);

        var country = new Country { Name = name };
        _context.Countries.Add(country);
        await _context.SaveChangesAsync(ct);

        return (Result.Success(), new CountryDto(country.Id, country.Name));
    }

    public Task<List<EventTypeDto>> GetEventTypesAsync(CancellationToken ct = default)
        => _context.EventTypes
            .AsNoTracking()
            .OrderBy(t => t.Id)
            .Select(t => new EventTypeDto(t.Id, t.Name))
            .ToListAsync(ct);

    public async Task<(Result Result, int? EventId)> CreateEventAsync(CreateEventRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return (Result.Failure(new[] { "El nombre del evento es obligatorio." }), null);

        if (!await _context.Countries.AnyAsync(c => c.Id == request.CountryId, ct))
            return (Result.Failure(new[] { "País no válido." }), null);

        if (!await _context.EventTypes.AnyAsync(t => t.Id == request.EventType, ct))
            return (Result.Failure(new[] { "Tipo de evento no válido." }), null);

        if (!string.IsNullOrWhiteSpace(request.RedId) &&
            !await _context.Reds.AnyAsync(r => r.Id == request.RedId, ct))
            return (Result.Failure(new[] { "Red no válida." }), null);

        var institutionNames = request.Institutions
            .Where(i => !string.IsNullOrWhiteSpace(i))
            .Select(i => i.Trim())
            .GroupBy(i => i.ToLower())
            .Select(g => g.First())
            .ToList();

        var institutions = new List<Institution>();
        foreach (var iname in institutionNames)
        {
            var inst = await _context.Institutions.FirstOrDefaultAsync(x => x.Nombre.ToLower() == iname.ToLower(), ct);
            if (inst is null)
            {
                inst = new Institution { Nombre = iname };
                _context.Institutions.Add(inst);
            }
            institutions.Add(inst);
        }

        var ev = new Event
        {
            Name = request.Name.Trim(),
            CountryId = request.CountryId,
            EventTypeId = request.EventType,
            Institutions = institutions,
            RedId = string.IsNullOrWhiteSpace(request.RedId) ? null : request.RedId,
            EvidenceFileId = request.EvidenceFileId,
            FechaInicio = request.FechaInicio,
            FechaFin = request.FechaFin,
        };

        foreach (var userId in request.OrganizadorIds.Distinct().Where(id => !string.IsNullOrWhiteSpace(id)))
        {
            if (await _context.Users.AnyAsync(u => u.Id == userId, ct))
                ev.Organizadores.Add(new EventOrganizador { UserId = userId });
        }

        _context.Events.Add(ev);
        await _context.SaveChangesAsync(ct);

        return (Result.Success(), ev.Id);
    }

    public async Task<Result> UpdateEventAsync(int id, UpdateEventRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result.Failure(new[] { "El nombre del evento es obligatorio." });

        var ev = await _context.Events.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (ev is null)
            return Result.Failure(new[] { "Evento no encontrado." });

        if (!await _context.Countries.AnyAsync(c => c.Id == request.CountryId, ct))
            return Result.Failure(new[] { "País no válido." });

        if (!await _context.EventTypes.AnyAsync(t => t.Id == request.EventType, ct))
            return Result.Failure(new[] { "Tipo de evento no válido." });

        if (!string.IsNullOrWhiteSpace(request.RedId) &&
            !await _context.Reds.AnyAsync(r => r.Id == request.RedId, ct))
            return Result.Failure(new[] { "Red no válida." });

        ev.Name        = request.Name.Trim();
        ev.CountryId   = request.CountryId;
        ev.EventTypeId = request.EventType;
        ev.RedId       = string.IsNullOrWhiteSpace(request.RedId) ? null : request.RedId;
        ev.FechaInicio = request.FechaInicio;
        ev.FechaFin    = request.FechaFin;

        // Detectar cambio de archivo: capturar el ID anterior ANTES de sobreescribir,
        // y encolar su borrado en MinIO si ha cambiado. Debe hacerse antes del primer
        // SaveChangesAsync para que el job quede en la misma transacción que el evento.
        var oldFileId = ev.EvidenceFileId;
        ev.EvidenceFileId = request.EvidenceFileId;

        if (_deletionQueue is not null
            && oldFileId.HasValue
            && oldFileId != request.EvidenceFileId)
        {
            var oldFile = await _context.StoredFiles.FindAsync(new object[] { oldFileId.Value }, ct);
            if (oldFile is not null)
                await _deletionQueue.EnqueueAsync(oldFile, ct);
        }

        var updatedInstitutions = new List<Institution>();
        foreach (var iname in request.Institutions
            .Where(i => !string.IsNullOrWhiteSpace(i))
            .Select(i => i.Trim())
            .GroupBy(i => i.ToLower())
            .Select(g => g.First()))
        {
            var inst = await _context.Institutions.FirstOrDefaultAsync(x => x.Nombre.ToLower() == iname.ToLower(), ct);
            if (inst is null)
            {
                inst = new Institution { Nombre = iname };
                _context.Institutions.Add(inst);
            }
            updatedInstitutions.Add(inst);
        }

        await _context.SaveChangesAsync(ct);

        var existingInstitutions = await _context.EventInstitutions.Where(ei => ei.EventId == id).ToListAsync(ct);
        _context.EventInstitutions.RemoveRange(existingInstitutions);
        foreach (var inst in updatedInstitutions)
            _context.EventInstitutions.Add(new EventInstitution { EventId = id, InstitutionId = inst.Id });

        var existingOrganizadores = await _context.EventOrganizadores.Where(o => o.EventId == id).ToListAsync(ct);
        _context.EventOrganizadores.RemoveRange(existingOrganizadores);
        foreach (var userId in request.OrganizadorIds.Distinct().Where(uid => !string.IsNullOrWhiteSpace(uid)))
        {
            if (await _context.Users.AnyAsync(u => u.Id == userId, ct))
                _context.EventOrganizadores.Add(new EventOrganizador { EventId = id, UserId = userId });
        }

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteEventAsync(int id, CancellationToken ct = default)
    {
        var ev = await _context.Events.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (ev is null)
            return Result.Failure(new[] { "Evento no encontrado." });

        if (await _context.Presentations.AnyAsync(p => p.EventId == id, ct))
            return Result.Failure(new[] { "No se puede eliminar un evento que tiene presentaciones registradas." });

        if (_deletionQueue is not null && ev.EvidenceFileId.HasValue)
        {
            var file = await _context.StoredFiles.FindAsync([ev.EvidenceFileId.Value], ct);
            if (file is not null)
                await _deletionQueue.EnqueueAsync(file, ct);
        }

        _context.Events.Remove(ev);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    public Task<List<PresentationDto>> GetMyPresentationsAsync(CancellationToken ct = default)
    {
        if (IsSuperuser)
            return GetAllPresentationsAsync(ct);

        return _context.Presentations
            .AsNoTracking()
            .Where(p => p.UserId == _currentUser.Id)
            .Select(p => new PresentationDto
            {
                Id = p.Id,
                Name = p.Name,
                EventId = p.EventId,
                EventName = p.Event.Name,
                Fecha = p.Fecha,
                UserId = p.UserId,
                User = new LinkedUserSummaryDto
                {
                    Id = p.User.Id,
                    UserName = p.User.UserName,
                    UserLastName1 = p.User.UserLastName1,
                    UserLastName2 = p.User.UserLastName2,
                    Email = p.User.Email,
                    IsTrained = p.User.IsTrained,
                    ScientificCategory = (int)p.User.ScientificCategory,
                    TeachingCategory = (int)p.User.TeachingCategory,
                    InvestigationCategory = (int)p.User.InvestigationCategory,
                    AreaId = p.User.AreaId,
                    AreaNombre = p.User.Area != null ? p.User.Area.Nombre : null,
                    UniversidadId = p.User.Area != null ? p.User.Area.UniversidadId : null,
                    UniversidadNombre = p.User.Area != null && p.User.Area.Universidad != null
                        ? p.User.Area.Universidad.Nombre : null,
                },
            })
            .OrderBy(p => p.EventName)
            .ThenBy(p => p.Name)
            .ToListAsync(ct);
    }

    public Task<List<PresentationDto>> GetAllPresentationsAsync(CancellationToken ct = default)
        => _context.Presentations
            .AsNoTracking()
            .Select(p => new PresentationDto
            {
                Id = p.Id,
                Name = p.Name,
                EventId = p.EventId,
                EventName = p.Event.Name,
                Fecha = p.Fecha,
                UserId = p.UserId,
                User = new LinkedUserSummaryDto
                {
                    Id = p.User.Id,
                    UserName = p.User.UserName,
                    UserLastName1 = p.User.UserLastName1,
                    UserLastName2 = p.User.UserLastName2,
                    Email = p.User.Email,
                    IsTrained = p.User.IsTrained,
                    ScientificCategory = (int)p.User.ScientificCategory,
                    TeachingCategory = (int)p.User.TeachingCategory,
                    InvestigationCategory = (int)p.User.InvestigationCategory,
                    AreaId = p.User.AreaId,
                    AreaNombre = p.User.Area != null ? p.User.Area.Nombre : null,
                    UniversidadId = p.User.Area != null ? p.User.Area.UniversidadId : null,
                    UniversidadNombre = p.User.Area != null && p.User.Area.Universidad != null
                        ? p.User.Area.Universidad.Nombre : null,
                },
            })
            .OrderBy(p => p.EventName)
            .ThenBy(p => p.Name)
            .ToListAsync(ct);

    public async Task<List<PresentationDto>> GetAreaPresentationsAsync(CancellationToken ct = default)
    {
        var areaId = await _context.GetUserAreaIdAsync(_currentUser.Id, ct) ?? string.Empty;
        return await _context.Presentations
            .AsNoTracking()
            .Where(p => p.Event.Organizadores.Any(o => o.User != null && o.User.AreaId == areaId)
                     || p.Event.Participaciones.Any(part => part.User != null && part.User.AreaId == areaId))
            .Select(p => new PresentationDto
            {
                Id = p.Id,
                Name = p.Name,
                EventId = p.EventId,
                EventName = p.Event.Name,
                Fecha = p.Fecha,
                UserId = p.UserId,
                User = new LinkedUserSummaryDto
                {
                    Id = p.User.Id,
                    UserName = p.User.UserName,
                    UserLastName1 = p.User.UserLastName1,
                    UserLastName2 = p.User.UserLastName2,
                    Email = p.User.Email,
                    IsTrained = p.User.IsTrained,
                    ScientificCategory = (int)p.User.ScientificCategory,
                    TeachingCategory = (int)p.User.TeachingCategory,
                    InvestigationCategory = (int)p.User.InvestigationCategory,
                    AreaId = p.User.AreaId,
                    AreaNombre = p.User.Area != null ? p.User.Area.Nombre : null,
                    UniversidadId = p.User.Area != null ? p.User.Area.UniversidadId : null,
                    UniversidadNombre = p.User.Area != null && p.User.Area.Universidad != null
                        ? p.User.Area.Universidad.Nombre : null,
                },
            })
            .OrderBy(p => p.EventName)
            .ThenBy(p => p.Name)
            .ToListAsync(ct);
    }

    public async Task<(Result Result, int? PresentationId)> CreatePresentationAsync(CreatePresentationRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return (Result.Failure(new[] { "El nombre de la presentación es obligatorio." }), null);

        if (!await _context.Events.AnyAsync(e => e.Id == request.EventId, ct))
            return (Result.Failure(new[] { "El evento seleccionado no existe." }), null);

        string presUserId;
        if (IsSuperuser)
        {
            if (string.IsNullOrWhiteSpace(request.TargetUserId))
                return (Result.Failure(new[] { "El Superuser debe especificar el usuario (TargetUserId)." }), null);
            if (!await _context.Users.AnyAsync(u => u.Id == request.TargetUserId, ct))
                return (Result.Failure(["Usuario destinatario no encontrado."]), null);
            presUserId = request.TargetUserId;
        }
        else
        {
            if (!await _context.Users.AnyAsync(u => u.Id == _currentUser.Id, ct))
                return (Result.Failure(["Usuario no encontrado."]), null);
            presUserId = _currentUser.Id!;
        }

        var presentation = new Presentation
        {
            Name = request.Name.Trim(),
            EventId = request.EventId,
            UserId = presUserId,
            Fecha = request.Fecha,
        };

        _context.Presentations.Add(presentation);
        await _context.SaveChangesAsync(ct);
        return (Result.Success(), presentation.Id);
    }

    public async Task<Result> UpdatePresentationAsync(int id, UpdatePresentationRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result.Failure(new[] { "El nombre de la presentación es obligatorio." });

        var presentation = await _context.Presentations.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (presentation is null)
            return Result.Failure(new[] { "Presentación no encontrada." });

        if (!IsSuperuser && presentation.UserId != _currentUser.Id)
            return Result.Failure(new[] { "No tienes permiso para modificar esta presentación." });

        if (!await _context.Events.AnyAsync(e => e.Id == request.EventId, ct))
            return Result.Failure(new[] { "El evento seleccionado no existe." });

        presentation.Name = request.Name.Trim();
        presentation.EventId = request.EventId;
        presentation.Fecha = request.Fecha;

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeletePresentationAsync(int id, CancellationToken ct = default)
    {
        var presentation = await _context.Presentations.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (presentation is null)
            return Result.Failure(new[] { "Presentación no encontrada." });

        if (!IsSuperuser && presentation.UserId != _currentUser.Id)
            return Result.Failure(new[] { "No tienes permiso para eliminar esta presentación." });

        _context.Presentations.Remove(presentation);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    private async Task<Dictionary<int, int>> GetPresentationCountsAsync(IEnumerable<int> eventIds, CancellationToken ct)
    {
        var ids = eventIds.ToList();
        if (ids.Count == 0) return new Dictionary<int, int>();

        return await _context.Presentations
            .Where(p => ids.Contains(p.EventId))
            .GroupBy(p => p.EventId)
            .Select(g => new { EventId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EventId, x => x.Count, ct);
    }
}
