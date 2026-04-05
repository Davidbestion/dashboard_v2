using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Domain.Constants;
using Dashboard_v2.Infrastructure.Data;
using Dashboard_v2.Infrastructure.Data.Interceptors;
using Dashboard_v2.Infrastructure.Identity;
using Dashboard_v2.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("Dashboard_v2Db");
        Guard.Against.Null(connectionString, message: "Connection string 'Dashboard_v2Db' not found.");

        builder.Services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        builder.Services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

        builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.UseNpgsql(connectionString);
            options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        builder.Services.AddScoped<ApplicationDbContextInitialiser>();

        builder.Services.AddSingleton(TimeProvider.System);

        var authProvider = builder.Configuration["Auth:Provider"] ?? "Ldap";
        if (authProvider.Equals("Ldap", StringComparison.OrdinalIgnoreCase))
            builder.Services.AddTransient<IIdentityService, LdapAuthService>();
        else
            builder.Services.AddTransient<IIdentityService, LocalAuthService>();

        builder.Services.AddSingleton<IJwtService, JwtService>();
        builder.Services.AddScoped<IPermissionService, PermissionService>();
        builder.Services.AddScoped<IAuthorCleanupService, AuthorCleanupService>();
        builder.Services.AddScoped<IAuthorResolutionService, AuthorResolutionService>();

        builder.Services.AddAuthorization(options =>
            options.AddPolicy(Policies.CanPurge, policy => policy.RequireRole(Roles.Superuser)));
    }
}
