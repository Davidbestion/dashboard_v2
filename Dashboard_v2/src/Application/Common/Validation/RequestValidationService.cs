using Dashboard_v2.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Dashboard_v2.Application.Common.Validation;

/// <summary>
/// Adaptador simple para reutilizar FluentValidation desde servicios CRUD sin depender
/// de una tubería de requests.
/// </summary>
public sealed class RequestValidationService : IRequestValidationService
{
    private readonly IServiceProvider _serviceProvider;

    public RequestValidationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task ValidateAndThrowAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default)
    {
        var validators = _serviceProvider.GetServices<IValidator<TRequest>>().ToArray();
        if (validators.Length == 0)
        {
            return;
        }

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count > 0)
        {
            throw new Dashboard_v2.Application.Common.Exceptions.ValidationException(failures);
        }
    }
}
