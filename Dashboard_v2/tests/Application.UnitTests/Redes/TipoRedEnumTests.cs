using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Domain.Enums;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.Redes;

public class TipoRedEnumTests
{
    // ── Valores del enum ────────────────────────────────────────────────────

    [TestCase(TipoRed.Universitaria, 0)]
    [TestCase(TipoRed.Nacional, 1)]
    [TestCase(TipoRed.Internacional, 2)]
    public void TipoRedHasExpectedIntValues(TipoRed tipo, int expectedValue)
    {
        ((int)tipo).ShouldBe(expectedValue);
    }

    [Test]
    public void TipoRedHasExactlyThreeValues()
    {
        Enum.GetValues<TipoRed>().Length.ShouldBe(3);
    }

    // ── Validación con Enum.IsDefined (lógica usada en el endpoint) ─────────

    [TestCase(0, true)]
    [TestCase(1, true)]
    [TestCase(2, true)]
    [TestCase(-1, false)]
    [TestCase(3, false)]
    [TestCase(99, false)]
    public void EnumIsDefinedReturnsExpectedResult(int value, bool expected)
    {
        Enum.IsDefined(typeof(TipoRed), value).ShouldBe(expected);
    }

    // ── Entidad Red: valor por defecto ──────────────────────────────────────

    [Test]
    public void RedDefaultTipoIsUniversitaria()
    {
        var red = new Red { Nombre = "Test" };

        red.Tipo.ShouldBe(TipoRed.Universitaria);
    }

    [TestCase(TipoRed.Universitaria)]
    [TestCase(TipoRed.Nacional)]
    [TestCase(TipoRed.Internacional)]
    public void RedAcceptsAllValidTipoValues(TipoRed tipo)
    {
        var red = new Red { Nombre = "Test", Tipo = tipo };

        red.Tipo.ShouldBe(tipo);
    }
}
