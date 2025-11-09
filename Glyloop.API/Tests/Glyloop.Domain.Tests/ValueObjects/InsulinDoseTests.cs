using Glyloop.Domain.ValueObjects;
using NUnit.Framework;

namespace Glyloop.Domain.Tests.ValueObjects;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[Category("Unit")]
public class InsulinDoseTests
{
    [TestCase(-0.5)]
    [TestCase(100.5)]
    [TestCase(0.1)]
    [TestCase(0.3)]
    [TestCase(0.7)]
    [TestCase(99.9)]
    public void Create_ShouldFail_WhenInvalid(decimal units)
    {
        var result = InsulinDose.Create(units);
        Assert.That(result.IsFailure, Is.True);
    }

    [TestCase(0)]
    [TestCase(0.5)]
    [TestCase(1.0)]
    [TestCase(10.5)]
    [TestCase(100)]
    public void Create_ShouldSucceed_WhenValid(decimal units)
    {
        var result = InsulinDose.Create(units);
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.Units, Is.EqualTo(units));
            Assert.That(result.Value.ToString(), Is.EqualTo($"{units}U"));
        });
    }
}


