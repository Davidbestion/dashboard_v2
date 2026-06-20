using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Domain.Enums;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Domain.UnitTests.Entities;

/// <summary>
/// Cubre la entidad Publication: valores por defecto, asignación de propiedades
/// y el enum PublicationType.
/// </summary>
[TestFixture]
public class PublicationEntityTests
{
    // ── Defaults ──────────────────────────────────────────────────────────────

    [Test]
    public void Publication_DefaultId_IsValidGuid()
    {
        var pub = new Publication { Title = "Test", PublicationData = "d", PublishedDate = "2024" };

        Guid.TryParse(pub.Id, out _).ShouldBeTrue();
    }

    [Test]
    public void Publication_EachInstance_GetsUniqueId()
    {
        var p1 = new Publication { Title = "A", PublicationData = "d", PublishedDate = "2024" };
        var p2 = new Publication { Title = "B", PublicationData = "d", PublishedDate = "2024" };

        p1.Id.ShouldNotBe(p2.Id);
    }

    [Test]
    public void Publication_DefaultNullableFields_AreNull()
    {
        var pub = new Publication { Title = "T", PublicationData = "d", PublishedDate = "2024" };

        pub.NormalizedTitle.ShouldBeNull();
        pub.UrlDoi.ShouldBeNull();
        pub.NormalizedUrlDoi.ShouldBeNull();
        pub.ProyectoId.ShouldBeNull();
        pub.RedId.ShouldBeNull();
        pub.EvidenceFileId.ShouldBeNull();
        pub.IndexedPublication.ShouldBeNull();
        pub.JournalPublication.ShouldBeNull();
    }

    [Test]
    public void Publication_DefaultCollections_AreEmptyNotNull()
    {
        var pub = new Publication { Title = "T", PublicationData = "d", PublishedDate = "2024" };

        pub.AuthorPublications.ShouldNotBeNull();
        pub.AuthorPublications.ShouldBeEmpty();
    }

    // ── Asignación de propiedades ─────────────────────────────────────────────

    [Test]
    public void Publication_CanAssignAllScalarProperties()
    {
        var pub = new Publication
        {
            Id = "fixed-pub-id",
            Title = "Inteligencia Artificial en Medicina",
            NormalizedTitle = "inteligencia artificial en medicina",
            PublicationData = "Revista Cubana de Ciencias, Vol. 1",
            UrlDoi = "https://doi.org/10.1000/xyz123",
            NormalizedUrlDoi = "doi.org/10.1000/xyz123",
            PublishedDate = "2023-06",
            PublicationType = PublicationType.Artículo_de_Divulgación,
            ProyectoId = "proy-1",
            RedId = "red-1",
            EvidenceFileId = 42,
        };

        pub.Id.ShouldBe("fixed-pub-id");
        pub.Title.ShouldBe("Inteligencia Artificial en Medicina");
        pub.NormalizedTitle.ShouldBe("inteligencia artificial en medicina");
        pub.PublicationData.ShouldBe("Revista Cubana de Ciencias, Vol. 1");
        pub.UrlDoi.ShouldBe("https://doi.org/10.1000/xyz123");
        pub.NormalizedUrlDoi.ShouldBe("doi.org/10.1000/xyz123");
        pub.PublishedDate.ShouldBe("2023-06");
        pub.PublicationType.ShouldBe(PublicationType.Artículo_de_Divulgación);
        pub.ProyectoId.ShouldBe("proy-1");
        pub.RedId.ShouldBe("red-1");
        pub.EvidenceFileId.ShouldBe(42);
    }

    // ── PublicationType enum ──────────────────────────────────────────────────

    [TestCase(PublicationType.Diario, 0)]
    [TestCase(PublicationType.Libro, 1)]
    [TestCase(PublicationType.Monografía, 2)]
    [TestCase(PublicationType.Capítulo, 3)]
    [TestCase(PublicationType.Artículo_de_Divulgación, 4)]
    public void PublicationType_HasExpectedIntegerValue(PublicationType tipo, int expected)
    {
        ((int)tipo).ShouldBe(expected);
    }

    [Test]
    public void PublicationType_AllValues_CanBeAssignedToPublication()
    {
        foreach (var tipo in Enum.GetValues<PublicationType>())
        {
            var pub = new Publication
            {
                Title = "T",
                PublicationData = "d",
                PublishedDate = "2024",
                PublicationType = tipo
            };
            pub.PublicationType.ShouldBe(tipo);
        }
    }

    // ── AuthorPublication ─────────────────────────────────────────────────────

    [Test]
    public void AuthorPublication_Properties_SetAndRead()
    {
        var ap = new AuthorPublication
        {
            AuthorId = "user-1",
            PublicationId = "pub-1"
        };

        ap.AuthorId.ShouldBe("user-1");
        ap.PublicationId.ShouldBe("pub-1");
    }
}
