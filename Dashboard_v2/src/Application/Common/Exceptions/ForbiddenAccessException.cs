namespace Dashboard_v2.Application.Common.Exceptions;

/// <summary>
/// Thrown when the current user lacks permission to perform the requested operation.
/// </summary>
public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException() : base() { }
}
