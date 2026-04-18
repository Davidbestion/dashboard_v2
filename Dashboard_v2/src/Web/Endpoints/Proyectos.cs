using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Proyectos;
using Microsoft.EntityFrameworkCore;
using Dashboard_v2.Application.Proyectos.Commands.CreateProyectoApoyoPrograma;
using Dashboard_v2.Application.Proyectos.Commands.CreateProyectoColabInternacional;
using Dashboard_v2.Application.Proyectos.Commands.CreateProyectoDesarrolloLocal;
using Dashboard_v2.Application.Proyectos.Commands.CreateProyectoEmpresarial;
using Dashboard_v2.Application.Proyectos.Commands.CreateProyectoEnRevision;
using Dashboard_v2.Application.Proyectos.Commands.CreateProyectoNoEmpresarial;
using Dashboard_v2.Application.Proyectos.Commands.CreateProyectoPNAP;
using Dashboard_v2.Application.Proyectos.Commands.DeleteProyecto;
using Dashboard_v2.Application.Proyectos.Commands.UpdateProyectoApoyoPrograma;
using Dashboard_v2.Application.Proyectos.Commands.UpdateProyectoColabInternacional;
using Dashboard_v2.Application.Proyectos.Commands.UpdateProyectoDesarrolloLocal;
using Dashboard_v2.Application.Proyectos.Commands.UpdateProyectoEmpresarial;
using Dashboard_v2.Application.Proyectos.Commands.UpdateProyectoEnRevision;
using Dashboard_v2.Application.Proyectos.Commands.UpdateProyectoNoEmpresarial;
using Dashboard_v2.Application.Proyectos.Commands.UpdateProyectoPNAP;
using Dashboard_v2.Application.Proyectos.Queries.GetProyectoApoyoPrograma;
using Dashboard_v2.Application.Proyectos.Queries.GetProyectoColabInternacional;
using Dashboard_v2.Application.Proyectos.Queries.GetProyectoDesarrolloLocal;
using Dashboard_v2.Application.Proyectos.Queries.GetProyectoEmpresarial;
using Dashboard_v2.Application.Proyectos.Queries.GetProyectoEnRevision;
using Dashboard_v2.Application.Proyectos.Queries.GetProyectoNoEmpresarial;
using Dashboard_v2.Application.Proyectos.Queries.GetProyectoPNAP;
using Dashboard_v2.Application.Proyectos.Queries.GetProyectos;
using Dashboard_v2.Application.Proyectos.Queries.GetTiposEjecucion;
using Dashboard_v2.Domain.Enums;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;
using Dashboard_v2.Web.Infrastructure;

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
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto)))
            .WithName("GetProyectos")
            .Produces<List<ProyectoResumenDto>>(200);
        // ── Tipos de ejecución disponibles para ProyectoEnRevision.Tipo ────────
        g.MapGet("tipos-ejecucion", GetTiposEjecucion)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto)))
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
    private static async Task<IResult> GetProyectos(ISender sender)
        => Results.Ok(await sender.Send(new GetProyectosQuery()));

    private static async Task<IResult> GetTiposEjecucion(ISender sender)
        => Results.Ok(await sender.Send(new GetTiposEjecucionQuery()));

    private static async Task<IResult> GetCatalogo(IApplicationDbContext context)
    {
        var items = await context.Proyectos
            .AsNoTracking()
            .OrderBy(p => p.Titulo)
            .Select(p => new ProyectoCatalogoDto(p.Id, p.Titulo))
            .ToListAsync();
        return Results.Ok(items);
    }

    private static async Task<IResult> GetPublicacionesDelProyecto(IApplicationDbContext context, string id)
    {
        var pubs = await context.Publications
            .AsNoTracking()
            .Where(p => p.ProyectoId == id)
            .Select(p => new { p.Id, p.Title, p.UrlDoi })
            .OrderBy(p => p.Title)
            .ToListAsync();
        return Results.Ok(pubs);
    }

    private static async Task<IResult> UnlinkPublicacion(
        IApplicationDbContext context, string id, string pubId)
    {
        var pub = await context.Publications
            .FirstOrDefaultAsync(p => p.Id == pubId && p.ProyectoId == id);
        if (pub is null) return Results.NotFound();
        pub.ProyectoId = null;
        await context.SaveChangesAsync(CancellationToken.None);
        return Results.NoContent();
    }

    private static async Task<IResult> GetPublicacionesDisponibles(IApplicationDbContext context)
    {
        var pubs = await context.Publications
            .AsNoTracking()
            .Where(p => p.ProyectoId == null)
            .Select(p => new { p.Id, p.Title, p.UrlDoi })
            .OrderBy(p => p.Title)
            .ToListAsync();
        return Results.Ok(pubs);
    }

    private static async Task<IResult> LinkPublicacion(
        IApplicationDbContext context, string id, string pubId)
    {
        var pub = await context.Publications
            .FirstOrDefaultAsync(p => p.Id == pubId);
        if (pub is null) return Results.NotFound();
        if (pub.ProyectoId is not null && pub.ProyectoId != id)
            return Results.BadRequest(new { errors = new[] { "Esta publicación ya está vinculada a otro proyecto." } });
        pub.ProyectoId = id;
        await context.SaveChangesAsync(CancellationToken.None);
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteProyecto(ISender sender, string id)
    {
        var result = await sender.Send(new DeleteProyectoCommand(id));
        if (!result.Succeeded)
        {
            if (result.Errors.Contains("Proyecto no encontrado."))
                return Results.NotFound(new { errors = result.Errors });
            return Results.BadRequest(new { errors = result.Errors });
        }
        return Results.Ok(new { message = "Proyecto eliminado." });
    }

    // ── En Revisión ───────────────────────────────────────────────────
    private static async Task<IResult> GetEnRevision(ISender sender, string id)
    {
        var dto = await sender.Send(new GetProyectoEnRevisionQuery(id));
        return dto is null ? Results.NotFound() : Results.Ok(dto);
    }

    private static async Task<IResult> CreateEnRevision(ISender sender, ProyectoEnRevisionBody b)
    {
        var (result, id) = await sender.Send(new CreateProyectoEnRevisionCommand
        {
            Titulo = b.Titulo, JefeId = b.JefeId,
            NumeroMiembros = b.NumeroMiembros, CantidadMiembrosUH = b.CantidadMiembrosUH,
            CantidadEstudiantes = b.CantidadEstudiantes,
            CantidadEstudiantesContratados = b.CantidadEstudiantesContratados,
            TributaFormacionDoctoral = b.TributaFormacionDoctoral,
            ClasificacionId = b.ClasificacionId, Situacion = b.Situacion, Tipo = b.Tipo,
        });
        if (!result.Succeeded) return Results.BadRequest(new { errors = result.Errors });
        return Results.Created($"/api/Proyectos/en-revision/{id}", new { id });
    }

    private static async Task<IResult> UpdateEnRevision(ISender sender, string id, ProyectoEnRevisionBody b)
    {
        var result = await sender.Send(new UpdateProyectoEnRevisionCommand
        {
            Id = id, Titulo = b.Titulo, JefeId = b.JefeId,
            NumeroMiembros = b.NumeroMiembros, CantidadMiembrosUH = b.CantidadMiembrosUH,
            CantidadEstudiantes = b.CantidadEstudiantes,
            CantidadEstudiantesContratados = b.CantidadEstudiantesContratados,
            TributaFormacionDoctoral = b.TributaFormacionDoctoral,
            ClasificacionId = b.ClasificacionId, Situacion = b.Situacion, Tipo = b.Tipo,
        });
        if (!result.Succeeded)
        {
            if (result.Errors.Contains("Proyecto no encontrado.")) return Results.NotFound(new { errors = result.Errors });
            return Results.BadRequest(new { errors = result.Errors });
        }
        return Results.Ok(new { message = "Proyecto actualizado." });
    }

    // ── Empresarial ───────────────────────────────────────────────────
    private static async Task<IResult> GetEmpresarial(ISender sender, string id)
    {
        var dto = await sender.Send(new GetProyectoEmpresarialQuery(id));
        return dto is null ? Results.NotFound() : Results.Ok(dto);
    }

    private static async Task<IResult> CreateEmpresarial(ISender sender, ProyectoEmpresarialBody b)
    {
        var (result, id) = await sender.Send(new CreateProyectoEmpresarialCommand
        {
            Titulo = b.Titulo, JefeId = b.JefeId,
            NumeroMiembros = b.NumeroMiembros, CantidadMiembrosUH = b.CantidadMiembrosUH,
            CantidadEstudiantes = b.CantidadEstudiantes,
            CantidadEstudiantesContratados = b.CantidadEstudiantesContratados,
            TributaFormacionDoctoral = b.TributaFormacionDoctoral,
            ClasificacionId = b.ClasificacionId,
            FechaInicio = b.FechaInicio, FechaCierre = b.FechaCierre,
            EstadoDeEjecucion = b.EstadoDeEjecucion, CodigoProyecto = b.CodigoProyecto,
            EntidadEjecutoraPrincipal = b.EntidadEjecutoraPrincipal,
            EntidadEjecutoraParticipante = b.EntidadEjecutoraParticipante,
            ContribucionSectoresEstrategicos = b.ContribucionSectoresEstrategicos,
            ContribucionEjesEstrategicos = b.ContribucionEjesEstrategicos,
            TributaDesarrolloLocal = b.TributaDesarrolloLocal,
            Empresa = b.Empresa,
        });
        if (!result.Succeeded) return Results.BadRequest(new { errors = result.Errors });
        return Results.Created($"/api/Proyectos/empresariales/{id}", new { id });
    }

    private static async Task<IResult> UpdateEmpresarial(ISender sender, string id, ProyectoEmpresarialBody b)
    {
        var result = await sender.Send(new UpdateProyectoEmpresarialCommand
        {
            Id = id, Titulo = b.Titulo, JefeId = b.JefeId,
            NumeroMiembros = b.NumeroMiembros, CantidadMiembrosUH = b.CantidadMiembrosUH,
            CantidadEstudiantes = b.CantidadEstudiantes,
            CantidadEstudiantesContratados = b.CantidadEstudiantesContratados,
            TributaFormacionDoctoral = b.TributaFormacionDoctoral,
            ClasificacionId = b.ClasificacionId,
            FechaInicio = b.FechaInicio, FechaCierre = b.FechaCierre,
            EstadoDeEjecucion = b.EstadoDeEjecucion, CodigoProyecto = b.CodigoProyecto,
            EntidadEjecutoraPrincipal = b.EntidadEjecutoraPrincipal,
            EntidadEjecutoraParticipante = b.EntidadEjecutoraParticipante,
            ContribucionSectoresEstrategicos = b.ContribucionSectoresEstrategicos,
            ContribucionEjesEstrategicos = b.ContribucionEjesEstrategicos,
            TributaDesarrolloLocal = b.TributaDesarrolloLocal,
            Empresa = b.Empresa,
        });
        if (!result.Succeeded)
        {
            if (result.Errors.Contains("Proyecto no encontrado.")) return Results.NotFound(new { errors = result.Errors });
            return Results.BadRequest(new { errors = result.Errors });
        }
        return Results.Ok(new { message = "Proyecto actualizado." });
    }

    // ── Apoyo a Programa ──────────────────────────────────────────────
    private static async Task<IResult> GetApoyoPrograma(ISender sender, string id)
    {
        var dto = await sender.Send(new GetProyectoApoyoProgramaQuery(id));
        return dto is null ? Results.NotFound() : Results.Ok(dto);
    }

    private static async Task<IResult> CreateApoyoPrograma(ISender sender, ProyectoApoyoProgramaBody b)
    {
        var (result, id) = await sender.Send(new CreateProyectoApoyoProgramaCommand
        {
            Titulo = b.Titulo, JefeId = b.JefeId,
            NumeroMiembros = b.NumeroMiembros, CantidadMiembrosUH = b.CantidadMiembrosUH,
            CantidadEstudiantes = b.CantidadEstudiantes,
            CantidadEstudiantesContratados = b.CantidadEstudiantesContratados,
            TributaFormacionDoctoral = b.TributaFormacionDoctoral,
            ClasificacionId = b.ClasificacionId,
            FechaInicio = b.FechaInicio, FechaCierre = b.FechaCierre,
            EstadoDeEjecucion = b.EstadoDeEjecucion, CodigoProyecto = b.CodigoProyecto,
            EntidadEjecutoraPrincipal = b.EntidadEjecutoraPrincipal,
            EntidadEjecutoraParticipante = b.EntidadEjecutoraParticipante,
            ContribucionSectoresEstrategicos = b.ContribucionSectoresEstrategicos,
            ContribucionEjesEstrategicos = b.ContribucionEjesEstrategicos,
            TributaDesarrolloLocal = b.TributaDesarrolloLocal,
            NombrePrograma = b.NombrePrograma, TipoPAP = b.TipoPAP,
        });
        if (!result.Succeeded) return Results.BadRequest(new { errors = result.Errors });
        return Results.Created($"/api/Proyectos/apoyo-programa/{id}", new { id });
    }

    private static async Task<IResult> UpdateApoyoPrograma(ISender sender, string id, ProyectoApoyoProgramaBody b)
    {
        var result = await sender.Send(new UpdateProyectoApoyoProgramaCommand
        {
            Id = id, Titulo = b.Titulo, JefeId = b.JefeId,
            NumeroMiembros = b.NumeroMiembros, CantidadMiembrosUH = b.CantidadMiembrosUH,
            CantidadEstudiantes = b.CantidadEstudiantes,
            CantidadEstudiantesContratados = b.CantidadEstudiantesContratados,
            TributaFormacionDoctoral = b.TributaFormacionDoctoral,
            ClasificacionId = b.ClasificacionId,
            FechaInicio = b.FechaInicio, FechaCierre = b.FechaCierre,
            EstadoDeEjecucion = b.EstadoDeEjecucion, CodigoProyecto = b.CodigoProyecto,
            EntidadEjecutoraPrincipal = b.EntidadEjecutoraPrincipal,
            EntidadEjecutoraParticipante = b.EntidadEjecutoraParticipante,
            ContribucionSectoresEstrategicos = b.ContribucionSectoresEstrategicos,
            ContribucionEjesEstrategicos = b.ContribucionEjesEstrategicos,
            TributaDesarrolloLocal = b.TributaDesarrolloLocal,
            NombrePrograma = b.NombrePrograma, TipoPAP = b.TipoPAP,
        });
        if (!result.Succeeded)
        {
            if (result.Errors.Contains("Proyecto no encontrado.")) return Results.NotFound(new { errors = result.Errors });
            return Results.BadRequest(new { errors = result.Errors });
        }
        return Results.Ok(new { message = "Proyecto actualizado." });
    }

    // ── Desarrollo Local ──────────────────────────────────────────────
    private static async Task<IResult> GetDesarrolloLocal(ISender sender, string id)
    {
        var dto = await sender.Send(new GetProyectoDesarrolloLocalQuery(id));
        return dto is null ? Results.NotFound() : Results.Ok(dto);
    }

    private static async Task<IResult> CreateDesarrolloLocal(ISender sender, ProyectoDesarrolloLocalBody b)
    {
        var (result, id) = await sender.Send(new CreateProyectoDesarrolloLocalCommand
        {
            Titulo = b.Titulo, JefeId = b.JefeId,
            NumeroMiembros = b.NumeroMiembros, CantidadMiembrosUH = b.CantidadMiembrosUH,
            CantidadEstudiantes = b.CantidadEstudiantes,
            CantidadEstudiantesContratados = b.CantidadEstudiantesContratados,
            TributaFormacionDoctoral = b.TributaFormacionDoctoral,
            ClasificacionId = b.ClasificacionId,
            FechaInicio = b.FechaInicio, FechaCierre = b.FechaCierre,
            EstadoDeEjecucion = b.EstadoDeEjecucion, CodigoProyecto = b.CodigoProyecto,
            EntidadEjecutoraPrincipal = b.EntidadEjecutoraPrincipal,
            EntidadEjecutoraParticipante = b.EntidadEjecutoraParticipante,
            ContribucionSectoresEstrategicos = b.ContribucionSectoresEstrategicos,
            ContribucionEjesEstrategicos = b.ContribucionEjesEstrategicos,
            Municipio = b.Municipio,
        });
        if (!result.Succeeded) return Results.BadRequest(new { errors = result.Errors });
        return Results.Created($"/api/Proyectos/desarrollo-local/{id}", new { id });
    }

    private static async Task<IResult> UpdateDesarrolloLocal(ISender sender, string id, ProyectoDesarrolloLocalBody b)
    {
        var result = await sender.Send(new UpdateProyectoDesarrolloLocalCommand
        {
            Id = id, Titulo = b.Titulo, JefeId = b.JefeId,
            NumeroMiembros = b.NumeroMiembros, CantidadMiembrosUH = b.CantidadMiembrosUH,
            CantidadEstudiantes = b.CantidadEstudiantes,
            CantidadEstudiantesContratados = b.CantidadEstudiantesContratados,
            TributaFormacionDoctoral = b.TributaFormacionDoctoral,
            ClasificacionId = b.ClasificacionId,
            FechaInicio = b.FechaInicio, FechaCierre = b.FechaCierre,
            EstadoDeEjecucion = b.EstadoDeEjecucion, CodigoProyecto = b.CodigoProyecto,
            EntidadEjecutoraPrincipal = b.EntidadEjecutoraPrincipal,
            EntidadEjecutoraParticipante = b.EntidadEjecutoraParticipante,
            ContribucionSectoresEstrategicos = b.ContribucionSectoresEstrategicos,
            ContribucionEjesEstrategicos = b.ContribucionEjesEstrategicos,
            Municipio = b.Municipio,
        });
        if (!result.Succeeded)
        {
            if (result.Errors.Contains("Proyecto no encontrado.")) return Results.NotFound(new { errors = result.Errors });
            return Results.BadRequest(new { errors = result.Errors });
        }
        return Results.Ok(new { message = "Proyecto actualizado." });
    }

    // ── No Empresarial ────────────────────────────────────────────────
    private static async Task<IResult> GetNoEmpresarial(ISender sender, string id)
    {
        var dto = await sender.Send(new GetProyectoNoEmpresarialQuery(id));
        return dto is null ? Results.NotFound() : Results.Ok(dto);
    }

    private static async Task<IResult> CreateNoEmpresarial(ISender sender, ProyectoNoEmpresarialBody b)
    {
        var (result, id) = await sender.Send(new CreateProyectoNoEmpresarialCommand
        {
            Titulo = b.Titulo, JefeId = b.JefeId,
            NumeroMiembros = b.NumeroMiembros, CantidadMiembrosUH = b.CantidadMiembrosUH,
            CantidadEstudiantes = b.CantidadEstudiantes,
            CantidadEstudiantesContratados = b.CantidadEstudiantesContratados,
            TributaFormacionDoctoral = b.TributaFormacionDoctoral,
            ClasificacionId = b.ClasificacionId,
            FechaInicio = b.FechaInicio, FechaCierre = b.FechaCierre,
            EstadoDeEjecucion = b.EstadoDeEjecucion, CodigoProyecto = b.CodigoProyecto,
            EntidadEjecutoraPrincipal = b.EntidadEjecutoraPrincipal,
            EntidadEjecutoraParticipante = b.EntidadEjecutoraParticipante,
            ContribucionSectoresEstrategicos = b.ContribucionSectoresEstrategicos,
            ContribucionEjesEstrategicos = b.ContribucionEjesEstrategicos,
            TributaDesarrolloLocal = b.TributaDesarrolloLocal,
            EntidadNoEmpresarial = b.EntidadNoEmpresarial,
        });
        if (!result.Succeeded) return Results.BadRequest(new { errors = result.Errors });
        return Results.Created($"/api/Proyectos/no-empresariales/{id}", new { id });
    }

    private static async Task<IResult> UpdateNoEmpresarial(ISender sender, string id, ProyectoNoEmpresarialBody b)
    {
        var result = await sender.Send(new UpdateProyectoNoEmpresarialCommand
        {
            Id = id, Titulo = b.Titulo, JefeId = b.JefeId,
            NumeroMiembros = b.NumeroMiembros, CantidadMiembrosUH = b.CantidadMiembrosUH,
            CantidadEstudiantes = b.CantidadEstudiantes,
            CantidadEstudiantesContratados = b.CantidadEstudiantesContratados,
            TributaFormacionDoctoral = b.TributaFormacionDoctoral,
            ClasificacionId = b.ClasificacionId,
            FechaInicio = b.FechaInicio, FechaCierre = b.FechaCierre,
            EstadoDeEjecucion = b.EstadoDeEjecucion, CodigoProyecto = b.CodigoProyecto,
            EntidadEjecutoraPrincipal = b.EntidadEjecutoraPrincipal,
            EntidadEjecutoraParticipante = b.EntidadEjecutoraParticipante,
            ContribucionSectoresEstrategicos = b.ContribucionSectoresEstrategicos,
            ContribucionEjesEstrategicos = b.ContribucionEjesEstrategicos,
            TributaDesarrolloLocal = b.TributaDesarrolloLocal,
            EntidadNoEmpresarial = b.EntidadNoEmpresarial,
        });
        if (!result.Succeeded)
        {
            if (result.Errors.Contains("Proyecto no encontrado.")) return Results.NotFound(new { errors = result.Errors });
            return Results.BadRequest(new { errors = result.Errors });
        }
        return Results.Ok(new { message = "Proyecto actualizado." });
    }

    // ── Colaboración Internacional ─────────────────────────────────────
    private static async Task<IResult> GetColabInternacional(ISender sender, string id)
    {
        var dto = await sender.Send(new GetProyectoColabInternacionalQuery(id));
        return dto is null ? Results.NotFound() : Results.Ok(dto);
    }

    private static async Task<IResult> CreateColabInternacional(ISender sender, ProyectoColabInternacionalBody b)
    {
        var (result, id) = await sender.Send(new CreateProyectoColabInternacionalCommand
        {
            Titulo = b.Titulo, JefeId = b.JefeId,
            NumeroMiembros = b.NumeroMiembros, CantidadMiembrosUH = b.CantidadMiembrosUH,
            CantidadEstudiantes = b.CantidadEstudiantes,
            CantidadEstudiantesContratados = b.CantidadEstudiantesContratados,
            TributaFormacionDoctoral = b.TributaFormacionDoctoral,
            ClasificacionId = b.ClasificacionId,
            FechaInicio = b.FechaInicio, FechaCierre = b.FechaCierre,
            EstadoDeEjecucion = b.EstadoDeEjecucion, CodigoProyecto = b.CodigoProyecto,
            EntidadEjecutoraPrincipal = b.EntidadEjecutoraPrincipal,
            EntidadEjecutoraParticipante = b.EntidadEjecutoraParticipante,
            ContribucionSectoresEstrategicos = b.ContribucionSectoresEstrategicos,
            ContribucionEjesEstrategicos = b.ContribucionEjesEstrategicos,
            TributaDesarrolloLocal = b.TributaDesarrolloLocal,
            FuenteFinanciacion = b.FuenteFinanciacion, TerminosReferencia = b.TerminosReferencia,
        });
        if (!result.Succeeded) return Results.BadRequest(new { errors = result.Errors });
        return Results.Created($"/api/Proyectos/colaboracion-internacional/{id}", new { id });
    }

    private static async Task<IResult> UpdateColabInternacional(ISender sender, string id, ProyectoColabInternacionalBody b)
    {
        var result = await sender.Send(new UpdateProyectoColabInternacionalCommand
        {
            Id = id, Titulo = b.Titulo, JefeId = b.JefeId,
            NumeroMiembros = b.NumeroMiembros, CantidadMiembrosUH = b.CantidadMiembrosUH,
            CantidadEstudiantes = b.CantidadEstudiantes,
            CantidadEstudiantesContratados = b.CantidadEstudiantesContratados,
            TributaFormacionDoctoral = b.TributaFormacionDoctoral,
            ClasificacionId = b.ClasificacionId,
            FechaInicio = b.FechaInicio, FechaCierre = b.FechaCierre,
            EstadoDeEjecucion = b.EstadoDeEjecucion, CodigoProyecto = b.CodigoProyecto,
            EntidadEjecutoraPrincipal = b.EntidadEjecutoraPrincipal,
            EntidadEjecutoraParticipante = b.EntidadEjecutoraParticipante,
            ContribucionSectoresEstrategicos = b.ContribucionSectoresEstrategicos,
            ContribucionEjesEstrategicos = b.ContribucionEjesEstrategicos,
            TributaDesarrolloLocal = b.TributaDesarrolloLocal,
            FuenteFinanciacion = b.FuenteFinanciacion, TerminosReferencia = b.TerminosReferencia,
        });
        if (!result.Succeeded)
        {
            if (result.Errors.Contains("Proyecto no encontrado.")) return Results.NotFound(new { errors = result.Errors });
            return Results.BadRequest(new { errors = result.Errors });
        }
        return Results.Ok(new { message = "Proyecto actualizado." });
    }

    // ── PNAP ──────────────────────────────────────────────────────────
    private static async Task<IResult> GetPNAP(ISender sender, string id)
    {
        var dto = await sender.Send(new GetProyectoPNAPQuery(id));
        return dto is null ? Results.NotFound() : Results.Ok(dto);
    }

    private static async Task<IResult> CreatePNAP(ISender sender, ProyectoPNAPBody b)
    {
        var (result, id) = await sender.Send(new CreateProyectoPNAPCommand
        {
            Titulo = b.Titulo, JefeId = b.JefeId,
            NumeroMiembros = b.NumeroMiembros, CantidadMiembrosUH = b.CantidadMiembrosUH,
            CantidadEstudiantes = b.CantidadEstudiantes,
            CantidadEstudiantesContratados = b.CantidadEstudiantesContratados,
            TributaFormacionDoctoral = b.TributaFormacionDoctoral,
            ClasificacionId = b.ClasificacionId,
            FechaInicio = b.FechaInicio, FechaCierre = b.FechaCierre,
            EstadoDeEjecucion = b.EstadoDeEjecucion, CodigoProyecto = b.CodigoProyecto,
            EntidadEjecutoraPrincipal = b.EntidadEjecutoraPrincipal,
            EntidadEjecutoraParticipante = b.EntidadEjecutoraParticipante,
            ContribucionSectoresEstrategicos = b.ContribucionSectoresEstrategicos,
            ContribucionEjesEstrategicos = b.ContribucionEjesEstrategicos,
            TributaDesarrolloLocal = b.TributaDesarrolloLocal,
            FinanciamientoUH = b.FinanciamientoUH,
        });
        if (!result.Succeeded) return Results.BadRequest(new { errors = result.Errors });
        return Results.Created($"/api/Proyectos/pnap/{id}", new { id });
    }

    private static async Task<IResult> UpdatePNAP(ISender sender, string id, ProyectoPNAPBody b)
    {
        var result = await sender.Send(new UpdateProyectoPNAPCommand
        {
            Id = id, Titulo = b.Titulo, JefeId = b.JefeId,
            NumeroMiembros = b.NumeroMiembros, CantidadMiembrosUH = b.CantidadMiembrosUH,
            CantidadEstudiantes = b.CantidadEstudiantes,
            CantidadEstudiantesContratados = b.CantidadEstudiantesContratados,
            TributaFormacionDoctoral = b.TributaFormacionDoctoral,
            ClasificacionId = b.ClasificacionId,
            FechaInicio = b.FechaInicio, FechaCierre = b.FechaCierre,
            EstadoDeEjecucion = b.EstadoDeEjecucion, CodigoProyecto = b.CodigoProyecto,
            EntidadEjecutoraPrincipal = b.EntidadEjecutoraPrincipal,
            EntidadEjecutoraParticipante = b.EntidadEjecutoraParticipante,
            ContribucionSectoresEstrategicos = b.ContribucionSectoresEstrategicos,
            ContribucionEjesEstrategicos = b.ContribucionEjesEstrategicos,
            TributaDesarrolloLocal = b.TributaDesarrolloLocal,
            FinanciamientoUH = b.FinanciamientoUH,
        });
        if (!result.Succeeded)
        {
            if (result.Errors.Contains("Proyecto no encontrado.")) return Results.NotFound(new { errors = result.Errors });
            return Results.BadRequest(new { errors = result.Errors });
        }
        return Results.Ok(new { message = "Proyecto actualizado." });
    }
}

