using Glyloop.Domain.Common;
using NUnit.Framework;

namespace Glyloop.Domain.Tests.Common;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[Category("Unit")]
public class ErrorTests
{
    [Test]
    public void Create_ShouldReturnErrorWithCodeAndMessage()
    {
        var error = Error.Create("A.B", "Some message");

        Assert.Multiple(() =>
        {
            Assert.That(error.Code, Is.EqualTo("A.B"));
            Assert.That(error.Message, Is.EqualTo("Some message"));
        });
    }

    [Test]
    public void None_ShouldBeEmptyError()
    {
        var none = Error.None;

        Assert.Multiple(() =>
        {
            Assert.That(none.Code, Is.EqualTo(string.Empty));
            Assert.That(none.Message, Is.EqualTo(string.Empty));
        });
    }

    [Test]
    public void ImplicitOperator_ShouldReturnCode()
    {
        var error = Error.Create("X.Y", "Z");

        string code = error;

        Assert.That(code, Is.EqualTo("X.Y"));
    }
}


