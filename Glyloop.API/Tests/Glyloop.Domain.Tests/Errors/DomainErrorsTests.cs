using Glyloop.Domain.Errors;
using NUnit.Framework;

namespace Glyloop.Domain.Tests.Errors;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[Category("Unit")]
public class DomainErrorsTests
{
    [Test]
    public void User_InvalidEmail_ShouldHaveExpectedCodeAndMessage()
    {
        var e = DomainErrors.User.InvalidEmail;
        Assert.Multiple(() =>
        {
            Assert.That(e.Code, Is.EqualTo("User.InvalidEmail"));
            Assert.That(e.Message, Is.EqualTo("The email address format is invalid."));
        });
    }

    [Test]
    public void User_InvalidTirRange_ShouldHaveExpectedCodeAndMessage()
    {
        var e = DomainErrors.User.InvalidTirRange;
        Assert.Multiple(() =>
        {
            Assert.That(e.Code, Is.EqualTo("User.InvalidTirRange"));
            Assert.That(e.Message, Does.Contain("TIR range lower bound must be less than upper bound"));
        });
    }

    [Test]
    public void DexcomLink_TokenExpired_ShouldHaveExpectedCodeAndMessage()
    {
        var e = DomainErrors.DexcomLink.TokenExpired;
        Assert.Multiple(() =>
        {
            Assert.That(e.Code, Is.EqualTo("DexcomLink.TokenExpired"));
            Assert.That(e.Message, Does.Contain("expired"));
        });
    }

    [Test]
    public void Event_EventInFuture_ShouldHaveExpectedCodeAndMessage()
    {
        var e = DomainErrors.Event.EventInFuture;
        Assert.Multiple(() =>
        {
            Assert.That(e.Code, Is.EqualTo("Event.EventInFuture"));
            Assert.That(e.Message, Does.Contain("future"));
        });
    }
}


