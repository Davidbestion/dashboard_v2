using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dashboard_v2.Application.FunctionalTests.Productions;

using static Testing;

[TestFixture]
public class ProfessorAuthorCreatorsFlowTests : BaseTestFixture
{
    [Test]
    public async Task Profesor_CanCreateAndUpdate_Registro_WithAdditionalCreators()
    {
        await RunAsUserAsync("prof.reg@local", "Testing1234!", ["Profesor"]);

        var countryId = await SeedCountryAsync();
        var institutionId = await SeedInstitutionAsync("Inst Registro");

        using var client = CreateClient();

        var createPayload = new
        {
            titulo = "Registro funcional",
            numeroCertificado = "REG-001",
            esInformatico = true,
            countryId,
            institutionId,
            additionalAuthorIds = Array.Empty<string>(),
            additionalAuthorNames = new[] { "Perez, Ana" },
            additionalUserIds = Array.Empty<string>()
        };

        var createResponse = await client.PostAsJsonAsync("/api/Registros", createPayload);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var registroId = await ExtractIdAsync(createResponse);

        var updatePayload = new
        {
            titulo = "Registro funcional actualizado",
            numeroCertificado = "REG-001A",
            esInformatico = false,
            countryId,
            institutionId,
            additionalAuthorIds = Array.Empty<string>(),
            additionalAuthorNames = new[] { "Lopez, Marta" },
            additionalUserIds = Array.Empty<string>()
        };

        var updateResponse = await client.PutAsJsonAsync($"/api/Registros/{registroId}", updatePayload);
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var creatorsCount = await ExecuteDbContextAsync(db =>
            db.AuthorRegistros.CountAsync(ar => ar.RegistroId == registroId));
        creatorsCount.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Test]
    public async Task Profesor_CanCreateAndUpdate_Norma_WithAdditionalCreators()
    {
        await RunAsUserAsync("prof.norm@local", "Testing1234!", ["Profesor"]);

        var institutionId = await SeedInstitutionAsync("Inst Norma");

        using var client = CreateClient();

        var createPayload = new
        {
            titulo = "Norma funcional",
            tipoNormaId = (int?)null,
            institutionId,
            additionalAuthorIds = Array.Empty<string>(),
            additionalAuthorNames = new[] { "Gomez, Raul" },
            additionalUserIds = Array.Empty<string>()
        };

        var createResponse = await client.PostAsJsonAsync("/api/Normas", createPayload);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var normaId = await ExtractIdAsync(createResponse);

        var updatePayload = new
        {
            titulo = "Norma funcional actualizada",
            tipoNormaId = (int?)null,
            institutionId,
            additionalAuthorIds = Array.Empty<string>(),
            additionalAuthorNames = new[] { "Suarez, Karla" },
            additionalUserIds = Array.Empty<string>()
        };

        var updateResponse = await client.PutAsJsonAsync($"/api/Normas/{normaId}", updatePayload);
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var creatorsCount = await ExecuteDbContextAsync(db =>
            db.AuthorNormas.CountAsync(an => an.NormaId == normaId));
        creatorsCount.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Test]
    public async Task Profesor_CanCreateAndUpdate_Producto_WithAdditionalCreators()
    {
        await RunAsUserAsync("prof.prod@local", "Testing1234!", ["Profesor"]);

        var institutionId = await SeedInstitutionAsync("Inst Producto");
        var tipoId = await SeedTipoProductoAsync();

        using var client = CreateClient();

        var createPayload = new
        {
            titulo = "Producto funcional",
            tipoProductoComercializadoId = tipoId,
            institutionId,
            additionalAuthorIds = Array.Empty<string>(),
            additionalAuthorNames = new[] { "Diaz, Abel" },
            additionalUserIds = Array.Empty<string>()
        };

        var createResponse = await client.PostAsJsonAsync("/api/ProductosComercializados", createPayload);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var productoId = await ExtractIdAsync(createResponse);

        var updatePayload = new
        {
            titulo = "Producto funcional actualizado",
            tipoProductoComercializadoId = tipoId,
            institutionId,
            additionalAuthorIds = Array.Empty<string>(),
            additionalAuthorNames = new[] { "Mendez, Laura" },
            additionalUserIds = Array.Empty<string>()
        };

        var updateResponse = await client.PutAsJsonAsync($"/api/ProductosComercializados/{productoId}", updatePayload);
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var creatorsCount = await ExecuteDbContextAsync(db =>
            db.AuthorProductosComercializados.CountAsync(ap => ap.ProductoComercializadoId == productoId));
        creatorsCount.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Test]
    public async Task Profesor_CanCreateAndUpdate_Patente_WithAdditionalCreators()
    {
        await RunAsUserAsync("prof.pat@local", "Testing1234!", ["Profesor"]);

        using var client = CreateClient();

        var createPayload = new
        {
            titulo = "Patente funcional",
            numeroSolicitudConcesion = "PAT-001",
            esNacional = true,
            additionalAuthorIds = Array.Empty<string>(),
            additionalAuthorNames = new[] { "Navarro, Iris" },
            additionalUserIds = Array.Empty<string>()
        };

        var createResponse = await client.PostAsJsonAsync("/api/Patentes", createPayload);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var patenteId = await ExtractIdAsync(createResponse);

        var updatePayload = new
        {
            titulo = "Patente funcional actualizada",
            numeroSolicitudConcesion = "PAT-001A",
            esNacional = false,
            additionalAuthorIds = Array.Empty<string>(),
            additionalAuthorNames = new[] { "Castro, Ines" },
            additionalUserIds = Array.Empty<string>()
        };

        var updateResponse = await client.PutAsJsonAsync($"/api/Patentes/{patenteId}", updatePayload);
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var creatorsCount = await ExecuteDbContextAsync(db =>
            db.AuthorPatentes.CountAsync(ap => ap.PatenteId == patenteId));
        creatorsCount.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Test]
    public async Task Vicedecano_CannotCreate_Producciones()
    {
        await RunAsUserAsync("vice@local", "Testing1234!", ["Vicedecano_de_investigacion"]);

        var countryId = await SeedCountryAsync();
        var institutionId = await SeedInstitutionAsync("Inst Vicedecano");
        var tipoId = await SeedTipoProductoAsync();

        using var client = CreateClient();

        var registroResponse = await client.PostAsJsonAsync("/api/Registros", new
        {
            titulo = "No debe crear",
            numeroCertificado = "X",
            esInformatico = true,
            countryId,
            institutionId
        });
        registroResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var normaResponse = await client.PostAsJsonAsync("/api/Normas", new
        {
            titulo = "No debe crear",
            tipo = "ISO",
            institutionId
        });
        normaResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var productoResponse = await client.PostAsJsonAsync("/api/ProductosComercializados", new
        {
            titulo = "No debe crear",
            tipoProductoComercializadoId = tipoId,
            institutionId
        });
        productoResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var patenteResponse = await client.PostAsJsonAsync("/api/Patentes", new
        {
            titulo = "No debe crear",
            numeroSolicitudConcesion = "NO",
            esNacional = true
        });
        patenteResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private static async Task<string> ExtractIdAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.TryGetProperty("id", out var idProperty).ShouldBeTrue();
        var id = idProperty.GetString();
        id.ShouldNotBeNullOrWhiteSpace();
        return id!;
    }

