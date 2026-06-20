using Dashboard_v2.Application.FileStorage.DTOs;
using Dashboard_v2.Application.FileStorage.Validators;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.FileStorage;

/// <summary>
/// Tests unitarios para <see cref="UploadFileRequestValidator"/>.
/// </summary>
[TestFixture]
public class UploadFileRequestValidatorTests
{
    private readonly UploadFileRequestValidator _validator = new();

    private static UploadFileRequest ValidRequest() => new()
    {
        FileName    = "certificado.pdf",
        ContentType = "application/pdf",
        SizeBytes   = 1024,
        Category    = "premios",
    };

    [Test]
    public async Task ValidRequest_PassesValidation()
    {
        var result = await _validator.ValidateAsync(ValidRequest());
        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task EmptyFileName_FailsValidation()
    {
        var req = new UploadFileRequest { FileName = "", ContentType = "application/pdf", SizeBytes = 1024, Category = "premios" };
        var result = await _validator.ValidateAsync(req);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(req.FileName));
    }

    [Test]
    public async Task FileNameTooLong_FailsValidation()
    {
        var req = new UploadFileRequest { FileName = new string('a', 265), ContentType = "application/pdf", SizeBytes = 1024, Category = "premios" };
        var result = await _validator.ValidateAsync(req);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(req.FileName));
    }

    [Test]
    public async Task DisallowedContentType_FailsValidation()
    {
        var req = new UploadFileRequest { FileName = "img.png", ContentType = "image/png", SizeBytes = 1024, Category = "premios" };
        var result = await _validator.ValidateAsync(req);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(req.ContentType));
    }

    [Test]
    [TestCase("application/pdf")]
    [TestCase("application/msword")]
    [TestCase("application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [TestCase("application/vnd.ms-excel")]
    [TestCase("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task AllowedContentTypes_PassValidation(string contentType)
    {
        var req = new UploadFileRequest { FileName = "file.pdf", ContentType = contentType, SizeBytes = 1024, Category = "premios" };
        var result = await _validator.ValidateAsync(req);
        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ZeroSizeFile_FailsValidation()
    {
        var req = new UploadFileRequest { FileName = "file.pdf", ContentType = "application/pdf", SizeBytes = 0, Category = "premios" };
        var result = await _validator.ValidateAsync(req);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(req.SizeBytes));
    }

    [Test]
    public async Task FileSizeExceedsLimit_FailsValidation()
    {
        var req = new UploadFileRequest { FileName = "file.pdf", ContentType = "application/pdf", SizeBytes = 21 * 1024 * 1024, Category = "premios" };
        var result = await _validator.ValidateAsync(req);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(req.SizeBytes));
    }

    [Test]
    public async Task EmptyCategory_FailsValidation()
    {
        var req = new UploadFileRequest { FileName = "file.pdf", ContentType = "application/pdf", SizeBytes = 1024, Category = "" };
        var result = await _validator.ValidateAsync(req);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(req.Category));
    }

    [Test]
    public async Task CategoryWithUpperCase_FailsValidation()
    {
        var req = new UploadFileRequest { FileName = "file.pdf", ContentType = "application/pdf", SizeBytes = 1024, Category = "Premios" };
        var result = await _validator.ValidateAsync(req);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(req.Category));
    }

    [Test]
    public async Task CategoryWithSpaces_FailsValidation()
    {
        var req = new UploadFileRequest { FileName = "file.pdf", ContentType = "application/pdf", SizeBytes = 1024, Category = "grupos investigacion" };
        var result = await _validator.ValidateAsync(req);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(req.Category));
    }

    [Test]
    public async Task CategoryAtMaxLength_PassesValidation()
    {
        var req = new UploadFileRequest { FileName = "file.pdf", ContentType = "application/pdf", SizeBytes = 1024, Category = new string('a', 50) };
        var result = await _validator.ValidateAsync(req);
        result.IsValid.ShouldBeTrue();
    }
}
