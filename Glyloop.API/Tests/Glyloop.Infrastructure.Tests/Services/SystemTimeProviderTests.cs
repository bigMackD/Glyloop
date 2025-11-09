using Glyloop.Infrastructure.Services;
using NUnit.Framework;

namespace Glyloop.Infrastructure.Tests.Services;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[Category("Unit")]
public class SystemTimeProviderTests
{
    [Test]
    public void UtcNow_ShouldApproximateSystemUtcNow()
    {
        var provider = new SystemTimeProvider();
        var before = DateTimeOffset.UtcNow;
        var value = provider.UtcNow;
        var after = DateTimeOffset.UtcNow;

        Assert.That(value, Is.InRange(before.AddSeconds(-1), after.AddSeconds(1)));
    }
}



