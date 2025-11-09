using Glyloop.Domain.Common;
using NUnit.Framework;

namespace Glyloop.Domain.Tests.Common;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[Category("Unit")]
public class EntityBaseTests
{
    private sealed class TestEntity : Entity<Guid>
    {
        public TestEntity(Guid id) : base(id) { }

        public void AddEvent(IDomainEvent @event) => RaiseDomainEvent(@event);
    }

    private sealed record DummyEvent(Guid EventId, DateTimeOffset OccurredAt, Guid CorrelationId, Guid CausationId) : IDomainEvent;

    [Test]
    public void Equality_ShouldUseId()
    {
        var id = Guid.NewGuid();
        var a = new TestEntity(id);
        var b = new TestEntity(id);
        var c = new TestEntity(Guid.NewGuid());

        Assert.Multiple(() =>
        {
            Assert.That(a, Is.EqualTo(b));
            Assert.That(a == b, Is.True);
            Assert.That(a != b, Is.False);
            Assert.That(a, Is.Not.EqualTo(c));
        });
    }

    [Test]
    public void DomainEvents_ShouldAddAndClear()
    {
        var entity = new TestEntity(Guid.NewGuid());
        var evt = new DummyEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid(), Guid.NewGuid());

        Assert.That(entity.DomainEvents, Is.Empty);

        entity.AddEvent(evt);

        Assert.That(entity.DomainEvents.Count, Is.EqualTo(1));

        entity.ClearDomainEvents();

        Assert.That(entity.DomainEvents, Is.Empty);
    }
}


