using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dashboard_v2.Domain.Entities;

namespace Dashboard_v2.Application.FunctionalTests.Proyectos;

using static Testing;

/// <summary>
/// Tests funcionales HTTP para los 7 tipos de proyecto del sistema.
/// Verifica que el ciclo CREATE → GET devuelve los códigos HTTP correctos
/// y que el rol Profesor no puede crear proyectos (autorización).
/// </summary>
[TestFixture]
public class ProyectoCrudFlowTests : BaseTestFixture
{
    // ── Helpers de seed ──────────────────────────────────────────────────────

    private static async Task<(string areaId, string clasificId, string jefeId)> SeedBaseAsync(string suffix)
    {
        return await ExecuteDbContextAsync(async db =>
        {
            var univ = new Universidad { Id = $"uh-{suffix}", Nombre = $"UH-{suffix}" };
            db.Universidades.Add(univ);

            var area = new Area { Id = $"area-{suffix}", Nombre = $"Área {suffix}", Descripcion = "d", UniversidadId = univ.Id };
            db.Areas.Add(area);

            var clasif = new Clasificacion { Id = $"clasif-{suffix}", Nombre = $"Clasif {suffix}" };
            db.Clasificaciones.Add(clasif);

            var jefe = new User
            {
                Id = $"jefe-{suffix}",
                UserName = $"jefe.{suffix}@local",
                Email = $"jefe.{suffix}@local",
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

    private static async Task<int> SeedMunicipioAsync(string suffix)
    {
        return await ExecuteDbContextAsync(async db =>
        {
            var prov = new Provincia { Nombre = $"Prov-{suffix}" };
            db.Provincias.Add(prov);
            await db.SaveChangesAsync();

            var mun = new Municipio { Nombre = $"Mun-{suffix}", ProvinciaId = prov.Id };
            db.Municipios.Add(mun);
            await db.SaveChangesAsync();
            return mun.Id;
        });
    }

    private static async Task<string> SeedInstitutionAsync(string suffix)
    {
        return await ExecuteDbContextAsync(async db =>
        {
            var inst = new Institution { Id = $"inst-{suffix}", Nombre = $"Inst {suffix}" };
            db.Institutions.Add(inst);
            await db.SaveChangesAsync();
            return inst.Id;
        });
    }

    private static async Task<int> SeedProgramaAsync(string suffix)
    {
        return await ExecuteDbContextAsync(async db =>
        {
            var prog = new Programa { Nombre = $"Prog-{suffix}" };
            db.Programas.Add(prog);
            await db.SaveChangesAsync();
            return prog.Id;
        });
    }

    private static async Task<string> ExtractIdAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.TryGetProperty("id", out var idProp).ShouldBeTrue();
        var id = idProp.GetString();
        id.ShouldNotBeNullOrWhiteSpace();
        return id!;
    }

    private static object BasePayload(string titulo, string jefeId, string clasifId, string areaId) => new
    {
        titulo,
        jefeId,
        numeroMiembros = 5,
        cantidadMiembrosUH = 3,
        cantidadEstudiantes = 1,
        cantidadEstudiantesContratados = 0,
        tributaFormacionDoctoral = false,
        clasificacionId = clasifId,
        areaId,
    };

    private static object EjecucionPayload(string titulo, string jefeId, string clasifId, string areaId) => new
    {
        titulo,
        jefeId,
        numeroMiembros = 5,
        cantidadMiembrosUH = 3,
        cantidadEstudiantes = 1,
        cantidadEstudiantesContratados = 0,
        tributaFormacionDoctoral = false,
        tributaDesarrolloLocal = false,
        clasificacionId = clasifId,
        areaId,
        fechaInicio = "2024-01-01",
        codigoProyecto = $"COD-{titulo[..3].ToUpper()}",
        estadosDeEjecucionIds = Array.Empty<int>(),
        entidadesEjecutorasPrincipalesIds = Array.Empty<string>(),
        entidadesEjecutorasParticipantesIds = Array.Empty<string>(),
        sectoresEstrategicosIds = Array.Empty<int>(),
        ejesEstrategicosIds = Array.Empty<int>(),
    };

    // ── Tests por tipo ───────────────────────────────────────────────────────

    [Test]
    public async Task JefeDeProyecto_CanCreate_ProyectoEnRevision()
    {
        await RunAsUserAsync("jefe.rev@local", "Testing1234!", ["Jefe_de_Proyecto"]);
        var (areaId, clasifId, jefeId) = await SeedBaseAsync("rev");
        using var client = CreateClient();

        var payload = new
        {
            titulo = "Proyecto en revisión funcional",
            jefeId,
            numeroMiembros = 4,
            cantidadMiembrosUH = 2,
            cantidadEstudiantes = 0,
            cantidadEstudiantesContratados = 0,
            tributaFormacionDoctoral = false,
            clasificacionId = clasifId,
            areaId,
            situacionesIds = Array.Empty<int>(),
            tipo = "Tesis",
        };

        var response = await client.PostAsJsonAsync("/api/Proyectos/en-revision", payload);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var id = await ExtractIdAsync(response);
        var getResponse = await client.GetAsync($"/api/Proyectos/en-revision/{id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task JefeDeProyecto_CanCreate_ProyectoEmpresarial()
    {
        await RunAsUserAsync("jefe.emp@local", "Testing1234!", ["Jefe_de_Proyecto"]);
        var (areaId, clasifId, jefeId) = await SeedBaseAsync("emp");
        var empresaId = await SeedInstitutionAsync("emp");
        using var client = CreateClient();

        var payload = new
        {
            titulo = "Proyecto empresarial funcional",
            jefeId,
            numeroMiembros = 6,
            cantidadMiembrosUH = 4,
            cantidadEstudiantes = 1,
            cantidadEstudiantesContratados = 0,
            tributaFormacionDoctoral = false,
            tributaDesarrolloLocal = false,
            clasificacionId = clasifId,
            areaId,
            fechaInicio = "2024-02-01",
            codigoProyecto = "COD-EMP",
            estadosDeEjecucionIds = Array.Empty<int>(),
            entidadesEjecutorasPrincipalesIds = Array.Empty<string>(),
            entidadesEjecutorasParticipantesIds = Array.Empty<string>(),
            sectoresEstrategicosIds = Array.Empty<int>(),
            ejesEstrategicosIds = Array.Empty<int>(),
            empresasIds = new[] { empresaId },
        };

        var response = await client.PostAsJsonAsync("/api/Proyectos/empresariales", payload);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var id = await ExtractIdAsync(response);
        var getResponse = await client.GetAsync($"/api/Proyectos/empresariales/{id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task JefeDeProyecto_CanCreate_ProyectoApoyoPrograma()
    {
        await RunAsUserAsync("jefe.pap@local", "Testing1234!", ["Jefe_de_Proyecto"]);
        var (areaId, clasifId, jefeId) = await SeedBaseAsync("pap");
        var programaId = await SeedProgramaAsync("pap");
        using var client = CreateClient();

        var payload = new
        {
            titulo = "Proyecto apoyo programa funcional",
            jefeId,
            numeroMiembros = 5,
            cantidadMiembrosUH = 3,
            cantidadEstudiantes = 0,
            cantidadEstudiantesContratados = 0,
            tributaFormacionDoctoral = true,
            tributaDesarrolloLocal = false,
            clasificacionId = clasifId,
            areaId,
            fechaInicio = "2024-03-01",
            codigoProyecto = "COD-PAP",
            estadosDeEjecucionIds = Array.Empty<int>(),
            entidadesEjecutorasPrincipalesIds = Array.Empty<string>(),
            entidadesEjecutorasParticipantesIds = Array.Empty<string>(),
            sectoresEstrategicosIds = Array.Empty<int>(),
            ejesEstrategicosIds = Array.Empty<int>(),
            programasIds = new[] { programaId },
            tipoPAP = 1, // Nacional
        };

        var response = await client.PostAsJsonAsync("/api/Proyectos/apoyo-programa", payload);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var id = await ExtractIdAsync(response);
        var getResponse = await client.GetAsync($"/api/Proyectos/apoyo-programa/{id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task JefeDeProyecto_CanCreate_ProyectoDesarrolloLocal()
    {
        await RunAsUserAsync("jefe.pdl@local", "Testing1234!", ["Jefe_de_Proyecto"]);
        var (areaId, clasifId, jefeId) = await SeedBaseAsync("pdl");
        var municipioId = await SeedMunicipioAsync("pdl");
        using var client = CreateClient();

        var payload = new
        {
            titulo = "Proyecto desarrollo local funcional",
            jefeId,
            numeroMiembros = 4,
            cantidadMiembrosUH = 2,
            cantidadEstudiantes = 0,
            cantidadEstudiantesContratados = 0,
            tributaFormacionDoctoral = false,
            tributaDesarrolloLocal = true,
            clasificacionId = clasifId,
            areaId,
            fechaInicio = "2024-04-01",
            codigoProyecto = "COD-PDL",
            estadosDeEjecucionIds = Array.Empty<int>(),
            entidadesEjecutorasPrincipalesIds = Array.Empty<string>(),
            entidadesEjecutorasParticipantesIds = Array.Empty<string>(),
            sectoresEstrategicosIds = Array.Empty<int>(),
            ejesEstrategicosIds = Array.Empty<int>(),
            municipioId,
        };

        var response = await client.PostAsJsonAsync("/api/Proyectos/desarrollo-local", payload);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var id = await ExtractIdAsync(response);
        var getResponse = await client.GetAsync($"/api/Proyectos/desarrollo-local/{id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task JefeDeProyecto_CanCreate_ProyectoNoEmpresarial()
    {
        await RunAsUserAsync("jefe.pne@local", "Testing1234!", ["Jefe_de_Proyecto"]);
        var (areaId, clasifId, jefeId) = await SeedBaseAsync("pne");
        using var client = CreateClient();

        var payload = new
        {
            titulo = "Proyecto no empresarial funcional",
            jefeId,
            numeroMiembros = 3,
            cantidadMiembrosUH = 3,
            cantidadEstudiantes = 0,
            cantidadEstudiantesContratados = 0,
            tributaFormacionDoctoral = false,
            tributaDesarrolloLocal = false,
            clasificacionId = clasifId,
            areaId,
            fechaInicio = "2024-05-01",
            codigoProyecto = "COD-PNE",
            estadosDeEjecucionIds = Array.Empty<int>(),
            entidadesEjecutorasPrincipalesIds = Array.Empty<string>(),
            entidadesEjecutorasParticipantesIds = Array.Empty<string>(),
            sectoresEstrategicosIds = Array.Empty<int>(),
            ejesEstrategicosIds = Array.Empty<int>(),
            entidadesIds = Array.Empty<string>(),
        };

        var response = await client.PostAsJsonAsync("/api/Proyectos/no-empresariales", payload);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var id = await ExtractIdAsync(response);
        var getResponse = await client.GetAsync($"/api/Proyectos/no-empresariales/{id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task JefeDeProyecto_CanCreate_ProyectoColaboracionInternacional()
    {
        await RunAsUserAsync("jefe.prci@local", "Testing1234!", ["Jefe_de_Proyecto"]);
        var (areaId, clasifId, jefeId) = await SeedBaseAsync("prci");
        using var client = CreateClient();

        var payload = new
        {
            titulo = "Proyecto colaboración internacional funcional",
            jefeId,
            numeroMiembros = 8,
            cantidadMiembrosUH = 5,
            cantidadEstudiantes = 2,
            cantidadEstudiantesContratados = 0,
            tributaFormacionDoctoral = true,
            tributaDesarrolloLocal = false,
            clasificacionId = clasifId,
            areaId,
            fechaInicio = "2024-06-01",
            codigoProyecto = "COD-PRCI",
            estadosDeEjecucionIds = Array.Empty<int>(),
            entidadesEjecutorasPrincipalesIds = Array.Empty<string>(),
            entidadesEjecutorasParticipantesIds = Array.Empty<string>(),
            sectoresEstrategicosIds = Array.Empty<int>(),
            ejesEstrategicosIds = Array.Empty<int>(),
            fuentesFinanciacionIds = Array.Empty<int>(),
            terminosReferencia = "Términos de referencia del proyecto internacional.",
        };

        var response = await client.PostAsJsonAsync("/api/Proyectos/colaboracion-internacional", payload);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var id = await ExtractIdAsync(response);
        var getResponse = await client.GetAsync($"/api/Proyectos/colaboracion-internacional/{id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task JefeDeProyecto_CanCreate_ProyectoPNAP()
    {
        await RunAsUserAsync("jefe.pnap@local", "Testing1234!", ["Jefe_de_Proyecto"]);
        var (areaId, clasifId, jefeId) = await SeedBaseAsync("pnap");
        using var client = CreateClient();

        var payload = new
        {
            titulo = "Proyecto PNAP funcional",
            jefeId,
            numeroMiembros = 7,
            cantidadMiembrosUH = 4,
            cantidadEstudiantes = 1,
            cantidadEstudiantesContratados = 1,
            tributaFormacionDoctoral = true,
            tributaDesarrolloLocal = false,
            clasificacionId = clasifId,
            areaId,
            fechaInicio = "2024-07-01",
            codigoProyecto = "COD-PNAP",
            estadosDeEjecucionIds = Array.Empty<int>(),
            entidadesEjecutorasPrincipalesIds = Array.Empty<string>(),
            entidadesEjecutorasParticipantesIds = Array.Empty<string>(),
            sectoresEstrategicosIds = Array.Empty<int>(),
            ejesEstrategicosIds = Array.Empty<int>(),
            fuentesFinanciacionIds = Array.Empty<int>(),
        };

        var response = await client.PostAsJsonAsync("/api/Proyectos/pnap", payload);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var id = await ExtractIdAsync(response);
        var getResponse = await client.GetAsync($"/api/Proyectos/pnap/{id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Profesor_CannotCreate_AnyProyecto()
    {
        await RunAsUserAsync("prof.proy@local", "Testing1234!", ["Profesor"]);
        var (areaId, clasifId, jefeId) = await SeedBaseAsync("prof-proy");
        using var client = CreateClient();

        var payload = new
        {
            titulo = "No debe crearse",
            jefeId,
            numeroMiembros = 1,
            cantidadMiembrosUH = 1,
            cantidadEstudiantes = 0,
            cantidadEstudiantesContratados = 0,
            tributaFormacionDoctoral = false,
            clasificacionId = clasifId,
            areaId,
            situacionesIds = Array.Empty<int>(),
            tipo = "Tesis",
        };

        var response = await client.PostAsJsonAsync("/api/Proyectos/en-revision", payload);
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
