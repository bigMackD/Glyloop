using Glyloop.Domain.ValueObjects;
using NUnit.Framework;

namespace Glyloop.Domain.Tests.ValueObjects;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[Category("Unit")]
public class TirRangeTests
{
    [Test]
    public void Create_ShouldFail_WhenBoundsOutOfRange()
    {
        Assert.That(TirRange.Create(-1, 100).IsFailure, Is.True);
        Assert.That(TirRange.Create(0, 1001).IsFailure, Is.True);
    }

    [Test]
    public void Create_ShouldFail_WhenLowerNotLessThanUpper()
    {
        Assert.That(TirRange.Create(100, 100).IsFailure, Is.True);
        Assert.That(TirRange.Create(150, 120).IsFailure, Is.True);
    }

    [Test]
    public void Create_ShouldSucceed_WhenValid()
    {
        var result = TirRange.Create(70, 180);
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.Lower, Is.EqualTo(70));
            Assert.That(result.Value.Upper, Is.EqualTo(180));
            Assert.That(result.Value.IsInRange(70), Is.True);
            Assert.That(result.Value.IsInRange(180), Is.True);
            Assert.That(result.Value.IsInRange(69), Is.False);
            Assert.That(result.Value.IsInRange(181), Is.False);
        });
    }

    [Test]
    public void Standard_ShouldReturnRecommendedRange()
    {
        var standard = TirRange.Standard();
        Assert.Multiple(() =>
        {
            Assert.That(standard.Lower, Is.EqualTo(70));
            Assert.That(standard.Upper, Is.EqualTo(180));
            Assert.That(standard.ToString(), Is.EqualTo("70-180 mg/dL"));
        });
    }
}


