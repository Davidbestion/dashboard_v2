using System;
using System.Net.Http.Headers;
using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Infrastructure.Configuration;
using Dashboard_v2.Infrastructure.Services;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class CrossRefServiceCollectionExtensions
{
    public static IServiceCollection AddCrossRefIntegration(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("CrossRef");
        var options = section.Get<CrossRefOptions>() ?? new CrossRefOptions();

        services.Configure<CrossRefOptions>(section);

        services.AddHttpClient<ICrossRefClient, CrossRefClient>(client =>
        {
            client.BaseAddress = new Uri(options.BaseAddress);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var userAgent = !string.IsNullOrWhiteSpace(options.ContactEmail)
                ? $"Dashboard_v2/1.0 (mailto:{options.ContactEmail})"
                : "Dashboard_v2/1.0";

            client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds > 0 ? options.TimeoutSeconds : 10);
        })
        .SetHandlerLifetime(TimeSpan.FromSeconds(options.HandlerLifetimeSeconds > 0 ? options.HandlerLifetimeSeconds : 300));

        return services;
    }
}
