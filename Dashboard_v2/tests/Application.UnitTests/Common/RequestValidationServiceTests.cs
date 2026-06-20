using Dashboard_v2.Application.Common.Exceptions;
using Dashboard_v2.Application.Common.Validation;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.Common;

[TestFixture]
public class RequestValidationServiceTests
{
    private record SimpleRequest(string Name);

    private class SimpleRequestValidator : AbstractValidator<SimpleRequest>
    {
        public SimpleRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
        }
    }

    private RequestValidationService BuildService(bool registerValidator = true)
    {
        var services = new ServiceCollection();
        if (registerValidator)
            services.AddScoped<IValidator<SimpleRequest>, SimpleRequestValidator>();
        var sp = services.BuildServiceProvider();
        return new RequestValidationService(sp);
    }

    [Test]
    public async Task ValidRequest_DoesNotThrow()
    {
        var sut = BuildService();
        await sut.ValidateAndThrowAsync(new SimpleRequest("Alice"));
        // no exception = pass
    }

    [Test]
    public async Task InvalidRequest_ThrowsValidationException()
    {
        var sut = BuildService();
        var ex = await Should.ThrowAsync<Dashboard_v2.Application.Common.Exceptions.ValidationException>(
            () => sut.ValidateAndThrowAsync(new SimpleRequest("")));
        ex.Errors.ShouldNotBeEmpty();
    }

    [Test]
    public async Task NoValidatorRegistered_DoesNotThrow()
    {
        var sut = BuildService(registerValidator: false);
        await sut.ValidateAndThrowAsync(new SimpleRequest(""));
        // no exception = pass
    }
}
