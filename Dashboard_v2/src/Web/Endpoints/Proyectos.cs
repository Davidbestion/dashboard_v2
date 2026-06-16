using Dashboard_v2.Application.Proyectos;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;
using AppResult = Dashboard_v2.Application.Common.Models.Result;

namespace Dashboard_v2.Web.Endpoints;

public class Proyectos : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder g)
    {
        // ── Listado general ───────────────────────────────────────────
        g.MapGet("", GetProyectos)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto), nameof(RolesEnum.Profesor), nameof(RolesEnum.Vicedecano_de_investigacion)))
            .WithName("GetProyectos")
            .Produces<List<ProyectoResumenDto>>(200);

        g.MapGet("tipos-ejecucion", GetTiposEjecucion)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto), nameof(RolesEnum.Profesor)))
            .WithName("GetTiposEjecucion")
            .Produces<List<string>>(200);

        g.MapGet("catalogo", GetCatalogo)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto), nameof(RolesEnum.Profesor)))
            .WithName("GetProyectosCatalogo")
            .Produces<List<ProyectoCatalogoDto>>(200);

        g.MapGet("participacion", GetMisProyectosParticipacion)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto), nameof(RolesEnum.Profesor)))
            .WithName("GetMisProyectosParticipacion")
            .Produces<List<ProyectoResumenDto>>(200);

        // ── Publicaciones derivadas por proyecto ──────────────────────
        g.MapGet("{id}/publicaciones", GetPublicacionesDelProyecto)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto)))
            .WithName("GetPublicacionesDelProyecto")
            .Produces(200);

        g.MapGet("publicaciones-disponibles", GetPublicacionesDisponibles)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto)))
            .WithName("GetPublicacionesDisponibles")
            .Produces(200);

        g.MapPost("{id}/publicaciones/{pubId}", LinkPublicacion)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto)))
            .WithName("LinkPublicacion")
            .Produces(204)
            .ProducesProblem(400)
            .ProducesProblem(404);

        g.MapDelete("{id}/publicaciones/{pubId}", UnlinkPublicacion)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto)))
            .WithName("UnlinkPublicacion")
            .Produces(204)
            .ProducesProblem(404);

        // ── Patentes derivadas por proyecto ───────────────────────────
        g.MapGet("{id}/patentes", GetPatentesDelProyecto)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto)))
            .WithName("GetPatentesDelProyecto")
            .Produces<List<ProyectoPatenteResumenDto>>(200);

        g.MapPost("{id}/patentes/{patenteId}", LinkPatenteAProyecto)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto)))
            .WithName("LinkPatenteAProyecto")
            .Produces(204)
            .ProducesProblem(400)
            .ProducesProblem(403)
            .ProducesProblem(404);

        g.MapDelete("{id}/patentes/{patenteId}", UnlinkPatenteDeProyecto)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto)))
            .WithName("UnlinkPatenteDeProyecto")
            .Produces(204)
            .ProducesProblem(403)
            .ProducesProblem(404);

        // ── Participantes ─────────────────────────────────────────────
        g.MapPut("{id}/participantes", SetParticipantes)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto)))
            .WithName("SetParticipantesProyecto")
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(404);

        // ── Delete compartido ─────────────────────────────────────────
        g.MapDelete("{id}", DeleteProyecto)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto)))
            .WithName("DeleteProyecto")
            .Produces(200)
            .ProducesProblem(404);

        // ── En Revisión ───────────────────────────────────────────────
        g.MapGet("en-revision/{id}", GetEnRevision)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto)))
            .WithName("GetProyectoEnRevision")
            .Produces<ProyectoEnRevisionDto>(200).ProducesProblem(404);

        g.MapPost("en-revision", CreateEnRevision)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto)))
            .WithName("CreateProyectoEnRevision")
            .Produces(201).ProducesProblem(400);

        g.MapPut("en-revision/{id}", UpdateEnRevision)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto)))
            .WithName("UpdateProyectoEnRevision")
            .Produces(200).ProducesProblem(400).ProducesProblem(404);

        // ── Empresarial (PE) ──────────────────────────────────────────
        g.MapGet("empresariales/{id}", GetEmpresarial)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto)))
            .WithName("GetProyectoEmpresarial")
            .Produces<ProyectoEmpresarialDto>(200).ProducesProblem(404);

        g.MapPost("empresariales", CreateEmpresarial)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto)))
            .WithName("CreateProyectoEmpresarial")
            .Produces(201).ProducesProblem(400);

        g.MapPut("empresariales/{id}", UpdateEmpresarial)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto)))
            .WithName("UpdateProyectoEmpresarial")
            .Produces(200).ProducesProblem(400).ProducesProblem(404);

        // ── Apoyo a Programa (PAP) ────────────────────────────────────
        g.MapGet("apoyo-programa/{id}", GetApoyoPrograma)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto)))
            .WithName("GetProyectoApoyoPrograma")
            .Produces<ProyectoApoyoProgramaDto>(200).ProducesProblem(404);

        g.MapPost("apoyo-programa", CreateApoyoPrograma)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto)))
            .WithName("CreateProyectoApoyoPrograma")
            .Produces(201).ProducesProblem(400);

        g.MapPut("apoyo-programa/{id}", UpdateApoyoPrograma)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto)))
            .WithName("UpdateProyectoApoyoPrograma")
            .Produces(200).ProducesProblem(400).ProducesProblem(404);

        // ── Desarrollo Local (PDL) ────────────────────────────────────
        g.MapGet("desarrollo-local/{id}", GetDesarrolloLocal)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto)))
            .WithName("GetProyectoDesarrolloLocal")
            .Produces<ProyectoDesarrolloLocalDto>(200).ProducesProblem(404);

        g.MapPost("desarrollo-local", CreateDesarrolloLocal)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto)))
            .WithName("CreateProyectoDesarrolloLocal")
            .Produces(201).ProducesProblem(400);

        g.MapPut("desarrollo-local/{id}", UpdateDesarrolloLocal)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto)))
            .WithName("UpdateProyectoDesarrolloLocal")
            .Produces(200).ProducesProblem(400).ProducesProblem(404);

        // ── No Empresarial (PNE) ──────────────────────────────────────
        g.MapGet("no-empresariales/{id}", GetNoEmpresarial)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto)))
            .WithName("GetProyectoNoEmpresarial")
            .Produces<ProyectoNoEmpresarialDto>(200).ProducesProblem(404);

        g.MapPost("no-empresariales", CreateNoEmpresarial)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto)))
            .WithName("CreateProyectoNoEmpresarial")
            .Produces(201).ProducesProblem(400);

        g.MapPut("no-empresariales/{id}", UpdateNoEmpresarial)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto)))
            .WithName("UpdateProyectoNoEmpresarial")
            .Produces(200).ProducesProblem(400).ProducesProblem(404);

        // ── Colaboración Internacional (PRCI) ─────────────────────────
        g.MapGet("colaboracion-internacional/{id}", GetColabInternacional)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto)))
            .WithName("GetProyectoColabInternacional")
            .Produces<ProyectoColabInternacionalDto>(200).ProducesProblem(404);

        g.MapPost("colaboracion-internacional", CreateColabInternacional)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto)))
            .WithName("CreateProyectoColabInternacional")
            .Produces(201).ProducesProblem(400);

        g.MapPut("colaboracion-internacional/{id}", UpdateColabInternacional)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto)))
            .WithName("UpdateProyectoColabInternacional")
            .Produces(200).ProducesProblem(400).ProducesProblem(404);

        // ── PNAP ──────────────────────────────────────────────────────
        g.MapGet("pnap/{id}", GetPNAP)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto)))
            .WithName("GetProyectoPNAP")
            .Produces<ProyectoPNAPDto>(200).ProducesProblem(404);

        g.MapPost("pnap", CreatePNAP)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto)))
            .WithName("CreateProyectoPNAP")
            .Produces(201).ProducesProblem(400);

        g.MapPut("pnap/{id}", UpdatePNAP)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto)))
            .WithName("UpdateProyectoPNAP")
            .Produces(200).ProducesProblem(400).ProducesProblem(404);
    }

    // ── Listado / Delete ───────────────────────────────────────────────
    private static async Task<IResult> GetProyectos(IProyectoQueryService svc, HttpContext http, CancellationToken ct)
    {
        if (http.User.IsInRole(nameof(RolesEnum.Vicedecano_de_investigacion)))
            return Results.Ok(await svc.GetAreaProyectosAsync(ct));
        return Results.Ok(await svc.GetAllAsync(ct));
    }

    private static async Task<IResult> GetTiposEjecucion(IProyectoQueryService svc, CancellationToken ct)
        => Results.Ok(await svc.GetTiposEjecucionAsync(ct));

    private static async Task<IResult> GetCatalogo(IProyectoQueryService svc, CancellationToken ct)
        => Results.Ok(await svc.GetCatalogoAsync(ct));

    private static async Task<IResult> GetMisProyectosParticipacion(IProyectoQueryService svc, CancellationToken ct)
        => Results.Ok(await svc.GetMisProyectosParticipacionAsync(ct));

    private static async Task<IResult> GetPublicacionesDelProyecto(IProyectoQueryService svc, string id, CancellationToken ct)
        => Results.Ok(await svc.GetPublicacionesDelProyectoAsync(id, ct));

    private static async Task<IResult> GetPublicacionesDisponibles(IProyectoQueryService svc, CancellationToken ct)
        => Results.Ok(await svc.GetPublicacionesDisponiblesAsync(ct));

    private static async Task<IResult> LinkPublicacion(IProyectoCommandService svc, string id, string pubId, CancellationToken ct)
    {
        var result = await svc.LinkPublicacionAsync(id, pubId, ct);
        if (result.Succeeded)
            return Results.NoContent();
        if (HasError(result, "Publicación no encontrada."))
            return Results.NotFound();
        return Results.BadRequest(new { errors = result.Errors });
    }

    private static async Task<IResult> UnlinkPublicacion(IProyectoCommandService svc, string id, string pubId, CancellationToken ct)
    {
        var result = await svc.UnlinkPublicacionAsync(id, pubId, ct);
        return result.Succeeded ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> GetPatentesDelProyecto(IProyectoQueryService svc, string id, CancellationToken ct)
        => Results.Ok(await svc.GetPatentesDelProyectoAsync(id, ct));

    private static async Task<IResult> LinkPatenteAProyecto(IProyectoCommandService svc, string id, string patenteId, CancellationToken ct)
    {
        var result = await svc.LinkPatenteAsync(id, patenteId, ct);
        if (result.Succeeded) return Results.NoContent();
        if (HasError(result, "Proyecto no encontrado.") || HasError(result, "Patente no encontrada."))
            return Results.NotFound(new { errors = result.Errors });
        if (HasError(result, "No tiene permisos sobre este proyecto."))
            return Results.Forbid();
        return Results.BadRequest(new { errors = result.Errors });
    }

    private static async Task<IResult> UnlinkPatenteDeProyecto(IProyectoCommandService svc, string id, string patenteId, CancellationToken ct)
    {
        var result = await svc.UnlinkPatenteAsync(id, patenteId, ct);
        if (result.Succeeded) return Results.NoContent();
        if (HasError(result, "Proyecto no encontrado.") || HasError(result, "Vínculo no encontrado."))
            return Results.NotFound(new { errors = result.Errors });
        if (HasError(result, "No tiene permisos sobre este proyecto."))
            return Results.Forbid();
        return Results.BadRequest(new { errors = result.Errors });
    }

    private static async Task<IResult> SetParticipantes(IProyectoCommandService svc, string id, SetParticipantesRequest request, CancellationToken ct)
    {
        var result = await svc.SetParticipantesAsync(id, request.ParticipantesIds, ct);
        if (!result.Succeeded)
            return HasError(result, "Proyecto no encontrado.")
                ? Results.NotFound(new { errors = result.Errors })
                : Results.BadRequest(new { errors = result.Errors });
        return Results.Ok(new { message = "Participantes actualizados." });
    }

    private static async Task<IResult> DeleteProyecto(IProyectoCommandService svc, string id, CancellationToken ct)
        => ToDeleteResult(await svc.DeleteAsync(id, ct));

    // ── En Revisión ───────────────────────────────────────────────────
    private static async Task<IResult> GetEnRevision(IProyectoQueryService svc, string id, CancellationToken ct)
        => ToGetByIdResult(await svc.GetEnRevisionByIdAsync(id, ct));

    private static async Task<IResult> CreateEnRevision(IProyectoCommandService svc, ProyectoEnRevisionUpsertRequest request, CancellationToken ct)
        => ToCreateResult(await svc.CreateEnRevisionAsync(request, ct), "/api/Proyectos/en-revision");

    private static async Task<IResult> UpdateEnRevision(IProyectoCommandService svc, string id, ProyectoEnRevisionUpsertRequest request, CancellationToken ct)
        => ToUpdateResult(await svc.UpdateEnRevisionAsync(id, request, ct));

    // ── Empresarial ───────────────────────────────────────────────────
    private static async Task<IResult> GetEmpresarial(IProyectoQueryService svc, string id, CancellationToken ct)
        => ToGetByIdResult(await svc.GetEmpresarialByIdAsync(id, ct));

    private static async Task<IResult> CreateEmpresarial(IProyectoCommandService svc, ProyectoEmpresarialUpsertRequest request, CancellationToken ct)
        => ToCreateResult(await svc.CreateEmpresarialAsync(request, ct), "/api/Proyectos/empresariales");

    private static async Task<IResult> UpdateEmpresarial(IProyectoCommandService svc, string id, ProyectoEmpresarialUpsertRequest request, CancellationToken ct)
        => ToUpdateResult(await svc.UpdateEmpresarialAsync(id, request, ct));

    // ── Apoyo a Programa ──────────────────────────────────────────────
    private static async Task<IResult> GetApoyoPrograma(IProyectoQueryService svc, string id, CancellationToken ct)
        => ToGetByIdResult(await svc.GetApoyoProgramaByIdAsync(id, ct));

    private static async Task<IResult> CreateApoyoPrograma(IProyectoCommandService svc, ProyectoApoyoProgramaUpsertRequest request, CancellationToken ct)
        => ToCreateResult(await svc.CreateApoyoProgramaAsync(request, ct), "/api/Proyectos/apoyo-programa");

    private static async Task<IResult> UpdateApoyoPrograma(IProyectoCommandService svc, string id, ProyectoApoyoProgramaUpsertRequest request, CancellationToken ct)
        => ToUpdateResult(await svc.UpdateApoyoProgramaAsync(id, request, ct));

    // ── Desarrollo Local ──────────────────────────────────────────────
    private static async Task<IResult> GetDesarrolloLocal(IProyectoQueryService svc, string id, CancellationToken ct)
        => ToGetByIdResult(await svc.GetDesarrolloLocalByIdAsync(id, ct));

    private static async Task<IResult> CreateDesarrolloLocal(IProyectoCommandService svc, ProyectoDesarrolloLocalUpsertRequest request, CancellationToken ct)
        => ToCreateResult(await svc.CreateDesarrolloLocalAsync(request, ct), "/api/Proyectos/desarrollo-local");

    private static async Task<IResult> UpdateDesarrolloLocal(IProyectoCommandService svc, string id, ProyectoDesarrolloLocalUpsertRequest request, CancellationToken ct)
        => ToUpdateResult(await svc.UpdateDesarrolloLocalAsync(id, request, ct));

    // ── No Empresarial ────────────────────────────────────────────────
    private static async Task<IResult> GetNoEmpresarial(IProyectoQueryService svc, string id, CancellationToken ct)
        => ToGetByIdResult(await svc.GetNoEmpresarialByIdAsync(id, ct));

    private static async Task<IResult> CreateNoEmpresarial(IProyectoCommandService svc, ProyectoNoEmpresarialUpsertRequest request, CancellationToken ct)
        => ToCreateResult(await svc.CreateNoEmpresarialAsync(request, ct), "/api/Proyectos/no-empresariales");

    private static async Task<IResult> UpdateNoEmpresarial(IProyectoCommandService svc, string id, ProyectoNoEmpresarialUpsertRequest request, CancellationToken ct)
        => ToUpdateResult(await svc.UpdateNoEmpresarialAsync(id, request, ct));

    // ── Colaboración Internacional ─────────────────────────────────────
    private static async Task<IResult> GetColabInternacional(IProyectoQueryService svc, string id, CancellationToken ct)
        => ToGetByIdResult(await svc.GetColabInternacionalByIdAsync(id, ct));

    private static async Task<IResult> CreateColabInternacional(IProyectoCommandService svc, ProyectoColabInternacionalUpsertRequest request, CancellationToken ct)
        => ToCreateResult(await svc.CreateColabInternacionalAsync(request, ct), "/api/Proyectos/colaboracion-internacional");

    private static async Task<IResult> UpdateColabInternacional(IProyectoCommandService svc, string id, ProyectoColabInternacionalUpsertRequest request, CancellationToken ct)
        => ToUpdateResult(await svc.UpdateColabInternacionalAsync(id, request, ct));

    // ── PNAP ──────────────────────────────────────────────────────────
    private static async Task<IResult> GetPNAP(IProyectoQueryService svc, string id, CancellationToken ct)
        => ToGetByIdResult(await svc.GetPNAPByIdAsync(id, ct));

    private static async Task<IResult> CreatePNAP(IProyectoCommandService svc, ProyectoPNAPUpsertRequest request, CancellationToken ct)
        => ToCreateResult(await svc.CreatePNAPAsync(request, ct), "/api/Proyectos/pnap");

    private static async Task<IResult> UpdatePNAP(IProyectoCommandService svc, string id, ProyectoPNAPUpsertRequest request, CancellationToken ct)
        => ToUpdateResult(await svc.UpdatePNAPAsync(id, request, ct));

    // ── Result helpers ─────────────────────────────────────────────────
    private static IResult ToGetByIdResult<TDto>(TDto? dto) where TDto : class
        => dto is null ? Results.NotFound() : Results.Ok(dto);

    private static IResult ToCreateResult((AppResult Result, string? Id) outcome, string routePrefix)
        => outcome.Result.Succeeded
            ? Results.Created($"{routePrefix}/{outcome.Id}", new { id = outcome.Id })
            : Results.BadRequest(new { errors = outcome.Result.Errors });

    private static IResult ToUpdateResult(AppResult result)
    {
        if (result.Succeeded) return Results.Ok(new { message = "Proyecto actualizado." });
        if (HasError(result, "Proyecto no encontrado.")) return Results.NotFound(new { errors = result.Errors });
        return Results.BadRequest(new { errors = result.Errors });
    }

    private static IResult ToDeleteResult(AppResult result)
    {
        if (result.Succeeded) return Results.Ok(new { message = "Proyecto eliminado." });
        if (HasError(result, "Proyecto no encontrado.")) return Results.NotFound(new { errors = result.Errors });
        return Results.BadRequest(new { errors = result.Errors });
    }

    private static bool HasError(AppResult result, string error)
        => result.Errors.Contains(error, StringComparer.Ordinal);
}
