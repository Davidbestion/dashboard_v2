using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Common.Models;
using Dashboard_v2.Domain.Entities;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Application.Registros;

public sealed class RegistroService : IRegistroService
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;
    private readonly IAuthorResolutionService _authorResolution;
    private readonly IProductionCreatorService _creatorService;

    public RegistroService(
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

    public async Task<List<RegistroDto>> GetAllAsync(CancellationToken ct = default)
    {
        IQueryable<Registro> query = _context.Registros;
        if (IsInRole(nameof(RolesEnum.Vicedecano_de_investigacion)))
        {
            var areaId = await _context.GetUserAreaIdAsync(_currentUser.Id, ct);
            if (!string.IsNullOrEmpty(areaId))
                query = query.Where(r => r.Creadores.Any(c => c.Author.User != null && c.Author.User.AreaId == areaId));
        }

        return await query
            .Include(r => r.Country)
            .Include(r => r.Institution)
            .Include(r => r.Creadores).ThenInclude(c => c.Author)
            .Select(r => new RegistroDto(
                r.Id, r.Titulo, r.NumeroCertificado, r.EsInformatico,
                r.CountryId, r.Country.Name, r.InstitutionId, r.Institution.Nombre, r.EvidenceFileId,
                r.Creadores.Select(c => c.Author.Name).ToList(),
                r.Creadores.Select(c => new CreatorDto(c.Author.Id, c.Author.Name, c.Author.UserId)).ToList()))
            .ToListAsync(ct);
    }

    public async Task<List<RegistroDto>> GetMisAsync(CancellationToken ct = default)
    {
        var currentAuthor = await _authorResolution.GetOrCreateForUserAsync(_currentUser.Id!, ct);
        if (currentAuthor == null)
            return [];

        return await _context.AuthorRegistros
            .Where(ar => ar.AuthorId == currentAuthor.Id)
            .Include(ar => ar.Registro).ThenInclude(r => r.Country)
            .Include(ar => ar.Registro).ThenInclude(r => r.Institution)
            .Include(ar => ar.Registro).ThenInclude(r => r.Creadores).ThenInclude(c => c.Author)
            .Select(ar => new RegistroDto(
                ar.Registro.Id, ar.Registro.Titulo, ar.Registro.NumeroCertificado, ar.Registro.EsInformatico,
                ar.Registro.CountryId, ar.Registro.Country.Name,
                ar.Registro.InstitutionId, ar.Registro.Institution.Nombre, ar.Registro.EvidenceFileId,
                ar.Registro.Creadores.Select(c => c.Author.Name).ToList(),
                ar.Registro.Creadores.Select(c => new CreatorDto(c.Author.Id, c.Author.Name, c.Author.UserId)).ToList()))
            .ToListAsync(ct);
    }

    public async Task<(Result Result, string? Id)> CreateAsync(CreateRegistroBody body, CancellationToken ct = default)
    {
        var currentAuthor = await _authorResolution.GetOrCreateForUserAsync(_currentUser.Id!, ct);
        if (currentAuthor == null)
            return (Result.Failure(["Usuario actual no valido."]), null);

        var registro = new Registro
        {
            Titulo = body.Titulo,
            NumeroCertificado = body.NumeroCertificado,
            EsInformatico = body.EsInformatico,
            CountryId = body.CountryId,
            InstitutionId = body.InstitutionId,
            EvidenceFileId = body.EvidenceFileId,
        };
        _context.Registros.Add(registro);

        registro.Creadores.Add(new AuthorRegistro { AuthorId = currentAuthor.Id, RegistroId = registro.Id });
        await _creatorService.AddAdditionalCreatorsAsync(
            registro.Creadores, currentAuthor.Id,
            authorId => new AuthorRegistro { AuthorId = authorId, RegistroId = registro.Id },
            c => c.AuthorId,
            body.AdditionalAuthorIds, body.AdditionalAuthorNames, body.AdditionalUserIds, ct);

        await _context.SaveChangesAsync(ct);
        return (Result.Success(), registro.Id);
    }

    public async Task<Result> UpdateAsync(string id, UpdateRegistroBody body, CancellationToken ct = default)
    {
        var registro = await _context.Registros
            .Include(r => r.Creadores)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
        if (registro == null)
            return Result.Failure(["Registro no encontrado."]);

        var currentAuthor = await _authorResolution.GetOrCreateForUserAsync(_currentUser.Id!, ct);
        if (currentAuthor == null)
            return Result.Failure(["Usuario actual no valido."]);

        if (!IsSuperuser)
        {
            var esCreador = await _context.AuthorRegistros.AnyAsync(ar => ar.RegistroId == id && ar.AuthorId == currentAuthor.Id, ct);
            if (!esCreador)
                return Result.Failure(["No tiene permisos sobre este registro."]);
        }

        registro.Titulo = body.Titulo;
        registro.NumeroCertificado = body.NumeroCertificado;
        registro.EsInformatico = body.EsInformatico;
        registro.CountryId = body.CountryId;
        registro.InstitutionId = body.InstitutionId;
        registro.EvidenceFileId = body.EvidenceFileId;

        var toRemove = registro.Creadores.Where(c => c.AuthorId != currentAuthor.Id).ToList();
        foreach (var creator in toRemove)
            registro.Creadores.Remove(creator);

        await _creatorService.AddAdditionalCreatorsAsync(
            registro.Creadores, currentAuthor.Id,
            authorId => new AuthorRegistro { AuthorId = authorId, RegistroId = registro.Id },
            c => c.AuthorId,
            body.AdditionalAuthorIds, body.AdditionalAuthorNames, body.AdditionalUserIds, ct);

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(string id, CancellationToken ct = default)
    {
        var registro = await _context.Registros.FindAsync(new object[] { id }, ct);
        if (registro == null)
            return Result.Failure(["Registro no encontrado."]);

        var currentAuthor = await _authorResolution.GetOrCreateForUserAsync(_currentUser.Id!, ct);
        if (currentAuthor == null)
            return Result.Failure(["Usuario actual no valido."]);

        if (!IsSuperuser)
        {
            var esCreador = await _context.AuthorRegistros.AnyAsync(ar => ar.RegistroId == id && ar.AuthorId == currentAuthor.Id, ct);
            if (!esCreador)
                return Result.Failure(["No tiene permisos sobre este registro."]);
        }

        _context.Registros.Remove(registro);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
