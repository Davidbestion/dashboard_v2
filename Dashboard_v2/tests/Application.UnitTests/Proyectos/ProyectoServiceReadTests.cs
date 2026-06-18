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
/// Tests for read-only ProyectoService methods.
/// </summary>
[TestFixture]
public class ProyectoServiceReadTests
{
    private ApplicationDbContext _db = null!;
    private ProyectoService _sut = null!;
    private string _areaId = "area-1";
    private string _clasificId = "clasif-1";
    private string _jefeId = "jefe-1";

    [SetUp]
    public void SetUp()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(opts);

        var jefe = new User
        {
            Id = _jefeId,
            UserName = "jefe",
            UserLastName1 = "Perez",
            Email = "jefe@uh.cu",
            PasswordHash = "hash",
            IsActive = true,
            BirthDate = DateTime.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UserRoles = new List<UserRole> { new() { UserId = _jefeId, Role = RolesEnum.Jefe_de_Proyecto } }
        };
        _db.Areas.Add(new Area { Id = _areaId, Nombre = "MATCOM", Descripcion = "d", UniversidadId = "uh" });
        _db.Clasificaciones.Add(new Clasificacion { Id = _clasificId, Nombre = "Básica" });
        _db.Users.Add(jefe);
        _db.SaveChanges();

        var validationMock = new Mock<IRequestValidationService>();
        validationMock.Setup(v => v.ValidateAndThrowAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var userMock = new Mock<IUser>();
        userMock.Setup(u => u.Id).Returns("super-1");
        userMock.Setup(u => u.Roles).Returns(new List<string> { nameof(RolesEnum.Superuser) });

        _sut = new ProyectoService(_db, userMock.Object, validationMock.Object);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Test]
    public async Task GetAllAsync_Empty_ReturnsEmpty()
    {
        var result = await _sut.GetAllAsync();
        result.ShouldBeEmpty();
    }

    // ── GetAreaProyectosAsync ─────────────────────────────────────────────────

