using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Universidades;
using Dashboard_v2.Application.Areas;
using Dashboard_v2.Application.Clasificaciones;
using Dashboard_v2.Application.AreasDelConocimiento;
using Dashboard_v2.Application.Events;
using Dashboard_v2.Application.LineasDeInvestigacion;
using Dashboard_v2.Application.GruposDeInvestigacion;
using Dashboard_v2.Application.Proyectos;
using Dashboard_v2.Application.Auth;
using Dashboard_v2.Application.Documents;
using Dashboard_v2.Application.Documents.Reports;
using Dashboard_v2.Domain.Constants;
using Dashboard_v2.Infrastructure.Data;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;
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
        builder.Services.AddScoped<UserAreaResolutionService>();

        var authProvider = builder.Configuration["Auth:Provider"] ?? "Ldap";
        if (authProvider.Equals("Ldap", StringComparison.OrdinalIgnoreCase))
            builder.Services.AddTransient<IIdentityService, LdapAuthService>();
        else
            builder.Services.AddTransient<IIdentityService, LocalAuthService>();

        builder.Services.AddSingleton<IJwtService, JwtService>();
        builder.Services.AddScoped<IPermissionService, PermissionService>();
        builder.Services.AddScoped<IAuthorCleanupService, AuthorCleanupService>();
        builder.Services.AddScoped<Dashboard_v2.Application.Common.Interfaces.IAuthorResolutionService, Dashboard_v2.Application.Common.AuthorResolutionService>();
        builder.Services.AddSingleton<IDocumentRenderer, DocumentRenderer>();
        // Servicios de aplicación (Service Layer)
        builder.Services.AddScoped<Dashboard_v2.Application.Universidades.IUniversidadService, Dashboard_v2.Application.Universidades.UniversidadService>();
        builder.Services.AddScoped<Dashboard_v2.Application.Areas.IAreaService, Dashboard_v2.Application.Areas.AreaService>();
        builder.Services.AddScoped<Dashboard_v2.Application.Clasificaciones.IClasificacionService, Dashboard_v2.Application.Clasificaciones.ClasificacionService>();
        builder.Services.AddScoped<Dashboard_v2.Application.AreasDelConocimiento.IAreaDelConocimientoService, Dashboard_v2.Application.AreasDelConocimiento.AreaDelConocimientoService>();
        builder.Services.AddScoped<Dashboard_v2.Application.LineasDeInvestigacion.ILineaDeInvestigacionService, Dashboard_v2.Application.LineasDeInvestigacion.LineaDeInvestigacionService>();
        builder.Services.AddScoped<Dashboard_v2.Application.GruposDeInvestigacion.IGrupoDeInvestigacionService, Dashboard_v2.Application.GruposDeInvestigacion.GrupoDeInvestigacionService>();
        builder.Services.AddScoped<Dashboard_v2.Application.GruposEstudiantiles.IGrupoEstudiantilService, Dashboard_v2.Application.GruposEstudiantiles.GrupoEstudiantilService>();
        builder.Services.AddScoped<Dashboard_v2.Application.Awards.IAwardService, Dashboard_v2.Application.Awards.AwardService>();
        builder.Services.AddScoped<Dashboard_v2.Application.Events.IEventService, Dashboard_v2.Application.Events.EventService>();
        builder.Services.AddScoped<Dashboard_v2.Application.Publications.IPublicationService, Dashboard_v2.Application.Publications.PublicationService>();
        builder.Services.AddScoped<Dashboard_v2.Application.Users.IUserService, Dashboard_v2.Application.Users.UserService>();
        builder.Services.AddScoped<Dashboard_v2.Application.Authors.IAuthorService, Dashboard_v2.Application.Authors.AuthorService>();
        builder.Services.AddScoped<Dashboard_v2.Application.Roles.IRoleService, Dashboard_v2.Application.Roles.RoleService>();
        builder.Services.AddScoped<IProyectoService, ProyectoService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<Dashboard_v2.Application.Documents.IDocumentService, Dashboard_v2.Application.Documents.DocumentService>();
        // Para agregar un nuevo reporte, agrega otra línea aquí:
        builder.Services.AddScoped<IDocumentReport, AnexoGruposReport>();
        builder.Services.AddScoped<IDocumentReport, AnexoGruposEstudiantilesReport>();
        builder.Services.AddScoped<IDocumentReport, AnexoPublicacionesReport>();
        builder.Services.AddScoped<IDocumentReport, AnexoPremiosReport>();
        builder.Services.AddScoped<IDocumentReport, AnexoEventosReport>();
        builder.Services.AddScoped<IDocumentReport, ProyectosReport>();

        builder.Services.AddAuthorization(options =>
            options.AddPolicy(Policies.CanPurge, policy => policy.RequireRole(nameof(RolesEnum.Superuser))));
    }
}
