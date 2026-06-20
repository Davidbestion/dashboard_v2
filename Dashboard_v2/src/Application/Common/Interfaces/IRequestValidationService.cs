namespace Dashboard_v2.Application.Common.Interfaces;

/// <summary>
/// Ejecuta de forma explícita los validadores de FluentValidation sobre un request
/// antes de que la capa de aplicación procese la operación.
/// </summary>
public interface IRequestValidationService
{
    /// <summary>
    /// Valida la instancia recibida y lanza una <see cref="Dashboard_v2.Application.Common.Exceptions.ValidationException"/>
    /// si encuentra errores.
    /// </summary>
    Task ValidateAndThrowAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default);
}
