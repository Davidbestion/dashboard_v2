using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Domain.Enums;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Domain.UnitTests.Redes;

public class RedEntityTests
{
    // ── Red: valores por defecto ─────────────────────────────────────────────

    [Test]
    public void Red_DefaultId_IsValidGuid()
    {
        var red = new Red { Nombre = "Test" };

        Guid.TryParse(red.Id, out _).ShouldBeTrue();
    }

    [Test]
    public void Red_DefaultTipo_IsUniversitaria()
    {
        var red = new Red { Nombre = "Test" };

        red.Tipo.ShouldBe(TipoRed.Universitaria);
    }

    [Test]
    public void Red_DefaultCantidadProfesores_IsZero()
    {
        var red = new Red { Nombre = "Test" };

        red.CantidadProfesores.ShouldBe(0);
    }

    [Test]
    public void Red_DefaultCountryId_IsNull()
    {
        var red = new Red { Nombre = "Test" };

        red.CountryId.ShouldBeNull();
    }

    [Test]
    public void Red_DefaultCoordinadorId_IsNull()
    {
        var red = new Red { Nombre = "Test" };

        red.CoordinadorId.ShouldBeNull();
    }

    [Test]
    public void Red_DefaultEvents_IsEmptyCollection()
    {
        var red = new Red { Nombre = "Test" };

        red.Events.ShouldNotBeNull();
        red.Events.ShouldBeEmpty();
    }

    [Test]
    public void Red_DefaultParticipaciones_IsEmptyCollection()
    {
        var red = new Red { Nombre = "Test" };

        red.Participaciones.ShouldNotBeNull();
        red.Participaciones.ShouldBeEmpty();
    }

    // ── Red: asignación de propiedades ───────────────────────────────────────

    [TestCase(TipoRed.Universitaria)]
    [TestCase(TipoRed.Nacional)]
    [TestCase(TipoRed.Internacional)]
    public void Red_Tipo_CanBeSetToAnyValidValue(TipoRed tipo)
    {
        var red = new Red { Nombre = "Test", Tipo = tipo };

        red.Tipo.ShouldBe(tipo);
    }

    [Test]
    public void Red_EachInstance_GetsUniqueId()
    {
        var red1 = new Red { Nombre = "A" };
        var red2 = new Red { Nombre = "B" };

        red1.Id.ShouldNotBe(red2.Id);
    }

    [Test]
    public void Red_CanAssignAllScalarProperties()
    {
        var red = new Red
        {
            Id = "fixed-id",
            Nombre = "Red de Prueba",
            Tipo = TipoRed.Nacional,
            CountryId = 5,
            CantidadProfesores = 42,
            CoordinadorId = "user-001",
        };

        red.Id.ShouldBe("fixed-id");
        red.Nombre.ShouldBe("Red de Prueba");
        red.Tipo.ShouldBe(TipoRed.Nacional);
        red.CountryId.ShouldBe(5);
        red.CantidadProfesores.ShouldBe(42);
        red.CoordinadorId.ShouldBe("user-001");
    }

    // ── ParticipacionEnRed ───────────────────────────────────────────────────

    [Test]
    public void ParticipacionEnRed_CanAssignForeignKeys()
    {
        var p = new ParticipacionEnRed
        {
            RedId = "red-001",
            AuthorId = "author-001",
        };

        p.RedId.ShouldBe("red-001");
        p.AuthorId.ShouldBe("author-001");
    }
}
