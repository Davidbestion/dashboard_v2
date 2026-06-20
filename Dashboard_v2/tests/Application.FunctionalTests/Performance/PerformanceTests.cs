using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dashboard_v2.Domain.Entities;

namespace Dashboard_v2.Application.FunctionalTests.Performance;

using static Testing;

/// <summary>
/// Tests de rendimiento contra PostgreSQL nativo (sin Testcontainers).
/// Miden tiempos reales de respuesta en escenarios de volumen representativos
/// del entorno de producción esperado.
/// </summary>
[TestFixture]
public class PerformanceTests : BaseTestFixture
{
    private const int ThresholdMs = 5000; // umbral para el test original de 20 registros

    [Test]
    public async Task GetPublications_20Records_RespondsWithinThreshold()
    {
        await RunAsUserAsync("perf.pub@local", "Testing1234!", ["Profesor"]);
        using var client = CreateClient();

        // Sembrar 20 publicaciones via API (antes de iniciar el cronómetro)
        for (int i = 1; i <= 20; i++)
        {
            var r = await client.PostAsJsonAsync("/api/Publications", new
            {
                title = $"Publicación de rendimiento #{i:D2}",
                publicationData = $"Revista de Ciencias, Vol. {i}",
                publicationType = 4, // Artículo_de_Divulgación — no requiere Index ni Group
                publishedDate = $"202{i % 5 + 1}",
                additionalAuthorIds = Array.Empty<string>(),
                additionalAuthorNames = Array.Empty<string>(),
                additionalUserIds = Array.Empty<string>()
            });
            r.IsSuccessStatusCode.ShouldBeTrue();
        }

        var sw = Stopwatch.StartNew();
        var response = await client.GetAsync("/api/Publications");
        sw.Stop();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetArrayLength().ShouldBe(20);

        sw.ElapsedMilliseconds.ShouldBeLessThan(ThresholdMs,
            $"GET /api/Publications con 20 registros tardó {sw.ElapsedMilliseconds}ms (umbral: {ThresholdMs}ms)");
    }

    [Test]
    public async Task GetProyectosCatalog_RespondsWithinThreshold()
    {
        await RunAsUserAsync("perf.proy@local", "Testing1234!", ["Jefe_de_Proyecto"]);
        using var client = CreateClient();

        var (areaId, clasifId, jefeId) = await SeedBaseForPerformanceAsync();

        // Sembrar 5 proyectos de distintos tipos
        for (int i = 1; i <= 5; i++)
        {
            await client.PostAsJsonAsync("/api/Proyectos/en-revision", new
            {
                titulo = $"Proyecto en revisión #{i}",
                jefeId,
                numeroMiembros = 3,
                cantidadMiembrosUH = 2,
                cantidadEstudiantes = 0,
                cantidadEstudiantesContratados = 0,
                tributaFormacionDoctoral = false,
                clasificacionId = clasifId,
                areaId,
                situacionesIds = Array.Empty<int>(),
                tipo = "Tesis",
            });
        }

        var sw = Stopwatch.StartNew();
        var response = await client.GetAsync("/api/Proyectos/catalogo");
        sw.Stop();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        sw.ElapsedMilliseconds.ShouldBeLessThan(ThresholdMs,
            $"GET /api/Proyectos/catalogo tardó {sw.ElapsedMilliseconds}ms (umbral: {ThresholdMs}ms)");
    }

    [Test]
    public async Task ConcurrentLoad_10ReadRequests_AllWithinThreshold()
    {
        await RunAsUserAsync("perf.load@local", "Testing1234!", ["Superuser"]);
        using var client = CreateClient();

        // Sembrar datos base
        for (int i = 1; i <= 10; i++)
        {
            await client.PostAsJsonAsync("/api/Publications", new
            {
                title = $"Carga de publicación #{i}",
                publicationData = "Datos de carga",
                publicationType = 4,
                publishedDate = "2024",
                additionalAuthorIds = Array.Empty<string>(),
                additionalAuthorNames = Array.Empty<string>(),
                additionalUserIds = Array.Empty<string>()
            });
        }

        const int concurrent = 10;
        var sw = Stopwatch.StartNew();
        var tasks = Enumerable.Range(0, concurrent).Select(_ =>
            client.GetAsync("/api/Publications/todas"));
        var responses = await Task.WhenAll(tasks);
        sw.Stop();

        foreach (var r in responses)
            r.StatusCode.ShouldBe(HttpStatusCode.OK);

        sw.ElapsedMilliseconds.ShouldBeLessThan(ThresholdMs * 2,
            $"10 lecturas concurrentes tardaron {sw.ElapsedMilliseconds}ms en total");
    }

    // ── Pruebas de volumen realista ──────────────────────────────────────────

    /// <summary>
    /// Siembra 10 000 publicaciones directamente en la BD y mide el tiempo
    /// de la consulta GET /api/Publications/todas (rol Superuser).
    /// Umbral: 3 segundos — razonable para una consulta sin paginación en
    /// PostgreSQL nativo con índice sobre UserId.
    /// </summary>
    [Test]
    public async Task GetAllPublications_10000Records_RespondsWithin3Seconds()
    {
        await RunAsUserAsync("perf.bulk@local", "Testing1234!", ["Superuser"]);

        await SeedPublicationsBulkAsync(count: 10_000);

        using var client = CreateClient();
        var sw = Stopwatch.StartNew();
        var response = await client.GetAsync("/api/Publications/todas");
        sw.Stop();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetArrayLength().ShouldBeGreaterThanOrEqualTo(10_000);

        TestContext.Out.WriteLine($"GET /todas con 10 000 registros: {sw.ElapsedMilliseconds} ms");
        sw.ElapsedMilliseconds.ShouldBeLessThan(3_000,
            $"GET /api/Publications/todas con 10 000 registros tardó {sw.ElapsedMilliseconds} ms (umbral: 3 000 ms)");
    }

