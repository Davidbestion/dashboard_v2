using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Proyectos;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Domain.Enums;
using Dashboard_v2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Shouldly;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Application.UnitTests.Proyectos;

/// <summary>
/// Tests for ProyectoService Create and Update operations.
/// </summary>
[TestFixture]
public class ProyectoServiceCrudTests
{
    private static ApplicationDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(opts);
    }

    private static ProyectoService BuildService(ApplicationDbContext db, IUser user)
    {
        var validationMock = new Mock<IRequestValidationService>();
        validationMock
            .Setup(v => v.ValidateAndThrowAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return new ProyectoService(db, user, validationMock.Object);
    }

    private static IUser MakeSuperuser(string id = "super-1")
    {
        var mock = new Mock<IUser>();
        mock.Setup(u => u.Id).Returns(id);
        mock.Setup(u => u.Roles).Returns(new List<string> { nameof(RolesEnum.Superuser) });
        return mock.Object;
    }

    private static IUser MakeJefe(string id)
    {
        var mock = new Mock<IUser>();
        mock.Setup(u => u.Id).Returns(id);
        mock.Setup(u => u.Roles).Returns(new List<string> { nameof(RolesEnum.Jefe_de_Proyecto) });
        return mock.Object;
    }

    /// <summary>Seeds Area, Clasificacion and a Jefe user. Returns their IDs.</summary>
    private static (string areaId, string clasificId, string jefeId) SeedBase(ApplicationDbContext db)
    {
        var area = new Area { Id = "area-1", Nombre = "MATCOM", Descripcion = "d", UniversidadId = "uh" };
        var clasif = new Clasificacion { Id = "clasif-1", Nombre = "Básica" };
        var jefe = new User
        {
            Id = "jefe-1",
            UserName = "jefe",
            UserLastName1 = "Pérez",
            Email = "jefe@uh.cu",
            PasswordHash = "hash",
            IsActive = true,
            BirthDate = DateTime.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            AreaId = area.Id,
            UserRoles = new List<UserRole>
            {
                new() { UserId = "jefe-1", Role = RolesEnum.Jefe_de_Proyecto }
            }
        };
        db.Areas.Add(area);
        db.Clasificaciones.Add(clasif);
        db.Users.Add(jefe);
        db.SaveChanges();
        return (area.Id, clasif.Id, jefe.Id);
    }

    /// <summary>Seeds an Institution and returns its Id.</summary>
    private static string SeedInstitution(ApplicationDbContext db, string nombre = "Empresa Cubana S.A.")
    {
        var inst = new Institution { Nombre = nombre };
        db.Institutions.Add(inst);
        db.SaveChanges();
        return inst.Id;
    }

    /// <summary>Seeds a plain User suitable as participante and returns its Id.</summary>
    private static string SeedParticipante(ApplicationDbContext db, string id, string nombre = "Participante")
    {
        var user = new User
        {
            Id = id,
            UserName = nombre,
            UserLastName1 = "Test",
            Email = $"{id}@uh.cu",
            PasswordHash = "hash",
            IsActive = true,
            BirthDate = DateTime.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Users.Add(user);
        db.SaveChanges();
        return user.Id;
    }

    private static ProyectoEmpresarialUpsertRequest BuildEmpresarialRequest(
        string clasificId, string jefeId,
        IList<string>? empresasIds = null,
        IList<string>? participantesIds = null) => new()
    {
        Titulo = "Proyecto Empresarial Test",
        JefeId = jefeId,
        ClasificacionId = clasificId,
        NumeroMiembros = 5,
        CantidadMiembrosUH = 3,
        FechaInicio = DateOnly.FromDateTime(DateTime.Today),
        CodigoProyecto = "PE-001",
        EmpresasIds = empresasIds ?? [],
        ParticipantesIds = participantesIds ?? []
    };

    // ── CreateEmpresarialAsync ────────────────────────────────────────────────

    [Test]
    public async Task CreateEmpresarialAsync_Superuser_Succeeds()
    {
        await using var db = CreateDb();
        var (_, clasificId, jefeId) = SeedBase(db);
        var instId = SeedInstitution(db);

        var service = BuildService(db, MakeSuperuser());
        var request = BuildEmpresarialRequest(clasificId, jefeId, [instId]);

        var (result, id) = await service.CreateEmpresarialAsync(request);

        result.Succeeded.ShouldBeTrue();
        id.ShouldNotBeNullOrWhiteSpace();

        var created = await db.Proyectos.OfType<ProyectoEmpresarial>()
            .Include(p => p.Empresas)
            .FirstOrDefaultAsync(p => p.Id == id);
        created.ShouldNotBeNull();
        created!.Titulo.ShouldBe("Proyecto Empresarial Test");
        created.CodigoProyecto.ShouldBe("PE-001");
        created.Empresas.Count.ShouldBe(1);
        created.Empresas.First().Id.ShouldBe(instId);
    }

    [Test]
    public async Task CreateEmpresarialAsync_WithParticipantes_AssignsCorrectly()
    {
        await using var db = CreateDb();
        var (_, clasificId, jefeId) = SeedBase(db);
        var instId = SeedInstitution(db);
        var part1 = SeedParticipante(db, "part-1", "Ana");
        var part2 = SeedParticipante(db, "part-2", "Luis");

        var service = BuildService(db, MakeSuperuser());
        var request = BuildEmpresarialRequest(clasificId, jefeId, [instId], [part1, part2]);

        var (result, id) = await service.CreateEmpresarialAsync(request);

        result.Succeeded.ShouldBeTrue();
        var created = await db.Proyectos.OfType<ProyectoEmpresarial>()
            .Include(p => p.Participantes)
            .FirstOrDefaultAsync(p => p.Id == id);
        created.ShouldNotBeNull();
        created!.Participantes.Count.ShouldBe(3); // jefe + part1 + part2
        created.Participantes.ShouldContain(u => u.Id == jefeId);
        created.Participantes.ShouldContain(u => u.Id == part1);
        created.Participantes.ShouldContain(u => u.Id == part2);
    }

    // ── UpdateEmpresarialAsync ────────────────────────────────────────────────

    [Test]
    public async Task UpdateEmpresarialAsync_JefeOwner_Succeeds()
    {
        await using var db = CreateDb();
        var (_, clasificId, jefeId) = SeedBase(db);
        var instId1 = SeedInstitution(db, "Primera Empresa");
        var instId2 = SeedInstitution(db, "Nueva Empresa");

        var service = BuildService(db, MakeJefe(jefeId));
        var (createResult, id) = await service.CreateEmpresarialAsync(
            BuildEmpresarialRequest(clasificId, jefeId, [instId1]));
        createResult.Succeeded.ShouldBeTrue();

        var updateRequest = new ProyectoEmpresarialUpsertRequest
        {
            Titulo = "Titulo Actualizado",
            JefeId = jefeId,
            ClasificacionId = clasificId,
            NumeroMiembros = 7,
            CantidadMiembrosUH = 4,
            FechaInicio = DateOnly.FromDateTime(DateTime.Today),
            CodigoProyecto = "PE-002",
            EmpresasIds = [instId2]
        };

        var result = await service.UpdateEmpresarialAsync(id!, updateRequest);

        result.Succeeded.ShouldBeTrue();
        var updated = await db.Proyectos.OfType<ProyectoEmpresarial>()
            .Include(p => p.Empresas)
            .FirstOrDefaultAsync(p => p.Id == id);
        updated!.Titulo.ShouldBe("Titulo Actualizado");
        updated.Empresas.Count.ShouldBe(1);
        updated.Empresas.First().Id.ShouldBe(instId2);
    }

    [Test]
    public async Task UpdateEmpresarialAsync_UpdatesParticipantes()
    {
        await using var db = CreateDb();
        var (_, clasificId, jefeId) = SeedBase(db);
        var instId = SeedInstitution(db);
        var part1 = SeedParticipante(db, "part-1", "Ana");
        var part2 = SeedParticipante(db, "part-2", "Luis");

        var service = BuildService(db, MakeSuperuser());
        var (_, id) = await service.CreateEmpresarialAsync(
            BuildEmpresarialRequest(clasificId, jefeId, [instId], [part1]));

        var updateRequest = new ProyectoEmpresarialUpsertRequest
        {
            Titulo = "Proyecto Empresarial Test",
            JefeId = jefeId,
            ClasificacionId = clasificId,
            NumeroMiembros = 5,
            CantidadMiembrosUH = 3,
            FechaInicio = DateOnly.FromDateTime(DateTime.Today),
            CodigoProyecto = "PE-001",
            EmpresasIds = [instId],
            ParticipantesIds = [part2]
        };

        var result = await service.UpdateEmpresarialAsync(id!, updateRequest);

        result.Succeeded.ShouldBeTrue();
        var updated = await db.Proyectos.OfType<ProyectoEmpresarial>()
            .Include(p => p.Participantes)
            .FirstOrDefaultAsync(p => p.Id == id);
        updated!.Participantes.Count.ShouldBe(2); // jefe + part2
        updated.Participantes.ShouldContain(u => u.Id == jefeId);
        updated.Participantes.ShouldContain(u => u.Id == part2);
        updated.Participantes.ShouldNotContain(u => u.Id == part1);
    }

    [Test]
    public async Task UpdateEmpresarialAsync_JefeUpdatesOthersProject_ReturnsForbidden()
    {
        await using var db = CreateDb();
        var (areaId, clasificId, jefeId) = SeedBase(db);

        db.Users.Add(new User
        {
            Id = "jefe-2",
            UserName = "jefe2",
            UserLastName1 = "G",
            Email = "jefe2@uh.cu",
            PasswordHash = "x",
            IsActive = true,
            BirthDate = DateTime.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            AreaId = areaId,
            UserRoles = new List<UserRole>
            {
                new() { UserId = "jefe-2", Role = RolesEnum.Jefe_de_Proyecto }
            }
        });
        await db.SaveChangesAsync();

        var service1 = BuildService(db, MakeJefe(jefeId));
        var (_, id) = await service1.CreateEmpresarialAsync(BuildEmpresarialRequest(clasificId, jefeId));

        var service2 = BuildService(db, MakeJefe("jefe-2"));
        var result = await service2.UpdateEmpresarialAsync(id!, BuildEmpresarialRequest(clasificId, "jefe-2"));

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("permiso"));
    }

    // ── UpdateEnRevisionAsync ─────────────────────────────────────────────────

    [Test]
    public async Task UpdateEnRevisionAsync_Superuser_Succeeds()
    {
        await using var db = CreateDb();
        var (_, clasificId, jefeId) = SeedBase(db);
        var situacion = new SituacionProyecto { Nombre = "En Revisión" };
        db.SituacionesProyecto.Add(situacion);
        await db.SaveChangesAsync();

        var proyecto = new ProyectoEnRevision
        {
            Id = "rev-1",
            Titulo = "Proyecto En Revisión",
            JefeId = jefeId,
            ClasificacionId = clasificId,
            NumeroMiembros = 2,
            Tipo = "PE"
        };
        db.Proyectos.Add(proyecto);
        await db.SaveChangesAsync();

        var service = BuildService(db, MakeSuperuser());
        var request = new ProyectoEnRevisionUpsertRequest
        {
            Titulo = "Revisión Actualizada",
            JefeId = jefeId,
            ClasificacionId = clasificId,
            NumeroMiembros = 4,
            CantidadMiembrosUH = 2,
            SituacionesIds = [situacion.Id],
            Tipo = "PI"
        };

        var result = await service.UpdateEnRevisionAsync("rev-1", request);

        result.Succeeded.ShouldBeTrue();
        var updated = await db.Proyectos.OfType<ProyectoEnRevision>()
            .Include(p => p.Situaciones)
            .FirstOrDefaultAsync(p => p.Id == "rev-1");
        updated!.Titulo.ShouldBe("Revisión Actualizada");
        updated.Situaciones.ShouldContain(s => s.Id == situacion.Id);
    }

    // ── CreateApoyoProgramaAsync ──────────────────────────────────────────────

    [Test]
    public async Task CreateApoyoProgramaAsync_Superuser_Succeeds()
    {
        await using var db = CreateDb();
        var (_, clasificId, jefeId) = SeedBase(db);
        var programa = new Programa { Nombre = "Programa Nacional de IA" };
        db.Programas.Add(programa);
        await db.SaveChangesAsync();

        var service = BuildService(db, MakeSuperuser());
        var request = new ProyectoApoyoProgramaUpsertRequest
        {
            Titulo = "PAP Nacional",
            JefeId = jefeId,
            ClasificacionId = clasificId,
            NumeroMiembros = 3,
            CantidadMiembrosUH = 2,
            FechaInicio = DateOnly.FromDateTime(DateTime.Today),
            CodigoProyecto = "PAP-001",
            ProgramasIds = [programa.Id],
            TipoPAP = TipoPAP.Nacional
        };

        var (result, id) = await service.CreateApoyoProgramaAsync(request);

        result.Succeeded.ShouldBeTrue();
        id.ShouldNotBeNullOrWhiteSpace();
        var created = await db.Proyectos.OfType<ProyectoApoyoPrograma>()
            .Include(p => p.Programas)
            .FirstOrDefaultAsync(p => p.Id == id);
        created.ShouldNotBeNull();
        created!.Programas.ShouldContain(p => p.Id == programa.Id);
        created.TipoPAP.ShouldBe(TipoPAP.Nacional);
    }

    // ── CreateDesarrolloLocalAsync ────────────────────────────────────────────

    [Test]
    public async Task CreateDesarrolloLocalAsync_Superuser_Succeeds()
    {
        await using var db = CreateDb();
        var (_, clasificId, jefeId) = SeedBase(db);
        var provincia = new Provincia { Nombre = "La Habana" };
        db.Provincias.Add(provincia);
        await db.SaveChangesAsync();
        var municipio = new Municipio { Nombre = "Plaza de la Revolución", ProvinciaId = provincia.Id };
        db.Municipios.Add(municipio);
        await db.SaveChangesAsync();

        var service = BuildService(db, MakeSuperuser());
        var request = new ProyectoDesarrolloLocalUpsertRequest
        {
            Titulo = "Desarrollo Local Habana",
            JefeId = jefeId,
            ClasificacionId = clasificId,
            NumeroMiembros = 4,
            CantidadMiembrosUH = 3,
            FechaInicio = DateOnly.FromDateTime(DateTime.Today),
            CodigoProyecto = "DL-001",
            MunicipioId = municipio.Id
        };

        var (result, id) = await service.CreateDesarrolloLocalAsync(request);

        result.Succeeded.ShouldBeTrue();
        var created = await db.Proyectos.OfType<ProyectoDesarrolloLocal>()
            .FirstOrDefaultAsync(p => p.Id == id);
        created!.MunicipioId.ShouldBe(municipio.Id);
    }

    // ── GetEmpresarialByIdAsync with data ─────────────────────────────────────

    [Test]
    public async Task GetEmpresarialByIdAsync_WithData_ReturnsDto()
    {
        await using var db = CreateDb();
        var (_, clasificId, jefeId) = SeedBase(db);
        var instId = SeedInstitution(db, "Mi Empresa");

        var service = BuildService(db, MakeSuperuser());
        var (_, id) = await service.CreateEmpresarialAsync(
            BuildEmpresarialRequest(clasificId, jefeId, [instId]));

        var dto = await service.GetEmpresarialByIdAsync(id!);

        dto.ShouldNotBeNull();
        dto!.Titulo.ShouldBe("Proyecto Empresarial Test");
        dto.Empresas.ShouldContain(e => e.Id == instId);
    }

    [Test]
    public async Task GetEmpresarialByIdAsync_WithParticipantes_ReturnsDtoWithParticipantes()
    {
        await using var db = CreateDb();
        var (_, clasificId, jefeId) = SeedBase(db);
        var instId = SeedInstitution(db);
        var partId = SeedParticipante(db, "part-dto-1", "Carlos");

        var service = BuildService(db, MakeSuperuser());
        var (_, id) = await service.CreateEmpresarialAsync(
            BuildEmpresarialRequest(clasificId, jefeId, [instId], [partId]));

        var dto = await service.GetEmpresarialByIdAsync(id!);

        dto.ShouldNotBeNull();
        dto!.Participantes.Count.ShouldBe(2); // jefe + partId
        dto.Participantes.ShouldContain(p => p.Id == jefeId);
        dto.Participantes.ShouldContain(p => p.Id == partId);
    }

    // ── GetEnRevisionByIdAsync with data ─────────────────────────────────────

    [Test]
    public async Task GetEnRevisionByIdAsync_WithData_ReturnsDto()
    {
        await using var db = CreateDb();
        var (_, clasificId, jefeId) = SeedBase(db);

        var service = BuildService(db, MakeSuperuser());
        var proyecto = new ProyectoEnRevision
        {
            Id = "rev-get-1",
            Titulo = "Proyecto Para Leer",
            JefeId = jefeId,
            ClasificacionId = clasificId,
            NumeroMiembros = 1,
            Tipo = "PE"
        };
        db.Proyectos.Add(proyecto);
        await db.SaveChangesAsync();

        var dto = await service.GetEnRevisionByIdAsync("rev-get-1");

        dto.ShouldNotBeNull();
        dto!.Titulo.ShouldBe("Proyecto Para Leer");
    }

    // ── GetAllAsync with data ─────────────────────────────────────────────────

    [Test]
    public async Task GetAllAsync_WithMultipleTypes_ReturnsAll()
    {
        await using var db = CreateDb();
        var (_, clasificId, jefeId) = SeedBase(db);

        var service = BuildService(db, MakeSuperuser());

        await service.CreateEnRevisionAsync(new ProyectoEnRevisionUpsertRequest
        {
            Titulo = "En Revisión",
            JefeId = jefeId,
            ClasificacionId = clasificId,
            NumeroMiembros = 1,
            CantidadMiembrosUH = 1,
            Tipo = "PE"
        });
        await service.CreateEmpresarialAsync(BuildEmpresarialRequest(clasificId, jefeId));

        var result = await service.GetAllAsync();
        result.Count.ShouldBe(2);
    }

    // ── GetCatalogoAsync with data ────────────────────────────────────────────

    [Test]
    public async Task GetCatalogoAsync_WithData_ReturnsCatalogDtos()
    {
        await using var db = CreateDb();
        var (_, clasificId, jefeId) = SeedBase(db);

        var service = BuildService(db, MakeSuperuser());
        await service.CreateEmpresarialAsync(BuildEmpresarialRequest(clasificId, jefeId));

        var result = await service.GetCatalogoAsync();

        result.Count.ShouldBe(1);
        result[0].Titulo.ShouldBe("Proyecto Empresarial Test");
    }

    // ── GetPublicacionesDelProyectoAsync ──────────────────────────────────────

    [Test]
    public async Task GetPublicacionesDelProyectoAsync_WithData_ReturnsPublicacionDtos()
    {
        await using var db = CreateDb();
        var (_, clasificId, jefeId) = SeedBase(db);

        var service = BuildService(db, MakeSuperuser());
        var (_, proyId) = await service.CreateEmpresarialAsync(BuildEmpresarialRequest(clasificId, jefeId));

        var pub = new Publication
        {
            Id = "pub-proj-1",
            Title = "Paper del Proyecto",
            NormalizedTitle = "paper del proyecto",
            PublishedDate = "2024",
            PublicationData = "{}",
            ProyectoId = proyId,
            AuthorPublications = new List<AuthorPublication>()
        };
        db.Publications.Add(pub);
        await db.SaveChangesAsync();

        var result = await service.GetPublicacionesDelProyectoAsync(proyId!);

        result.Count.ShouldBe(1);
        result[0].Title.ShouldBe("Paper del Proyecto");
    }

    // ── GetApoyoProgramaByIdAsync with data ───────────────────────────────────

    [Test]
    public async Task GetApoyoProgramaByIdAsync_WithData_ReturnsDto()
    {
        await using var db = CreateDb();
        var (_, clasificId, jefeId) = SeedBase(db);
        var programa = new Programa { Nombre = "PN de Biotecnología" };
        db.Programas.Add(programa);
        await db.SaveChangesAsync();

        var service = BuildService(db, MakeSuperuser());
        var (_, id) = await service.CreateApoyoProgramaAsync(new ProyectoApoyoProgramaUpsertRequest
        {
            Titulo = "PAP Test",
            JefeId = jefeId,
            ClasificacionId = clasificId,
            NumeroMiembros = 2,
            CantidadMiembrosUH = 1,
            FechaInicio = DateOnly.FromDateTime(DateTime.Today),
            CodigoProyecto = "PAP-99",
            ProgramasIds = [programa.Id],
            TipoPAP = TipoPAP.Sectorial
        });

        var dto = await service.GetApoyoProgramaByIdAsync(id!);

        dto.ShouldNotBeNull();
        dto!.Titulo.ShouldBe("PAP Test");
        dto.Programas.ShouldContain(p => p.Nombre == "PN de Biotecnología");
    }

    // ── CreateNoEmpresarialAsync ──────────────────────────────────────────────

    [Test]
    public async Task CreateNoEmpresarialAsync_Succeeds()
    {
        await using var db = CreateDb();
        var (_, clasificId, jefeId) = SeedBase(db);
        var entidad = new Institution { Nombre = "Centro de Investigaciones" };
        db.Institutions.Add(entidad);
        await db.SaveChangesAsync();

        var service = BuildService(db, MakeSuperuser());
        var (result, id) = await service.CreateNoEmpresarialAsync(new ProyectoNoEmpresarialUpsertRequest
        {
            Titulo = "No Empresarial",
            JefeId = jefeId,
            ClasificacionId = clasificId,
            NumeroMiembros = 4,
            CantidadMiembrosUH = 3,
            FechaInicio = DateOnly.FromDateTime(DateTime.Today),
            CodigoProyecto = "NE-001",
            EntidadesIds = [entidad.Id]
        });

        result.Succeeded.ShouldBeTrue();
        var created = await db.Proyectos.OfType<ProyectoNoEmpresarial>()
            .Include(p => p.Entidades)
            .FirstOrDefaultAsync(p => p.Id == id);
        created!.Entidades.ShouldContain(e => e.Id == entidad.Id);
    }

    // ── CreateColabInternacionalAsync ─────────────────────────────────────────

    [Test]
    public async Task CreateColabInternacionalAsync_Succeeds()
    {
        await using var db = CreateDb();
        var (_, clasificId, jefeId) = SeedBase(db);
        var fuente = new FuenteFinanciacion { Nombre = "EU Horizon" };
        db.FuentesFinanciacion.Add(fuente);
        await db.SaveChangesAsync();

        var service = BuildService(db, MakeSuperuser());
        var (result, id) = await service.CreateColabInternacionalAsync(new ProyectoColabInternacionalUpsertRequest
        {
            Titulo = "Colab Internacional",
            JefeId = jefeId,
            ClasificacionId = clasificId,
            NumeroMiembros = 6,
            CantidadMiembrosUH = 4,
            FechaInicio = DateOnly.FromDateTime(DateTime.Today),
            CodigoProyecto = "CI-001",
            FuentesFinanciacionIds = [fuente.Id],
            TerminosReferencia = "Colaboración sur-sur"
        });

        result.Succeeded.ShouldBeTrue();
        var created = await db.Proyectos.OfType<ProyectoColabInternacional>()
            .Include(p => p.FuentesFinanciacion)
            .FirstOrDefaultAsync(p => p.Id == id);
        created!.FuentesFinanciacion.ShouldContain(f => f.Id == fuente.Id);
        created.TerminosReferencia.ShouldBe("Colaboración sur-sur");
    }

    // ── CreatePNAPAsync ───────────────────────────────────────────────────────

    [Test]
    public async Task CreatePNAPAsync_Succeeds()
    {
        await using var db = CreateDb();
        var (_, clasificId, jefeId) = SeedBase(db);
        var fuente = new FuenteFinanciacion { Nombre = "Presupuesto UH" };
        db.FuentesFinanciacion.Add(fuente);
        await db.SaveChangesAsync();

        var service = BuildService(db, MakeSuperuser());
        var (result, id) = await service.CreatePNAPAsync(new ProyectoPNAPUpsertRequest
        {
            Titulo = "PNAP Test",
            JefeId = jefeId,
            ClasificacionId = clasificId,
            NumeroMiembros = 5,
            CantidadMiembrosUH = 3,
            FechaInicio = DateOnly.FromDateTime(DateTime.Today),
            CodigoProyecto = "PNAP-001",
            FuentesFinanciacionIds = [fuente.Id]
        });

        result.Succeeded.ShouldBeTrue();
        var created = await db.Proyectos.OfType<ProyectoPNAP>()
            .Include(p => p.FuentesFinanciacion)
            .FirstOrDefaultAsync(p => p.Id == id);
        created!.FuentesFinanciacion.ShouldContain(f => f.Id == fuente.Id);
    }
}
