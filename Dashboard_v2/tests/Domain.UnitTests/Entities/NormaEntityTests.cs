using Dashboard_v2.Domain.Entities;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Domain.UnitTests.Entities;

/// <summary>
/// Cubre las entidades Norma, TipoNorma y AuthorNorma.
/// Verifica especialmente que TipoNormaId puede ser null (refactoring de tipo string a FK nullable).
/// </summary>
[TestFixture]
public class NormaEntityTests
{
    // ── Norma: valores por defecto ────────────────────────────────────────────

    [Test]
    public void Norma_DefaultId_IsValidGuid()
    {
        var norma = new Norma { Titulo = "ISO 9001", InstitutionId = "inst-1" };

        Guid.TryParse(norma.Id, out _).ShouldBeTrue();
    }

    [Test]
    public void Norma_EachInstance_GetsUniqueId()
    {
        var n1 = new Norma { Titulo = "A", InstitutionId = "i1" };
        var n2 = new Norma { Titulo = "B", InstitutionId = "i1" };

        n1.Id.ShouldNotBe(n2.Id);
    }

    [Test]
    public void Norma_DefaultTipoNormaId_IsNull()
    {
        var norma = new Norma { Titulo = "Sin tipo", InstitutionId = "inst-1" };

        norma.TipoNormaId.ShouldBeNull();
        norma.TipoNorma.ShouldBeNull();
    }

    [Test]
    public void Norma_DefaultCreadores_IsEmptyNotNull()
    {
        var norma = new Norma { Titulo = "T", InstitutionId = "i1" };

        norma.Creadores.ShouldNotBeNull();
        norma.Creadores.ShouldBeEmpty();
    }

    // ── Norma: asignación de propiedades ──────────────────────────────────────

    [Test]
    public void Norma_CanAssignTipoNormaId()
    {
        var norma = new Norma
        {
            Titulo = "ISO 9001",
            TipoNormaId = 3,
            InstitutionId = "inst-1"
        };

        norma.TipoNormaId.ShouldBe(3);
    }

    [Test]
    public void Norma_CanAssignNullTipoNormaId()
    {
        var norma = new Norma
        {
            Titulo = "Norma sin tipo",
            TipoNormaId = null,
            InstitutionId = "inst-1"
        };

        norma.TipoNormaId.ShouldBeNull();
    }

    [Test]
    public void Norma_CanAssignAllScalarProperties()
    {
        var norma = new Norma
        {
            Id = "fixed-norma",
            Titulo = "Resolución ministerial 001/2024",
            TipoNormaId = 7,
            InstitutionId = "inst-42",
        };

        norma.Id.ShouldBe("fixed-norma");
        norma.Titulo.ShouldBe("Resolución ministerial 001/2024");
        norma.TipoNormaId.ShouldBe(7);
        norma.InstitutionId.ShouldBe("inst-42");
    }

    // ── TipoNorma ──────────────────────────────────────────────────────────────

    [Test]
    public void TipoNorma_CanAssignIdAndNombre()
    {
        var tipo = new TipoNorma { Id = 1, Nombre = "ISO" };

        tipo.Id.ShouldBe(1);
        tipo.Nombre.ShouldBe("ISO");
    }

    [Test]
    public void TipoNorma_DifferentInstances_HaveIndependentIds()
    {
        var t1 = new TipoNorma { Id = 1, Nombre = "ISO" };
        var t2 = new TipoNorma { Id = 2, Nombre = "IRAM" };

        t1.Id.ShouldNotBe(t2.Id);
    }

    // ── AuthorNorma ────────────────────────────────────────────────────────────

    [Test]
    public void AuthorNorma_Properties_SetAndRead()
    {
        var an = new AuthorNorma
        {
            AuthorId = "user-1",
            NormaId = "norma-1"
        };

        an.AuthorId.ShouldBe("user-1");
        an.NormaId.ShouldBe("norma-1");
    }
}
