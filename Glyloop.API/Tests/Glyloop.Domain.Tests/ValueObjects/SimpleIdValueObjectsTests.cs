using Glyloop.Domain.ValueObjects;
using NUnit.Framework;

namespace Glyloop.Domain.Tests.ValueObjects;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[Category("Unit")]
public class SimpleIdValueObjectsTests
{
    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(int.MinValue)]
    public void ExerciseTypeId_Create_ShouldThrow_WhenNonPositive(int value)
    {
        Assert.Throws<ArgumentException>(() => ExerciseTypeId.Create(value));
    }

    [TestCase(1)]
    [TestCase(10)]
    public void ExerciseTypeId_Create_ShouldSucceed_WhenPositive(int value)
    {
        var id = ExerciseTypeId.Create(value);
        Assert.That(id.Value, Is.EqualTo(value));
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(int.MinValue)]
    public void MealTagId_Create_ShouldThrow_WhenNonPositive(int value)
    {
        Assert.Throws<ArgumentException>(() => MealTagId.Create(value));
    }

    [TestCase(1)]
    [TestCase(5)]
    public void MealTagId_Create_ShouldSucceed_WhenPositive(int value)
    {
        var id = MealTagId.Create(value);
        Assert.That(id.Value, Is.EqualTo(value));
    }
}