// ── Request body records ───────────────────────────────────────────────

internal record ProyectoEnRevisionBody(
    string Titulo, string JefeId,
    int NumeroMiembros, int CantidadMiembrosUH, int CantidadEstudiantes,
    int CantidadEstudiantesContratados, bool TributaFormacionDoctoral,
    string ClasificacionId,
    string Situacion, string Tipo);

internal record ProyectoEmpresarialBody(
    string Titulo, string JefeId,
    int NumeroMiembros, int CantidadMiembrosUH, int CantidadEstudiantes,
    int CantidadEstudiantesContratados, bool TributaFormacionDoctoral,
    string ClasificacionId,
    DateOnly FechaInicio, DateOnly? FechaCierre,
    string EstadoDeEjecucion, string CodigoProyecto,
    string EntidadEjecutoraPrincipal, string? EntidadEjecutoraParticipante,
    string? ContribucionSectoresEstrategicos, string? ContribucionEjesEstrategicos,
    bool TributaDesarrolloLocal,
    string Empresa);

internal record ProyectoApoyoProgramaBody(
    string Titulo, string JefeId,
    int NumeroMiembros, int CantidadMiembrosUH, int CantidadEstudiantes,
    int CantidadEstudiantesContratados, bool TributaFormacionDoctoral,
    string ClasificacionId,
    DateOnly FechaInicio, DateOnly? FechaCierre,
    string EstadoDeEjecucion, string CodigoProyecto,
    string EntidadEjecutoraPrincipal, string? EntidadEjecutoraParticipante,
    string? ContribucionSectoresEstrategicos, string? ContribucionEjesEstrategicos,
    bool TributaDesarrolloLocal,
    string NombrePrograma, TipoPAP TipoPAP);

