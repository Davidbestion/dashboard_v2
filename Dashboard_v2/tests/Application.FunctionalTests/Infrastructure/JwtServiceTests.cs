using System.IdentityModel.Tokens.Jwt;
using Dashboard_v2.Infrastructure.Services;
using Microsoft.Extensions.Configuration;

namespace Dashboard_v2.Application.FunctionalTests.Infrastructure;

[TestFixture]
public class JwtServiceTests
{
    private JwtService _sut = default!;

    private const string Secret = "TestSecretKeyThatIsAtLeast32CharsLong!";
    private const string Issuer = "TestIssuer";
    private const string Audience = "TestAudience";

    [SetUp]
    public void SetUp()
    {
        _sut = new JwtService(BuildConfig(new Dictionary<string, string?>
        {
            { "Jwt:Secret", Secret },
            { "Jwt:Issuer", Issuer },
            { "Jwt:Audience", Audience },
            { "Jwt:ExpiryMinutes", "60" }
        }));
    }

    // ── Estructura del token ──────────────────────────────────────────────────

    [Test]
    public void GenerateToken_ValidInputs_ReturnsNonEmptyString()
    {
        var token = _sut.GenerateToken("u1", "Juan", "juan@test.com", ["Profesor"]);
        token.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void GenerateToken_IsValidJwtFormat()
    {
        var token = _sut.GenerateToken("u1", "Juan", "juan@test.com", []);
        new JwtSecurityTokenHandler().CanReadToken(token).ShouldBeTrue();
    }

    // ── Claims ────────────────────────────────────────────────────────────────

    [Test]
    public void GenerateToken_ContainsSubjectClaim()
    {
        var token = _sut.GenerateToken("user-123", "Juan", "juan@test.com", []);
        var parsed = Parse(token);
        parsed.Subject.ShouldBe("user-123");
    }

    [Test]
    public void GenerateToken_ContainsEmailClaim()
    {
        var token = _sut.GenerateToken("u", "N", "correo@ejemplo.com", []);
        var parsed = Parse(token);
        GetClaim(parsed, JwtRegisteredClaimNames.Email).ShouldBe("correo@ejemplo.com");
    }

    [Test]
    public void GenerateToken_ContainsUniqueNameClaim()
    {
        var token = _sut.GenerateToken("u", "NombreUsuario", "x@test.com", []);
        var parsed = Parse(token);
        GetClaim(parsed, JwtRegisteredClaimNames.UniqueName).ShouldBe("NombreUsuario");
    }

    [Test]
    public void GenerateToken_ContainsJtiAsValidGuid()
    {
        var token = _sut.GenerateToken("u", "N", "x@test.com", []);
        var jti = GetClaim(Parse(token), JwtRegisteredClaimNames.Jti);
        jti.ShouldNotBeNullOrEmpty();
        Guid.TryParse(jti, out _).ShouldBeTrue("jti debe ser un GUID válido");
    }

    [Test]
    public void GenerateToken_SingleRole_IsIncluded()
    {
        var token = _sut.GenerateToken("u", "N", "x@test.com", ["Superuser"]);
        Parse(token).Claims.Any(c => c.Value == "Superuser").ShouldBeTrue();
    }

    [Test]
    public void GenerateToken_MultipleRoles_AllIncluded()
    {
        string[] roles = ["Profesor", "Jefe_de_Grupo_de_investigacion"];
        var token = _sut.GenerateToken("u", "N", "x@test.com", roles);
        var claims = Parse(token).Claims;
        foreach (var role in roles)
            claims.Any(c => c.Value == role).ShouldBeTrue($"El token debe contener el rol '{role}'");
    }

    [Test]
    public void GenerateToken_NoRoles_HasNoRoleClaims()
    {
        var token = _sut.GenerateToken("u", "N", "x@test.com", []);
        var roleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
        Parse(token).Claims.Any(c => c.Type == roleClaimType).ShouldBeFalse();
    }

    // ── Issuer / Audience ─────────────────────────────────────────────────────

    [Test]
    public void GenerateToken_HasCorrectIssuer()
    {
        var token = _sut.GenerateToken("u", "N", "x@test.com", []);
        Parse(token).Issuer.ShouldBe(Issuer);
    }

    [Test]
    public void GenerateToken_HasCorrectAudience()
    {
        var token = _sut.GenerateToken("u", "N", "x@test.com", []);
        Parse(token).Audiences.ShouldContain(Audience);
    }

    // ── Expiración ────────────────────────────────────────────────────────────

    [Test]
    public void GenerateToken_ExpiresApproximatelyAfterConfiguredMinutes()
    {
        var before = DateTime.UtcNow;
        var token = _sut.GenerateToken("u", "N", "x@test.com", []);
        var after = DateTime.UtcNow;

        var validTo = Parse(token).ValidTo;
        validTo.ShouldBeGreaterThan(before.AddMinutes(59));
        validTo.ShouldBeLessThan(after.AddMinutes(61));
    }

    // ── Unicidad ─────────────────────────────────────────────────────────────

    [Test]
    public void GenerateToken_TwoCallsSameUser_ProduceDifferentJti()
    {
        var t1 = _sut.GenerateToken("u", "N", "x@test.com", []);
        var t2 = _sut.GenerateToken("u", "N", "x@test.com", []);
        var jti1 = GetClaim(Parse(t1), JwtRegisteredClaimNames.Jti);
        var jti2 = GetClaim(Parse(t2), JwtRegisteredClaimNames.Jti);
        jti1.ShouldNotBe(jti2, "Cada token debe tener un jti único aunque el usuario sea el mismo");
    }

    // ── Error de configuración ────────────────────────────────────────────────

    [Test]
    public void GenerateToken_MissingSecret_ThrowsInvalidOperationException()
    {
        var service = new JwtService(BuildConfig(new Dictionary<string, string?>()));
        Should.Throw<InvalidOperationException>(() =>
            service.GenerateToken("u", "N", "x@test.com", []));
    }

    [Test]
    public void GenerateToken_DefaultsApplied_WhenIssuerAndAudienceAbsent()
    {
        var service = new JwtService(BuildConfig(new Dictionary<string, string?>
        {
            { "Jwt:Secret", Secret }
        }));

        var token = service.GenerateToken("u", "N", "x@test.com", []);
        var parsed = Parse(token);

        // Cuando faltan, se usan los defaults "Dashboard_v2"
        parsed.Issuer.ShouldBe("Dashboard_v2");
        parsed.Audiences.ShouldContain("Dashboard_v2");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static IConfiguration BuildConfig(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static JwtSecurityToken Parse(string token) =>
        new JwtSecurityTokenHandler().ReadJwtToken(token);

    private static string GetClaim(JwtSecurityToken token, string claimType) =>
        token.Claims.First(c => c.Type == claimType).Value;
}