    private ProyectoService MakeServiceForUser(string userId)
    {
        var userMock = new Mock<IUser>();
        userMock.Setup(u => u.Id).Returns(userId);
        var valMock = new Mock<IRequestValidationService>();
        valMock.Setup(v => v.ValidateAndThrowAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return new ProyectoService(_db, userMock.Object, valMock.Object);
    }

    [Test]
    public async Task GetAreaProyectosAsync_Empty_ReturnsEmpty()
    {
        _db.Users.Add(new User { Id = "vice-1", UserName = "vice1", UserLastName1 = "V", Email = "v1@uh.cu", AreaId = _areaId });
        await _db.SaveChangesAsync();

        var result = await MakeServiceForUser("vice-1").GetAreaProyectosAsync();
        result.ShouldBeEmpty();
    }

    [Test]
    public async Task GetAreaProyectosAsync_ReturnsProjectsWhoseJefeIsInSameArea()
    {
        var jefe = await _db.Users.FindAsync(_jefeId);
        jefe!.AreaId = _areaId;
        _db.Users.Add(new User { Id = "vice-2", UserName = "vice2", UserLastName1 = "V", Email = "v2@uh.cu", AreaId = _areaId });
        _db.Proyectos.Add(new ProyectoEnRevision { Id = "p-area-1", Titulo = "Proyecto del Área", JefeId = _jefeId, ClasificacionId = _clasificId, Tipo = "PE" });
        await _db.SaveChangesAsync();

        var result = await MakeServiceForUser("vice-2").GetAreaProyectosAsync();

        result.Count.ShouldBe(1);
        result[0].Titulo.ShouldBe("Proyecto del Área");
    }

    [Test]
    public async Task GetAreaProyectosAsync_ExcludesProjectsFromOtherAreas()
    {
        var otherAreaId = "area-other";
        _db.Areas.Add(new Area { Id = otherAreaId, Nombre = "Otra", Descripcion = "d", UniversidadId = "uh" });
        var jefe = await _db.Users.FindAsync(_jefeId);
        jefe!.AreaId = otherAreaId;
        _db.Users.Add(new User { Id = "vice-3", UserName = "vice3", UserLastName1 = "V", Email = "v3@uh.cu", AreaId = _areaId });
        _db.Proyectos.Add(new ProyectoEnRevision { Id = "p-other-1", Titulo = "Proyecto de Otra Área", JefeId = _jefeId, ClasificacionId = _clasificId, Tipo = "PE" });
        await _db.SaveChangesAsync();

        var result = await MakeServiceForUser("vice-3").GetAreaProyectosAsync();

        result.ShouldBeEmpty();
    }

    [Test]
    public async Task GetAreaProyectosAsync_ReturnsProjectsWhereParticipanteIsInSameArea()
    {
        _db.Users.Add(new User { Id = "vice-4", UserName = "vice4", UserLastName1 = "V", Email = "v4@uh.cu", AreaId = _areaId });
        var participante = new User { Id = "part-area-1", UserName = "part", UserLastName1 = "P", Email = "p@uh.cu", AreaId = _areaId };
        _db.Users.Add(participante);
        await _db.SaveChangesAsync();

        var proyecto = new ProyectoEnRevision { Id = "p-part-1", Titulo = "Proyecto con Participante", JefeId = _jefeId, ClasificacionId = _clasificId, Tipo = "PE" };
        _db.Proyectos.Add(proyecto);
        await _db.SaveChangesAsync();
        proyecto.Participantes.Add(participante);
        await _db.SaveChangesAsync();

        var result = await MakeServiceForUser("vice-4").GetAreaProyectosAsync();

        result.Count.ShouldBe(1);
        result[0].Titulo.ShouldBe("Proyecto con Participante");
    }

    // ── GetCatalogoAsync ──────────────────────────────────────────────────────

    [Test]
    public async Task GetCatalogoAsync_Empty_ReturnsEmpty()
    {
        var result = await _sut.GetCatalogoAsync();
        result.ShouldBeEmpty();
    }

    // ── GetTiposEjecucionAsync ────────────────────────────────────────────────

    [Test]
    public async Task GetTiposEjecucionAsync_ReturnsNonEmpty()
    {
        var result = await _sut.GetTiposEjecucionAsync();
        result.ShouldNotBeEmpty();
    }

    // ── GetPublicacionesDelProyectoAsync ──────────────────────────────────────

    [Test]
    public async Task GetPublicacionesDelProyectoAsync_NoPublications_ReturnsEmpty()
    {
        var result = await _sut.GetPublicacionesDelProyectoAsync("any-project-id");
        result.ShouldBeEmpty();
    }

    // ── GetPublicacionesDisponiblesAsync ──────────────────────────────────────

    [Test]
    public async Task GetPublicacionesDisponiblesAsync_Empty_ReturnsEmpty()
    {
        var result = await _sut.GetPublicacionesDisponiblesAsync();
        result.ShouldBeEmpty();
    }

    // ── GetXByIdAsync – not found ─────────────────────────────────────────────

    [Test]
    public async Task GetEnRevisionByIdAsync_NotFound_ReturnsNull()
        => (await _sut.GetEnRevisionByIdAsync("nonexistent")).ShouldBeNull();

    [Test]
    public async Task GetEmpresarialByIdAsync_NotFound_ReturnsNull()
        => (await _sut.GetEmpresarialByIdAsync("nonexistent")).ShouldBeNull();

    [Test]
    public async Task GetApoyoProgramaByIdAsync_NotFound_ReturnsNull()
        => (await _sut.GetApoyoProgramaByIdAsync("nonexistent")).ShouldBeNull();

    [Test]
    public async Task GetDesarrolloLocalByIdAsync_NotFound_ReturnsNull()
        => (await _sut.GetDesarrolloLocalByIdAsync("nonexistent")).ShouldBeNull();

    [Test]
    public async Task GetNoEmpresarialByIdAsync_NotFound_ReturnsNull()
        => (await _sut.GetNoEmpresarialByIdAsync("nonexistent")).ShouldBeNull();

    [Test]
    public async Task GetColabInternacionalByIdAsync_NotFound_ReturnsNull()
        => (await _sut.GetColabInternacionalByIdAsync("nonexistent")).ShouldBeNull();

    [Test]
    public async Task GetPNAPByIdAsync_NotFound_ReturnsNull()
        => (await _sut.GetPNAPByIdAsync("nonexistent")).ShouldBeNull();

    // ── UpdateXAsync – not found ──────────────────────────────────────────────

    [Test]
    public async Task UpdateEnRevisionAsync_NotFound_ReturnsFailure()
    {
        var request = new ProyectoEnRevisionUpsertRequest
        {
            Titulo = "T",
            JefeId = _jefeId,
            ClasificacionId = _clasificId,
            NumeroMiembros = 1,
            CantidadMiembrosUH = 1,
            Tipo = "PE"
        };
        var result = await _sut.UpdateEnRevisionAsync("nonexistent", request);
        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("no encontrado"));
    }

    [Test]
    public async Task UpdateEmpresarialAsync_NotFound_ReturnsFailure()
    {
        var request = new ProyectoEmpresarialUpsertRequest
        {
            Titulo = "T",
            JefeId = _jefeId,
            ClasificacionId = _clasificId,
            NumeroMiembros = 1,
            CantidadMiembrosUH = 1,
            FechaInicio = DateOnly.FromDateTime(DateTime.Today),
            CodigoProyecto = "PE-001"
        };
        var result = await _sut.UpdateEmpresarialAsync("nonexistent", request);
        result.Succeeded.ShouldBeFalse();
    }

    // ── GetColabInternacionalByIdAsync with data ──────────────────────────────

