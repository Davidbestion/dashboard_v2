using Dashboard_v2.Application.Proyectos;
using Dashboard_v2.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;
using Dashboard_v2.Web.Infrastructure;
using AppResult = Dashboard_v2.Application.Common.Models.Result;

namespace Dashboard_v2.Web.Endpoints;

/// <summary>
/// Endpoints de gestión de proyectos bajo /api/Proyectos.
/// Acceso según operación: <c>Superuser</c> y <c>Jefe_de_Proyecto</c> para CRUD;
/// adicionalmente <c>Profesor</c> puede acceder al catálogo mínimo para vincular publicaciones.
/// </summary>
public class Proyectos : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder g)
    {
        // ── Listado general ───────────────────────────────────────────
        g.MapGet("", GetProyectos)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto), nameof(RolesEnum.Profesor)))
            .WithName("GetProyectos")
            .Produces<List<ProyectoResumenDto>>(200);
        // ── Tipos de ejecución disponibles para ProyectoEnRevision.Tipo ────────
        g.MapGet("tipos-ejecucion", GetTiposEjecucion)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto), nameof(RolesEnum.Profesor)))
            .WithName("GetTiposEjecucion")
            .Produces<List<string>>(200);

        // ── Catálogo mínimo para vinculación desde publicaciones ──────────────────
        g.MapGet("catalogo", GetCatalogo)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto), nameof(RolesEnum.Profesor)))
            .WithName("GetProyectosCatalogo")
            .Produces<List<ProyectoCatalogoDto>>(200);

        // ── Publicaciones derivadas por proyecto ────────────────────────
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

        // ── Patentes derivadas por proyecto (Jefe de Proyecto) ────────
        g.MapGet("{id}/patentes", GetPatentesDelProyecto)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto)))
            .WithName("GetPatentesDelProyecto")
            .Produces(200);

        g.MapPost("{id}/patentes/{patenteId}", LinkPatenteAProyecto)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto)))
            .WithName("LinkPatenteAProyecto")
            .Produces(204)
            .ProducesProblem(400)
            .ProducesProblem(404);

        g.MapDelete("{id}/patentes/{patenteId}", UnlinkPatenteDeProyecto)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto)))
            .WithName("UnlinkPatenteDeProyecto")
            .Produces(204)
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
    private static async Task<IResult> GetProyectos(IProyectoService proyectoService, CancellationToken ct)
        => Results.Ok(await proyectoService.GetAllAsync(ct));

    private static async Task<IResult> GetTiposEjecucion(IProyectoService proyectoService, CancellationToken ct)
        => Results.Ok(await proyectoService.GetTiposEjecucionAsync(ct));

    private static async Task<IResult> GetCatalogo(IProyectoService proyectoService, CancellationToken ct)
        => Results.Ok(await proyectoService.GetCatalogoAsync(ct));

    private static async Task<IResult> GetPublicacionesDelProyecto(IProyectoService proyectoService, string id, CancellationToken ct)
        => Results.Ok(await proyectoService.GetPublicacionesDelProyectoAsync(id, ct));

    private static async Task<IResult> UnlinkPublicacion(IProyectoService proyectoService, string id, string pubId, CancellationToken ct)
    {
        var result = await proyectoService.UnlinkPublicacionAsync(id, pubId, ct);
        return result.Succeeded ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> GetPublicacionesDisponibles(IProyectoService proyectoService, CancellationToken ct)
        => Results.Ok(await proyectoService.GetPublicacionesDisponiblesAsync(ct));

    private static async Task<IResult> LinkPublicacion(IProyectoService proyectoService, string id, string pubId, CancellationToken ct)
    {
        var result = await proyectoService.LinkPublicacionAsync(id, pubId, ct);
        if (result.Succeeded)
        {
            return Results.NoContent();
        }

        if (HasError(result, "Publicación no encontrada."))
        {
            return Results.NotFound();
        }

        return Results.BadRequest(new { errors = result.Errors });
    }

    private static async Task<IResult> DeleteProyecto(IProyectoService proyectoService, string id, CancellationToken ct)
    {
        var result = await proyectoService.DeleteAsync(id, ct);
        return ToDeleteResult(result);
    }

    // ── En Revisión ───────────────────────────────────────────────────
    private static async Task<IResult> GetEnRevision(IProyectoService proyectoService, string id, CancellationToken ct)
        => ToGetByIdResult(await proyectoService.GetEnRevisionByIdAsync(id, ct));

    private static async Task<IResult> CreateEnRevision(IProyectoService proyectoService, ProyectoEnRevisionUpsertRequest request, CancellationToken ct)
        => ToCreateResult(await proyectoService.CreateEnRevisionAsync(request, ct), "/api/Proyectos/en-revision");

    private static async Task<IResult> UpdateEnRevision(IProyectoService proyectoService, string id, ProyectoEnRevisionUpsertRequest request, CancellationToken ct)
        => ToUpdateResult(await proyectoService.UpdateEnRevisionAsync(id, request, ct));

    // ── Empresarial ───────────────────────────────────────────────────
    private static async Task<IResult> GetEmpresarial(IProyectoService proyectoService, string id, CancellationToken ct)
        => ToGetByIdResult(await proyectoService.GetEmpresarialByIdAsync(id, ct));

    private static async Task<IResult> CreateEmpresarial(IProyectoService proyectoService, ProyectoEmpresarialUpsertRequest request, CancellationToken ct)
        => ToCreateResult(await proyectoService.CreateEmpresarialAsync(request, ct), "/api/Proyectos/empresariales");

    private static async Task<IResult> UpdateEmpresarial(IProyectoService proyectoService, string id, ProyectoEmpresarialUpsertRequest request, CancellationToken ct)
        => ToUpdateResult(await proyectoService.UpdateEmpresarialAsync(id, request, ct));

    // ── Apoyo a Programa ──────────────────────────────────────────────
    private static async Task<IResult> GetApoyoPrograma(IProyectoService proyectoService, string id, CancellationToken ct)
        => ToGetByIdResult(await proyectoService.GetApoyoProgramaByIdAsync(id, ct));

    private static async Task<IResult> CreateApoyoPrograma(IProyectoService proyectoService, ProyectoApoyoProgramaUpsertRequest request, CancellationToken ct)
        => ToCreateResult(await proyectoService.CreateApoyoProgramaAsync(request, ct), "/api/Proyectos/apoyo-programa");

    private static async Task<IResult> UpdateApoyoPrograma(IProyectoService proyectoService, string id, ProyectoApoyoProgramaUpsertRequest request, CancellationToken ct)
        => ToUpdateResult(await proyectoService.UpdateApoyoProgramaAsync(id, request, ct));

    // ── Desarrollo Local ──────────────────────────────────────────────
    private static async Task<IResult> GetDesarrolloLocal(IProyectoService proyectoService, string id, CancellationToken ct)
        => ToGetByIdResult(await proyectoService.GetDesarrolloLocalByIdAsync(id, ct));

    private static async Task<IResult> CreateDesarrolloLocal(IProyectoService proyectoService, ProyectoDesarrolloLocalUpsertRequest request, CancellationToken ct)
        => ToCreateResult(await proyectoService.CreateDesarrolloLocalAsync(request, ct), "/api/Proyectos/desarrollo-local");

    private static async Task<IResult> UpdateDesarrolloLocal(IProyectoService proyectoService, string id, ProyectoDesarrolloLocalUpsertRequest request, CancellationToken ct)
        => ToUpdateResult(await proyectoService.UpdateDesarrolloLocalAsync(id, request, ct));

    // ── No Empresarial ────────────────────────────────────────────────
    private static async Task<IResult> GetNoEmpresarial(IProyectoService proyectoService, string id, CancellationToken ct)
        => ToGetByIdResult(await proyectoService.GetNoEmpresarialByIdAsync(id, ct));

    private static async Task<IResult> CreateNoEmpresarial(IProyectoService proyectoService, ProyectoNoEmpresarialUpsertRequest request, CancellationToken ct)
        => ToCreateResult(await proyectoService.CreateNoEmpresarialAsync(request, ct), "/api/Proyectos/no-empresariales");

    private static async Task<IResult> UpdateNoEmpresarial(IProyectoService proyectoService, string id, ProyectoNoEmpresarialUpsertRequest request, CancellationToken ct)
        => ToUpdateResult(await proyectoService.UpdateNoEmpresarialAsync(id, request, ct));

    // ── Colaboración Internacional ─────────────────────────────────────
    private static async Task<IResult> GetColabInternacional(IProyectoService proyectoService, string id, CancellationToken ct)
        => ToGetByIdResult(await proyectoService.GetColabInternacionalByIdAsync(id, ct));

    private static async Task<IResult> CreateColabInternacional(IProyectoService proyectoService, ProyectoColabInternacionalUpsertRequest request, CancellationToken ct)
        => ToCreateResult(await proyectoService.CreateColabInternacionalAsync(request, ct), "/api/Proyectos/colaboracion-internacional");

    private static async Task<IResult> UpdateColabInternacional(IProyectoService proyectoService, string id, ProyectoColabInternacionalUpsertRequest request, CancellationToken ct)
        => ToUpdateResult(await proyectoService.UpdateColabInternacionalAsync(id, request, ct));

    // ── PNAP ──────────────────────────────────────────────────────────
    private static async Task<IResult> GetPNAP(IProyectoService proyectoService, string id, CancellationToken ct)
        => ToGetByIdResult(await proyectoService.GetPNAPByIdAsync(id, ct));

    private static async Task<IResult> CreatePNAP(IProyectoService proyectoService, ProyectoPNAPUpsertRequest request, CancellationToken ct)
        => ToCreateResult(await proyectoService.CreatePNAPAsync(request, ct), "/api/Proyectos/pnap");

    private static async Task<IResult> UpdatePNAP(IProyectoService proyectoService, string id, ProyectoPNAPUpsertRequest request, CancellationToken ct)
        => ToUpdateResult(await proyectoService.UpdatePNAPAsync(id, request, ct));

    private static IResult ToGetByIdResult<TDto>(TDto? dto)
        where TDto : class
        => dto is null ? Results.NotFound() : Results.Ok(dto);

    private static IResult ToCreateResult((AppResult Result, string? Id) outcome, string routePrefix)
    {
        if (!outcome.Result.Succeeded)
        {
            return Results.BadRequest(new { errors = outcome.Result.Errors });
        }

        return Results.Created($"{routePrefix}/{outcome.Id}", new { id = outcome.Id });
    }

    private static IResult ToUpdateResult(AppResult result)
    {
        if (!result.Succeeded)
        {
            if (HasError(result, "Proyecto no encontrado."))
            {
                return Results.NotFound(new { errors = result.Errors });
            }

            return Results.BadRequest(new { errors = result.Errors });
        }

        return Results.Ok(new { message = "Proyecto actualizado." });
    }

    private static IResult ToDeleteResult(AppResult result)
    {
        if (!result.Succeeded)
        {
            if (HasError(result, "Proyecto no encontrado."))
            {
                return Results.NotFound(new { errors = result.Errors });
            }

            return Results.BadRequest(new { errors = result.Errors });
        }

        return Results.Ok(new { message = "Proyecto eliminado." });
    }

    private static bool HasError(AppResult result, string error)
        => result.Errors.Contains(error, StringComparer.Ordinal);

    // ── Patentes derivadas ─────────────────────────────────────────────
    private static async Task<IResult> GetPatentesDelProyecto(
        IApplicationDbContext db, IUser currentUser, string id, CancellationToken ct)
    {
        if (!await db.Proyectos.AnyAsync(p => p.Id == id, ct))
            return Results.NotFound(new { errors = new[] { "Proyecto no encontrado." } });

        var roles = currentUser.Roles ?? [];
        if (!roles.Contains(nameof(RolesEnum.Superuser)))
        {
            var eJefe = await db.Proyectos.AnyAsync(
                p => p.Id == id && p.JefeId == currentUser.Id, ct);
            if (!eJefe)
                return Results.Forbid();
        }

        var list = await db.ProyectoPatentes
            .Where(pp => pp.ProyectoId == id)
            .Select(pp => new
            {
                patenteId = pp.PatenteId,
                titulo = pp.Patente.Titulo,
                numeroSolicitudConcesion = pp.Patente.NumeroSolicitudConcesion,
                esNacional = pp.Patente.EsNacional,
                creador = pp.Patente.Creadores
                    .OrderBy(c => c.Author.Name)
                    .Select(c => c.Author.Name)
                    .FirstOrDefault(),
                creadores = pp.Patente.Creadores
                    .OrderBy(c => c.Author.Name)
                    .Select(c => c.Author.Name)
                    .ToList()
            })
            .ToListAsync(ct);
        return Results.Ok(list);
    }

    private static async Task<IResult> LinkPatenteAProyecto(
        IApplicationDbContext db, IUser currentUser, string id, string patenteId, CancellationToken ct)
    {
        var roles = currentUser.Roles ?? [];
        var proyecto = await db.Proyectos.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (proyecto == null)
            return Results.NotFound(new { errors = new[] { "Proyecto no encontrado." } });
        if (!await db.Patentes.AnyAsync(p => p.Id == patenteId, ct))
            return Results.NotFound(new { errors = new[] { "Patente no encontrada." } });
        if (await db.ProyectoPatentes.AnyAsync(pp => pp.ProyectoId == id && pp.PatenteId == patenteId, ct))
            return Results.BadRequest(new { errors = new[] { "El vínculo ya existe." } });

        if (!roles.Contains(nameof(RolesEnum.Superuser)) && proyecto.JefeId != currentUser.Id)
            return Results.Forbid();

        db.ProyectoPatentes.Add(new Dashboard_v2.Domain.Entities.ProyectoPatente
        {
            ProyectoId = id,
            PatenteId = patenteId
        });
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> UnlinkPatenteDeProyecto(
        IApplicationDbContext db, IUser currentUser, string id, string patenteId, CancellationToken ct)
    {
        var roles = currentUser.Roles ?? [];
        var proyecto = await db.Proyectos.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (proyecto == null)
            return Results.NotFound(new { errors = new[] { "Proyecto no encontrado." } });

        if (!roles.Contains(nameof(RolesEnum.Superuser)) && proyecto.JefeId != currentUser.Id)
            return Results.Forbid();

        var link = await db.ProyectoPatentes
            .FirstOrDefaultAsync(pp => pp.ProyectoId == id && pp.PatenteId == patenteId, ct);
        if (link == null)
            return Results.NotFound(new { errors = new[] { "Vínculo no encontrado." } });

        db.ProyectoPatentes.Remove(link);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
