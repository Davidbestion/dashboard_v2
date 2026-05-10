using Dashboard_v2.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;
using System.Collections.Generic;
using System.Linq;

namespace Dashboard_v2.Web.Endpoints;

public class Patentes : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("", GetPatentes)
            .RequireAuthorization()
            .WithName("GetPatentes")
            .Produces<List<PatenteDto>>(200);

        groupBuilder.MapGet("mis", GetMisPatentes)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Jefe_de_Proyecto), nameof(RolesEnum.Superuser)))
            .WithName("GetMisPatentes")
            .Produces<List<PatenteDto>>(200);

        groupBuilder.MapGet("{id}/proyectos", GetProyectosDePatente)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Jefe_de_Proyecto), nameof(RolesEnum.Superuser)))
            .WithName("GetProyectosDePatente")
            .Produces<List<ProyectoPatenteDto>>(200)
            .ProducesProblem(404);

        groupBuilder.MapPost("{id}/proyectos/{proyectoId}", LinkProyectoAPatente)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Jefe_de_Proyecto), nameof(RolesEnum.Superuser)))
            .WithName("LinkProyectoAPatente")
            .Produces(204)
            .ProducesProblem(400)
            .ProducesProblem(404);

        groupBuilder.MapDelete("{id}/proyectos/{proyectoId}", UnlinkProyectoDePatente)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Jefe_de_Proyecto), nameof(RolesEnum.Superuser)))
            .WithName("UnlinkProyectoDePatente")
            .Produces(204)
            .ProducesProblem(404);

        groupBuilder.MapPost("", CreatePatente)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Jefe_de_Proyecto), nameof(RolesEnum.Superuser)))
            .WithName("CreatePatente")
            .Produces(201)
            .ProducesProblem(400);

        groupBuilder.MapPut("{id}", UpdatePatente)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Jefe_de_Proyecto), nameof(RolesEnum.Superuser)))
            .WithName("UpdatePatente")
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(404);

        groupBuilder.MapDelete("{id}", DeletePatente)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Jefe_de_Proyecto), nameof(RolesEnum.Superuser)))
            .WithName("DeletePatente")
            .Produces(200)
            .ProducesProblem(404);
    }

    private static async Task<IResult> GetPatentes(IApplicationDbContext db)
    {
        var list = await db.Patentes
            .Include(p => p.Creadores).ThenInclude(c => c.Author)
            .Select(p => new PatenteDto(
                p.Id,
                p.Titulo,
                p.NumeroSolicitudConcesion,
                p.EsNacional,
                p.Creadores.Select(c => c.Author.Name).ToList(),
                p.Creadores.Select(c => new CreatorDto(c.Author.Id, c.Author.Name, c.Author.UserId)).ToList()))
            .ToListAsync();
        return Results.Ok(list);
    }

    private static async Task<IResult> GetMisPatentes(IApplicationDbContext db, IUser currentUser, IAuthorResolutionService authorResolution)
    {
        var currentAuthor = await authorResolution.GetOrCreateForUserAsync(currentUser.Id!, CancellationToken.None);
        if (currentAuthor == null)
            return Results.Ok(new List<PatenteDto>());

        var list = await db.AuthorPatentes
            .Where(ap => ap.AuthorId == currentAuthor.Id)
            .Include(ap => ap.Patente).ThenInclude(p => p.Creadores).ThenInclude(c => c.Author)
            .Select(ap => new PatenteDto(
                ap.Patente.Id,
                ap.Patente.Titulo,
                ap.Patente.NumeroSolicitudConcesion,
                ap.Patente.EsNacional,
                ap.Patente.Creadores.Select(c => c.Author.Name).ToList(),
                ap.Patente.Creadores.Select(c => new CreatorDto(c.Author.Id, c.Author.Name, c.Author.UserId)).ToList()))
            .ToListAsync();
        return Results.Ok(list);
    }

    private static async Task<IResult> GetProyectosDePatente(IApplicationDbContext db, string id)
    {
        if (!await db.Patentes.AnyAsync(p => p.Id == id))
            return Results.NotFound(new { errors = new[] { "Patente no encontrada." } });

        var list = await db.ProyectoPatentes
            .Where(pp => pp.PatenteId == id)
            .Include(pp => pp.Proyecto)
            .Select(pp => new ProyectoPatenteDto(pp.ProyectoId, pp.Proyecto.Titulo))
            .ToListAsync();
        return Results.Ok(list);
    }

    private static async Task<IResult> LinkProyectoAPatente(
        IApplicationDbContext db, IUser currentUser, IAuthorResolutionService authorResolution, string id, string proyectoId)
    {
        if (!await db.Patentes.AnyAsync(p => p.Id == id))
            return Results.NotFound(new { errors = new[] { "Patente no encontrada." } });
        if (!await db.Proyectos.AnyAsync(p => p.Id == proyectoId))
            return Results.NotFound(new { errors = new[] { "Proyecto no encontrado." } });
        if (await db.ProyectoPatentes.AnyAsync(pp => pp.PatenteId == id && pp.ProyectoId == proyectoId))
            return Results.BadRequest(new { errors = new[] { "El vinculo ya existe." } });

        var roles = currentUser.Roles ?? [];
        if (!roles.Contains(nameof(RolesEnum.Superuser)) && !roles.Contains(nameof(RolesEnum.Jefe_de_Proyecto)))
        {
            var currentAuthor = await authorResolution.GetOrCreateForUserAsync(currentUser.Id!, CancellationToken.None);
            if (currentAuthor == null)
                return Results.BadRequest(new { errors = new[] { "Usuario actual no valido." } });

            var esCreador = await db.AuthorPatentes.AnyAsync(
                ap => ap.PatenteId == id && ap.AuthorId == currentAuthor.Id);
            if (!esCreador)
                return Results.BadRequest(new { errors = new[] { "Solo puedes vincular proyectos a tus propias patentes." } });
        }

        db.ProyectoPatentes.Add(new Dashboard_v2.Domain.Entities.ProyectoPatente
        {
            ProyectoId = proyectoId,
            PatenteId = id
        });
        await db.SaveChangesAsync(CancellationToken.None);
        return Results.NoContent();
    }

    private static async Task<IResult> UnlinkProyectoDePatente(
        IApplicationDbContext db, IUser currentUser, IAuthorResolutionService authorResolution, string id, string proyectoId)
    {
        var link = await db.ProyectoPatentes
            .FirstOrDefaultAsync(pp => pp.PatenteId == id && pp.ProyectoId == proyectoId);
        if (link == null)
            return Results.NotFound(new { errors = new[] { "Vinculo no encontrado." } });

        var roles = currentUser.Roles ?? [];
        if (!roles.Contains(nameof(RolesEnum.Superuser)) && !roles.Contains(nameof(RolesEnum.Jefe_de_Proyecto)))
        {
            var currentAuthor = await authorResolution.GetOrCreateForUserAsync(currentUser.Id!, CancellationToken.None);
            if (currentAuthor == null)
                return Results.BadRequest(new { errors = new[] { "Usuario actual no valido." } });

            var esCreador = await db.AuthorPatentes.AnyAsync(
                ap => ap.PatenteId == id && ap.AuthorId == currentAuthor.Id);
            if (!esCreador)
                return Results.BadRequest(new { errors = new[] { "Solo puedes desvincular proyectos de tus propias patentes." } });
        }

        db.ProyectoPatentes.Remove(link);
        await db.SaveChangesAsync(CancellationToken.None);
        return Results.NoContent();
    }

    private static async Task<IResult> CreatePatente(
        IApplicationDbContext db,
        IUser currentUser,
        IAuthorResolutionService authorResolution,
        CreatePatenteBody body)
    {
        var currentAuthor = await authorResolution.GetOrCreateForUserAsync(currentUser.Id!, CancellationToken.None);
        if (currentAuthor == null)
            return Results.BadRequest(new { errors = new[] { "Usuario actual no valido." } });

        var p = new Dashboard_v2.Domain.Entities.Patente
        {
            Titulo = body.Titulo,
            NumeroSolicitudConcesion = body.NumeroSolicitudConcesion,
            EsNacional = body.EsNacional
        };
        db.Patentes.Add(p);

        p.Creadores.Add(new Dashboard_v2.Domain.Entities.AuthorPatente { AuthorId = currentAuthor.Id, PatenteId = p.Id });
        await AddAdditionalCreatorsAsync(db, authorResolution, p, currentAuthor.Id, body.AdditionalAuthorIds, body.AdditionalAuthorNames, body.AdditionalUserIds);

        await db.SaveChangesAsync(CancellationToken.None);
        return Results.Created($"/api/Patentes/{p.Id}", new { id = p.Id });
    }

    private static async Task<IResult> UpdatePatente(
        IApplicationDbContext db,
        IUser currentUser,
        IAuthorResolutionService authorResolution,
        string id,
        UpdatePatenteBody body)
    {
        var p = await db.Patentes
            .Include(x => x.Creadores)
            .FirstOrDefaultAsync(x => x.Id == id, CancellationToken.None);
        if (p == null)
            return Results.NotFound(new { errors = new[] { "Patente no encontrada." } });

        var currentAuthor = await authorResolution.GetOrCreateForUserAsync(currentUser.Id!, CancellationToken.None);
        if (currentAuthor == null)
            return Results.BadRequest(new { errors = new[] { "Usuario actual no valido." } });

        var roles = currentUser.Roles ?? [];
        if (!roles.Contains(nameof(RolesEnum.Superuser)) && !roles.Contains(nameof(RolesEnum.Jefe_de_Proyecto)))
        {
            var esCreador = await db.AuthorPatentes.AnyAsync(ap => ap.PatenteId == id && ap.AuthorId == currentAuthor.Id);
            if (!esCreador)
                return Results.Forbid();
        }

        p.Titulo = body.Titulo;
        p.NumeroSolicitudConcesion = body.NumeroSolicitudConcesion;
        p.EsNacional = body.EsNacional;

        var toRemove = p.Creadores.Where(c => c.AuthorId != currentAuthor.Id).ToList();
        foreach (var creator in toRemove)
            p.Creadores.Remove(creator);

        await AddAdditionalCreatorsAsync(db, authorResolution, p, currentAuthor.Id, body.AdditionalAuthorIds, body.AdditionalAuthorNames, body.AdditionalUserIds);

        await db.SaveChangesAsync(CancellationToken.None);
        return Results.Ok(new { message = "Patente actualizada." });
    }

    private static async Task<IResult> DeletePatente(IApplicationDbContext db, IUser currentUser, IAuthorResolutionService authorResolution, string id)
    {
        var p = await db.Patentes.FindAsync(new object[] { id }, CancellationToken.None);
        if (p == null)
            return Results.NotFound(new { errors = new[] { "Patente no encontrada." } });

        var currentAuthor = await authorResolution.GetOrCreateForUserAsync(currentUser.Id!, CancellationToken.None);
        if (currentAuthor == null)
            return Results.BadRequest(new { errors = new[] { "Usuario actual no valido." } });

        var roles = currentUser.Roles ?? [];
        if (!roles.Contains(nameof(RolesEnum.Superuser)) && !roles.Contains(nameof(RolesEnum.Jefe_de_Proyecto)))
        {
            var esCreador = await db.AuthorPatentes.AnyAsync(ap => ap.PatenteId == id && ap.AuthorId == currentAuthor.Id);
            if (!esCreador)
                return Results.Forbid();
        }

        db.Patentes.Remove(p);
        await db.SaveChangesAsync(CancellationToken.None);
        return Results.Ok(new { message = "Patente eliminada." });
    }

    private static async Task AddAdditionalCreatorsAsync(
        IApplicationDbContext db,
        IAuthorResolutionService authorResolution,
        Dashboard_v2.Domain.Entities.Patente patente,
        string currentAuthorId,
        IEnumerable<string>? additionalAuthorIds,
        IEnumerable<string>? additionalAuthorNames,
        IEnumerable<string>? additionalUserIds)
    {
        foreach (var authorId in (additionalAuthorIds ?? []).Where(id => !string.IsNullOrWhiteSpace(id)))
        {
            if (authorId == currentAuthorId) continue;
            if (patente.Creadores.Any(c => c.AuthorId == authorId)) continue;
            if (!await db.Authors.AnyAsync(a => a.Id == authorId, CancellationToken.None)) continue;
            patente.Creadores.Add(new Dashboard_v2.Domain.Entities.AuthorPatente { AuthorId = authorId, PatenteId = patente.Id });
        }

        foreach (var authorName in (additionalAuthorNames ?? []).Where(name => !string.IsNullOrWhiteSpace(name)))
        {
            var resolved = await authorResolution.ResolveByNameAsync(authorName, CancellationToken.None);
            if (resolved.Id == currentAuthorId) continue;
            if (patente.Creadores.Any(c => c.AuthorId == resolved.Id)) continue;
            patente.Creadores.Add(new Dashboard_v2.Domain.Entities.AuthorPatente { AuthorId = resolved.Id, PatenteId = patente.Id });
        }

        foreach (var userId in (additionalUserIds ?? []).Where(id => !string.IsNullOrWhiteSpace(id)))
        {
            var resolved = await authorResolution.GetOrCreateForUserAsync(userId, CancellationToken.None);
            if (resolved == null || resolved.Id == currentAuthorId) continue;
            if (patente.Creadores.Any(c => c.AuthorId == resolved.Id)) continue;
            patente.Creadores.Add(new Dashboard_v2.Domain.Entities.AuthorPatente { AuthorId = resolved.Id, PatenteId = patente.Id });
        }
    }
}

public record PatenteDto(string Id, string Titulo, string NumeroSolicitudConcesion, bool EsNacional, List<string> Creadores, List<CreatorDto> CreadoresDetalle);
public record ProyectoPatenteDto(string ProyectoId, string ProyectoTitulo);
public record CreatePatenteBody(
    string Titulo,
    string NumeroSolicitudConcesion,
    bool EsNacional,
    List<string>? AdditionalAuthorIds = null,
    List<string>? AdditionalAuthorNames = null,
    List<string>? AdditionalUserIds = null);
public record UpdatePatenteBody(
    string Titulo,
    string NumeroSolicitudConcesion,
    bool EsNacional,
    List<string>? AdditionalAuthorIds = null,
    List<string>? AdditionalAuthorNames = null,
    List<string>? AdditionalUserIds = null);
public record CreatorDto(string Id, string Name, string? UserId);
