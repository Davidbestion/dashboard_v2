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

    // ─── Resource ────────────────────────────────────────────────────────────

    [Test]
    public void Resource_Properties_SetAndRead()
    {
        var now = DateTimeOffset.UtcNow;
        var resource = new Resource
        {
            Type = "Document",
            OwnerId = "user-1",
            Name = "Informe anual",
            Metadata = "{\"key\":\"value\"}",
            Created = now,
            CreatedBy = "admin",
            LastModified = now,
            LastModifiedBy = "admin"
        };

        resource.Type.ShouldBe("Document");
        resource.OwnerId.ShouldBe("user-1");
        resource.Name.ShouldBe("Informe anual");
        resource.Metadata.ShouldBe("{\"key\":\"value\"}");
        resource.Created.ShouldBe(now);
        resource.CreatedBy.ShouldBe("admin");
        resource.LastModified.ShouldBe(now);
        resource.LastModifiedBy.ShouldBe("admin");
    }

    [Test]
    public void Resource_NullMetadata_IsAllowed()
    {
        var resource = new Resource
        {
            Type = "Report",
            OwnerId = "user-2",
            Name = "Sin metadata",
            Metadata = null
        };

        resource.Metadata.ShouldBeNull();
    }
}
