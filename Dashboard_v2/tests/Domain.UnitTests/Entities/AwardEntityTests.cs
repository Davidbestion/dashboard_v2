using Dashboard_v2.Domain.Entities;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Domain.UnitTests.Entities;

/// <summary>
/// Cubre las entidades Award, AwardType y UserAwarded.
/// </summary>
[TestFixture]
public class AwardEntityTests
{
    // ── Award ─────────────────────────────────────────────────────────────────

    [Test]
    public void Award_DefaultUserAwardeds_IsEmptyNotNull()
    {
        var award = new Award { Name = "Premio Nacional" };

        award.UserAwardees.ShouldNotBeNull();
        award.UserAwardees.ShouldBeEmpty();
    }

    [Test]
    public void Award_CanAssignAllScalarProperties()
    {
        var award = new Award
        {
            Id = 99,
            Name = "Premio Relevante",
            AwardTypeId = 3,
        };

        award.Id.ShouldBe(99);
        award.Name.ShouldBe("Premio Relevante");
        award.AwardTypeId.ShouldBe(3);
    }

    [Test]
    public void Award_IntegerId_DefaultsToZero()
    {
        var award = new Award { Name = "Sin ID asignado" };

        award.Id.ShouldBe(0);
    }

    // ── AwardType ─────────────────────────────────────────────────────────────

    [Test]
    public void AwardType_CanAssignIdAndName()
    {
        var type = new AwardType { Id = 1, Name = "Internacional" };

        type.Id.ShouldBe(1);
        type.Name.ShouldBe("Internacional");
    }

    // ── UserAwarded ────────────────────────────────────────────────────────────

    [Test]
    public void UserAwarded_Properties_SetAndRead()
    {
        var ua = new UserAwarded
        {
            UserId = "user-1",
            AwardId = 5
        };

        ua.UserId.ShouldBe("user-1");
        ua.AwardId.ShouldBe(5);
    }
}
