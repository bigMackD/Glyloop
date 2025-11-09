using Glyloop.Domain.Common;
using NUnit.Framework;

namespace Glyloop.Domain.Tests.Common;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[Category("Unit")]
public class ResultTests
{
    [Test]
    public void Success_ShouldCreateSuccessWithoutError()
    {
        var result = Result.Success();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.IsFailure, Is.False);
            Assert.That(result.Error, Is.EqualTo(Error.None));
        });
    }

    [Test]
    public void Failure_ShouldCreateFailureWithError()
    {
        var error = Error.Create("Test.Code", "Test message");

        var result = Result.Failure(error);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error, Is.EqualTo(error));
        });
    }

    [Test]
    public void SuccessT_ShouldExposeValue_WhenSuccess()
    {
        var result = Result.Success(42);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.EqualTo(42));
        });
    }

    [Test]
    public void FailureT_ShouldThrow_WhenAccessingValue()
    {
        var error = Error.Create("X", "Y");
        var result = Result.Failure<int>(error);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.Throws<InvalidOperationException>(() => _ = result.Value);
        });
    }

    [Test]
    public void ImplicitConversion_ShouldCreateSuccessT()
    {
        Result<int> result = 7;

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.EqualTo(7));
        });
    }
}


