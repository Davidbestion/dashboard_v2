using Dashboard_v2.Domain.Entities;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Domain.UnitTests.Entities;

/// <summary>
/// Cubre entidades POCO simples (join tables y Resource) que no tienen lógica
/// de negocio pero cuyas propiedades deben ser ejercitadas para cobertura.
/// </summary>
[TestFixture]
public class SimpleEntityTests
{
    // ─── AuthorPatente ────────────────────────────────────────────────────────

    [Test]
    public void AuthorPatente_Properties_SetAndRead()
    {
        var entity = new AuthorPatente
        {
            AuthorId = "a-1",
            PatenteId = "p-1"
        };

        entity.AuthorId.ShouldBe("a-1");
        entity.PatenteId.ShouldBe("p-1");
    }

    // ─── AuthorRegistro ───────────────────────────────────────────────────────

    [Test]
    public void AuthorRegistro_Properties_SetAndRead()
    {
        var entity = new AuthorRegistro
        {
            AuthorId = "a-2",
            RegistroId = "r-2"
        };

        entity.AuthorId.ShouldBe("a-2");
        entity.RegistroId.ShouldBe("r-2");
    }

    // ─── AuthorProductoComercializado ─────────────────────────────────────────

    [Test]
    public void AuthorProductoComercializado_Properties_SetAndRead()
    {
        var entity = new AuthorProductoComercializado
        {
            AuthorId = "a-3",
            ProductoComercializadoId = "pc-3"
        };

        entity.AuthorId.ShouldBe("a-3");
        entity.ProductoComercializadoId.ShouldBe("pc-3");
    }

}
