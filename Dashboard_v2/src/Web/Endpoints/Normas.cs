using Dashboard_v2.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;
using System.Collections.Generic;
using System.Linq;

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
            .ProducesProblem(404);

        groupBuilder.MapDelete("{id}", DeleteNorma)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Jefe_de_Proyecto), nameof(RolesEnum.Superuser)))
            .WithName("DeleteNorma")
            .Produces(200)
            .ProducesProblem(404);
    }

    private async Task<IResult> GetNormas(IApplicationDbContext db)
    {
        var list = await db.Normas
            .Include(n => n.Institution)
            .Include(n => n.Creadores).ThenInclude(c => c.Author)
            .Select(n => new NormaDto(
                n.Id, n.Titulo, n.Tipo, n.InstitutionId, n.Institution.Nombre,
                n.Creadores.Select(c => c.Author.Name).ToList(),
                n.Creadores.Select(c => new NormaCreatorDto(c.Author.Id, c.Author.Name, c.Author.UserId)).ToList()))
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
                an.Norma.Creadores.Select(c => new NormaCreatorDto(c.Author.Id, c.Author.Name, c.Author.UserId)).ToList()))
            .ToListAsync();
        return Results.Ok(list);
    }

    private async Task<IResult> CreateNorma(IApplicationDbContext db, IUser currentUser, IAuthorResolutionService authorResolution, CreateNormaBody body)
    {
        var currentAuthor = await authorResolution.GetOrCreateForUserAsync(currentUser.Id!, CancellationToken.None);
        if (currentAuthor == null)
            return Results.BadRequest(new { errors = new[] { "Usuario actual no valido." } });

        var norma = new Dashboard_v2.Domain.Entities.Norma
        {
            Titulo = body.Titulo,
            Tipo = body.Tipo,
            InstitutionId = body.InstitutionId
        };
        db.Normas.Add(norma);

        norma.Creadores.Add(new Dashboard_v2.Domain.Entities.AuthorNorma { AuthorId = currentAuthor.Id, NormaId = norma.Id });
        await AddAdditionalCreatorsAsync(db, authorResolution, norma, currentAuthor.Id, body.AdditionalAuthorIds, body.AdditionalAuthorNames, body.AdditionalUserIds);

        await db.SaveChangesAsync(CancellationToken.None);
        return Results.Created($"/api/Normas/{norma.Id}", new { id = norma.Id });
    }

    private async Task<IResult> UpdateNorma(IApplicationDbContext db, IUser currentUser, IAuthorResolutionService authorResolution, string id, UpdateNormaBody body)
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

        await AddAdditionalCreatorsAsync(db, authorResolution, norma, currentAuthor.Id, body.AdditionalAuthorIds, body.AdditionalAuthorNames, body.AdditionalUserIds);

        await db.SaveChangesAsync(CancellationToken.None);
        return Results.Ok(new { message = "Norma actualizada." });
    }

    private async Task<IResult> DeleteNorma(IApplicationDbContext db, IUser currentUser, IAuthorResolutionService authorResolution, string id)
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

    private static async Task AddAdditionalCreatorsAsync(
        IApplicationDbContext db,
        IAuthorResolutionService authorResolution,
        Dashboard_v2.Domain.Entities.Norma norma,
        string currentAuthorId,
        IEnumerable<string>? additionalAuthorIds,
        IEnumerable<string>? additionalAuthorNames,
        IEnumerable<string>? additionalUserIds)
    {
        foreach (var authorId in (additionalAuthorIds ?? []).Where(id => !string.IsNullOrWhiteSpace(id)))
        {
            if (authorId == currentAuthorId) continue;
            if (norma.Creadores.Any(c => c.AuthorId == authorId)) continue;
            if (!await db.Authors.AnyAsync(a => a.Id == authorId, CancellationToken.None)) continue;
            norma.Creadores.Add(new Dashboard_v2.Domain.Entities.AuthorNorma { AuthorId = authorId, NormaId = norma.Id });
        }

        foreach (var authorName in (additionalAuthorNames ?? []).Where(name => !string.IsNullOrWhiteSpace(name)))
        {
            var resolved = await authorResolution.ResolveByNameAsync(authorName, CancellationToken.None);
            if (resolved.Id == currentAuthorId) continue;
            if (norma.Creadores.Any(c => c.AuthorId == resolved.Id)) continue;
            norma.Creadores.Add(new Dashboard_v2.Domain.Entities.AuthorNorma { AuthorId = resolved.Id, NormaId = norma.Id });
        }

        foreach (var userId in (additionalUserIds ?? []).Where(id => !string.IsNullOrWhiteSpace(id)))
        {
            var resolved = await authorResolution.GetOrCreateForUserAsync(userId, CancellationToken.None);
            if (resolved == null || resolved.Id == currentAuthorId) continue;
            if (norma.Creadores.Any(c => c.AuthorId == resolved.Id)) continue;
            norma.Creadores.Add(new Dashboard_v2.Domain.Entities.AuthorNorma { AuthorId = resolved.Id, NormaId = norma.Id });
        }
    }
}

public record NormaDto(string Id, string Titulo, string Tipo, string InstitutionId, string InstitutionNombre, List<string> Creadores, List<NormaCreatorDto> CreadoresDetalle);
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
public record NormaCreatorDto(string Id, string Name, string? UserId);
