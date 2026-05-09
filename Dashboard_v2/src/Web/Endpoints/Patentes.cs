using Dashboard_v2.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

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
            .RequireAuthorization(p => p.RequireRole("Superuser"))
            .WithName("CreatePatente")
            .Produces(201)
            .ProducesProblem(400);

        groupBuilder.MapPut("{id}", UpdatePatente)
            .RequireAuthorization(p => p.RequireRole("Superuser"))
            .WithName("UpdatePatente")
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(404);

        groupBuilder.MapDelete("{id}", DeletePatente)
            .RequireAuthorization(p => p.RequireRole("Superuser"))
            .WithName("DeletePatente")
            .Produces(200)
            .ProducesProblem(404);
    }

    private static async Task<IResult> GetPatentes(IApplicationDbContext db)
    {
        var list = await db.Patentes
            .Include(p => p.Creadores).ThenInclude(c => c.User)
            .Select(p => new PatenteDto(
                p.Id,
                p.Titulo,
                p.NumeroSolicitudConcesion,
                p.EsNacional,
                p.Creadores.Select(c => c.User.UserName + " " + c.User.UserLastName1).ToList()))
            .ToListAsync();
        return Results.Ok(list);
    }

    private static async Task<IResult> GetMisPatentes(IApplicationDbContext db, IUser currentUser)
    {
        var userId = currentUser.Id;
        var list = await db.UserPatentes
            .Where(up => up.UserId == userId)
            .Include(up => up.Patente).ThenInclude(p => p.Creadores).ThenInclude(c => c.User)
            .Select(up => new PatenteDto(
                up.Patente.Id,
                up.Patente.Titulo,
                up.Patente.NumeroSolicitudConcesion,
                up.Patente.EsNacional,
                up.Patente.Creadores.Select(c => c.User.UserName + " " + c.User.UserLastName1).ToList()))
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
        IApplicationDbContext db, IUser currentUser, string id, string proyectoId)
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
            var esCreador = await db.UserPatentes.AnyAsync(
                up => up.PatenteId == id && up.UserId == currentUser.Id);
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
        IApplicationDbContext db, IUser currentUser, string id, string proyectoId)
    {
        var link = await db.ProyectoPatentes
            .FirstOrDefaultAsync(pp => pp.PatenteId == id && pp.ProyectoId == proyectoId);
        if (link == null)
            return Results.NotFound(new { errors = new[] { "Vinculo no encontrado." } });

        var roles = currentUser.Roles ?? [];
        if (!roles.Contains(nameof(RolesEnum.Superuser)) && !roles.Contains(nameof(RolesEnum.Jefe_de_Proyecto)))
        {
            var esCreador = await db.UserPatentes.AnyAsync(
                up => up.PatenteId == id && up.UserId == currentUser.Id);
            if (!esCreador)
                return Results.BadRequest(new { errors = new[] { "Solo puedes desvincular proyectos de tus propias patentes." } });
        }

        db.ProyectoPatentes.Remove(link);
        await db.SaveChangesAsync(CancellationToken.None);
        return Results.NoContent();
    }

    private async Task<IResult> CreatePatente(IApplicationDbContext db, CreatePatenteBody body)
    {
        var p = new Dashboard_v2.Domain.Entities.Patente
        {
            Titulo = body.Titulo,
            NumeroSolicitudConcesion = body.NumeroSolicitudConcesion,
            EsNacional = body.EsNacional
        };
        db.Patentes.Add(p);
        await db.SaveChangesAsync(CancellationToken.None);
        return Results.Created($"/api/Patentes/{p.Id}", new { id = p.Id });
    }

    private async Task<IResult> UpdatePatente(IApplicationDbContext db, string id, UpdatePatenteBody body)
    {
        var p = await db.Patentes.FindAsync(new object[] { id }, CancellationToken.None);
        if (p == null)
            return Results.NotFound(new { errors = new[] { "Patente no encontrada." } });
        p.Titulo = body.Titulo;
        p.NumeroSolicitudConcesion = body.NumeroSolicitudConcesion;
        p.EsNacional = body.EsNacional;
        await db.SaveChangesAsync(CancellationToken.None);
        return Results.Ok(new { message = "Patente actualizada." });
    }

    private async Task<IResult> DeletePatente(IApplicationDbContext db, string id)
    {
        var p = await db.Patentes.FindAsync(new object[] { id }, CancellationToken.None);
        if (p == null)
            return Results.NotFound(new { errors = new[] { "Patente no encontrada." } });
        db.Patentes.Remove(p);
        await db.SaveChangesAsync(CancellationToken.None);
        return Results.Ok(new { message = "Patente eliminada." });
    }
}

public record PatenteDto(string Id, string Titulo, string NumeroSolicitudConcesion, bool EsNacional, List<string> Creadores);
public record ProyectoPatenteDto(string ProyectoId, string ProyectoTitulo);
public record CreatePatenteBody(string Titulo, string NumeroSolicitudConcesion, bool EsNacional);
public record UpdatePatenteBody(string Titulo, string NumeroSolicitudConcesion, bool EsNacional);
