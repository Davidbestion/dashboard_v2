using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Dashboard_v2.Application.FunctionalTests.Concurrency;

using static Testing;

/// <summary>
/// Tests de concurrencia: verifican que el sistema maneja múltiples
/// solicitudes simultáneas sin deadlocks, errores ni corrupción de datos.
/// </summary>
[TestFixture]
public class ConcurrencyTests : BaseTestFixture
{
    [Test]
    public async Task ConcurrentPublicationCreation_5Requests_AllSucceed()
    {
        await RunAsUserAsync("concurrent.pub@local", "Testing1234!", ["Profesor"]);
        using var client = CreateClient();

        const int count = 5;
        var tasks = Enumerable.Range(1, count).Select(i =>
            client.PostAsJsonAsync("/api/Publications", new
            {
                title = $"Publicación concurrente #{i}",
                publicationData = $"Revista de prueba, Vol. {i}",
                publicationType = 4,
                publishedDate = "2024",
                additionalAuthorIds = Array.Empty<string>(),
                additionalAuthorNames = Array.Empty<string>(),
                additionalUserIds = Array.Empty<string>()
            }));

        var responses = await Task.WhenAll(tasks);

        foreach (var r in responses)
            r.StatusCode.ShouldBe(HttpStatusCode.Created);

        var ids = new HashSet<string>();
        foreach (var r in responses)
        {
            var body = await r.Content.ReadFromJsonAsync<JsonElement>();
            ids.Add(body.GetProperty("id").GetString()!);
        }
        ids.Count.ShouldBe(count, "Cada solicitud concurrente debe generar un ID único");
    }

    [Test]
    public async Task ConcurrentReadRequests_10Requests_AllReturn200()
    {
        await RunAsUserAsync("concurrent.read@local", "Testing1234!", ["Profesor"]);
        using var client = CreateClient();

        // Crear una publicación base para que el endpoint no esté vacío
        await client.PostAsJsonAsync("/api/Publications", new
        {
            title = "Publicación base para reads concurrentes",
            publicationData = "Revista X",
            publicationType = 4,
            publishedDate = "2024",
            additionalAuthorIds = Array.Empty<string>(),
            additionalAuthorNames = Array.Empty<string>(),
            additionalUserIds = Array.Empty<string>()
        });

        const int count = 10;
        var tasks = Enumerable.Range(0, count).Select(_ =>
            client.GetAsync("/api/Publications"));

        var responses = await Task.WhenAll(tasks);

        foreach (var r in responses)
            r.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task ConcurrentReadWrite_InterleavedRequests_NoDeadlock()
    {
        await RunAsUserAsync("concurrent.rw@local", "Testing1234!", ["Profesor"]);
        using var client = CreateClient();

        // 3 escrituras y 3 lecturas simultáneas
        var writes = Enumerable.Range(1, 3).Select(i =>
            client.PostAsJsonAsync("/api/Publications", new
            {
                title = $"Escritura concurrente #{i}",
                publicationData = "Datos",
                publicationType = 4,
                publishedDate = "2024",
                additionalAuthorIds = Array.Empty<string>(),
                additionalAuthorNames = Array.Empty<string>(),
                additionalUserIds = Array.Empty<string>()
            }));

        var reads = Enumerable.Range(0, 3).Select(_ =>
            client.GetAsync("/api/Publications"));

        var allResponses = await Task.WhenAll(writes.Concat(reads));

        // Las escrituras deben devolver 201, las lecturas 200
        var writeResponses = allResponses.Take(3).ToList();
        var readResponses = allResponses.Skip(3).ToList();

        foreach (var r in writeResponses)
            r.StatusCode.ShouldBe(HttpStatusCode.Created);

        foreach (var r in readResponses)
            r.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task ConcurrentNomencladorRequests_DifferentNames_AllSucceed()
    {
        await RunAsUserAsync("concurrent.nom@local", "Testing1234!", ["Superuser"]);
        using var client = CreateClient();

        // 8 requests con nombres distintos → cada una debe crear su propio registro
        const int count = 8;
        var tasks = Enumerable.Range(1, count).Select(i =>
            client.PostAsJsonAsync("/api/Nomencladores/tiposnorma", new { nombre = $"TipoNorma Concurrente #{i}" }));

        var responses = await Task.WhenAll(tasks);

        // Todas deben devolver 201 Created (nombres únicos, no hay colisión)
        foreach (var r in responses)
            r.StatusCode.ShouldBe(HttpStatusCode.Created,
                $"Cada request con nombre distinto debe crear un registro nuevo (201)");

        // Verificar que se crearon exactamente los 8 registros en la BD
        var dbCount = await ExecuteDbContextAsync(db =>
            db.TiposNorma.CountAsync(t => t.Nombre.StartsWith("TipoNorma Concurrente #")));
        dbCount.ShouldBe(count, "Deben existir exactamente 8 registros creados concurrentemente");
    }
}
