using Dashboard_v2.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Dashboard_v2.Web.Endpoints;

public class Redes : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("", GetRedes)
            .RequireAuthorization(p => p.RequireRole("Superuser", "Jefe_de_Redes", "Profesor", "Vicedecano_de_investigacion"))
            .WithName("GetRedes")
            .Produces<List<RedDto>>(200);

        groupBuilder.MapPost("", CreateRed)
            .RequireAuthorization(p => p.RequireRole("Superuser", "Jefe_de_Redes"))
            .WithName("CreateRed")
            .Produces(201)
            .ProducesProblem(400);

        groupBuilder.MapPut("{id}", UpdateRed)
            .RequireAuthorization(p => p.RequireRole("Superuser", "Jefe_de_Redes"))
            .WithName("UpdateRed")
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(404);

        groupBuilder.MapDelete("{id}", DeleteRed)
            .RequireAuthorization(p => p.RequireRole("Superuser", "Jefe_de_Redes"))
            .WithName("DeleteRed")
            .Produces(200)
            .ProducesProblem(404);

        // List events and assign/unassign events for a red
        groupBuilder.MapGet("{id}/events", GetEventsForRed)
            .RequireAuthorization(p => p.RequireRole("Superuser", "Jefe_de_Redes"))
            .WithName("GetEventsForRed")
            .Produces<List<EventForRedDto>>(200);

        groupBuilder.MapPost("{id}/events", SetEventsForRed)
            .RequireAuthorization(p => p.RequireRole("Superuser", "Jefe_de_Redes"))
            .WithName("SetEventsForRed")
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(404);
    }

    private async Task<IResult> GetRedes(IApplicationDbContext db)
    {
        var list = await db.Reds
            .Select(r => new RedDto(r.Id, r.Nombre, r.CountryId, r.Country != null ? r.Country.Name : null, r.CantidadProfesores))
            .ToListAsync();

        return Results.Ok(list);
    }

    private async Task<IResult> CreateRed(IApplicationDbContext db, CreateRedBody body)
    {
        var entity = new Dashboard_v2.Domain.Entities.Red
        {
            Nombre = body.Nombre,
            CountryId = body.CountryId,
            CantidadProfesores = body.CantidadProfesores,
        };

        db.Reds.Add(entity);
        await db.SaveChangesAsync(CancellationToken.None);

        return Results.Created($"/api/Redes/{entity.Id}", new { id = entity.Id });
    }

    private async Task<IResult> UpdateRed(IApplicationDbContext db, string id, UpdateRedBody body)
    {
        var e = await db.Reds.FindAsync(new object[] { id }, CancellationToken.None);
        if (e == null)
            return Results.NotFound(new { errors = new[] { "Red no encontrada." } });

        e.Nombre = body.Nombre;
        e.CountryId = body.CountryId;
        e.CantidadProfesores = body.CantidadProfesores;

        await db.SaveChangesAsync(CancellationToken.None);
        return Results.Ok(new { message = "Red actualizada." });
    }

    private async Task<IResult> DeleteRed(IApplicationDbContext db, string id)
    {
        var e = await db.Reds.FindAsync(new object[] { id }, CancellationToken.None);
        if (e == null)
            return Results.NotFound(new { errors = new[] { "Red no encontrada." } });

        db.Reds.Remove(e);
        await db.SaveChangesAsync(CancellationToken.None);
        return Results.Ok(new { message = "Red eliminada." });
    }

    private async Task<IResult> GetEventsForRed(IApplicationDbContext db, string id)
    {
        // return all events with a flag indicating whether they are coordinated by this red
        var list = await db.Events
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .Select(e => new EventForRedDto(e.Id, e.Name, e.RedId == id))
            .ToListAsync();

        return Results.Ok(list);
    }

    private async Task<IResult> SetEventsForRed(IApplicationDbContext db, string id, SetEventsBody body)
    {
        var red = await db.Reds.FindAsync(new object[] { id }, CancellationToken.None);
        if (red == null)
            return Results.NotFound(new { errors = new[] { "Red no encontrada." } });

        var eventIds = (body?.EventIds ?? new List<int>()).Distinct().ToList();

        // Validate provided event ids exist
        var events = await db.Events.Where(e => eventIds.Contains(e.Id)).ToListAsync();
        if (events.Count != eventIds.Count)
            return Results.BadRequest(new { errors = new[] { "Uno o más eventos no existen." } });

        // Unassign events that were previously assigned to this red but are not in the new list
        var currentlyAssigned = await db.Events.Where(e => e.RedId == id).ToListAsync();
        var toUnassign = currentlyAssigned.Where(e => !eventIds.Contains(e.Id)).ToList();
        foreach (var e in toUnassign) e.RedId = null;

        // Assign selected events to this red (this will overwrite any previous red assignment)
        foreach (var e in events) e.RedId = id;

        await db.SaveChangesAsync(CancellationToken.None);
        return Results.Ok(new { message = "Eventos actualizados." });
    }
}

public record RedDto(string Id, string Nombre, int? CountryId, string? CountryName, int CantidadProfesores);
public record CreateRedBody(string Nombre, int CountryId, int CantidadProfesores);
public record UpdateRedBody(string Nombre, int CountryId, int CantidadProfesores);

public record EventForRedDto(int Id, string Name, bool Assigned);
public record SetEventsBody(List<int> EventIds);
