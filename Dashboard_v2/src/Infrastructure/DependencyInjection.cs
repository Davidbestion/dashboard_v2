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
using System;
using Dashboard_v2.Infrastructure.Configuration;
using Minio;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers all Infrastructure layer services with the DI container: EF Core, identity,
/// file storage, document generation, publication database providers, and dashboard.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Configures and registers Infrastructure services including database, authentication,
    /// storage, and external API clients.
    /// </summary>
    public static void AddInfrastructureServices(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("Dashboard_v2Db");
        Guard.Against.Null(connectionString, message: "Connection string 'Dashboard_v2Db' not found.");

        builder.Services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();

        builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.UseNpgsql(connectionString, o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
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
        builder.Services.AddScoped<IAuthorCleanupService, AuthorCleanupService>();
        builder.Services.AddScoped<Dashboard_v2.Application.Common.Interfaces.IAuthorResolutionService, Dashboard_v2.Application.Common.AuthorResolutionService>();
        builder.Services.AddScoped<Dashboard_v2.Application.Common.Interfaces.IProductionCreatorService, Dashboard_v2.Application.Common.ProductionCreatorService>();
        builder.Services.AddSingleton<ICustomDocumentRenderer, AnexoPremiosCustomRenderer>();
        builder.Services.AddSingleton<ICustomDocumentRenderer, AnexoRegistrosCustomRenderer>();
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
        builder.Services.AddScoped<Dashboard_v2.Application.Events.EventService>();
        builder.Services.AddScoped<Dashboard_v2.Application.Events.IEventService>(sp => sp.GetRequiredService<Dashboard_v2.Application.Events.EventService>());
        builder.Services.AddScoped<Dashboard_v2.Application.Events.IPresentationService>(sp => sp.GetRequiredService<Dashboard_v2.Application.Events.EventService>());
        builder.Services.AddScoped<Dashboard_v2.Application.Publications.IPublicationService, Dashboard_v2.Application.Publications.PublicationService>();
        builder.Services.AddCrossRefIntegration(builder.Configuration);
        builder.Services.AddOpenAireIntegration();
        // Publication database resolver (ISSN -> database/group).
        // Providers are registered in resolution priority order:
        //   1. LocalCsv  — Scopus/Scimago CSV files (Group 1, includes quartile). Singleton, loaded at startup.
        //   2. WosExcel  — Clarivate WoS change Excel files (Group 1 or 2, no quartile). Singleton, loaded at startup.
        //   3. MEDLINE   — NLM E-utilities API (Group 2). Free, no key. ~3 req/s limit.
        //   4. SciELO    — SciELO Article Meta API (Group 2). Free, no key.
        //   5. DOAJ      — DOAJ REST API (Group 3). Free, no key.
        // The resolver tries each provider in order and returns on the first match.
        builder.Services.Configure<Dashboard_v2.Infrastructure.Configuration.PublicationDatabaseOptions>(builder.Configuration.GetSection("PublicationDatabase"));

        // 1. LocalCsv/Scimago (Singleton — loads CSV files once at startup)
        builder.Services.AddSingleton<Dashboard_v2.Application.Common.Interfaces.IPublicationDatabaseProvider>(sp =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Dashboard_v2.Infrastructure.Configuration.PublicationDatabaseOptions>>().Value;
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Dashboard_v2.Infrastructure.Services.Providers.LocalCsvPublicationDatabaseProvider>>();
            // ScimagoFiles takes priority; fall back to the old LocalMappingFiles key for compatibility
            var files = (opts.ScimagoFiles?.Count > 0 ? opts.ScimagoFiles : opts.LocalMappingFiles) ?? [];
            return new Dashboard_v2.Infrastructure.Services.Providers.LocalCsvPublicationDatabaseProvider(files, logger);
        });

        // 2. WosExcel (Singleton — loads .xlsx change files once at startup)
        var contentRoot = builder.Environment.ContentRootPath;
        builder.Services.AddSingleton<Dashboard_v2.Application.Common.Interfaces.IPublicationDatabaseProvider>(sp =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Dashboard_v2.Infrastructure.Configuration.PublicationDatabaseOptions>>().Value;
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Dashboard_v2.Infrastructure.Services.Providers.WosExcelPublicationDatabaseProvider>>();
            var dir = opts.WosDirectory;
            if (!string.IsNullOrWhiteSpace(dir) && !System.IO.Path.IsPathRooted(dir))
                dir = System.IO.Path.Combine(contentRoot, dir);
            return new Dashboard_v2.Infrastructure.Services.Providers.WosExcelPublicationDatabaseProvider(dir, logger);
        });

        // 3. MEDLINE via NLM E-utilities
        builder.Services.AddHttpClient<Dashboard_v2.Infrastructure.Services.Providers.MedlinePublicationDatabaseProvider>(client =>
        {
            client.BaseAddress = new Uri("https://eutils.ncbi.nlm.nih.gov/entrez/eutils/");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            client.Timeout = TimeSpan.FromSeconds(8);
        });
        builder.Services.AddScoped<Dashboard_v2.Application.Common.Interfaces.IPublicationDatabaseProvider>(
            sp => sp.GetRequiredService<Dashboard_v2.Infrastructure.Services.Providers.MedlinePublicationDatabaseProvider>());

        // 4. SciELO via Article Meta API
        builder.Services.AddHttpClient<Dashboard_v2.Infrastructure.Services.Providers.SciELOPublicationDatabaseProvider>(client =>
        {
            client.BaseAddress = new Uri("https://articlemeta.scielo.org/");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            client.Timeout = TimeSpan.FromSeconds(8);
        });
        builder.Services.AddScoped<Dashboard_v2.Application.Common.Interfaces.IPublicationDatabaseProvider>(
            sp => sp.GetRequiredService<Dashboard_v2.Infrastructure.Services.Providers.SciELOPublicationDatabaseProvider>());

        // 5. DOAJ
        builder.Services.AddHttpClient<Dashboard_v2.Infrastructure.Services.Providers.DoajPublicationDatabaseProvider>(client =>
        {
            client.BaseAddress = new Uri("https://doaj.org/api/v3/");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            client.Timeout = TimeSpan.FromSeconds(8);
        });
        builder.Services.AddScoped<Dashboard_v2.Application.Common.Interfaces.IPublicationDatabaseProvider>(
            sp => sp.GetRequiredService<Dashboard_v2.Infrastructure.Services.Providers.DoajPublicationDatabaseProvider>());

        builder.Services.AddScoped<Dashboard_v2.Application.Common.Interfaces.IPublicationDatabaseResolver, Dashboard_v2.Infrastructure.Services.PublicationDatabaseResolver>();
        builder.Services.AddScoped<Dashboard_v2.Application.Users.IUserService, Dashboard_v2.Application.Users.UserService>();
        builder.Services.AddScoped<Dashboard_v2.Application.Authors.IAuthorService, Dashboard_v2.Application.Authors.AuthorService>();
        builder.Services.AddScoped<Dashboard_v2.Application.Roles.IRoleService, Dashboard_v2.Application.Roles.RoleService>();
        builder.Services.AddScoped<ProyectoService>();
        builder.Services.AddScoped<Dashboard_v2.Application.Proyectos.IProyectoQueryService>(sp => sp.GetRequiredService<ProyectoService>());
        builder.Services.AddScoped<Dashboard_v2.Application.Proyectos.IProyectoCommandService>(sp => sp.GetRequiredService<ProyectoService>());
        builder.Services.AddScoped<Dashboard_v2.Application.Redes.IRedService, Dashboard_v2.Application.Redes.RedService>();
        builder.Services.AddScoped<Dashboard_v2.Application.Patentes.IPatenteService, Dashboard_v2.Application.Patentes.PatenteService>();
        builder.Services.AddScoped<Dashboard_v2.Application.Normas.INormaService, Dashboard_v2.Application.Normas.NormaService>();
        builder.Services.AddScoped<Dashboard_v2.Application.Registros.IRegistroService, Dashboard_v2.Application.Registros.RegistroService>();
        builder.Services.AddScoped<Dashboard_v2.Application.ProductosComercializados.IProductoComercializadoService, Dashboard_v2.Application.ProductosComercializados.ProductoComercializadoService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<Dashboard_v2.Application.Documents.IDocumentService, Dashboard_v2.Application.Documents.DocumentService>();
        builder.Services.AddScoped<Dashboard_v2.Application.Dashboard.IDashboardService, Dashboard_v2.Infrastructure.Services.DashboardService>();
        // Para agregar un nuevo reporte, agrega otra línea aquí:
        builder.Services.AddScoped<IDocumentReport, AnexoGruposReport>();
        builder.Services.AddScoped<IDocumentReport, AnexoGruposEstudiantilesReport>();
        builder.Services.AddScoped<IDocumentReport, AnexoPublicacionesReport>();
        builder.Services.AddScoped<IDocumentReport, AnexoRegistrosReport>();
        builder.Services.AddScoped<IDocumentReport, AnexoPremiosReport>();
        builder.Services.AddScoped<IDocumentReport, AnexoEventosReport>();
        builder.Services.AddScoped<IDocumentReport, ProyectosReport>();
        builder.Services.AddScoped<IDocumentReport, AnexoRedesNacInterReport>();
        builder.Services.AddScoped<IZipDocumentReport, AnexoRedesUniversitariasReport>();

        // ── MinIO / Almacenamiento de archivos ────────────────────────────────
        var minioSection = builder.Configuration
            .GetSection(Dashboard_v2.Infrastructure.Configuration.MinioOptions.SectionName);
        builder.Services.Configure<Dashboard_v2.Infrastructure.Configuration.MinioOptions>(minioSection);

        var minioOpts = minioSection
            .Get<Dashboard_v2.Infrastructure.Configuration.MinioOptions>();

        if (minioOpts is not null)
        {
            builder.Services.AddMinio(configureClient => configureClient
                .WithEndpoint(minioOpts.Endpoint)
                .WithCredentials(minioOpts.AccessKey, minioOpts.SecretKey)
                .WithSSL(minioOpts.UseSSL));

            // Registrar la implementación una sola vez y exponerla bajo ambas interfaces.
            builder.Services.AddSingleton<Dashboard_v2.Infrastructure.Services.MinioFileStorageService>();
            builder.Services.AddSingleton<Dashboard_v2.Application.Common.Interfaces.IFileStorageService>(
                sp => sp.GetRequiredService<Dashboard_v2.Infrastructure.Services.MinioFileStorageService>());
            builder.Services.AddSingleton<Dashboard_v2.Application.Common.Interfaces.IStorageBucketInitialiser>(
                sp => sp.GetRequiredService<Dashboard_v2.Infrastructure.Services.MinioFileStorageService>());
            builder.Services.AddScoped<Dashboard_v2.Application.FileStorage.IStoredFileService,
                Dashboard_v2.Application.FileStorage.StoredFileService>();
        }

        builder.Services.AddAuthorization(options =>
            options.AddPolicy(Policies.CanPurge, policy => policy.RequireRole(nameof(RolesEnum.Superuser))));
    }
}
