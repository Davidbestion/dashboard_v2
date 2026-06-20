using Dashboard_v2.Domain.Entities;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Domain.UnitTests.Enums;

[TestFixture]
public class CategoryExtensionsTests
{
    // ── TeachingCategory ─────────────────────────────────────────────────────

    [TestCase(TeachingCategory.None, "Sin categoría docente")]
    [TestCase(TeachingCategory.Titular, " Profesor Titular")]
    [TestCase(TeachingCategory.Auxiliar, "Profesor Auxiliar")]
    [TestCase(TeachingCategory.Asistente, "Profesor Asistente")]
    [TestCase(TeachingCategory.Instructor, "Instructor")]
    public void TeachingCategory_ToDisplayString_ReturnsExpected(TeachingCategory category, string expected)
    {
        category.ToDisplayString().ShouldBe(expected);
    }

    [Test]
    public void TeachingCategory_UnknownValue_ReturnsDesconocida()
    {
        ((TeachingCategory)99).ToDisplayString().ShouldBe("Desconocida");
    }

    // ── ScientificCategory ───────────────────────────────────────────────────

    [TestCase(ScientificCategory.None, "Sin categoría científica")]
    [TestCase(ScientificCategory.Licenciado, "Licenciado")]
    [TestCase(ScientificCategory.Master, "Master")]
    [TestCase(ScientificCategory.Doctor, "Doctor")]
    public void ScientificCategory_ToDisplayString_ReturnsExpected(ScientificCategory category, string expected)
    {
        category.ToDisplayString().ShouldBe(expected);
    }

    [Test]
    public void ScientificCategory_UnknownValue_ReturnsDesconocido()
    {
        ((ScientificCategory)99).ToDisplayString().ShouldBe("Desconocido");
    }

    // ── InvestigationCategory ────────────────────────────────────────────────

    [TestCase(InvestigationCategory.None, "Sin categoría de investigación")]
    [TestCase(InvestigationCategory.Titular, "Investigador Titular")]
    [TestCase(InvestigationCategory.Auxiliar, "Investigador Auxiliar")]
    [TestCase(InvestigationCategory.Agregado, "Investigador Agregado")]
    [TestCase(InvestigationCategory.Asociado, "Investigador Asociado")]
    public void InvestigationCategory_ToDisplayString_ReturnsExpected(InvestigationCategory category, string expected)
    {
        category.ToDisplayString().ShouldBe(expected);
    }

    [Test]
    public void InvestigationCategory_UnknownValue_ReturnsDesconocida()
    {
        ((InvestigationCategory)99).ToDisplayString().ShouldBe("Desconocida");
    }
}