internal record ProyectoDesarrolloLocalBody(
    string Titulo, string JefeId,
    int NumeroMiembros, int CantidadMiembrosUH, int CantidadEstudiantes,
    int CantidadEstudiantesContratados, bool TributaFormacionDoctoral,
    string ClasificacionId,
    DateOnly FechaInicio, DateOnly? FechaCierre,
    string EstadoDeEjecucion, string CodigoProyecto,
    string EntidadEjecutoraPrincipal, string? EntidadEjecutoraParticipante,
    string? ContribucionSectoresEstrategicos, string? ContribucionEjesEstrategicos,
    string Municipio);

internal record ProyectoNoEmpresarialBody(
    string Titulo, string JefeId,
    int NumeroMiembros, int CantidadMiembrosUH, int CantidadEstudiantes,
    int CantidadEstudiantesContratados, bool TributaFormacionDoctoral,
    string ClasificacionId,
    DateOnly FechaInicio, DateOnly? FechaCierre,
    string EstadoDeEjecucion, string CodigoProyecto,
    string EntidadEjecutoraPrincipal, string? EntidadEjecutoraParticipante,
    string? ContribucionSectoresEstrategicos, string? ContribucionEjesEstrategicos,
    bool TributaDesarrolloLocal,
    string EntidadNoEmpresarial);

