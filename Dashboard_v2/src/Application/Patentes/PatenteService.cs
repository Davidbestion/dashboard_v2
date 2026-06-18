using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Common.Models;
using Dashboard_v2.Domain.Entities;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Application.Patentes;

public sealed class PatenteService : IPatenteService
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;
    private readonly IAuthorResolutionService _authorResolution;
    private readonly IProductionCreatorService _creatorService;

    public PatenteService(
        IApplicationDbContext context,
        IUser currentUser,
        IAuthorResolutionService authorResolution,
        IProductionCreatorService creatorService)
    {
        _context = context;
        _currentUser = currentUser;
        _authorResolution = authorResolution;
        _creatorService = creatorService;
    }

    private bool IsInRole(string role) => _currentUser.Roles?.Contains(role) == true;
    private bool IsSuperuser => IsInRole(nameof(RolesEnum.Superuser));
    private bool IsSuperuserOrJefeDeProyecto => IsSuperuser || IsInRole(nameof(RolesEnum.Jefe_de_Proyecto));

    public async Task<List<PatenteDto>> GetAllAsync(CancellationToken ct = default)
    {
        IQueryable<Patente> query = _context.Patentes;
        if (IsInRole(nameof(RolesEnum.Vicedecano_de_investigacion)))
        {
            var areaId = await _context.GetUserAreaIdAsync(_currentUser.Id, ct);
            if (!string.IsNullOrEmpty(areaId))
                query = query.Where(p => p.Creadores.Any(c => c.Author.User != null && c.Author.User.AreaId == areaId));
        }

        return await query
            .Include(p => p.Creadores).ThenInclude(c => c.Author)
            .Select(p => new PatenteDto(
                p.Id, p.Titulo, p.NumeroSolicitudConcesion, p.EsNacional,
                p.Creadores.Select(c => c.Author.Name).ToList(),
                p.Creadores.Select(c => new CreatorDto(c.Author.Id, c.Author.Name, c.Author.UserId)).ToList()))
            .ToListAsync(ct);
    }

    public async Task<List<PatenteDto>> GetMisAsync(CancellationToken ct = default)
    {
        var currentAuthor = await _authorResolution.GetOrCreateForUserAsync(_currentUser.Id!, ct);
        if (currentAuthor == null)
            return [];

        return await _context.AuthorPatentes
            .Where(ap => ap.AuthorId == currentAuthor.Id)
            .Include(ap => ap.Patente).ThenInclude(p => p.Creadores).ThenInclude(c => c.Author)
            .Select(ap => new PatenteDto(
                ap.Patente.Id,
                ap.Patente.Titulo,
                ap.Patente.NumeroSolicitudConcesion,
                ap.Patente.EsNacional,
                ap.Patente.Creadores.Select(c => c.Author.Name).ToList(),
                ap.Patente.Creadores.Select(c => new CreatorDto(c.Author.Id, c.Author.Name, c.Author.UserId)).ToList()))
            .ToListAsync(ct);
    }

    public async Task<(bool Found, List<ProyectoPatenteDto> Proyectos)> GetProyectosDeAsync(string patenteId, CancellationToken ct = default)
    {
        if (!await _context.Patentes.AnyAsync(p => p.Id == patenteId, ct))
            return (false, []);

        var list = await _context.ProyectoPatentes
            .Where(pp => pp.PatenteId == patenteId)
            .Include(pp => pp.Proyecto)
            .Select(pp => new ProyectoPatenteDto(pp.ProyectoId, pp.Proyecto.Titulo))
            .ToListAsync(ct);

        return (true, list);
    }

    public async Task<Result> LinkProyectoAsync(string patenteId, string proyectoId, CancellationToken ct = default)
    {
        if (!await _context.Patentes.AnyAsync(p => p.Id == patenteId, ct))
            return Result.Failure(["Patente no encontrada."]);
        if (!await _context.Proyectos.AnyAsync(p => p.Id == proyectoId, ct))
            return Result.Failure(["Proyecto no encontrado."]);
        if (await _context.ProyectoPatentes.AnyAsync(pp => pp.PatenteId == patenteId && pp.ProyectoId == proyectoId, ct))
            return Result.Failure(["El vinculo ya existe."]);

        if (!IsSuperuserOrJefeDeProyecto)
        {
            var currentAuthor = await _authorResolution.GetOrCreateForUserAsync(_currentUser.Id!, ct);
            if (currentAuthor == null)
                return Result.Failure(["Usuario actual no valido."]);

            var esCreador = await _context.AuthorPatentes.AnyAsync(ap => ap.PatenteId == patenteId && ap.AuthorId == currentAuthor.Id, ct);
            if (!esCreador)
                return Result.Failure(["No tiene permisos sobre esta patente."]);
        }

        _context.ProyectoPatentes.Add(new ProyectoPatente { ProyectoId = proyectoId, PatenteId = patenteId });
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> UnlinkProyectoAsync(string patenteId, string proyectoId, CancellationToken ct = default)
    {
        var link = await _context.ProyectoPatentes
            .FirstOrDefaultAsync(pp => pp.PatenteId == patenteId && pp.ProyectoId == proyectoId, ct);
        if (link == null)
            return Result.Failure(["Vinculo no encontrado."]);

        if (!IsSuperuserOrJefeDeProyecto)
        {
            var currentAuthor = await _authorResolution.GetOrCreateForUserAsync(_currentUser.Id!, ct);
            if (currentAuthor == null)
                return Result.Failure(["Usuario actual no valido."]);

            var esCreador = await _context.AuthorPatentes.AnyAsync(ap => ap.PatenteId == patenteId && ap.AuthorId == currentAuthor.Id, ct);
            if (!esCreador)
                return Result.Failure(["No tiene permisos sobre esta patente."]);
        }

        _context.ProyectoPatentes.Remove(link);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<(Result Result, string? Id)> CreateAsync(CreatePatenteBody body, CancellationToken ct = default)
    {
        var currentAuthor = await _authorResolution.GetOrCreateForUserAsync(_currentUser.Id!, ct);
        if (currentAuthor == null)
            return (Result.Failure(["Usuario actual no valido."]), null);

        var p = new Patente
        {
            Titulo = body.Titulo,
            NumeroSolicitudConcesion = body.NumeroSolicitudConcesion,
            EsNacional = body.EsNacional
        };
        _context.Patentes.Add(p);

        p.Creadores.Add(new AuthorPatente { AuthorId = currentAuthor.Id, PatenteId = p.Id });
        await _creatorService.AddAdditionalCreatorsAsync(
            p.Creadores, currentAuthor.Id,
            authorId => new AuthorPatente { AuthorId = authorId, PatenteId = p.Id },
            c => c.AuthorId,
            body.AdditionalAuthorIds, body.AdditionalAuthorNames, body.AdditionalUserIds, ct);

        await _context.SaveChangesAsync(ct);
        return (Result.Success(), p.Id);
    }

    public async Task<Result> UpdateAsync(string id, UpdatePatenteBody body, CancellationToken ct = default)
    {
        var p = await _context.Patentes
            .Include(x => x.Creadores)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p == null)
            return Result.Failure(["Patente no encontrada."]);

        var currentAuthor = await _authorResolution.GetOrCreateForUserAsync(_currentUser.Id!, ct);
        if (currentAuthor == null)
            return Result.Failure(["Usuario actual no valido."]);

        if (!IsSuperuser)
        {
            var esCreador = await _context.AuthorPatentes.AnyAsync(ap => ap.PatenteId == id && ap.AuthorId == currentAuthor.Id, ct);
            if (!esCreador)
                return Result.Failure(["No tiene permisos sobre esta patente."]);
        }

        p.Titulo = body.Titulo;
        p.NumeroSolicitudConcesion = body.NumeroSolicitudConcesion;
        p.EsNacional = body.EsNacional;

        var toRemove = p.Creadores.Where(c => c.AuthorId != currentAuthor.Id).ToList();
        foreach (var creator in toRemove)
            p.Creadores.Remove(creator);

        await _creatorService.AddAdditionalCreatorsAsync(
            p.Creadores, currentAuthor.Id,
            authorId => new AuthorPatente { AuthorId = authorId, PatenteId = p.Id },
            c => c.AuthorId,
            body.AdditionalAuthorIds, body.AdditionalAuthorNames, body.AdditionalUserIds, ct);

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(string id, CancellationToken ct = default)
    {
        var p = await _context.Patentes.FindAsync(new object[] { id }, ct);
        if (p == null)
            return Result.Failure(["Patente no encontrada."]);

        var currentAuthor = await _authorResolution.GetOrCreateForUserAsync(_currentUser.Id!, ct);
        if (currentAuthor == null)
            return Result.Failure(["Usuario actual no valido."]);

        if (!IsSuperuser)
        {
            var esCreador = await _context.AuthorPatentes.AnyAsync(ap => ap.PatenteId == id && ap.AuthorId == currentAuthor.Id, ct);
            if (!esCreador)
                return Result.Failure(["No tiene permisos sobre esta patente."]);
        }

        _context.Patentes.Remove(p);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
