using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Domain.Entities;
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
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Jefe_de_Proyecto), nameof(RolesEnum.Superuser)))
            .WithName("CreateRegistro")
            .Produces(201)
            .ProducesProblem(400);

        groupBuilder.MapPut("{id}", UpdateRegistro)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Jefe_de_Proyecto), nameof(RolesEnum.Superuser)))
            .WithName("UpdateRegistro")
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(403)
            .ProducesProblem(404);

        groupBuilder.MapDelete("{id}", DeleteRegistro)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Jefe_de_Proyecto), nameof(RolesEnum.Superuser)))
            .WithName("DeleteRegistro")
            .Produces(200)
            .ProducesProblem(403)
            .ProducesProblem(404);
    }

    private static async Task<IResult> GetRegistros(IApplicationDbContext db)
    {
        var list = await db.Registros
            .Include(r => r.Country)
            .Include(r => r.Institution)
            .Include(r => r.Creadores).ThenInclude(c => c.Author)
            .Select(r => new RegistroDto(
                r.Id, r.Titulo, r.NumeroCertificado, r.EsInformatico,
                r.CountryId, r.Country.Name, r.InstitutionId, r.Institution.Nombre, r.EvidenceFileId,
                r.Creadores.Select(c => c.Author.Name).ToList(),
                r.Creadores.Select(c => new CreatorDto(c.Author.Id, c.Author.Name, c.Author.UserId)).ToList()))
            .ToListAsync();
        return Results.Ok(list);
    }

    private static async Task<IResult> GetMisRegistros(IApplicationDbContext db, IUser currentUser, IAuthorResolutionService authorResolution)
    {
        var currentAuthor = await authorResolution.GetOrCreateForUserAsync(currentUser.Id!, CancellationToken.None);
        if (currentAuthor == null)
            return Results.Ok(new List<RegistroDto>());

        var list = await db.AuthorRegistros
            .Where(ar => ar.AuthorId == currentAuthor.Id)
            .Include(ar => ar.Registro).ThenInclude(r => r.Country)
            .Include(ar => ar.Registro).ThenInclude(r => r.Institution)
            .Include(ar => ar.Registro).ThenInclude(r => r.Creadores).ThenInclude(c => c.Author)
            .Select(ar => new RegistroDto(
                ar.Registro.Id, ar.Registro.Titulo, ar.Registro.NumeroCertificado, ar.Registro.EsInformatico,
                ar.Registro.CountryId, ar.Registro.Country.Name,
                ar.Registro.InstitutionId, ar.Registro.Institution.Nombre, ar.Registro.EvidenceFileId,
                ar.Registro.Creadores.Select(c => c.Author.Name).ToList(),
                ar.Registro.Creadores.Select(c => new CreatorDto(c.Author.Id, c.Author.Name, c.Author.UserId)).ToList()))
            .ToListAsync();
        return Results.Ok(list);
    }

    private static async Task<IResult> CreateRegistro(IApplicationDbContext db, IUser currentUser, IAuthorResolutionService authorResolution, IProductionCreatorService creatorService, CreateRegistroBody body)
    {
        var currentAuthor = await authorResolution.GetOrCreateForUserAsync(currentUser.Id!, CancellationToken.None);
        if (currentAuthor == null)
            return Results.BadRequest(new { errors = new[] { "Usuario actual no valido." } });

        var registro = new Registro
        {
            Titulo = body.Titulo,
            NumeroCertificado = body.NumeroCertificado,
            EsInformatico = body.EsInformatico,
            CountryId = body.CountryId,
            InstitutionId = body.InstitutionId,
            EvidenceFileId = body.EvidenceFileId,
        };
        db.Registros.Add(registro);

        registro.Creadores.Add(new AuthorRegistro { AuthorId = currentAuthor.Id, RegistroId = registro.Id });
        await creatorService.AddAdditionalCreatorsAsync(
            registro.Creadores, currentAuthor.Id,
            authorId => new AuthorRegistro { AuthorId = authorId, RegistroId = registro.Id },
            c => c.AuthorId,
            body.AdditionalAuthorIds, body.AdditionalAuthorNames, body.AdditionalUserIds);

        await db.SaveChangesAsync(CancellationToken.None);
        return Results.Created($"/api/Registros/{registro.Id}", new { id = registro.Id });
    }

    private static async Task<IResult> UpdateRegistro(IApplicationDbContext db, IUser currentUser, IAuthorResolutionService authorResolution, IProductionCreatorService creatorService, string id, UpdateRegistroBody body)
    {
        var registro = await db.Registros
            .Include(r => r.Creadores)
            .FirstOrDefaultAsync(r => r.Id == id, CancellationToken.None);
        if (registro == null)
            return Results.NotFound(new { errors = new[] { "Registro no encontrado." } });

        var currentAuthor = await authorResolution.GetOrCreateForUserAsync(currentUser.Id!, CancellationToken.None);
        if (currentAuthor == null)
            return Results.BadRequest(new { errors = new[] { "Usuario actual no valido." } });

        var roles = currentUser.Roles ?? [];
        if (!roles.Contains(nameof(RolesEnum.Superuser)) && !roles.Contains(nameof(RolesEnum.Jefe_de_Proyecto)))
        {
            var esCreador = await db.AuthorRegistros.AnyAsync(ar => ar.RegistroId == id && ar.AuthorId == currentAuthor.Id);
            if (!esCreador)
                return Results.Forbid();
        }

        registro.Titulo = body.Titulo;
        registro.NumeroCertificado = body.NumeroCertificado;
        registro.EsInformatico = body.EsInformatico;
        registro.CountryId = body.CountryId;
        registro.InstitutionId = body.InstitutionId;
        registro.EvidenceFileId = body.EvidenceFileId;

        var toRemove = registro.Creadores.Where(c => c.AuthorId != currentAuthor.Id).ToList();
        foreach (var creator in toRemove)
            registro.Creadores.Remove(creator);

        await creatorService.AddAdditionalCreatorsAsync(
            registro.Creadores, currentAuthor.Id,
            authorId => new AuthorRegistro { AuthorId = authorId, RegistroId = registro.Id },
            c => c.AuthorId,
            body.AdditionalAuthorIds, body.AdditionalAuthorNames, body.AdditionalUserIds);

        await db.SaveChangesAsync(CancellationToken.None);
        return Results.Ok(new { message = "Registro actualizado." });
    }

    private static async Task<IResult> DeleteRegistro(IApplicationDbContext db, IUser currentUser, IAuthorResolutionService authorResolution, string id)
    {
        var registro = await db.Registros.FindAsync(new object[] { id }, CancellationToken.None);
        if (registro == null)
            return Results.NotFound(new { errors = new[] { "Registro no encontrado." } });

        var currentAuthor = await authorResolution.GetOrCreateForUserAsync(currentUser.Id!, CancellationToken.None);
        if (currentAuthor == null)
            return Results.BadRequest(new { errors = new[] { "Usuario actual no valido." } });

        var roles = currentUser.Roles ?? [];
        if (!roles.Contains(nameof(RolesEnum.Superuser)) && !roles.Contains(nameof(RolesEnum.Jefe_de_Proyecto)))
        {
            var esCreador = await db.AuthorRegistros.AnyAsync(ar => ar.RegistroId == id && ar.AuthorId == currentAuthor.Id);
            if (!esCreador)
                return Results.Forbid();
        }

        db.Registros.Remove(registro);
        await db.SaveChangesAsync(CancellationToken.None);
        return Results.Ok(new { message = "Registro eliminado." });
    }
}

public record RegistroDto(
    string Id,
    string Titulo,
    string NumeroCertificado,
    bool EsInformatico,
    int CountryId,
    string CountryName,
    string InstitutionId,
    string InstitutionNombre,
    int? EvidenceFileId,
    List<string> Creadores,
    List<CreatorDto> CreadoresDetalle);
public record CreateRegistroBody(
    string Titulo,
    string NumeroCertificado,
    bool EsInformatico,
    int CountryId,
    string InstitutionId,
    int? EvidenceFileId = null,
    List<string>? AdditionalAuthorIds = null,
    List<string>? AdditionalAuthorNames = null,
    List<string>? AdditionalUserIds = null);
public record UpdateRegistroBody(
    string Titulo,
    string NumeroCertificado,
    bool EsInformatico,
    int CountryId,
    string InstitutionId,
    int? EvidenceFileId = null,
    List<string>? AdditionalAuthorIds = null,
    List<string>? AdditionalAuthorNames = null,
    List<string>? AdditionalUserIds = null);