internal record ProyectoColabInternacionalBody(
    string Titulo, string JefeId,
    int NumeroMiembros, int CantidadMiembrosUH, int CantidadEstudiantes,
    int CantidadEstudiantesContratados, bool TributaFormacionDoctoral,
    string ClasificacionId,
    DateOnly FechaInicio, DateOnly? FechaCierre,
    string EstadoDeEjecucion, string CodigoProyecto,
    string EntidadEjecutoraPrincipal, string? EntidadEjecutoraParticipante,
    string? ContribucionSectoresEstrategicos, string? ContribucionEjesEstrategicos,
    bool TributaDesarrolloLocal,
    string FuenteFinanciacion, string TerminosReferencia);

internal record ProyectoPNAPBody(
    string Titulo, string JefeId,
    int NumeroMiembros, int CantidadMiembrosUH, int CantidadEstudiantes,
    int CantidadEstudiantesContratados, bool TributaFormacionDoctoral,
    string ClasificacionId,
    DateOnly FechaInicio, DateOnly? FechaCierre,
    string EstadoDeEjecucion, string CodigoProyecto,
    string EntidadEjecutoraPrincipal, string? EntidadEjecutoraParticipante,
    string? ContribucionSectoresEstrategicos, string? ContribucionEjesEstrategicos,
    bool TributaDesarrolloLocal,
    string FinanciamientoUH);

/// <summary>Par mínimo Id/Título para el selector de proyectos en el formulario de publicaciones.</summary>
public record ProyectoCatalogoDto(string Id, string Titulo);
