using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Common.Models;
using Dashboard_v2.Domain.Entities;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Application.ProductosComercializados;

public sealed class ProductoComercializadoService : IProductoComercializadoService
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;
    private readonly IAuthorResolutionService _authorResolution;
    private readonly IProductionCreatorService _creatorService;

    public ProductoComercializadoService(
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

    public async Task<List<ProductoDto>> GetAllAsync(CancellationToken ct = default)
    {
        IQueryable<ProductoComercializado> query = _context.ProductosComercializados;
        if (IsInRole(nameof(RolesEnum.Vicedecano_de_investigacion)))
        {
            var areaId = await _context.GetUserAreaIdAsync(_currentUser.Id, ct);
            if (!string.IsNullOrEmpty(areaId))
                query = query.Where(p => p.Creadores.Any(c => c.Author.User != null && c.Author.User.AreaId == areaId));
        }

        return await query
            .Include(p => p.TipoProductoComercializado)
            .Include(p => p.Institution)
            .Include(p => p.Creadores).ThenInclude(c => c.Author)
            .Select(p => new ProductoDto(
                p.Id, p.Titulo,
                p.TipoProductoComercializadoId, p.TipoProductoComercializado.Nombre,
                p.InstitutionId, p.Institution.Nombre,
                p.Creadores.Select(c => c.Author.Name).ToList(),
                p.Creadores.Select(c => new CreatorDto(c.Author.Id, c.Author.Name, c.Author.UserId)).ToList()))
            .ToListAsync(ct);
    }

    public async Task<List<ProductoDto>> GetMisAsync(CancellationToken ct = default)
    {
        var currentAuthor = await _authorResolution.GetOrCreateForUserAsync(_currentUser.Id!, ct);
        if (currentAuthor == null)
            return [];

        return await _context.AuthorProductosComercializados
            .Where(ap => ap.AuthorId == currentAuthor.Id)
            .Include(ap => ap.ProductoComercializado).ThenInclude(p => p.TipoProductoComercializado)
            .Include(ap => ap.ProductoComercializado).ThenInclude(p => p.Institution)
            .Include(ap => ap.ProductoComercializado).ThenInclude(p => p.Creadores).ThenInclude(c => c.Author)
            .Select(ap => new ProductoDto(
                ap.ProductoComercializado.Id, ap.ProductoComercializado.Titulo,
                ap.ProductoComercializado.TipoProductoComercializadoId,
                ap.ProductoComercializado.TipoProductoComercializado.Nombre,
                ap.ProductoComercializado.InstitutionId, ap.ProductoComercializado.Institution.Nombre,
                ap.ProductoComercializado.Creadores.Select(c => c.Author.Name).ToList(),
                ap.ProductoComercializado.Creadores.Select(c => new CreatorDto(c.Author.Id, c.Author.Name, c.Author.UserId)).ToList()))
            .ToListAsync(ct);
    }

    public async Task<(Result Result, string? Id)> CreateAsync(CreateProductoBody body, CancellationToken ct = default)
    {
        var currentAuthor = await _authorResolution.GetOrCreateForUserAsync(_currentUser.Id!, ct);
        if (currentAuthor == null)
            return (Result.Failure(["Usuario actual no valido."]), null);

        var item = new ProductoComercializado
        {
            Titulo = body.Titulo,
            TipoProductoComercializadoId = body.TipoProductoComercializadoId,
            InstitutionId = body.InstitutionId
        };
        _context.ProductosComercializados.Add(item);

        item.Creadores.Add(new AuthorProductoComercializado { AuthorId = currentAuthor.Id, ProductoComercializadoId = item.Id });
        await _creatorService.AddAdditionalCreatorsAsync(
            item.Creadores, currentAuthor.Id,
            authorId => new AuthorProductoComercializado { AuthorId = authorId, ProductoComercializadoId = item.Id },
            c => c.AuthorId,
            body.AdditionalAuthorIds, body.AdditionalAuthorNames, body.AdditionalUserIds, ct);

        await _context.SaveChangesAsync(ct);
        return (Result.Success(), item.Id);
    }

    public async Task<Result> UpdateAsync(string id, UpdateProductoBody body, CancellationToken ct = default)
    {
        var item = await _context.ProductosComercializados
            .Include(p => p.Creadores)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
        if (item == null)
            return Result.Failure(["Producto no encontrado."]);

        var currentAuthor = await _authorResolution.GetOrCreateForUserAsync(_currentUser.Id!, ct);
        if (currentAuthor == null)
            return Result.Failure(["Usuario actual no valido."]);

        if (!IsSuperuser)
        {
            var esCreador = await _context.AuthorProductosComercializados.AnyAsync(ap => ap.ProductoComercializadoId == id && ap.AuthorId == currentAuthor.Id, ct);
            if (!esCreador)
                return Result.Failure(["No tiene permisos sobre este producto."]);
        }

        item.Titulo = body.Titulo;
        item.TipoProductoComercializadoId = body.TipoProductoComercializadoId;
        item.InstitutionId = body.InstitutionId;

        var toRemove = item.Creadores.Where(c => c.AuthorId != currentAuthor.Id).ToList();
        foreach (var creator in toRemove)
            item.Creadores.Remove(creator);

        await _creatorService.AddAdditionalCreatorsAsync(
            item.Creadores, currentAuthor.Id,
            authorId => new AuthorProductoComercializado { AuthorId = authorId, ProductoComercializadoId = item.Id },
            c => c.AuthorId,
            body.AdditionalAuthorIds, body.AdditionalAuthorNames, body.AdditionalUserIds, ct);

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(string id, CancellationToken ct = default)
    {
        var item = await _context.ProductosComercializados.FindAsync(new object[] { id }, ct);
        if (item == null)
            return Result.Failure(["Producto no encontrado."]);

        var currentAuthor = await _authorResolution.GetOrCreateForUserAsync(_currentUser.Id!, ct);
        if (currentAuthor == null)
            return Result.Failure(["Usuario actual no valido."]);

        if (!IsSuperuser)
        {
            var esCreador = await _context.AuthorProductosComercializados.AnyAsync(ap => ap.ProductoComercializadoId == id && ap.AuthorId == currentAuthor.Id, ct);
            if (!esCreador)
                return Result.Failure(["No tiene permisos sobre este producto."]);
        }

        _context.ProductosComercializados.Remove(item);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
