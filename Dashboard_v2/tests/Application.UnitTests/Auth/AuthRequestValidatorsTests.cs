using Dashboard_v2.Application.Auth;
using FluentValidation.TestHelper;
using NUnit.Framework;

namespace Dashboard_v2.Application.UnitTests.Auth;

[TestFixture]
public class LoginRequestValidatorTests
{
    private LoginRequestValidator _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new LoginRequestValidator();

    [Test]
    public void ValidRequest_PassesValidation()
    {
        var request = new LoginRequest { Email = "user@example.com", Password = "secret" };
        var result = _sut.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void EmptyEmail_FailsValidation()
    {
        var request = new LoginRequest { Email = "", Password = "secret" };
        var result = _sut.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Test]
    public void InvalidEmail_FailsValidation()
    {
        var request = new LoginRequest { Email = "not-an-email", Password = "secret" };
        var result = _sut.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Test]
    public void EmptyPassword_FailsValidation()
    {
        var request = new LoginRequest { Email = "user@example.com", Password = "" };
        var result = _sut.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}

[TestFixture]
public class RegisterRequestValidatorTests
{
    private RegisterRequestValidator _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new RegisterRequestValidator();

    private static RegisterRequest ValidRequest() => new()
    {
        UserName = "johndoe",
        UserLastName1 = "Doe",
        Email = "john@example.com",
        Password = "secret1",
        BirthDate = new DateTime(1990, 1, 1),
    };

    [Test]
    public void ValidRequest_PassesValidation()
    {
        var result = _sut.TestValidate(ValidRequest());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void EmptyUserName_FailsValidation()
    {
        var req = ValidRequest() with { UserName = "" };
        _sut.TestValidate(req).ShouldHaveValidationErrorFor(x => x.UserName);
    }

    [Test]
    public void TooShortUserName_FailsValidation()
    {
        var req = ValidRequest() with { UserName = "ab" };
        _sut.TestValidate(req).ShouldHaveValidationErrorFor(x => x.UserName);
    }

    [Test]
    public void TooLongUserName_FailsValidation()
    {
        var req = ValidRequest() with { UserName = new string('a', 101) };
        _sut.TestValidate(req).ShouldHaveValidationErrorFor(x => x.UserName);
    }

    [Test]
    public void EmptyLastName1_FailsValidation()
    {
        var req = ValidRequest() with { UserLastName1 = "" };
        _sut.TestValidate(req).ShouldHaveValidationErrorFor(x => x.UserLastName1);
    }

    [Test]
    public void InvalidEmail_FailsValidation()
    {
        var req = ValidRequest() with { Email = "bad-email" };
        _sut.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Test]
    public void EmptyPassword_FailsValidation()
    {
        var req = ValidRequest() with { Password = "" };
        _sut.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Test]
    public void TooShortPassword_FailsValidation()
    {
        var req = ValidRequest() with { Password = "abc" };
        _sut.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Test]
    public void FutureBirthDate_FailsValidation()
    {
        var req = ValidRequest() with { BirthDate = DateTime.Today.AddDays(1) };
        _sut.TestValidate(req).ShouldHaveValidationErrorFor(x => x.BirthDate);
    }

    [Test]
    public void EmptyBirthDate_FailsValidation()
    {
        var req = ValidRequest() with { BirthDate = default };
        _sut.TestValidate(req).ShouldHaveValidationErrorFor(x => x.BirthDate);
    }

    [Test]
    public void TooLongLastName2_FailsValidation()
    {
        var req = ValidRequest() with { UserLastName2 = new string('z', 257) };
        _sut.TestValidate(req).ShouldHaveValidationErrorFor(x => x.UserLastName2);
    }

    [Test]
    public void NullLastName2_PassesValidation()
    {
        var req = ValidRequest() with { UserLastName2 = null };
        _sut.TestValidate(req).ShouldNotHaveValidationErrorFor(x => x.UserLastName2);
    }
}