    [Test]
    public async Task GetColabInternacionalByIdAsync_WithData_ReturnsDto()
    {
        var fuente = new FuenteFinanciacion { Nombre = "EU Horizon" };
        _db.FuentesFinanciacion.Add(fuente);
        await _db.SaveChangesAsync();

        var (result, id) = await _sut.CreateColabInternacionalAsync(new ProyectoColabInternacionalUpsertRequest
        {
            Titulo = "Colab Test",
            JefeId = _jefeId,
            ClasificacionId = _clasificId,
            NumeroMiembros = 3,
            CantidadMiembrosUH = 2,
            FechaInicio = DateOnly.FromDateTime(DateTime.Today),
            CodigoProyecto = "CI-001",
            FuentesFinanciacionIds = [fuente.Id],
            TerminosReferencia = "TDR 2024"
        });
        result.Succeeded.ShouldBeTrue();

        var dto = await _sut.GetColabInternacionalByIdAsync(id!);
        dto.ShouldNotBeNull();
        dto!.FuentesFinanciacion.ShouldContain(f => f.Nombre == "EU Horizon");
        dto.TerminosReferencia.ShouldBe("TDR 2024");
    }

    // ── GetDesarrolloLocalByIdAsync with data ─────────────────────────────────

    [Test]
    public async Task GetDesarrolloLocalByIdAsync_WithData_ReturnsDto()
    {
        var provincia = new Provincia { Nombre = "La Habana" };
        _db.Provincias.Add(provincia);
        await _db.SaveChangesAsync();
        var municipio = new Municipio { Nombre = "Plaza de la Revolución", ProvinciaId = provincia.Id };
        _db.Municipios.Add(municipio);
        await _db.SaveChangesAsync();

        var (result, id) = await _sut.CreateDesarrolloLocalAsync(new ProyectoDesarrolloLocalUpsertRequest
        {
            Titulo = "Desarrollo Local Test",
            JefeId = _jefeId,
            ClasificacionId = _clasificId,
            NumeroMiembros = 3,
            CantidadMiembrosUH = 2,
            FechaInicio = DateOnly.FromDateTime(DateTime.Today),
            CodigoProyecto = "DL-001",
            MunicipioId = municipio.Id
        });
        result.Succeeded.ShouldBeTrue();

        var dto = await _sut.GetDesarrolloLocalByIdAsync(id!);
        dto.ShouldNotBeNull();
        dto!.MunicipioId.ShouldBe(municipio.Id);
        dto.MunicipioNombre.ShouldBe("Plaza de la Revolución");
        dto.ProvinciaNombre.ShouldBe("La Habana");
    }

    // ── GetNoEmpresarialByIdAsync with data ───────────────────────────────────

    [Test]
    public async Task GetNoEmpresarialByIdAsync_WithData_ReturnsDto()
    {
        var entidad = new Institution { Nombre = "Ministerio de Ciencia" };
        _db.Institutions.Add(entidad);
        await _db.SaveChangesAsync();

        var (result, id) = await _sut.CreateNoEmpresarialAsync(new ProyectoNoEmpresarialUpsertRequest
        {
            Titulo = "No Empresarial Test",
            JefeId = _jefeId,
            ClasificacionId = _clasificId,
            NumeroMiembros = 2,
            CantidadMiembrosUH = 1,
            FechaInicio = DateOnly.FromDateTime(DateTime.Today),
            CodigoProyecto = "NE-001",
            EntidadesIds = [entidad.Id]
        });
        result.Succeeded.ShouldBeTrue();

        var dto = await _sut.GetNoEmpresarialByIdAsync(id!);
        dto.ShouldNotBeNull();
        dto!.Entidades.ShouldContain(e => e.Nombre == "Ministerio de Ciencia");
    }

    // ── GetPNAPByIdAsync with data ────────────────────────────────────────────

    [Test]
    public async Task GetPNAPByIdAsync_WithData_ReturnsDto()
    {
        var fuente = new FuenteFinanciacion { Nombre = "Presupuesto UH" };
        _db.FuentesFinanciacion.Add(fuente);
        await _db.SaveChangesAsync();

        var (result, id) = await _sut.CreatePNAPAsync(new ProyectoPNAPUpsertRequest
        {
            Titulo = "PNAP Test",
            JefeId = _jefeId,
            ClasificacionId = _clasificId,
            NumeroMiembros = 4,
            CantidadMiembrosUH = 3,
            FechaInicio = DateOnly.FromDateTime(DateTime.Today),
            CodigoProyecto = "PNAP-001",
            FuentesFinanciacionIds = [fuente.Id]
        });
        result.Succeeded.ShouldBeTrue();

        var dto = await _sut.GetPNAPByIdAsync(id!);
        dto.ShouldNotBeNull();
        dto!.FuentesFinanciacion.ShouldContain(f => f.Nombre == "Presupuesto UH");
    }
}
