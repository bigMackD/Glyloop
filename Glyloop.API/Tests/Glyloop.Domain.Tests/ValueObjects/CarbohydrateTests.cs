using Glyloop.Domain.ValueObjects;
using NUnit.Framework;

namespace Glyloop.Domain.Tests.ValueObjects;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[Category("Unit")]
public class CarbohydrateTests
{
    [TestCase(-1)]
    [TestCase(301)]
    [TestCase(int.MinValue)]
    [TestCase(int.MaxValue)]
    public void Create_ShouldFail_WhenOutOfRange(int grams)
    {
        var result = Carbohydrate.Create(grams);
        Assert.That(result.IsFailure, Is.True);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(299)]
    [TestCase(300)]
    public void Create_ShouldSucceed_WhenInRange(int grams)
    {
        var result = Carbohydrate.Create(grams);
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.Grams, Is.EqualTo(grams));
            Assert.That(result.Value.ToString(), Is.EqualTo($"{grams}g"));
        });
    }
}


