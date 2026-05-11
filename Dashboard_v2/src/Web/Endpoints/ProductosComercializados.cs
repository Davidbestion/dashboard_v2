using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Web.Endpoints;

public class ProductosComercializados : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("", GetAll)
            .RequireAuthorization()
            .WithName("GetProductosComercializados")
            .Produces<List<ProductoDto>>(200);

        groupBuilder.MapGet("mis", GetMis)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Jefe_de_Proyecto), nameof(RolesEnum.Superuser)))
            .WithName("GetMisProductosComercializados")
            .Produces<List<ProductoDto>>(200);

        groupBuilder.MapPost("", Create)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Jefe_de_Proyecto), nameof(RolesEnum.Superuser)))
            .WithName("CreateProductoComercializado")
            .Produces(201)
            .ProducesProblem(400);

        groupBuilder.MapPut("{id}", Update)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Jefe_de_Proyecto), nameof(RolesEnum.Superuser)))
            .WithName("UpdateProductoComercializado")
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(403)
            .ProducesProblem(404);

        groupBuilder.MapDelete("{id}", Delete)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Jefe_de_Proyecto), nameof(RolesEnum.Superuser)))
            .WithName("DeleteProductoComercializado")
            .Produces(200)
            .ProducesProblem(403)
            .ProducesProblem(404);
    }

    private static async Task<IResult> GetAll(IApplicationDbContext db)
    {
        var list = await db.ProductosComercializados
            .Include(p => p.TipoProductoComercializado)
            .Include(p => p.Institution)
            .Include(p => p.Creadores).ThenInclude(c => c.Author)
            .Select(p => new ProductoDto(
                p.Id, p.Titulo,
                p.TipoProductoComercializadoId, p.TipoProductoComercializado.Nombre,
                p.InstitutionId, p.Institution.Nombre,
                p.Creadores.Select(c => c.Author.Name).ToList(),
                p.Creadores.Select(c => new CreatorDto(c.Author.Id, c.Author.Name, c.Author.UserId)).ToList()))
            .ToListAsync();
        return Results.Ok(list);
    }

    private static async Task<IResult> GetMis(IApplicationDbContext db, IUser currentUser, IAuthorResolutionService authorResolution)
    {
        var currentAuthor = await authorResolution.GetOrCreateForUserAsync(currentUser.Id!, CancellationToken.None);
        if (currentAuthor == null)
            return Results.Ok(new List<ProductoDto>());

        var list = await db.AuthorProductosComercializados
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
            .ToListAsync();
        return Results.Ok(list);
    }

    private static async Task<IResult> Create(IApplicationDbContext db, IUser currentUser, IAuthorResolutionService authorResolution, IProductionCreatorService creatorService, CreateProductoBody body)
    {
        var currentAuthor = await authorResolution.GetOrCreateForUserAsync(currentUser.Id!, CancellationToken.None);
        if (currentAuthor == null)
            return Results.BadRequest(new { errors = new[] { "Usuario actual no valido." } });

        var item = new ProductoComercializado
        {
            Titulo = body.Titulo,
            TipoProductoComercializadoId = body.TipoProductoComercializadoId,
            InstitutionId = body.InstitutionId
        };
        db.ProductosComercializados.Add(item);

        item.Creadores.Add(new AuthorProductoComercializado { AuthorId = currentAuthor.Id, ProductoComercializadoId = item.Id });
        await creatorService.AddAdditionalCreatorsAsync(
            item.Creadores, currentAuthor.Id,
            authorId => new AuthorProductoComercializado { AuthorId = authorId, ProductoComercializadoId = item.Id },
            c => c.AuthorId,
            body.AdditionalAuthorIds, body.AdditionalAuthorNames, body.AdditionalUserIds);

        await db.SaveChangesAsync(CancellationToken.None);
        return Results.Created($"/api/ProductosComercializados/{item.Id}", new { id = item.Id });
    }

    private static async Task<IResult> Update(IApplicationDbContext db, IUser currentUser, IAuthorResolutionService authorResolution, IProductionCreatorService creatorService, string id, UpdateProductoBody body)
    {
        var item = await db.ProductosComercializados
            .Include(p => p.Creadores)
            .FirstOrDefaultAsync(p => p.Id == id, CancellationToken.None);
        if (item == null)
            return Results.NotFound(new { errors = new[] { "Producto no encontrado." } });

        var currentAuthor = await authorResolution.GetOrCreateForUserAsync(currentUser.Id!, CancellationToken.None);
        if (currentAuthor == null)
            return Results.BadRequest(new { errors = new[] { "Usuario actual no valido." } });

        var roles = currentUser.Roles ?? [];
        if (!roles.Contains(nameof(RolesEnum.Superuser)) && !roles.Contains(nameof(RolesEnum.Jefe_de_Proyecto)))
        {
            var esCreador = await db.AuthorProductosComercializados.AnyAsync(ap => ap.ProductoComercializadoId == id && ap.AuthorId == currentAuthor.Id);
            if (!esCreador)
                return Results.Forbid();
        }

        item.Titulo = body.Titulo;
        item.TipoProductoComercializadoId = body.TipoProductoComercializadoId;
        item.InstitutionId = body.InstitutionId;

        var toRemove = item.Creadores.Where(c => c.AuthorId != currentAuthor.Id).ToList();
        foreach (var creator in toRemove)
            item.Creadores.Remove(creator);

        await creatorService.AddAdditionalCreatorsAsync(
            item.Creadores, currentAuthor.Id,
            authorId => new AuthorProductoComercializado { AuthorId = authorId, ProductoComercializadoId = item.Id },
            c => c.AuthorId,
            body.AdditionalAuthorIds, body.AdditionalAuthorNames, body.AdditionalUserIds);

        await db.SaveChangesAsync(CancellationToken.None);
        return Results.Ok(new { message = "Producto actualizado." });
    }

    private static async Task<IResult> Delete(IApplicationDbContext db, IUser currentUser, IAuthorResolutionService authorResolution, string id)
    {
        var item = await db.ProductosComercializados.FindAsync(new object[] { id }, CancellationToken.None);
        if (item == null)
            return Results.NotFound(new { errors = new[] { "Producto no encontrado." } });

        var currentAuthor = await authorResolution.GetOrCreateForUserAsync(currentUser.Id!, CancellationToken.None);
        if (currentAuthor == null)
            return Results.BadRequest(new { errors = new[] { "Usuario actual no valido." } });

        var roles = currentUser.Roles ?? [];
        if (!roles.Contains(nameof(RolesEnum.Superuser)) && !roles.Contains(nameof(RolesEnum.Jefe_de_Proyecto)))
        {
            var esCreador = await db.AuthorProductosComercializados.AnyAsync(ap => ap.ProductoComercializadoId == id && ap.AuthorId == currentAuthor.Id);
            if (!esCreador)
                return Results.Forbid();
        }

        db.ProductosComercializados.Remove(item);
        await db.SaveChangesAsync(CancellationToken.None);
        return Results.Ok(new { message = "Producto eliminado." });
    }
}

public record ProductoDto(
    string Id,
    string Titulo,
    string TipoProductoComercializadoId,
    string TipoProductoComercializadoNombre,
    string InstitutionId,
    string InstitutionNombre,
    List<string> Creadores,
    List<CreatorDto> CreadoresDetalle);
public record CreateProductoBody(
    string Titulo,
    string TipoProductoComercializadoId,
    string InstitutionId,
    List<string>? AdditionalAuthorIds = null,
    List<string>? AdditionalAuthorNames = null,
    List<string>? AdditionalUserIds = null);
public record UpdateProductoBody(
    string Titulo,
    string TipoProductoComercializadoId,
    string InstitutionId,
    List<string>? AdditionalAuthorIds = null,
    List<string>? AdditionalAuthorNames = null,
    List<string>? AdditionalUserIds = null);
