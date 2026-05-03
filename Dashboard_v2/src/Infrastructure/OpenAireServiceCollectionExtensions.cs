using System;
using System.Net.Http.Headers;
using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Infrastructure.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class OpenAireServiceCollectionExtensions
{
    public static IServiceCollection AddOpenAireIntegration(this IServiceCollection services)
    {
        services.AddHttpClient<IOpenAireClient, OpenAireClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.openaire.eu/graph/v1/researchProducts");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Dashboard_v2/1.0");
            // OpenAIRE can be slower than CrossRef for some queries
            client.Timeout = TimeSpan.FromSeconds(20);
        })
        .SetHandlerLifetime(TimeSpan.FromMinutes(5));

        return services;
    }
}
