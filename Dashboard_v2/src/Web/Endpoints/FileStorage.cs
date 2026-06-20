using Dashboard_v2.Application.FileStorage;
using Dashboard_v2.Application.FileStorage.DTOs;
using Dashboard_v2.Web.Infrastructure;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Web.Endpoints;

/// <summary>
/// Endpoints para gestión de archivos almacenados en MinIO.
///
/// Cualquier usuario autenticado puede subir, listar y descargar sus propios archivos.
/// La eliminación y el reemplazo también están restringidos al propietario del archivo
/// (controlado en la capa de Application).
///
/// Flujo de subida recomendado desde el frontend:
///   1. POST /api/FileStorage          → obtiene el Id del archivo creado.
///   2. Guardar ese Id en la entidad correspondiente (ej. UserAwarded.CertificateFileId).
///
/// Flujo de descarga recomendado:
///   - GET /api/FileStorage/{id}/url   → URL presignada directa a MinIO (sin cargar el backend).
///   - GET /api/FileStorage/{id}/download → descarga pasando por la API (útil en entornos cerrados).
/// </summary>
public class FileStorage : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        // GET /api/FileStorage — lista los archivos del usuario autenticado
        groupBuilder.MapGet("", GetAll)
            .RequireAuthorization()
            .WithName("GetStoredFiles")
            .Produces<IReadOnlyList<StoredFileDto>>(200);

        // GET /api/FileStorage/{id} — metadata de un archivo
        groupBuilder.MapGet("{id:int}", GetById)
            .RequireAuthorization()
            .WithName("GetStoredFileById")
            .Produces<StoredFileDto>(200)
            .ProducesProblem(404);

        // GET /api/FileStorage/{id}/url — URL presignada para descarga directa desde MinIO
        groupBuilder.MapGet("{id:int}/url", GetDownloadUrl)
            .RequireAuthorization()
            .WithName("GetStoredFileDownloadUrl")
            .Produces<string>(200)
            .ProducesProblem(404);

        // GET /api/FileStorage/{id}/download — descarga el binario pasando por la API
        groupBuilder.MapGet("{id:int}/download", Download)
            .RequireAuthorization()
            .WithName("DownloadStoredFile")
            .Produces(200)
            .ProducesProblem(404);

        // POST /api/FileStorage — sube un nuevo archivo (multipart/form-data)
        groupBuilder.MapPost("", Upload)
            .RequireAuthorization()
            .WithName("UploadStoredFile")
            .Produces<StoredFileDto>(201)
            .ProducesProblem(400)
            .DisableAntiforgery(); // JWT en cookie, no se usa antiforgery

        // PUT /api/FileStorage/{id} — reemplaza el archivo existente
        groupBuilder.MapPut("{id:int}", Replace)
            .RequireAuthorization()
            .WithName("ReplaceStoredFile")
            .Produces<StoredFileDto>(200)
            .ProducesProblem(400)
            .ProducesProblem(404)
            .DisableAntiforgery();

        // DELETE /api/FileStorage/{id}
        groupBuilder.MapDelete("{id:int}", Delete)
            .RequireAuthorization()
            .WithName("DeleteStoredFile")
            .Produces(204)
            .ProducesProblem(404);
    }

    private static async Task<IResult> GetAll(IStoredFileService service, CancellationToken ct)
    {
        var files = await service.GetAllAsync(ct);
        return Results.Ok(files);
    }

    private static async Task<IResult> GetById(int id, IStoredFileService service, CancellationToken ct)
    {
        var file = await service.GetByIdAsync(id, ct);
        return Results.Ok(file);
    }

    private static async Task<IResult> GetDownloadUrl(
        int id,
        IStoredFileService service,
        [Microsoft.AspNetCore.Mvc.FromQuery] int expirySeconds = 3600,
        CancellationToken ct = default)
    {
        var url = await service.GetDownloadUrlAsync(id, expirySeconds, ct);
        return Results.Ok(url);
    }

    private static async Task<IResult> Download(int id, IStoredFileService service, CancellationToken ct)
    {
        var (content, contentType, fileName) = await service.DownloadAsync(id, ct);
        return Results.File(content, contentType, fileName);
    }

    private static async Task<IResult> Upload(
        IFormFile file,
        IStoredFileService service,
        [Microsoft.AspNetCore.Mvc.FromForm] string? category,
        CancellationToken ct)
    {
        var request = new UploadFileRequest
        {
            FileName    = file.FileName,
            ContentType = file.ContentType,
            SizeBytes   = file.Length,
            Category    = category ?? "general",
        };

        await using var stream = file.OpenReadStream();
        var dto = await service.UploadAsync(request, stream, ct);
        return Results.Created($"/api/FileStorage/{dto.Id}", dto);
    }

    private static async Task<IResult> Replace(
        int id,
        IFormFile file,
        IStoredFileService service,
        [Microsoft.AspNetCore.Mvc.FromForm] string? category,
        CancellationToken ct)
    {
        var request = new UploadFileRequest
        {
            FileName    = file.FileName,
            ContentType = file.ContentType,
            SizeBytes   = file.Length,
            Category    = category ?? "general",
        };

        await using var stream = file.OpenReadStream();
        var dto = await service.ReplaceAsync(id, request, stream, ct);
        return Results.Ok(dto);
    }

    private static async Task<IResult> Delete(int id, IStoredFileService service, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return Results.NoContent();
    }
}
