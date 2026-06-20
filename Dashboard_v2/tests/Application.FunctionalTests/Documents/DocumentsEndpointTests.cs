using System.Net;

namespace Dashboard_v2.Application.FunctionalTests.Documents;

using static Testing;

[TestFixture]
public class DocumentsEndpointTests : BaseTestFixture
{
    [Test]
    public async Task Superuser_CanDownload_Report_AsExcel()
    {
        await RunAsUserAsync("super.docs@local", "Testing1234!", ["Superuser"]);
        using var client = CreateClient();

        var response = await client.GetAsync("/api/Documents/anexo-registros");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var contentType = response.Content.Headers.ContentType?.MediaType;
        contentType.ShouldBe("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    [Test]
    public async Task Profesor_CannotDownload_Reports()
    {
        await RunAsUserAsync("prof.docs@local", "Testing1234!", ["Profesor"]);
        using var client = CreateClient();

        var response = await client.GetAsync("/api/Documents/anexo-registros");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Unauthenticated_Request_Returns_Unauthorized()
    {
        // No user set — TestAuthHandler devuelve NoResult → 401
        using var client = CreateClient();

        var response = await client.GetAsync("/api/Documents/anexo-registros");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
