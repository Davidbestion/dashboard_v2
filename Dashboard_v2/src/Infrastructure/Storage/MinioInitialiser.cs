using Dashboard_v2.Application.Common.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
namespace Dashboard_v2.Infrastructure.Storage;

/// <summary>
/// Método de extensión para garantizar que el bucket de MinIO exista al arrancar la aplicación.
/// Se llama desde <c>Program.cs</c> tras la inicialización de la base de datos.
/// </summary>
public static class MinioInitialiser
{
    /// <summary>
    /// Verifica que el bucket configurado exista en MinIO y lo crea si no existe.
    /// No hace nada si <see cref="IStorageBucketInitialiser"/> no está registrado en el contenedor de DI.
    /// </summary>
    public static async Task InitialiseMinioAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var initialiser = scope.ServiceProvider.GetService<IStorageBucketInitialiser>();
        if (initialiser is null)
            return;

        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(MinioInitialiser));

        try
        {
            await initialiser.EnsureBucketExistsAsync();
            logger.LogInformation("MinIO inicializado correctamente.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al inicializar el bucket de MinIO. Compruebe que MinIO esté en funcionamiento.");
            // No relanzamos — la app puede arrancar aunque MinIO falle,
            // solo fallará cuando se intente subir o descargar un archivo.
        }
    }
}
