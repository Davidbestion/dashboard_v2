using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Web.Endpoints;

public class Normas : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("", GetNormas)
            .RequireAuthorization()
            .WithName("GetNormas")
            .Produces<List<NormaDto>>(200);

        groupBuilder.MapGet("mis", GetMisNormas)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Jefe_de_Proyecto), nameof(RolesEnum.Superuser)))
            .WithName("GetMisNormas")
            .Produces<List<NormaDto>>(200);

        groupBuilder.MapPost("", CreateNorma)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Jefe_de_Proyecto), nameof(RolesEnum.Superuser)))
            .WithName("CreateNorma")
            .Produces(201)
            .ProducesProblem(400);

        groupBuilder.MapPut("{id}", UpdateNorma)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Jefe_de_Proyecto), nameof(RolesEnum.Superuser)))
            .WithName("UpdateNorma")
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(403)
            .ProducesProblem(404);

        groupBuilder.MapDelete("{id}", DeleteNorma)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Jefe_de_Proyecto), nameof(RolesEnum.Superuser)))
            .WithName("DeleteNorma")
            .Produces(200)
            .ProducesProblem(403)
            .ProducesProblem(404);
    }

    private static async Task<IResult> GetNormas(IApplicationDbContext db)
    {
        var list = await db.Normas
            .Include(n => n.Institution)
            .Include(n => n.Creadores).ThenInclude(c => c.Author)
            .Select(n => new NormaDto(
                n.Id, n.Titulo, n.Tipo, n.InstitutionId, n.Institution.Nombre,
                n.Creadores.Select(c => c.Author.Name).ToList(),
                n.Creadores.Select(c => new CreatorDto(c.Author.Id, c.Author.Name, c.Author.UserId)).ToList()))
            .ToListAsync();
        return Results.Ok(list);
    }

    private static async Task<IResult> GetMisNormas(IApplicationDbContext db, IUser currentUser, IAuthorResolutionService authorResolution)
    {
        var currentAuthor = await authorResolution.GetOrCreateForUserAsync(currentUser.Id!, CancellationToken.None);
        if (currentAuthor == null)
            return Results.Ok(new List<NormaDto>());

        var list = await db.AuthorNormas
            .Where(an => an.AuthorId == currentAuthor.Id)
            .Include(an => an.Norma).ThenInclude(n => n.Institution)
            .Include(an => an.Norma).ThenInclude(n => n.Creadores).ThenInclude(c => c.Author)
            .Select(an => new NormaDto(
                an.Norma.Id, an.Norma.Titulo, an.Norma.Tipo,
                an.Norma.InstitutionId, an.Norma.Institution.Nombre,
                an.Norma.Creadores.Select(c => c.Author.Name).ToList(),
                an.Norma.Creadores.Select(c => new CreatorDto(c.Author.Id, c.Author.Name, c.Author.UserId)).ToList()))
            .ToListAsync();
        return Results.Ok(list);
    }

    private static async Task<IResult> CreateNorma(IApplicationDbContext db, IUser currentUser, IAuthorResolutionService authorResolution, IProductionCreatorService creatorService, CreateNormaBody body)
    {
        var currentAuthor = await authorResolution.GetOrCreateForUserAsync(currentUser.Id!, CancellationToken.None);
        if (currentAuthor == null)
            return Results.BadRequest(new { errors = new[] { "Usuario actual no valido." } });

        var norma = new Norma
        {
            Titulo = body.Titulo,
            Tipo = body.Tipo,
            InstitutionId = body.InstitutionId
        };
        db.Normas.Add(norma);

        norma.Creadores.Add(new AuthorNorma { AuthorId = currentAuthor.Id, NormaId = norma.Id });
        await creatorService.AddAdditionalCreatorsAsync(
            norma.Creadores, currentAuthor.Id,
            authorId => new AuthorNorma { AuthorId = authorId, NormaId = norma.Id },
            c => c.AuthorId,
            body.AdditionalAuthorIds, body.AdditionalAuthorNames, body.AdditionalUserIds);

        await db.SaveChangesAsync(CancellationToken.None);
        return Results.Created($"/api/Normas/{norma.Id}", new { id = norma.Id });
    }

    private static async Task<IResult> UpdateNorma(IApplicationDbContext db, IUser currentUser, IAuthorResolutionService authorResolution, IProductionCreatorService creatorService, string id, UpdateNormaBody body)
    {
        var norma = await db.Normas
            .Include(n => n.Creadores)
            .FirstOrDefaultAsync(n => n.Id == id, CancellationToken.None);
        if (norma == null)
            return Results.NotFound(new { errors = new[] { "Norma no encontrada." } });

        var currentAuthor = await authorResolution.GetOrCreateForUserAsync(currentUser.Id!, CancellationToken.None);
        if (currentAuthor == null)
            return Results.BadRequest(new { errors = new[] { "Usuario actual no valido." } });

        var roles = currentUser.Roles ?? [];
        if (!roles.Contains(nameof(RolesEnum.Superuser)) && !roles.Contains(nameof(RolesEnum.Jefe_de_Proyecto)))
        {
            var esCreador = await db.AuthorNormas.AnyAsync(an => an.NormaId == id && an.AuthorId == currentAuthor.Id);
            if (!esCreador)
                return Results.Forbid();
        }

        norma.Titulo = body.Titulo;
        norma.Tipo = body.Tipo;
        norma.InstitutionId = body.InstitutionId;

        var toRemove = norma.Creadores.Where(c => c.AuthorId != currentAuthor.Id).ToList();
        foreach (var creator in toRemove)
            norma.Creadores.Remove(creator);

        await creatorService.AddAdditionalCreatorsAsync(
            norma.Creadores, currentAuthor.Id,
            authorId => new AuthorNorma { AuthorId = authorId, NormaId = norma.Id },
            c => c.AuthorId,
            body.AdditionalAuthorIds, body.AdditionalAuthorNames, body.AdditionalUserIds);

        await db.SaveChangesAsync(CancellationToken.None);
        return Results.Ok(new { message = "Norma actualizada." });
    }

    private static async Task<IResult> DeleteNorma(IApplicationDbContext db, IUser currentUser, IAuthorResolutionService authorResolution, string id)
    {
        var norma = await db.Normas.FindAsync(new object[] { id }, CancellationToken.None);
        if (norma == null)
            return Results.NotFound(new { errors = new[] { "Norma no encontrada." } });

        var currentAuthor = await authorResolution.GetOrCreateForUserAsync(currentUser.Id!, CancellationToken.None);
        if (currentAuthor == null)
            return Results.BadRequest(new { errors = new[] { "Usuario actual no valido." } });

        var roles = currentUser.Roles ?? [];
        if (!roles.Contains(nameof(RolesEnum.Superuser)) && !roles.Contains(nameof(RolesEnum.Jefe_de_Proyecto)))
        {
            var esCreador = await db.AuthorNormas.AnyAsync(an => an.NormaId == id && an.AuthorId == currentAuthor.Id);
            if (!esCreador)
                return Results.Forbid();
        }

        db.Normas.Remove(norma);
        await db.SaveChangesAsync(CancellationToken.None);
        return Results.Ok(new { message = "Norma eliminada." });
    }
}

public record NormaDto(string Id, string Titulo, string Tipo, string InstitutionId, string InstitutionNombre, List<string> Creadores, List<CreatorDto> CreadoresDetalle);
public record CreateNormaBody(
    string Titulo,
    string Tipo,
    string InstitutionId,
    List<string>? AdditionalAuthorIds = null,
    List<string>? AdditionalAuthorNames = null,
    List<string>? AdditionalUserIds = null);
public record UpdateNormaBody(
    string Titulo,
    string Tipo,
    string InstitutionId,
    List<string>? AdditionalAuthorIds = null,
    List<string>? AdditionalAuthorNames = null,
    List<string>? AdditionalUserIds = null);
