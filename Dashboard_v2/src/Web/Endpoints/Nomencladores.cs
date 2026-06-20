using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Web.Infrastructure;
using Microsoft.EntityFrameworkCore;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Web.Endpoints;

public class Nomencladores : EndpointGroupBase
{
    private static readonly string[] NombreRequeridoError = ["El nombre es obligatorio."];

    public override void Map(RouteGroupBuilder g)
    {
        var canWrite = new Action<Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder>(
            p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto)));

        var canRead = new Action<Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder>(
            p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto),
                               nameof(RolesEnum.Vicedecano_de_investigacion)));

        g.MapGet("estados",    GetEstados)       .RequireAuthorization(canRead) .WithName("GetEstadosProyecto");
        g.MapPost("estados",   CreateEstado)     .RequireAuthorization(canWrite).WithName("CreateEstadoProyecto");
        g.MapGet("situaciones", GetSituaciones)  .RequireAuthorization(canRead) .WithName("GetSituacionesProyecto");
        g.MapPost("situaciones", CreateSituacion).RequireAuthorization(canWrite).WithName("CreateSituacionProyecto");
        g.MapGet("sectores",   GetSectores)      .RequireAuthorization(canRead) .WithName("GetSectoresEstrategicos");
        g.MapPost("sectores",  CreateSector)     .RequireAuthorization(canWrite).WithName("CreateSectorEstrategico");
        g.MapGet("ejes",       GetEjes)          .RequireAuthorization(canRead) .WithName("GetEjesEstrategicos");
        g.MapPost("ejes",      CreateEje)        .RequireAuthorization(canWrite).WithName("CreateEjeEstrategico");
        g.MapGet("fuentes",    GetFuentes)       .RequireAuthorization(canRead) .WithName("GetFuentesFinanciacion");
        g.MapPost("fuentes",   CreateFuente)     .RequireAuthorization(canWrite).WithName("CreateFuenteFinanciacion");
        g.MapGet("programas",  GetProgramas)     .RequireAuthorization(canRead) .WithName("GetProgramas");
        g.MapPost("programas", CreatePrograma)   .RequireAuthorization(canWrite).WithName("CreatePrograma");
        g.MapGet("provincias", GetProvincias)    .RequireAuthorization(canRead) .WithName("GetProvincias");
        g.MapGet("municipios", GetMunicipios)    .RequireAuthorization(canRead) .WithName("GetMunicipios");
        g.MapGet("basesdedatos",  GetBasesDeDatos)   .RequireAuthorization().WithName("GetBasesDeDatosPublicacion");
        g.MapPost("basesdedatos", CreateBaseDeDatos) .RequireAuthorization().WithName("CreateBaseDeDatosPublicacion");
        g.MapGet("tiposnorma",    GetTiposNorma)     .RequireAuthorization().WithName("GetTiposNorma");
        g.MapPost("tiposnorma",   CreateTipoNorma)   .RequireAuthorization().WithName("CreateTipoNorma");
        g.MapGet("tiposproducto",  GetTiposProducto)   .RequireAuthorization().WithName("GetTiposProductoComercializado");
        g.MapPost("tiposproducto", CreateTipoProducto) .RequireAuthorization().WithName("CreateTipoProductoComercializado");
    }

    // ── GET ──────────────────────────────────────────────────────────────────

    private static async Task<IResult> GetEstados(IApplicationDbContext db, CancellationToken ct)
        => Results.Ok(await db.EstadosProyecto.OrderBy(x => x.Nombre).Select(x => new { x.Id, x.Nombre }).ToListAsync(ct));

    private static async Task<IResult> GetSituaciones(IApplicationDbContext db, CancellationToken ct)
        => Results.Ok(await db.SituacionesProyecto.OrderBy(x => x.Nombre).Select(x => new { x.Id, x.Nombre }).ToListAsync(ct));

    private static async Task<IResult> GetSectores(IApplicationDbContext db, CancellationToken ct)
        => Results.Ok(await db.SectoresEstrategicos.OrderBy(x => x.Nombre).Select(x => new { x.Id, x.Nombre }).ToListAsync(ct));

    private static async Task<IResult> GetEjes(IApplicationDbContext db, CancellationToken ct)
        => Results.Ok(await db.EjesEstrategicos.OrderBy(x => x.Nombre).Select(x => new { x.Id, x.Nombre }).ToListAsync(ct));

    private static async Task<IResult> GetFuentes(IApplicationDbContext db, CancellationToken ct)
        => Results.Ok(await db.FuentesFinanciacion.OrderBy(x => x.Nombre).Select(x => new { x.Id, x.Nombre }).ToListAsync(ct));

    private static async Task<IResult> GetProgramas(IApplicationDbContext db, CancellationToken ct)
        => Results.Ok(await db.Programas.OrderBy(x => x.Nombre).Select(x => new { x.Id, x.Nombre }).ToListAsync(ct));

    private static async Task<IResult> GetProvincias(IApplicationDbContext db, CancellationToken ct)
        => Results.Ok(await db.Provincias.OrderBy(x => x.Nombre).Select(x => new { x.Id, x.Nombre }).ToListAsync(ct));

    private static async Task<IResult> GetMunicipios(IApplicationDbContext db, CancellationToken ct)
        => Results.Ok(await db.Municipios.OrderBy(x => x.Nombre).Select(x => new { x.Id, x.Nombre, x.ProvinciaId }).ToListAsync(ct));

    private static async Task<IResult> GetBasesDeDatos(IApplicationDbContext db, CancellationToken ct)
        => Results.Ok(await db.BasesDeDatosPublicacion.OrderBy(x => x.Nombre).Select(x => new { x.Id, x.Nombre }).ToListAsync(ct));

    private static async Task<IResult> CreateBaseDeDatos(IApplicationDbContext db, NomencladorCreateRequest req, CancellationToken ct)
    {
        var nombre = req.Nombre?.Trim();
        if (string.IsNullOrEmpty(nombre)) return Results.BadRequest(new { errors = NombreRequeridoError });
        var ex = await db.BasesDeDatosPublicacion.FirstOrDefaultAsync(x => x.Nombre.ToLower() == nombre.ToLower(), ct);
        if (ex is not null) return Results.Ok(new { ex.Id, ex.Nombre });
        var e = new BaseDeDatosPublicacion { Nombre = nombre };
        db.BasesDeDatosPublicacion.Add(e);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/Nomencladores/basesdedatos/{e.Id}", new { e.Id, e.Nombre });
    }

    // ── POST (upsert-by-name) ─────────────────────────────────────────────────

    private static async Task<IResult> CreateEstado(IApplicationDbContext db, NomencladorCreateRequest req, CancellationToken ct)
    {
        var nombre = req.Nombre?.Trim();
        if (string.IsNullOrEmpty(nombre)) return Results.BadRequest(new { errors = NombreRequeridoError });
        var ex = await db.EstadosProyecto.FirstOrDefaultAsync(x => x.Nombre.ToLower() == nombre.ToLower(), ct);
        if (ex is not null) return Results.Ok(new { ex.Id, ex.Nombre });
        var e = new EstadoProyecto { Nombre = nombre };
        db.EstadosProyecto.Add(e);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/Nomencladores/estados/{e.Id}", new { e.Id, e.Nombre });
    }

    private static async Task<IResult> CreateSituacion(IApplicationDbContext db, NomencladorCreateRequest req, CancellationToken ct)
    {
        var nombre = req.Nombre?.Trim();
        if (string.IsNullOrEmpty(nombre)) return Results.BadRequest(new { errors = NombreRequeridoError });
        var ex = await db.SituacionesProyecto.FirstOrDefaultAsync(x => x.Nombre.ToLower() == nombre.ToLower(), ct);
        if (ex is not null) return Results.Ok(new { ex.Id, ex.Nombre });
        var e = new SituacionProyecto { Nombre = nombre };
        db.SituacionesProyecto.Add(e);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/Nomencladores/situaciones/{e.Id}", new { e.Id, e.Nombre });
    }

    private static async Task<IResult> CreateSector(IApplicationDbContext db, NomencladorCreateRequest req, CancellationToken ct)
    {
        var nombre = req.Nombre?.Trim();
        if (string.IsNullOrEmpty(nombre)) return Results.BadRequest(new { errors = NombreRequeridoError });
        var ex = await db.SectoresEstrategicos.FirstOrDefaultAsync(x => x.Nombre.ToLower() == nombre.ToLower(), ct);
        if (ex is not null) return Results.Ok(new { ex.Id, ex.Nombre });
        var e = new SectorEstrategico { Nombre = nombre };
        db.SectoresEstrategicos.Add(e);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/Nomencladores/sectores/{e.Id}", new { e.Id, e.Nombre });
    }

    private static async Task<IResult> CreateEje(IApplicationDbContext db, NomencladorCreateRequest req, CancellationToken ct)
    {
        var nombre = req.Nombre?.Trim();
        if (string.IsNullOrEmpty(nombre)) return Results.BadRequest(new { errors = NombreRequeridoError });
        var ex = await db.EjesEstrategicos.FirstOrDefaultAsync(x => x.Nombre.ToLower() == nombre.ToLower(), ct);
        if (ex is not null) return Results.Ok(new { ex.Id, ex.Nombre });
        var e = new EjeEstrategico { Nombre = nombre };
        db.EjesEstrategicos.Add(e);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/Nomencladores/ejes/{e.Id}", new { e.Id, e.Nombre });
    }

    private static async Task<IResult> CreateFuente(IApplicationDbContext db, NomencladorCreateRequest req, CancellationToken ct)
    {
        var nombre = req.Nombre?.Trim();
        if (string.IsNullOrEmpty(nombre)) return Results.BadRequest(new { errors = NombreRequeridoError });
        var ex = await db.FuentesFinanciacion.FirstOrDefaultAsync(x => x.Nombre.ToLower() == nombre.ToLower(), ct);
        if (ex is not null) return Results.Ok(new { ex.Id, ex.Nombre });
        var e = new FuenteFinanciacion { Nombre = nombre };
        db.FuentesFinanciacion.Add(e);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/Nomencladores/fuentes/{e.Id}", new { e.Id, e.Nombre });
    }

    private static async Task<IResult> CreatePrograma(IApplicationDbContext db, NomencladorCreateRequest req, CancellationToken ct)
    {
        var nombre = req.Nombre?.Trim();
        if (string.IsNullOrEmpty(nombre)) return Results.BadRequest(new { errors = NombreRequeridoError });
        var ex = await db.Programas.FirstOrDefaultAsync(x => x.Nombre.ToLower() == nombre.ToLower(), ct);
        if (ex is not null) return Results.Ok(new { ex.Id, ex.Nombre });
        var e = new Programa { Nombre = nombre };
        db.Programas.Add(e);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/Nomencladores/programas/{e.Id}", new { e.Id, e.Nombre });
    }
    private static async Task<IResult> GetTiposNorma(IApplicationDbContext db, CancellationToken ct)
        => Results.Ok(await db.TiposNorma.OrderBy(x => x.Nombre).Select(x => new { x.Id, x.Nombre }).ToListAsync(ct));

    private static async Task<IResult> CreateTipoNorma(IApplicationDbContext db, NomencladorCreateRequest req, CancellationToken ct)
    {
        var nombre = req.Nombre?.Trim();
        if (string.IsNullOrEmpty(nombre)) return Results.BadRequest(new { errors = NombreRequeridoError });
        var ex = await db.TiposNorma.FirstOrDefaultAsync(x => x.Nombre.ToLower() == nombre.ToLower(), ct);
        if (ex is not null) return Results.Ok(new { ex.Id, ex.Nombre });
        var e = new Domain.Entities.TipoNorma { Nombre = nombre };
        db.TiposNorma.Add(e);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/Nomencladores/tiposnorma/{e.Id}", new { e.Id, e.Nombre });
    }

    private static async Task<IResult> GetTiposProducto(IApplicationDbContext db, CancellationToken ct)
        => Results.Ok(await db.TipoProductosComercializados.OrderBy(x => x.Nombre).Select(x => new { x.Id, x.Nombre }).ToListAsync(ct));

    private static async Task<IResult> CreateTipoProducto(IApplicationDbContext db, NomencladorCreateRequest req, CancellationToken ct)
    {
        var nombre = req.Nombre?.Trim();
        if (string.IsNullOrEmpty(nombre)) return Results.BadRequest(new { errors = NombreRequeridoError });
        var ex = await db.TipoProductosComercializados.FirstOrDefaultAsync(x => x.Nombre.ToLower() == nombre.ToLower(), ct);
        if (ex is not null) return Results.Ok(new { ex.Id, ex.Nombre });
        var e = new Domain.Entities.TipoProductoComercializado { Nombre = nombre };
        db.TipoProductosComercializados.Add(e);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/Nomencladores/tiposproducto/{e.Id}", new { e.Id, e.Nombre });
    }
}

internal record NomencladorCreateRequest(string? Nombre);
