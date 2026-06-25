namespace Dashboard_v2.Application.Publications;

/// <summary>
/// Thrown when the CrossRef API does not respond within the configured timeout.
/// Callers should handle this separately from "not found" responses.
/// </summary>
public sealed class CrossRefTimeoutException : Exception
{
    public CrossRefTimeoutException() : base("CrossRef did not respond in time.") { }
    public CrossRefTimeoutException(string message) : base(message) { }
    public CrossRefTimeoutException(string message, Exception inner) : base(message, inner) { }
}
