using Dashboard_v2.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Web.Endpoints;

public class Registros : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("", GetRegistros)
            .RequireAuthorization()
            .WithName("GetRegistros")
            .Produces<List<RegistroDto>>(200);

        groupBuilder.MapGet("mis", GetMisRegistros)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Jefe_de_Proyecto), nameof(RolesEnum.Superuser)))
            .WithName("GetMisRegistros")
            .Produces<List<RegistroDto>>(200);

        groupBuilder.MapPost("", CreateRegistro)
            .RequireAuthorization(p => p.RequireRole("Superuser"))
            .WithName("CreateRegistro")
            .Produces(201)
            .ProducesProblem(400);

        groupBuilder.MapPut("{id}", UpdateRegistro)
            .RequireAuthorization(p => p.RequireRole("Superuser"))
            .WithName("UpdateRegistro")
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(404);

        groupBuilder.MapDelete("{id}", DeleteRegistro)
            .RequireAuthorization(p => p.RequireRole("Superuser"))
            .WithName("DeleteRegistro")
            .Produces(200)
            .ProducesProblem(404);
    }

    private async Task<IResult> GetRegistros(IApplicationDbContext db)
    {
        var list = await db.Registros
            .Include(r => r.Country)
            .Include(r => r.Institution)
            .Include(r => r.Creadores).ThenInclude(c => c.User)
            .Select(r => new RegistroDto(
                r.Id, r.Titulo, r.NumeroCertificado, r.EsInformatico,
                r.CountryId, r.Country.Name, r.InstitutionId, r.Institution.Nombre, r.EvidenceFileId,
                r.Creadores.Select(c => c.User.UserName + " " + c.User.UserLastName1).ToList()))
            .ToListAsync();
        return Results.Ok(list);
    }

    private static async Task<IResult> GetMisRegistros(IApplicationDbContext db, IUser currentUser)
    {
        var userId = currentUser.Id;
        var list = await db.UserRegistros
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Registro).ThenInclude(r => r.Country)
            .Include(ur => ur.Registro).ThenInclude(r => r.Institution)
            .Include(ur => ur.Registro).ThenInclude(r => r.Creadores).ThenInclude(c => c.User)
            .Select(ur => new RegistroDto(
                ur.Registro.Id, ur.Registro.Titulo, ur.Registro.NumeroCertificado, ur.Registro.EsInformatico,
                ur.Registro.CountryId, ur.Registro.Country.Name,
                ur.Registro.InstitutionId, ur.Registro.Institution.Nombre, ur.Registro.EvidenceFileId,
                ur.Registro.Creadores.Select(c => c.User.UserName + " " + c.User.UserLastName1).ToList()))
            .ToListAsync();
        return Results.Ok(list);
    }

    private async Task<IResult> CreateRegistro(IApplicationDbContext db, CreateRegistroBody body)
    {
        var registro = new Dashboard_v2.Domain.Entities.Registro
        {
            Titulo = body.Titulo,
            NumeroCertificado = body.NumeroCertificado,
            EsInformatico = body.EsInformatico,
            CountryId = body.CountryId,
            InstitutionId = body.InstitutionId,
            EvidenceFileId = body.EvidenceFileId,
        };

        db.Registros.Add(registro);
        await db.SaveChangesAsync(CancellationToken.None);

        return Results.Created($"/api/Registros/{registro.Id}", new { id = registro.Id });
    }

    private async Task<IResult> UpdateRegistro(IApplicationDbContext db, string id, UpdateRegistroBody body)
    {
        var registro = await db.Registros.FindAsync(new object[] { id }, CancellationToken.None);
        if (registro == null)
            return Results.NotFound(new { errors = new[] { "Registro no encontrado." } });

        registro.Titulo = body.Titulo;
        registro.NumeroCertificado = body.NumeroCertificado;
        registro.EsInformatico = body.EsInformatico;
        registro.CountryId = body.CountryId;
        registro.InstitutionId = body.InstitutionId;
        registro.EvidenceFileId = body.EvidenceFileId;

        await db.SaveChangesAsync(CancellationToken.None);
        return Results.Ok(new { message = "Registro actualizado." });
    }

    private async Task<IResult> DeleteRegistro(IApplicationDbContext db, string id)
    {
        var registro = await db.Registros.FindAsync(new object[] { id }, CancellationToken.None);
        if (registro == null)
            return Results.NotFound(new { errors = new[] { "Registro no encontrado." } });

        db.Registros.Remove(registro);
        await db.SaveChangesAsync(CancellationToken.None);
        return Results.Ok(new { message = "Registro eliminado." });
    }
}

public record RegistroDto(string Id, string Titulo, string NumeroCertificado, bool EsInformatico, int CountryId, string CountryName, string InstitutionId, string InstitutionNombre, int? EvidenceFileId, List<string> Creadores);
public record CreateRegistroBody(string Titulo, string NumeroCertificado, bool EsInformatico, int CountryId, string InstitutionId, int? EvidenceFileId = null);
public record UpdateRegistroBody(string Titulo, string NumeroCertificado, bool EsInformatico, int CountryId, string InstitutionId, int? EvidenceFileId = null);
