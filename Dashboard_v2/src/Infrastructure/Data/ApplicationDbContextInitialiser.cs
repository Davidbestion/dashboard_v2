using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Domain.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dashboard_v2.Infrastructure.Data;

/// <summary>
/// Métodos de extensión para registrar la inicialización de la BD en el pipeline de arranque de la app.
/// </summary>
public static class InitialiserExtensions
{
    /// <summary>
    /// Punto de entrada para inicializar la BD al arrancar la app.<br/>
    /// Aplica migraciones pendientes de EF Core y ejecuta el seeding inicial.<br/>
    /// Se llama desde <c>Program.cs</c> justo antes de <c>app.Run()</c>.
    /// </summary>
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();

        await initialiser.InitialiseAsync();
        await initialiser.SeedAsync();
    }
}

/// <summary>
/// Responsable de la inicialización de la base de datos:<br/>
/// aplica migraciones de EF Core y realiza el seeding de datos obligatorios
/// (el superusuario por defecto).
/// </summary>
public class ApplicationDbContextInitialiser
{
    private readonly ILogger<ApplicationDbContextInitialiser> _logger;
    private readonly ApplicationDbContext _context;

    public ApplicationDbContextInitialiser(
        ILogger<ApplicationDbContextInitialiser> logger,
        ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    /// <summary>Aplica todas las migraciones de EF Core pendientes a la BD.</summary>
    public async Task InitialiseAsync()
    {
        try
        {
            await _context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initialising the database.");
            throw;
        }
    }

    /// <summary>Wrapper de TrySeedAsync que captura errores y los registra en el log.</summary>
    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    /// <summary>
    /// Crea el usuario superusuario por defecto si no existe, y le asigna el rol Superuser.<br/>
    /// Es <b>idempotente</b>: se puede llamar múltiples veces sin crear duplicados.<br/>
    /// Credenciales iniciales: <c>superuser@localhost</c> / <c>Superuser1!</c>
    /// </summary>
    public async Task TrySeedAsync()
    {
        const string superuserName = "superuser";
        const string superuserEmail = "superuser@localhost";
        const string superuserPassword = "Superuser1!";

        var superuser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == superuserName);
        if (superuser == null)
        {
            superuser = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = superuserName,
                UserLastName1 = "Superuser",
                Email = superuserEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(superuserPassword),
                BirthDate = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc),
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _context.Users.Add(superuser);
            await _context.SaveChangesAsync();
        }

        var hasRole = await _context.UserRoles
            .AnyAsync(ur => ur.UserId == superuser.Id && ur.Role == Roles.Superuser);
        if (!hasRole)
        {
            _context.UserRoles.Add(new UserRole { UserId = superuser.Id, Role = Roles.Superuser });
            await _context.SaveChangesAsync();
        }

        // ── Seed AwardTypes ───────────────────────────────────────────────────────────────────
        if (!await _context.AwardTypes.AnyAsync())
        {
            _context.AwardTypes.AddRange(
                new AwardType { Id = 0, Name = "Premio de la Academia de Ciencias"                  },
                new AwardType { Id = 1, Name = "Premio MES"                                         },
                new AwardType { Id = 2, Name = "Premio CITMA Innovación Tecnológica"                },
                new AwardType { Id = 3, Name = "Premio CITMA Estudiantes y Jóvenes Investigadores"  },
                new AwardType { Id = 4, Name = "Premio Forum Ciencia y Técnica"                     },
                new AwardType { Id = 5, Name = "Premio Investigación UH"                            },
                new AwardType { Id = 6, Name = "Otros premios (prensa, salud, sociedad, etc.)"      },
                new AwardType { Id = 7, Name = "Premio Internacional"                               }
            );
            await _context.SaveChangesAsync();
        }

        // ── Seed EventTypes ───────────────────────────────────────────────────────────────────
        if (!await _context.EventTypes.AnyAsync())
        {
            _context.EventTypes.AddRange(
                new EventType { Id = 0, Name = "Internacional" },
                new EventType { Id = 1, Name = "Nacional"      },
                new EventType { Id = 2, Name = "Regional"      },
                new EventType { Id = 3, Name = "Local"         }
            );
            await _context.SaveChangesAsync();
        }

        // ── Seed Clasificaciones de proyecto ─────────────────────────────────────────────────
        if (!await _context.Clasificaciones.AnyAsync())
        {
            _context.Clasificaciones.AddRange(
                new Clasificacion { Id = "clasificacion-basica",        Nombre = "Básica"         },
                new Clasificacion { Id = "clasificacion-aplicada",      Nombre = "Aplicada"       },
                new Clasificacion { Id = "clasificacion-experimental",  Nombre = "Experimental"   },
                new Clasificacion { Id = "clasificacion-innovacion",    Nombre = "Innovación"     }
            );
            await _context.SaveChangesAsync();
        }
    }
}
