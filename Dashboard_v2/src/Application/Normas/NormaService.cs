using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Common.Models;
using Dashboard_v2.Domain.Entities;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Application.Normas;

public sealed class NormaService : INormaService
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;
    private readonly IAuthorResolutionService _authorResolution;
    private readonly IProductionCreatorService _creatorService;

    public NormaService(
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

    public async Task<List<NormaDto>> GetAllAsync(CancellationToken ct = default)
    {
        IQueryable<Norma> query = _context.Normas;
        if (IsInRole(nameof(RolesEnum.Vicedecano_de_investigacion)))
        {
            var areaId = await _context.GetUserAreaIdAsync(_currentUser.Id, ct);
            if (!string.IsNullOrEmpty(areaId))
                query = query.Where(n => n.Creadores.Any(c => c.Author.User != null && c.Author.User.AreaId == areaId));
        }

        return await query
            .Include(n => n.TipoNorma)
            .Include(n => n.Institution)
            .Include(n => n.Creadores).ThenInclude(c => c.Author)
            .Select(n => new NormaDto(
                n.Id, n.Titulo,
                n.TipoNormaId, n.TipoNorma != null ? n.TipoNorma.Nombre : null,
                n.InstitutionId, n.Institution.Nombre,
                n.Creadores.Select(c => c.Author.Name).ToList(),
                n.Creadores.Select(c => new CreatorDto(c.Author.Id, c.Author.Name, c.Author.UserId)).ToList()))
            .ToListAsync(ct);
    }

    public async Task<List<NormaDto>> GetMisAsync(CancellationToken ct = default)
    {
        var currentAuthor = await _authorResolution.GetOrCreateForUserAsync(_currentUser.Id!, ct);
        if (currentAuthor == null)
            return [];

        return await _context.AuthorNormas
            .Where(an => an.AuthorId == currentAuthor.Id)
            .Include(an => an.Norma).ThenInclude(n => n.TipoNorma)
            .Include(an => an.Norma).ThenInclude(n => n.Institution)
            .Include(an => an.Norma).ThenInclude(n => n.Creadores).ThenInclude(c => c.Author)
            .Select(an => new NormaDto(
                an.Norma.Id, an.Norma.Titulo,
                an.Norma.TipoNormaId, an.Norma.TipoNorma != null ? an.Norma.TipoNorma.Nombre : null,
                an.Norma.InstitutionId, an.Norma.Institution.Nombre,
                an.Norma.Creadores.Select(c => c.Author.Name).ToList(),
                an.Norma.Creadores.Select(c => new CreatorDto(c.Author.Id, c.Author.Name, c.Author.UserId)).ToList()))
            .ToListAsync(ct);
    }

    public async Task<(Result Result, string? Id)> CreateAsync(CreateNormaBody body, CancellationToken ct = default)
    {
        var currentAuthor = await _authorResolution.GetOrCreateForUserAsync(_currentUser.Id!, ct);
        if (currentAuthor == null)
            return (Result.Failure(["Usuario actual no valido."]), null);

        var norma = new Norma
        {
            Titulo = body.Titulo,
            TipoNormaId = body.TipoNormaId,
            InstitutionId = body.InstitutionId
        };
        _context.Normas.Add(norma);

        norma.Creadores.Add(new AuthorNorma { AuthorId = currentAuthor.Id, NormaId = norma.Id });
        await _creatorService.AddAdditionalCreatorsAsync(
            norma.Creadores, currentAuthor.Id,
            authorId => new AuthorNorma { AuthorId = authorId, NormaId = norma.Id },
            c => c.AuthorId,
            body.AdditionalAuthorIds, body.AdditionalAuthorNames, body.AdditionalUserIds, ct);

        await _context.SaveChangesAsync(ct);
        return (Result.Success(), norma.Id);
    }

    public async Task<Result> UpdateAsync(string id, UpdateNormaBody body, CancellationToken ct = default)
    {
        var norma = await _context.Normas
            .Include(n => n.Creadores)
            .FirstOrDefaultAsync(n => n.Id == id, ct);
        if (norma == null)
            return Result.Failure(["Norma no encontrada."]);

        var currentAuthor = await _authorResolution.GetOrCreateForUserAsync(_currentUser.Id!, ct);
        if (currentAuthor == null)
            return Result.Failure(["Usuario actual no valido."]);

        if (!IsSuperuser)
        {
            var esCreador = await _context.AuthorNormas.AnyAsync(an => an.NormaId == id && an.AuthorId == currentAuthor.Id, ct);
            if (!esCreador)
                return Result.Failure(["No tiene permisos sobre esta norma."]);
        }

        norma.Titulo = body.Titulo;
        norma.TipoNormaId = body.TipoNormaId;
        norma.InstitutionId = body.InstitutionId;

        var toRemove = norma.Creadores.Where(c => c.AuthorId != currentAuthor.Id).ToList();
        foreach (var creator in toRemove)
            norma.Creadores.Remove(creator);

        await _creatorService.AddAdditionalCreatorsAsync(
            norma.Creadores, currentAuthor.Id,
            authorId => new AuthorNorma { AuthorId = authorId, NormaId = norma.Id },
            c => c.AuthorId,
            body.AdditionalAuthorIds, body.AdditionalAuthorNames, body.AdditionalUserIds, ct);

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(string id, CancellationToken ct = default)
    {
        var norma = await _context.Normas.FindAsync(new object[] { id }, ct);
        if (norma == null)
            return Result.Failure(["Norma no encontrada."]);

        var currentAuthor = await _authorResolution.GetOrCreateForUserAsync(_currentUser.Id!, ct);
        if (currentAuthor == null)
            return Result.Failure(["Usuario actual no valido."]);

        if (!IsSuperuser)
        {
            var esCreador = await _context.AuthorNormas.AnyAsync(an => an.NormaId == id && an.AuthorId == currentAuthor.Id, ct);
            if (!esCreador)
                return Result.Failure(["No tiene permisos sobre esta norma."]);
        }

        _context.Normas.Remove(norma);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
