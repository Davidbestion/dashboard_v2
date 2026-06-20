using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Dashboard_v2.Application.FunctionalTests.Publications;

using static Testing;

[TestFixture]
public class PublicationFlowTests : BaseTestFixture
{
    [Test]
    public async Task Profesor_CanCreate_And_Update_Publication()
    {
        await RunAsUserAsync("prof.pub@local", "Testing1234!", ["Profesor"]);
        using var client = CreateClient();

        var createPayload = new
        {
            title = "Artículo funcional de prueba",
            publicationData = "Revista Cubana de Ciencias Informáticas, Vol. 18",
            publicationType = 4, // Artículo_de_Divulgación
            publishedDate = "2024",
            additionalAuthorIds = Array.Empty<string>(),
            additionalAuthorNames = new[] { "García, Marta" },
            additionalUserIds = Array.Empty<string>()
        };

        var createResponse = await client.PostAsJsonAsync("/api/Publications", createPayload);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var body = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        body.TryGetProperty("id", out var idProp).ShouldBeTrue();
        var id = idProp.GetString();
        id.ShouldNotBeNullOrWhiteSpace();

        var updatePayload = new
        {
            title = "Artículo funcional de prueba (revisado)",
            publicationData = "Revista Cubana de Ciencias Informáticas, Vol. 18, No. 2",
            publicationType = 4,
            publishedDate = "2024-06",
            additionalAuthorIds = Array.Empty<string>(),
            additionalAuthorNames = new[] { "Rodríguez, Luis" },
            additionalUserIds = Array.Empty<string>()
        };

        var updateResponse = await client.PutAsJsonAsync($"/api/Publications/{id}", updatePayload);
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task FindDuplicates_ReturnsCandidates_WhenTitleMatches()
    {
        await RunAsUserAsync("prof.dup@local", "Testing1234!", ["Profesor"]);
        using var client = CreateClient();

        var createPayload = new
        {
            title = "Estudio sobre inteligencia artificial en educación",
            publicationData = "Revista de Educación Superior",
            publicationType = 4,
            publishedDate = "2023",
            additionalAuthorIds = Array.Empty<string>(),
            additionalAuthorNames = Array.Empty<string>(),
            additionalUserIds = Array.Empty<string>()
        };

        var createResponse = await client.PostAsJsonAsync("/api/Publications", createPayload);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var duplicatesResponse = await client.GetAsync(
            "/api/Publications/duplicates?title=Estudio+sobre+inteligencia+artificial+en+educaci%C3%B3n");
        duplicatesResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var candidates = await duplicatesResponse.Content.ReadFromJsonAsync<JsonElement>();
        candidates.ValueKind.ShouldBe(JsonValueKind.Array);
        candidates.GetArrayLength().ShouldBeGreaterThan(0);
    }

    [Test]
    public async Task Vicedecano_CannotCreate_Publication()
    {
        await RunAsUserAsync("vice.pub@local", "Testing1234!", ["Vicedecano_de_investigacion"]);
        using var client = CreateClient();

        var payload = new
        {
            title = "No debe crearse",
            publicationData = "Revista X",
            publicationType = 4,
            publishedDate = "2024",
            additionalAuthorIds = Array.Empty<string>(),
            additionalAuthorNames = Array.Empty<string>(),
            additionalUserIds = Array.Empty<string>()
        };

        var response = await client.PostAsJsonAsync("/api/Publications", payload);
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
