using Glyloop.Domain.ValueObjects;
using NUnit.Framework;

namespace Glyloop.Domain.Tests.ValueObjects;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[Category("Unit")]
public class NoteTextTests
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Create_ShouldFail_WhenNullOrWhitespace(string? text)
    {
        var result = NoteText.Create(text);
        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public void Create_ShouldFail_WhenTooLong()
    {
        var longText = new string('a', 501);
        var result = NoteText.Create(longText);

        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public void Create_ShouldSucceed_WhenWithinBounds_AndTrimmed()
    {
        var result = NoteText.Create("  hello  ");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.Text, Is.EqualTo("hello"));
            Assert.That(result.Value.ToString(), Is.EqualTo("hello"));
        });
    }

    [Test]
    public void CreateOptional_ShouldReturnNull_WhenEmpty()
    {
        var optional = NoteText.CreateOptional("   ");
        Assert.That(optional, Is.Null);
    }

    [Test]
    public void CreateOptional_ShouldReturnValue_WhenValid()
    {
        var optional = NoteText.CreateOptional("note");
        Assert.That(optional, Is.Not.Null);
        Assert.That(optional!.Text, Is.EqualTo("note"));
    }
}


