using Glyloop.Domain.ValueObjects;
using NUnit.Framework;

namespace Glyloop.Domain.Tests.Common;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[Category("Unit")]
public class ValueObjectEqualityTests
{
    [Test]
    public void Equals_ShouldBeTrue_ForSameValues()
    {
        var a = TirRange.Create(70, 180).Value;
        var b = TirRange.Create(70, 180).Value;

        Assert.Multiple(() =>
        {
            Assert.That(a.Equals(b), Is.True);
            Assert.That(a == b, Is.True);
            Assert.That(a != b, Is.False);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        });
    }

    [Test]
    public void Equals_ShouldBeFalse_ForDifferentValues()
    {
        var a = TirRange.Create(70, 180).Value;
        var b = TirRange.Create(80, 180).Value;

        Assert.Multiple(() =>
        {
            Assert.That(a.Equals(b), Is.False);
            Assert.That(a == b, Is.False);
            Assert.That(a != b, Is.True);
        });
    }
}


