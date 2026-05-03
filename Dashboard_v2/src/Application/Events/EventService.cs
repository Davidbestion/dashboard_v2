using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Common.Models;
using Dashboard_v2.Application.Events;
using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dashboard_v2.Application.Events;

public sealed class EventService : IEventService
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;
    private readonly IAuthorResolutionService _authorResolution;

    public EventService(
        IApplicationDbContext context,
        IUser currentUser,
        IAuthorResolutionService authorResolution)
    {
        _context = context;
        _currentUser = currentUser;
        _authorResolution = authorResolution;
    }

    public async Task<List<EventDto>> GetMyEventsAsync(CancellationToken ct = default)
    {
        var authorId = await _context.Authors
            .AsNoTracking()
            .Where(a => a.UserId == _currentUser.Id)
            .Select(a => a.Id)
            .FirstOrDefaultAsync(ct);

        if (authorId is null)
            return new List<EventDto>();

        return await _context.Events
            .AsNoTracking()
            .Where(e => e.Presentations.Any(p => p.AuthorPresentations.Any(ap => ap.AuthorId == authorId)))
            .Select(e => new EventDto
            {
                Id = e.Id,
                Name = e.Name,
                CountryId = e.CountryId,
                CountryName = e.Country.Name,
                EventTypeId = e.EventTypeId,
                EventTypeName = e.EventType.Name,
                Institutions = e.Institutions.Select(i => i.Nombre).ToList(),
                PresentationCount = e.Presentations.Count(p => p.AuthorPresentations.Any(ap => ap.AuthorId == authorId)),
                RedId = e.RedId,
                RedName = e.Red != null ? e.Red.Nombre : null,
            })
            .OrderBy(e => e.Name)
            .ToListAsync(ct);
    }

    public Task<List<EventDto>> GetAllEventsAsync(CancellationToken ct = default)
        => _context.Events
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .Select(e => new EventDto
            {
                Id = e.Id,
                Name = e.Name,
                CountryId = e.CountryId,
                CountryName = e.Country.Name,
                EventTypeId = e.EventTypeId,
                EventTypeName = e.EventType.Name,
                Institutions = e.Institutions.Select(i => i.Nombre).ToList(),
                PresentationCount = e.Presentations.Count,
                RedId = e.RedId,
                RedName = e.Red != null ? e.Red.Nombre : null,
            })
            .ToListAsync(ct);

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

        var exists = await _context.Countries
            .AnyAsync(c => c.Name.ToLower() == name.ToLower(), ct);
        if (exists)
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

        if (!string.IsNullOrWhiteSpace(request.RedId))
        {
            if (!await _context.Reds.AnyAsync(r => r.Id == request.RedId, ct))
                return (Result.Failure(new[] { "Red no válida." }), null);
        }

        var institutionNames = request.Institutions
            .Where(i => !string.IsNullOrWhiteSpace(i))
            .Select(i => i.Trim())
            .GroupBy(i => i.ToLower())
            .Select(g => g.First())
            .ToList();

        var institutions = new List<Institution>();
        foreach (var name in institutionNames)
        {
            var inst = await _context.Institutions.FirstOrDefaultAsync(x => x.Nombre.ToLower() == name.ToLower(), ct);
            if (inst == null)
            {
                inst = new Institution { Nombre = name };
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
        };

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

        if (!string.IsNullOrWhiteSpace(request.RedId))
        {
            if (!await _context.Reds.AnyAsync(r => r.Id == request.RedId, ct))
                return Result.Failure(new[] { "Red no válida." });
        }

        ev.Name = request.Name.Trim();
        ev.CountryId = request.CountryId;
        ev.EventTypeId = request.EventType;
        ev.RedId = string.IsNullOrWhiteSpace(request.RedId) ? null : request.RedId;
        var updatedNames = request.Institutions
            .Where(i => !string.IsNullOrWhiteSpace(i))
            .Select(i => i.Trim())
            .GroupBy(i => i.ToLower())
            .Select(g => g.First())
            .ToList();

        var updatedInstitutions = new List<Institution>();
        foreach (var name in updatedNames)
        {
            var inst = await _context.Institutions.FirstOrDefaultAsync(x => x.Nombre.ToLower() == name.ToLower(), ct);
            if (inst == null)
            {
                inst = new Institution { Nombre = name };
                _context.Institutions.Add(inst);
            }
            updatedInstitutions.Add(inst);
        }

        ev.Institutions = updatedInstitutions;

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteEventAsync(int id, CancellationToken ct = default)
    {
        var ev = await _context.Events.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (ev is null)
            return Result.Failure(new[] { "Evento no encontrado." });

        var hasPresentations = await _context.Presentations
            .AnyAsync(p => p.EventId == id, ct);

        if (hasPresentations)
            return Result.Failure(new[] { "No se puede eliminar un evento que tiene presentaciones registradas." });

        _context.Events.Remove(ev);
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }

    public Task<List<PresentationDto>> GetMyPresentationsAsync(CancellationToken ct = default)
    {
        return _context.Presentations
            .AsNoTracking()
            .Where(p => p.AuthorPresentations.Any(ap => ap.Author.UserId == _currentUser.Id))
            .Select(p => new PresentationDto
            {
                Id = p.Id,
                Name = p.Name,
                EventId = p.EventId,
                EventName = p.Event.Name,
                Authors = p.AuthorPresentations.Select(ap => new PresentationAuthorDto
                {
                    Id = ap.Author.Id,
                    Name = ap.Author.Name,
                    UserId = ap.Author.UserId,
                    LinkedUser = ap.Author.User == null ? null : new LinkedUserSummaryDto
                    {
                        Id = ap.Author.User.Id,
                        UserName = ap.Author.User.UserName,
                        UserLastName1 = ap.Author.User.UserLastName1,
                        UserLastName2 = ap.Author.User.UserLastName2,
                        Email = ap.Author.User.Email,
                        IsTrained = ap.Author.User.IsTrained,
                        ScientificCategory = (int)ap.Author.User.ScientificCategory,
                        TeachingCategory = (int)ap.Author.User.TeachingCategory,
                        InvestigationCategory = (int)ap.Author.User.InvestigationCategory,
                        AreaId = ap.Author.User.AreaId,
                        AreaNombre = ap.Author.User.Area != null ? ap.Author.User.Area.Nombre : null,
                        UniversidadId = ap.Author.User.Area != null ? ap.Author.User.Area.UniversidadId : null,
                        UniversidadNombre = ap.Author.User.Area != null && ap.Author.User.Area.Universidad != null
                            ? ap.Author.User.Area.Universidad.Nombre
                            : null
                    }
                }).ToList(),
            })
            .OrderBy(p => p.EventName)
            .ThenBy(p => p.Name)
            .ToListAsync(ct);
    }

    public Task<List<PresentationDto>> GetAllPresentationsAsync(CancellationToken ct = default)
    {
        return _context.Presentations
            .AsNoTracking()
            .Select(p => new PresentationDto
            {
                Id = p.Id,
                Name = p.Name,
                EventId = p.EventId,
                EventName = p.Event.Name,
                Authors = p.AuthorPresentations.Select(ap => new PresentationAuthorDto
                {
                    Id = ap.Author.Id,
                    Name = ap.Author.Name,
                    UserId = ap.Author.UserId,
                    LinkedUser = ap.Author.User == null ? null : new LinkedUserSummaryDto
                    {
                        Id = ap.Author.User.Id,
                        UserName = ap.Author.User.UserName,
                        UserLastName1 = ap.Author.User.UserLastName1,
                        UserLastName2 = ap.Author.User.UserLastName2,
                        Email = ap.Author.User.Email,
                        IsTrained = ap.Author.User.IsTrained,
                        ScientificCategory = (int)ap.Author.User.ScientificCategory,
                        TeachingCategory = (int)ap.Author.User.TeachingCategory,
                        InvestigationCategory = (int)ap.Author.User.InvestigationCategory,
                        AreaId = ap.Author.User.AreaId,
                        AreaNombre = ap.Author.User.Area != null ? ap.Author.User.Area.Nombre : null,
                        UniversidadId = ap.Author.User.Area != null ? ap.Author.User.Area.UniversidadId : null,
                        UniversidadNombre = ap.Author.User.Area != null && ap.Author.User.Area.Universidad != null
                            ? ap.Author.User.Area.Universidad.Nombre
                            : null
                    }
                }).ToList(),
            })
            .OrderBy(p => p.EventName)
            .ThenBy(p => p.Name)
            .ToListAsync(ct);
    }

    public async Task<(Result Result, int? PresentationId)> CreatePresentationAsync(CreatePresentationRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return (Result.Failure(new[] { "El nombre de la presentación es obligatorio." }), null);

        var eventExists = await _context.Events
            .AnyAsync(e => e.Id == request.EventId, ct);
        if (!eventExists)
            return (Result.Failure(new[] { "El evento seleccionado no existe." }), null);

        var author = await _authorResolution.GetOrCreateForUserAsync(_currentUser.Id!, ct);
        if (author is null)
            return (Result.Failure(["Usuario no encontrado."]), null);

        var presentation = new Presentation
        {
            Name = request.Name.Trim(),
            EventId = request.EventId,
            AuthorPresentations = new List<AuthorPresentation> { new AuthorPresentation { AuthorId = author.Id } },
        };

        foreach (var coid in request.CoauthorIds.Where(id => !string.IsNullOrWhiteSpace(id)))
        {
            if (coid != author.Id && await _context.Authors.AnyAsync(a => a.Id == coid, ct))
                presentation.AuthorPresentations.Add(new AuthorPresentation { AuthorId = coid });
        }

        foreach (var userId in request.CoauthorUserIds.Where(id => !string.IsNullOrWhiteSpace(id)))
        {
            if (userId == _currentUser.Id)
                continue;

            var coAuthor = await _authorResolution.GetOrCreateForUserAsync(userId, ct);
            if (coAuthor == null)
                continue;

            if (presentation.AuthorPresentations.All(ap => ap.AuthorId != coAuthor.Id))
                presentation.AuthorPresentations.Add(new AuthorPresentation { AuthorId = coAuthor.Id });
        }

        foreach (var name in request.CoauthorNames.Where(n => !string.IsNullOrWhiteSpace(n)))
        {
            var resolved = await _authorResolution.ResolveByNameAsync(name, ct);
            if (presentation.AuthorPresentations.All(ap => ap.AuthorId != resolved.Id))
                presentation.AuthorPresentations.Add(new AuthorPresentation { AuthorId = resolved.Id });
        }

        _context.Presentations.Add(presentation);
        await _context.SaveChangesAsync(ct);

        return (Result.Success(), presentation.Id);
    }

    public async Task<Result> UpdatePresentationAsync(int id, UpdatePresentationRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result.Failure(new[] { "El nombre de la presentación es obligatorio." });

        var authorId = await _context.Authors
            .AsNoTracking()
            .Where(a => a.UserId == _currentUser.Id)
            .Select(a => a.Id)
            .FirstOrDefaultAsync(ct);

        if (authorId is null)
            return Result.Failure(new[] { "No tienes un perfil de autor." });

        var presentation = await _context.Presentations
            .Include(p => p.AuthorPresentations)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (presentation is null)
            return Result.Failure(new[] { "Presentación no encontrada." });

        if (!presentation.AuthorPresentations.Any(ap => ap.AuthorId == authorId))
            return Result.Failure(new[] { "No tienes permiso para modificar esta presentación." });

        var eventExists = await _context.Events.AnyAsync(e => e.Id == request.EventId, ct);
        if (!eventExists)
            return Result.Failure(new[] { "El evento seleccionado no existe." });

        presentation.Name = request.Name.Trim();
        presentation.EventId = request.EventId;

        var toRemove = presentation.AuthorPresentations
            .Where(ap => ap.AuthorId != authorId)
            .ToList();
        foreach (var link in toRemove)
            _context.AuthorPresentations.Remove(link);

        foreach (var coid in request.CoauthorIds.Where(i => !string.IsNullOrWhiteSpace(i)))
        {
            if (coid != authorId && await _context.Authors.AnyAsync(a => a.Id == coid, ct))
                presentation.AuthorPresentations.Add(new AuthorPresentation { AuthorId = coid });
        }

        foreach (var userId in request.CoauthorUserIds.Where(id => !string.IsNullOrWhiteSpace(id)))
        {
            if (userId == _currentUser.Id)
                continue;

            var coAuthor = await _authorResolution.GetOrCreateForUserAsync(userId, ct);
            if (coAuthor == null)
                continue;

            if (presentation.AuthorPresentations.All(ap => ap.AuthorId != coAuthor.Id))
                presentation.AuthorPresentations.Add(new AuthorPresentation { AuthorId = coAuthor.Id });
        }

        foreach (var name in request.CoauthorNames.Where(n => !string.IsNullOrWhiteSpace(n)))
        {
            var resolved = await _authorResolution.ResolveByNameAsync(name, ct);
            if (presentation.AuthorPresentations.All(ap => ap.AuthorId != resolved.Id))
                presentation.AuthorPresentations.Add(new AuthorPresentation { AuthorId = resolved.Id });
        }

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeletePresentationAsync(int id, CancellationToken ct = default)
    {
        var authorId = await _context.Authors
            .AsNoTracking()
            .Where(a => a.UserId == _currentUser.Id)
            .Select(a => a.Id)
            .FirstOrDefaultAsync(ct);

        if (authorId is null)
            return Result.Failure(new[] { "No tienes un perfil de autor." });

        var presentation = await _context.Presentations
            .Include(p => p.AuthorPresentations)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (presentation is null)
            return Result.Failure(new[] { "Presentación no encontrada." });

        if (!presentation.AuthorPresentations.Any(ap => ap.AuthorId == authorId))
            return Result.Failure(new[] { "No tienes permiso para eliminar esta presentación." });

        _context.Presentations.Remove(presentation);
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
