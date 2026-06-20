using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Documents.Reports;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Domain.Enums;
using Dashboard_v2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.Documents.Reports;

[TestFixture]
public class AnexoRedesUniversitariasReportTests
{
    private ApplicationDbContext _db = null!;
    private Mock<IDocumentRenderer> _rendererMock = null!;
    private AnexoRedesUniversitariasReport _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);
        _rendererMock = new Mock<IDocumentRenderer>();
        _rendererMock
            .Setup(r => r.Render(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, object>>()))
            .Returns(new byte[] { 1, 2, 3 });
        _sut = new AnexoRedesUniversitariasReport(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public void ReportName_ReturnsExpected()
    {
        _sut.ReportName.ShouldBe("anexo-redes-universitarias");
    }

    [Test]
    public async Task GenerateAsync_NoReds_ReturnsEmptyZip()
    {
        var bytes = await _sut.GenerateAsync(_rendererMock.Object);
        bytes.ShouldNotBeNull();
        bytes.Length.ShouldBeGreaterThan(0); // valid (empty) zip
        _rendererMock.Verify(r => r.Render(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, object>>()), Times.Never);
    }

    [Test]
    public async Task GenerateAsync_WithUniversitariaRed_CallsRendererOnce()
    {
        _db.Reds.Add(new Red { Nombre = "Red Univ 1", Tipo = TipoRed.Universitaria });
        await _db.SaveChangesAsync();

        await _sut.GenerateAsync(_rendererMock.Object);
        _rendererMock.Verify(r => r.Render("AnexoRedUniversitaria", It.IsAny<IReadOnlyDictionary<string, object>>()), Times.Once);
    }

    [Test]
    public async Task GenerateAsync_SkipsNonUniversitariaReds()
    {
        _db.Reds.Add(new Red { Nombre = "Red Nac", Tipo = TipoRed.Nacional });
        _db.Reds.Add(new Red { Nombre = "Red Inter", Tipo = TipoRed.Internacional });
        await _db.SaveChangesAsync();

        await _sut.GenerateAsync(_rendererMock.Object);
        _rendererMock.Verify(r => r.Render(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, object>>()), Times.Never);
    }

    [Test]
    public async Task GenerateAsync_MultipleReds_CallsRendererForEach()
    {
        _db.Reds.Add(new Red { Nombre = "Red Univ 1", Tipo = TipoRed.Universitaria });
        _db.Reds.Add(new Red { Nombre = "Red Univ 2", Tipo = TipoRed.Universitaria });
        await _db.SaveChangesAsync();

        await _sut.GenerateAsync(_rendererMock.Object);
        _rendererMock.Verify(r => r.Render("AnexoRedUniversitaria", It.IsAny<IReadOnlyDictionary<string, object>>()), Times.Exactly(2));
    }

    [Test]
    public async Task GenerateAsync_ReturnsValidZipBytes()
    {
        _db.Reds.Add(new Red { Nombre = "Red Univ 1", Tipo = TipoRed.Universitaria });
        await _db.SaveChangesAsync();

        var result = await _sut.GenerateAsync(_rendererMock.Object);
        // ZIP magic bytes: PK (0x50 0x4B)
        result[0].ShouldBe((byte)0x50);
        result[1].ShouldBe((byte)0x4B);
    }

    // ── BuildVariables — eventos reales ───────────────────────────────────────

    [Test]
    public async Task GenerateAsync_WithEvents_PopulatesEventosList()
    {
        var red = new Red { Nombre = "Red Univ Events", Tipo = TipoRed.Universitaria };
        _db.Reds.Add(red);
        var country = new Country { Name = "Cuba" };
        _db.Countries.Add(country);
        _db.EventTypes.Add(new EventType { Id = 1, Name = "Congreso" });
        await _db.SaveChangesAsync();

        _db.Events.Add(new Event
        {
            Name = "Congreso Universitario",
            CountryId = country.Id,
            EventTypeId = 1,
            RedId = red.Id
        });
        await _db.SaveChangesAsync();

        IReadOnlyDictionary<string, object>? captured = null;
        _rendererMock
            .Setup(r => r.Render(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, object>>()))
            .Callback<string, IReadOnlyDictionary<string, object>>((_, vars) => captured = vars)
            .Returns(new byte[] { 1, 2, 3 });

        await _sut.GenerateAsync(_rendererMock.Object);

        captured.ShouldNotBeNull();
        var eventos = (List<AnexoEventoRedRowDto>)captured!["EventosRed"];
        eventos.Count.ShouldBe(1);
        eventos[0].Nombre.ShouldBe("Congreso Universitario");
        eventos[0].FechaLugar.ShouldBe("Cuba");
    }

    [Test]
    public async Task GenerateAsync_NoEvents_FallsBackToEmptyRow()
    {
        _db.Reds.Add(new Red { Nombre = "Red Sin Eventos", Tipo = TipoRed.Universitaria });
        await _db.SaveChangesAsync();

        IReadOnlyDictionary<string, object>? captured = null;
        _rendererMock
            .Setup(r => r.Render(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, object>>()))
            .Callback<string, IReadOnlyDictionary<string, object>>((_, vars) => captured = vars)
            .Returns(new byte[] { 1, 2, 3 });

        await _sut.GenerateAsync(_rendererMock.Object);

        captured.ShouldNotBeNull();
        var eventos = (List<AnexoEventoRedRowDto>)captured!["EventosRed"];
        eventos.Count.ShouldBe(1);
        eventos[0].Nombre.ShouldBe(string.Empty);
    }

    [Test]
    public async Task GenerateAsync_WithParticipaciones_PopulatesAreasParticipantes()
    {
        var red = new Red { Nombre = "Red Con Áreas", Tipo = TipoRed.Universitaria };
        _db.Reds.Add(red);
        var area = new Domain.Entities.Area { Id = "area-rc-1", Nombre = "Facultad de Matemáticas" };
        _db.Areas.Add(area);
        var user = new Domain.Entities.User
        {
            Id = "user-p-1", UserName = "prof", UserLastName1 = "P", Email = "p@test.cu",
            AreaId = area.Id
        };
        _db.Users.Add(user);
        var author = Dashboard_v2.Domain.Entities.Author.Create("Participante");
        author.UserId = user.Id;
        _db.Authors.Add(author);
        await _db.SaveChangesAsync();

        _db.ParticipacionesEnRed.Add(new Domain.Entities.ParticipacionEnRed
        {
            RedId = red.Id,
            AuthorId = author.Id
        });
        await _db.SaveChangesAsync();

        IReadOnlyDictionary<string, object>? captured = null;
        _rendererMock
            .Setup(r => r.Render(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, object>>()))
            .Callback<string, IReadOnlyDictionary<string, object>>((_, vars) => captured = vars)
            .Returns(new byte[] { 1, 2, 3 });

        await _sut.GenerateAsync(_rendererMock.Object);

        captured.ShouldNotBeNull();
        var areasParticipantes = (List<AnexoAreaParticipanteRowDto>)captured!["AreasParticipantes"];
        areasParticipantes.Count.ShouldBe(1);
        areasParticipantes[0].AreaUH.ShouldBe("Facultad de Matemáticas");
    }

    [Test]
    public async Task GenerateAsync_NombreRed_IsIncludedInVariables()
    {
        _db.Reds.Add(new Red { Nombre = "Mi Red Universitaria", Tipo = TipoRed.Universitaria });
        await _db.SaveChangesAsync();

        IReadOnlyDictionary<string, object>? captured = null;
        _rendererMock
            .Setup(r => r.Render(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, object>>()))
            .Callback<string, IReadOnlyDictionary<string, object>>((_, vars) => captured = vars)
            .Returns(new byte[] { 1, 2, 3 });

        await _sut.GenerateAsync(_rendererMock.Object);

        captured.ShouldNotBeNull();
        captured!["NombreRed"].ShouldBe("Mi Red Universitaria");
    }
}
