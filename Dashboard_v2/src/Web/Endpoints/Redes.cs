using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Domain.Enums;
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

        // GET /api/Redes/mis-redes
        // Jefe_de_Redes → todas las redes con info de coordinadores.
        // Profesor       → solo las redes que coordina.
        groupBuilder.MapGet("mis-redes", GetMisRedes)
            .RequireAuthorization(p => p.RequireRole("Jefe_de_Redes", "Profesor", "Superuser"))
            .WithName("GetMisRedes")
            .Produces<List<RedConCoordinadoresDto>>(200);

        // PUT /api/Redes/{id}/coordinadores/{areaId} — Jefe_de_Redes asigna coordinador a un área de una red
        groupBuilder.MapPut("{id}/coordinadores/{areaId}", AsignarCoordinador)
            .RequireAuthorization(p => p.RequireRole("Jefe_de_Redes", "Superuser"))
            .WithName("AsignarCoordinador")
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(404);

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

    private async Task<IResult> GetMisRedes(IApplicationDbContext db, IUser currentUser, HttpContext http)
    {
        var isJefe = http.User.IsInRole("Jefe_de_Redes") || http.User.IsInRole("Superuser");

        IQueryable<Dashboard_v2.Domain.Entities.Red> query;

        if (isJefe)
        {
            // El Jefe ve TODAS las redes
            query = db.Reds.AsNoTracking();
        }
        else
        {
            // El coordinador (Profesor) ve solo las redes que coordina
            var coordinatedRedIds = await db.RedesCoordinadas
                .AsNoTracking()
                .Where(rc => rc.CoordinadorId == currentUser.Id)
                .Select(rc => rc.RedId)
                .Distinct()
                .ToListAsync();

            if (coordinatedRedIds.Count == 0)
                return Results.Ok(new List<RedConCoordinadoresDto>());

            query = db.Reds.AsNoTracking().Where(r => coordinatedRedIds.Contains(r.Id));
        }

        var reds = await query
            .Include(r => r.RedesCoordinadas)
                .ThenInclude(rc => rc.Area)
            .Include(r => r.RedesCoordinadas)
                .ThenInclude(rc => rc.Coordinador)
            .Include(r => r.Country)
            .OrderBy(r => r.Nombre)
            .ToListAsync();

        var result = reds.Select(r => new RedConCoordinadoresDto(
            r.Id,
            r.Nombre,
            (int)r.Tipo,
            r.Country?.Name,
            r.CantidadProfesores,
            r.RedesCoordinadas.Select(rc => new CoordinadorAreaDto(
                rc.AreaId,
                rc.Area?.Nombre ?? string.Empty,
                rc.CoordinadorId,
                rc.Coordinador != null
                    ? $"{rc.Coordinador.UserName} {rc.Coordinador.UserLastName1}"
                    : string.Empty,
                rc.Coordinador?.Email ?? string.Empty
            )).ToList()
        )).ToList();

        return Results.Ok(result);
    }

    private async Task<IResult> AsignarCoordinador(
        IApplicationDbContext db,
        string id,
        string areaId,
        AsignarCoordinadorBody body)
    {
        var red = await db.Reds.FindAsync(new object[] { id }, CancellationToken.None);
        if (red == null)
            return Results.NotFound(new { errors = new[] { "Red no encontrada." } });

        // Verificar que el usuario coordinador existe
        var coordinador = await db.Users.FindAsync(new object[] { body.CoordinadorId }, CancellationToken.None);
        if (coordinador == null)
            return Results.BadRequest(new { errors = new[] { "Usuario coordinador no encontrado." } });

        // Buscar o crear el registro RedCoordinada
        var rc = await db.RedesCoordinadas
            .FirstOrDefaultAsync(x => x.RedId == id && x.AreaId == areaId, CancellationToken.None);

        if (rc == null)
        {
            rc = new Dashboard_v2.Domain.Entities.RedCoordinada
            {
                RedId = id,
                AreaId = areaId,
                CoordinadorId = body.CoordinadorId,
            };
            db.RedesCoordinadas.Add(rc);
        }
        else
        {
            rc.CoordinadorId = body.CoordinadorId;
        }

        await db.SaveChangesAsync(CancellationToken.None);
        return Results.Ok(new { message = "Coordinador asignado correctamente." });
    }

    private async Task<IResult> GetRedes(IApplicationDbContext db)
    {
        var list = await db.Reds
            .Select(r => new RedDto(r.Id, r.Nombre, r.CountryId, r.Country != null ? r.Country.Name : null, r.CantidadProfesores, (int)r.Tipo))
            .ToListAsync();

        return Results.Ok(list);
    }

    private async Task<IResult> CreateRed(IApplicationDbContext db, CreateRedBody body)
    {
        if (!Enum.IsDefined(typeof(TipoRed), body.Tipo))
            return Results.BadRequest(new { errors = new[] { "Tipo de red no válido." } });

        var entity = new Dashboard_v2.Domain.Entities.Red
        {
            Nombre = body.Nombre,
            CountryId = body.CountryId,
            CantidadProfesores = body.CantidadProfesores,
            Tipo = (TipoRed)body.Tipo,
        };

        db.Reds.Add(entity);
        await db.SaveChangesAsync(CancellationToken.None);

        return Results.Created($"/api/Redes/{entity.Id}", new { id = entity.Id });
    }

    private async Task<IResult> UpdateRed(IApplicationDbContext db, string id, UpdateRedBody body)
    {
        if (!Enum.IsDefined(typeof(TipoRed), body.Tipo))
            return Results.BadRequest(new { errors = new[] { "Tipo de red no válido." } });

        var e = await db.Reds.FindAsync(new object[] { id }, CancellationToken.None);
        if (e == null)
            return Results.NotFound(new { errors = new[] { "Red no encontrada." } });

        e.Nombre = body.Nombre;
        e.CountryId = body.CountryId;
        e.CantidadProfesores = body.CantidadProfesores;
        e.Tipo = (TipoRed)body.Tipo;

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

public record RedDto(string Id, string Nombre, int? CountryId, string? CountryName, int CantidadProfesores, int Tipo);
public record RedConCoordinadoresDto(string Id, string Nombre, int Tipo, string? CountryName, int CantidadProfesores, List<CoordinadorAreaDto> Coordinadores);
public record CoordinadorAreaDto(string AreaId, string AreaNombre, string CoordinadorId, string CoordinadorNombre, string CoordinadorEmail);
public record CreateRedBody(string Nombre, int CountryId, int CantidadProfesores, int Tipo);
public record UpdateRedBody(string Nombre, int CountryId, int CantidadProfesores, int Tipo);

public record EventForRedDto(int Id, string Name, bool Assigned);
public record SetEventsBody(List<int> EventIds);
public record AsignarCoordinadorBody(string CoordinadorId);
