using Dashboard_v2.Domain.Common;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Domain.UnitTests.Common;

// Minimal concrete entity for testing BaseEntity.
file sealed class TestEntity : BaseEntity { }

[TestFixture]
public class BaseEntityTests
{
    [Test]
    public void AddDomainEvent_EventIsTracked()
    {
        var entity = new TestEntity();
        var evt = new Mock_BaseEvent();

        entity.AddDomainEvent(evt);

        entity.DomainEvents.Count.ShouldBe(1);
        entity.DomainEvents.ShouldContain(evt);
    }

    [Test]
    public void RemoveDomainEvent_EventIsRemoved()
    {
        var entity = new TestEntity();
        var evt = new Mock_BaseEvent();
        entity.AddDomainEvent(evt);

        entity.RemoveDomainEvent(evt);

        entity.DomainEvents.ShouldBeEmpty();
    }

    [Test]
    public void ClearDomainEvents_AllEventsAreRemoved()
    {
        var entity = new TestEntity();
        entity.AddDomainEvent(new Mock_BaseEvent());
        entity.AddDomainEvent(new Mock_BaseEvent());

        entity.ClearDomainEvents();

        entity.DomainEvents.ShouldBeEmpty();
    }

    [Test]
    public void DomainEvents_InitiallyEmpty()
    {
        var entity = new TestEntity();
        entity.DomainEvents.ShouldBeEmpty();
    }
}

// Minimal concrete BaseEvent so we don't pull in MediatR just for this.
file sealed class Mock_BaseEvent : BaseEvent { }

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
    public void InheritsBaseEntity_DomainEventsAvailable()
    {
        var entity = new TestAuditableEntity();
        entity.DomainEvents.ShouldBeEmpty();
    }
}
