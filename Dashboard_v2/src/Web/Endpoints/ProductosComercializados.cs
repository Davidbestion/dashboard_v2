using Dashboard_v2.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;
using System.Collections.Generic;
using System.Linq;

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
            .ProducesProblem(404);

        groupBuilder.MapDelete("{id}", Delete)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Jefe_de_Proyecto), nameof(RolesEnum.Superuser)))
            .WithName("DeleteProductoComercializado")
            .Produces(200)
            .ProducesProblem(404);
    }

    private async Task<IResult> GetAll(IApplicationDbContext db)
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
                p.Creadores.Select(c => new ProductoCreatorDto(c.Author.Id, c.Author.Name, c.Author.UserId)).ToList()))
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
                ap.ProductoComercializado.Creadores.Select(c => new ProductoCreatorDto(c.Author.Id, c.Author.Name, c.Author.UserId)).ToList()))
            .ToListAsync();
        return Results.Ok(list);
    }

    private async Task<IResult> Create(IApplicationDbContext db, IUser currentUser, IAuthorResolutionService authorResolution, CreateProductoBody body)
    {
        var currentAuthor = await authorResolution.GetOrCreateForUserAsync(currentUser.Id!, CancellationToken.None);
        if (currentAuthor == null)
            return Results.BadRequest(new { errors = new[] { "Usuario actual no valido." } });

        var item = new Dashboard_v2.Domain.Entities.ProductoComercializado
        {
            Titulo = body.Titulo,
            TipoProductoComercializadoId = body.TipoProductoComercializadoId,
            InstitutionId = body.InstitutionId
        };
        db.ProductosComercializados.Add(item);

        item.Creadores.Add(new Dashboard_v2.Domain.Entities.AuthorProductoComercializado { AuthorId = currentAuthor.Id, ProductoComercializadoId = item.Id });
        await AddAdditionalCreatorsAsync(db, authorResolution, item, currentAuthor.Id, body.AdditionalAuthorIds, body.AdditionalAuthorNames, body.AdditionalUserIds);

        await db.SaveChangesAsync(CancellationToken.None);
        return Results.Created($"/api/ProductosComercializados/{item.Id}", new { id = item.Id });
    }

    private async Task<IResult> Update(IApplicationDbContext db, IUser currentUser, IAuthorResolutionService authorResolution, string id, UpdateProductoBody body)
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

        await AddAdditionalCreatorsAsync(db, authorResolution, item, currentAuthor.Id, body.AdditionalAuthorIds, body.AdditionalAuthorNames, body.AdditionalUserIds);

        await db.SaveChangesAsync(CancellationToken.None);
        return Results.Ok(new { message = "Producto actualizado." });
    }

    private async Task<IResult> Delete(IApplicationDbContext db, IUser currentUser, IAuthorResolutionService authorResolution, string id)
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

    private static async Task AddAdditionalCreatorsAsync(
        IApplicationDbContext db,
        IAuthorResolutionService authorResolution,
        Dashboard_v2.Domain.Entities.ProductoComercializado item,
        string currentAuthorId,
        IEnumerable<string>? additionalAuthorIds,
        IEnumerable<string>? additionalAuthorNames,
        IEnumerable<string>? additionalUserIds)
    {
        foreach (var authorId in (additionalAuthorIds ?? []).Where(id => !string.IsNullOrWhiteSpace(id)))
        {
            if (authorId == currentAuthorId) continue;
            if (item.Creadores.Any(c => c.AuthorId == authorId)) continue;
            if (!await db.Authors.AnyAsync(a => a.Id == authorId, CancellationToken.None)) continue;
            item.Creadores.Add(new Dashboard_v2.Domain.Entities.AuthorProductoComercializado { AuthorId = authorId, ProductoComercializadoId = item.Id });
        }

        foreach (var authorName in (additionalAuthorNames ?? []).Where(name => !string.IsNullOrWhiteSpace(name)))
        {
            var resolved = await authorResolution.ResolveByNameAsync(authorName, CancellationToken.None);
            if (resolved.Id == currentAuthorId) continue;
            if (item.Creadores.Any(c => c.AuthorId == resolved.Id)) continue;
            item.Creadores.Add(new Dashboard_v2.Domain.Entities.AuthorProductoComercializado { AuthorId = resolved.Id, ProductoComercializadoId = item.Id });
        }

        foreach (var userId in (additionalUserIds ?? []).Where(id => !string.IsNullOrWhiteSpace(id)))
        {
            var resolved = await authorResolution.GetOrCreateForUserAsync(userId, CancellationToken.None);
            if (resolved == null || resolved.Id == currentAuthorId) continue;
            if (item.Creadores.Any(c => c.AuthorId == resolved.Id)) continue;
            item.Creadores.Add(new Dashboard_v2.Domain.Entities.AuthorProductoComercializado { AuthorId = resolved.Id, ProductoComercializadoId = item.Id });
        }
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
    List<ProductoCreatorDto> CreadoresDetalle);
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
public record ProductoCreatorDto(string Id, string Name, string? UserId);
