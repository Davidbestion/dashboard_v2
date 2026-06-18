using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Documents;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.Documents;

[TestFixture]
public class DocumentServiceTests
{
    private Mock<IDocumentReport> _reportMock = null!;
    private Mock<IZipDocumentReport> _zipReportMock = null!;
    private Mock<IDocumentRenderer> _rendererMock = null!;
    private DocumentService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _reportMock = new Mock<IDocumentReport>();
        _reportMock.Setup(r => r.ReportName).Returns("test-report");
        _reportMock.Setup(r => r.TemplateName).Returns("TestTemplate");
        _reportMock.Setup(r => r.AllowedRoles).Returns(["Superuser"]);
        _reportMock
            .Setup(r => r.GatherVariablesAsync(It.IsAny<IReadOnlyDictionary<string, string>?>(), default))
            .ReturnsAsync(new Dictionary<string, object> { ["key"] = "value" });

        _zipReportMock = new Mock<IZipDocumentReport>();
        _zipReportMock.Setup(r => r.ReportName).Returns("zip-report");
        _zipReportMock.Setup(r => r.AllowedRoles).Returns(["Superuser", "Jefe_de_Redes"]);
        _zipReportMock
            .Setup(r => r.GenerateAsync(It.IsAny<IDocumentRenderer>(), default))
            .ReturnsAsync(new byte[] { 1, 2, 3 });

        _rendererMock = new Mock<IDocumentRenderer>();
        _rendererMock
            .Setup(r => r.Render(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, object>>()))
            .Returns(new byte[] { 10, 20, 30 });

        _sut = new DocumentService(
            new[] { _reportMock.Object },
            new[] { _zipReportMock.Object },
            _rendererMock.Object);
    }

    [Test]
    public void IsZipReport_ExistingZipReport_ReturnsTrue()
    {
        _sut.IsZipReport("zip-report").ShouldBeTrue();
    }

    [Test]
    public void IsZipReport_RegularReport_ReturnsFalse()
    {
        _sut.IsZipReport("test-report").ShouldBeFalse();
    }

    [Test]
    public void IsZipReport_UnknownReport_ReturnsFalse()
    {
        _sut.IsZipReport("unknown").ShouldBeFalse();
    }

    [Test]
    public async Task GenerateAsync_ZipReport_DelegatesToZipReport()
    {
        var result = await _sut.GenerateAsync("zip-report");
        result.ShouldBe(new byte[] { 1, 2, 3 });
        _zipReportMock.Verify(r => r.GenerateAsync(_rendererMock.Object, default), Times.Once);
    }

    [Test]
    public async Task GenerateAsync_RegularReport_CallsGatherAndRender()
    {
        var result = await _sut.GenerateAsync("test-report");
        result.ShouldBe(new byte[] { 10, 20, 30 });
        _reportMock.Verify(r => r.GatherVariablesAsync(null, default), Times.Once);
        _rendererMock.Verify(r => r.Render("TestTemplate", It.IsAny<IReadOnlyDictionary<string, object>>()), Times.Once);
    }

    [Test]
    public async Task GenerateAsync_CaseInsensitiveReportName_Succeeds()
    {
        var result = await _sut.GenerateAsync("TEST-REPORT");
        result.ShouldNotBeNull();
    }

    [Test]
    public void GenerateAsync_UnknownReport_ThrowsKeyNotFoundException()
    {
        Should.Throw<KeyNotFoundException>(() => _sut.GenerateAsync("does-not-exist"));
    }

    [Test]
    public async Task GenerateAsync_WithParameters_PassesThemToReport()
    {
        var parameters = new Dictionary<string, string> { ["year"] = "2024" };
        await _sut.GenerateAsync("test-report", parameters);
        _reportMock.Verify(r => r.GatherVariablesAsync(
            It.Is<IReadOnlyDictionary<string, string>>(p => p["year"] == "2024"), default), Times.Once);
    }

    // ── GetAllowedRoles ──────────────────────────────────────────────────────

    [Test]
    public void GetAllowedRoles_RegularReport_ReturnsItsAllowedRoles()
    {
        var roles = _sut.GetAllowedRoles("test-report");

        roles.ShouldNotBeNull();
        roles.ShouldBe(["Superuser"]);
    }

    [Test]
    public void GetAllowedRoles_ZipReport_ReturnsItsAllowedRoles()
    {
        var roles = _sut.GetAllowedRoles("zip-report");

        roles.ShouldNotBeNull();
        roles.ShouldBe(["Superuser", "Jefe_de_Redes"]);
    }

    [Test]
    public void GetAllowedRoles_UnknownReport_ReturnsNull()
    {
        _sut.GetAllowedRoles("does-not-exist").ShouldBeNull();
    }

    [Test]
    public void GetAllowedRoles_CaseInsensitiveReportName_Succeeds()
    {
        _sut.GetAllowedRoles("TEST-REPORT").ShouldBe(["Superuser"]);
    }
}
