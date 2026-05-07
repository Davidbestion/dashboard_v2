namespace Dashboard_v2.Application.FileStorage.DTOs;

/// <summary>
/// DTO de lectura que representa la metadata de un archivo almacenado.
/// No expone la clave interna de MinIO al cliente.
/// </summary>
public sealed class StoredFileDto
{
    public int Id { get; init; }
    public string FileName { get; init; } = default!;
    public string ContentType { get; init; } = default!;
    public long SizeBytes { get; init; }
    public string UploadedById { get; init; } = default!;
    public DateTimeOffset Created { get; init; }
}