    /// <summary>
    /// Simula 100 peticiones GET concurrentes al endpoint de publicaciones
    /// propias (rol Profesor) con 10 000 registros en la BD.
    /// Se mide el tiempo total del lote; el umbral (10 s) equivale a que
    /// ninguna petición individual supere ~100 ms de media.
    /// </summary>
    [Test]
    public async Task ConcurrentLoad_100ReadRequests_10000Records_WithinThreshold()
    {
        await RunAsUserAsync("perf.conc@local", "Testing1234!", ["Superuser"]);

        await SeedPublicationsBulkAsync(count: 10_000);

        const int concurrent = 100;
        using var client = CreateClient();

        // Pre-calentamiento: evita que la primera petición incluya el JIT
        await client.GetAsync("/api/Publications/todas");

        var sw = Stopwatch.StartNew();
        var tasks = Enumerable.Range(0, concurrent)
            .Select(_ => client.GetAsync("/api/Publications/todas"));
        var responses = await Task.WhenAll(tasks);
        sw.Stop();

        foreach (var r in responses)
            r.StatusCode.ShouldBe(HttpStatusCode.OK);

        var avgMs = sw.ElapsedMilliseconds / (double)concurrent;
        TestContext.Out.WriteLine(
            $"100 peticiones concurrentes con 10 000 registros: total {sw.ElapsedMilliseconds} ms, media {avgMs:F1} ms/req");

        sw.ElapsedMilliseconds.ShouldBeLessThan(10_000,
            $"100 peticiones concurrentes tardaron {sw.ElapsedMilliseconds} ms en total (umbral: 10 000 ms)");
    }

    /// <summary>
    /// Mide el tiempo de escritura: creación de 100 publicaciones secuenciales
    /// via API para estimar la latencia de escritura en producción.
    /// </summary>
    [Test]
    public async Task CreatePublications_100Sequential_MeasuresWriteLatency()
    {
        await RunAsUserAsync("perf.write@local", "Testing1234!", ["Profesor"]);
        using var client = CreateClient();

        var times = new List<long>(100);
        for (int i = 1; i <= 100; i++)
        {
            var sw = Stopwatch.StartNew();
            var r = await client.PostAsJsonAsync("/api/Publications", new
            {
                title = $"Publicación escritura #{i:D3}",
                publicationData = $"Revista de Prueba Vol. {i}, pp. {i * 10}-{i * 10 + 8}",
                publicationType = 4, // Artículo_de_Divulgación — no requiere Index ni Group
                publishedDate = $"202{i % 4 + 1}",
                additionalAuthorIds = Array.Empty<string>(),
                additionalAuthorNames = Array.Empty<string>(),
                additionalUserIds = Array.Empty<string>()
            });
            sw.Stop();
            r.IsSuccessStatusCode.ShouldBeTrue();
            times.Add(sw.ElapsedMilliseconds);
        }

        var avg = times.Average();
        var p95 = times.Order().ElementAt(94);
        var max = times.Max();

        TestContext.Out.WriteLine($"Escritura de 100 publicaciones — media: {avg:F1} ms, P95: {p95} ms, máx: {max} ms");

        avg.ShouldBeLessThan(500,
            $"Latencia media de escritura {avg:F1} ms supera umbral de 500 ms");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static async Task SeedPublicationsBulkAsync(int count)
    {
        await ExecuteDbContextAsync(async db =>
        {
            var types = Enum.GetValues<Domain.Enums.PublicationType>();
            var batch = new List<Publication>(count);

            for (int i = 0; i < count; i++)
            {
                batch.Add(new Publication
                {
                    Id              = Guid.NewGuid().ToString(),
                    Title           = $"Publicación de volumen #{i + 1:D5}",
                    PublicationData = $"Revista Internacional Vol. {i % 100 + 1}, No. {i % 12 + 1}",
                    PublicationType = types[i % types.Length],
                    PublishedDate   = $"202{i % 4 + 1}",
                });
            }

            await db.Publications.AddRangeAsync(batch);
            await db.SaveChangesAsync();
            return 0;
        });
    }

    private static async Task<(string areaId, string clasifId, string jefeId)> SeedBaseForPerformanceAsync()
    {
        return await ExecuteDbContextAsync(async db =>
        {
            var univ = new Universidad { Id = "uh-perf", Nombre = "UH Perf" };
            db.Universidades.Add(univ);

            var area = new Area { Id = "area-perf", Nombre = "Área Perf", Descripcion = "d", UniversidadId = univ.Id };
            db.Areas.Add(area);

            var clasif = new Clasificacion { Id = "clasif-perf", Nombre = "Clasif Perf" };
            db.Clasificaciones.Add(clasif);

            var jefe = new User
            {
                Id = "jefe-perf",
                UserName = "jefe.perf@local",
                Email = "jefe.perf@local",
                UserLastName1 = "Jefe",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Testing1234!"),
                BirthDate = DateTime.SpecifyKind(new DateTime(1985, 1, 1), DateTimeKind.Utc),
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                AreaId = area.Id,
            };
            db.Users.Add(jefe);

            await db.SaveChangesAsync();
            return (area.Id, clasif.Id, jefe.Id);
        });
    }
}