    private static async Task<int> SeedCountryAsync()
    {
        var id = await ExecuteDbContextAsync(async db =>
        {
            var existing = await db.Countries.FirstOrDefaultAsync();
            if (existing != null) return existing.Id;

            var country = new Country { Name = "Cuba" };
            db.Countries.Add(country);
            await db.SaveChangesAsync();
            return country.Id;
        });

        return id;
    }

    private static async Task<string> SeedInstitutionAsync(string name)
    {
        var id = await ExecuteDbContextAsync(async db =>
        {
            var existing = await db.Institutions.FirstOrDefaultAsync(i => i.Nombre == name);
            if (existing != null) return existing.Id;

            var institution = new Institution { Id = Guid.NewGuid().ToString(), Nombre = name };
            db.Institutions.Add(institution);
            await db.SaveChangesAsync();
            return institution.Id;
        });

        return id;
    }

    private static async Task<string> SeedTipoProductoAsync()
    {
        var id = await ExecuteDbContextAsync(async db =>
        {
            var existing = await db.TipoProductosComercializados.FirstOrDefaultAsync();
            if (existing != null) return existing.Id;

            var tipo = new TipoProductoComercializado { Id = Guid.NewGuid().ToString(), Nombre = "Software" };
            db.TipoProductosComercializados.Add(tipo);
            await db.SaveChangesAsync();
            return tipo.Id;
        });

        return id;
    }
}
