using Dashboard_v2.Domain.Common;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Domain.UnitTests.Common;

// Minimal concrete BaseAuditableEntity for testing.
file sealed class TestAuditableEntity : BaseAuditableEntity { }

[TestFixture]
public class BaseAuditableEntityTests
{
    [Test]
    public void Properties_SetAndRead_ReturnExpectedValues()
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new TestAuditableEntity
        {
            Created = now,
            CreatedBy = "user-1",
            LastModified = now.AddHours(1),
            LastModifiedBy = "user-2"
        };

        entity.Created.ShouldBe(now);
        entity.CreatedBy.ShouldBe("user-1");
        entity.LastModified.ShouldBe(now.AddHours(1));
        entity.LastModifiedBy.ShouldBe("user-2");
    }

    [Test]
    public void InheritsBaseEntity_HasIntId()
    {
        var entity = new TestAuditableEntity();
        entity.Id.ShouldBe(0);
    }
}
