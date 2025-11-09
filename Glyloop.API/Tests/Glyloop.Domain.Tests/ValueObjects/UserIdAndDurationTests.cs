using Glyloop.Domain.ValueObjects;
using NUnit.Framework;

namespace Glyloop.Domain.Tests.ValueObjects;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[Category("Unit")]
public class UserIdAndDurationTests
{
    [Test]
    public void UserId_Create_ShouldThrow_WhenEmpty()
    {
        Assert.Throws<ArgumentException>(() => UserId.Create(Guid.Empty));
    }

    [Test]
    public void UserId_Create_ShouldReturnValue_WhenValid()
    {
        var guid = Guid.NewGuid();
        var userId = UserId.Create(guid);
        Assert.That(userId.Value, Is.EqualTo(guid));
    }

    [TestCase(0)]
    [TestCase(301)]
    public void ExerciseDuration_Create_ShouldFail_WhenOutOfRange(int minutes)
    {
        var result = ExerciseDuration.Create(minutes);
        Assert.That(result.IsFailure, Is.True);
    }

    [TestCase(1)]
    [TestCase(300)]
    public void ExerciseDuration_Create_ShouldSucceed_WhenWithinRange(int minutes)
    {
        var result = ExerciseDuration.Create(minutes);
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.Minutes, Is.EqualTo(minutes));
            Assert.That(result.Value.ToString(), Is.EqualTo($"{minutes} min"));
        });
    }
}


