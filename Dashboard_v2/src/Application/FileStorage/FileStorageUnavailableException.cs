namespace Dashboard_v2.Application.FileStorage;

/// <summary>
/// Thrown when the file storage backend is unavailable or returns an unexpected error.
/// Callers should surface this as a 503 Service Unavailable response.
/// </summary>
public sealed class FileStorageUnavailableException : Exception
{
    public FileStorageUnavailableException(string message) : base(message) { }
    public FileStorageUnavailableException(string message, Exception inner) : base(message, inner) { }
}
