using System.Reflection;
using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Common.Validation;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        builder.Services.AddScoped<IRequestValidationService, RequestValidationService>();
    }
}
