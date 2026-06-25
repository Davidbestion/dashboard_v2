using Dashboard_v2.Application.Redes;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Web.Endpoints;

/// <summary>
/// API endpoints for research network management.
/// </summary>
public class Redes : EndpointGroupBase
{
    /// <summary>Registers the Redes route group with CRUD, participant, and event-linking endpoints.</summary>
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("", GetRedes)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Redes), nameof(RolesEnum.Profesor), nameof(RolesEnum.Vicedecano_de_investigacion)))
            .WithName("GetRedes")
            .Produces<List<RedDto>>(200);

        groupBuilder.MapGet("mis-redes", GetMisRedes)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Jefe_de_Redes), nameof(RolesEnum.Profesor), nameof(RolesEnum.Superuser)))
            .WithName("GetMisRedes")
            .Produces<List<RedConCoordinadorDto>>(200);

        groupBuilder.MapPut("{id}/coordinador", SetCoordinador)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Jefe_de_Redes), nameof(RolesEnum.Superuser)))
            .WithName("SetCoordinador")
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(404);

        groupBuilder.MapGet("{id}/participantes", GetParticipantes)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Jefe_de_Redes), nameof(RolesEnum.Superuser)))
            .WithName("GetParticipantesRed")
            .Produces<List<ParticipanteRedDto>>(200)
            .ProducesProblem(404);

        groupBuilder.MapPost("{id}/participantes/{authorId}", AddParticipante)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Jefe_de_Redes), nameof(RolesEnum.Superuser)))
            .WithName("AddParticipanteRed")
            .Produces(204)
            .ProducesProblem(400)
            .ProducesProblem(404);

        groupBuilder.MapDelete("{id}/participantes/{authorId}", RemoveParticipante)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Jefe_de_Redes), nameof(RolesEnum.Superuser)))
            .WithName("RemoveParticipanteRed")
            .Produces(204)
            .ProducesProblem(404);

        groupBuilder.MapPost("", CreateRed)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Redes)))
            .WithName("CreateRed")
            .Produces(201)
            .ProducesProblem(400);

        groupBuilder.MapPut("{id}", UpdateRed)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Redes)))
            .WithName("UpdateRed")
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(404);

        groupBuilder.MapDelete("{id}", DeleteRed)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Redes)))
            .WithName("DeleteRed")
            .Produces(200)
            .ProducesProblem(404);

        groupBuilder.MapGet("{id}/events", GetEventsForRed)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Redes)))
            .WithName("GetEventsForRed")
            .Produces<List<EventForRedDto>>(200);

        groupBuilder.MapPost("{id}/events", SetEventsForRed)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Redes)))
            .WithName("SetEventsForRed")
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(404);
    }

    private static async Task<IResult> GetRedes(IRedService redService, CancellationToken ct)
        => Results.Ok(await redService.GetRedesAsync(ct));

    private static async Task<IResult> GetMisRedes(IRedService redService, CancellationToken ct)
        => Results.Ok(await redService.GetMisRedesAsync(ct));

    private static async Task<IResult> SetCoordinador(IRedService redService, string id, SetCoordinadorBody body, CancellationToken ct)
    {
        var result = await redService.SetCoordinadorAsync(id, body.CoordinadorId, ct);
        if (!result.Succeeded)
            return HasError(result, "Red no encontrada.")
                ? Results.NotFound(new { errors = result.Errors })
                : Results.BadRequest(new { errors = result.Errors });

        return Results.Ok(new { message = "Coordinador actualizado." });
    }

    private static async Task<IResult> GetParticipantes(IRedService redService, string id, CancellationToken ct)
    {
        var (found, participantes) = await redService.GetParticipantesAsync(id, ct);
        if (!found)
            return Results.NotFound(new { errors = new[] { "Red no encontrada." } });

        return Results.Ok(participantes);
    }

    private static async Task<IResult> AddParticipante(IRedService redService, string id, string authorId, CancellationToken ct)
    {
        var result = await redService.AddParticipanteAsync(id, authorId, ct);
        if (!result.Succeeded)
            return HasError(result, "Red no encontrada.") || HasError(result, "Autor no encontrado.")
                ? Results.NotFound(new { errors = result.Errors })
                : Results.BadRequest(new { errors = result.Errors });

        return Results.NoContent();
    }

    private static async Task<IResult> RemoveParticipante(IRedService redService, string id, string authorId, CancellationToken ct)
    {
        var result = await redService.RemoveParticipanteAsync(id, authorId, ct);
        if (!result.Succeeded)
            return Results.NotFound(new { errors = result.Errors });

        return Results.NoContent();
    }

    private static async Task<IResult> CreateRed(IRedService redService, CreateRedBody body, CancellationToken ct)
    {
        var (result, id) = await redService.CreateRedAsync(body, ct);
        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors });

        return Results.Created($"/api/Redes/{id}", new { id });
    }

    private static async Task<IResult> UpdateRed(IRedService redService, string id, UpdateRedBody body, CancellationToken ct)
    {
        var result = await redService.UpdateRedAsync(id, body, ct);
        if (!result.Succeeded)
            return HasError(result, "Red no encontrada.")
                ? Results.NotFound(new { errors = result.Errors })
                : Results.BadRequest(new { errors = result.Errors });

        return Results.Ok(new { message = "Red actualizada." });
    }

    private static async Task<IResult> DeleteRed(IRedService redService, string id, CancellationToken ct)
    {
        var result = await redService.DeleteRedAsync(id, ct);
        if (!result.Succeeded)
            return Results.NotFound(new { errors = result.Errors });

        return Results.Ok(new { message = "Red eliminada." });
    }

    private static async Task<IResult> GetEventsForRed(IRedService redService, string id, CancellationToken ct)
        => Results.Ok(await redService.GetEventsForRedAsync(id, ct));

    private static async Task<IResult> SetEventsForRed(IRedService redService, string id, SetEventsBody body, CancellationToken ct)
    {
        var result = await redService.SetEventsForRedAsync(id, body?.EventIds ?? [], ct);
        if (!result.Succeeded)
            return HasError(result, "Red no encontrada.")
                ? Results.NotFound(new { errors = result.Errors })
                : Results.BadRequest(new { errors = result.Errors });

        return Results.Ok(new { message = "Eventos actualizados." });
    }

    private static bool HasError(Dashboard_v2.Application.Common.Models.Result result, string error)
        => result.Errors.Contains(error, StringComparer.Ordinal);
}
