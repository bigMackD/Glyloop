using FluentValidation;
using FluentValidation.Results;
using Glyloop.Application.Common.Behaviors;
using Glyloop.Domain.Common;
using MediatR;
using NSubstitute;
using NUnit.Framework;

namespace Glyloop.Application.Tests;

/// <summary>
/// Unit tests for ValidationBehavior covering generic and non-generic Result flows,
/// including pass-through behavior, aggregation of errors, and repeat invocations.
/// </summary>
[TestFixture]
[Category("Unit")]
public class ValidationBehaviorTests
{
    private static RequestHandlerDelegate<Result<string>> NextGeneric(string value = "OK")
        => () => Task.FromResult(Result.Success(value));

    private static RequestHandlerDelegate<Result> NextNonGeneric()
        => () => Task.FromResult(Result.Success());

    public record DummyGenericRequest(string Name) : IRequest<Result<string>>;
    public record DummyNonGenericRequest(string Name) : IRequest<Result>;

    [Test]
    public async Task Handle_NoValidators_GenericResult_ShouldCallNextAndReturnSuccess()
    {
        // Arrange
        var validators = Enumerable.Empty<IValidator<DummyGenericRequest>>();
        var behavior = new ValidationBehavior<DummyGenericRequest, Result<string>>(validators);
        var request = new DummyGenericRequest("john");

        // Act
        var result = await behavior.Handle(request, NextGeneric("VALUE"), CancellationToken.None);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.EqualTo("VALUE"));
    }

    [Test]
    public async Task Handle_NoValidators_NonGenericResult_ShouldCallNextAndReturnSuccess()
    {
        // Arrange
        var validators = Enumerable.Empty<IValidator<DummyNonGenericRequest>>();
        var behavior = new ValidationBehavior<DummyNonGenericRequest, Result>(validators);
        var request = new DummyNonGenericRequest("john");

        // Act
        var result = await behavior.Handle(request, NextNonGeneric(), CancellationToken.None);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task Handle_WithValidatorFailures_GenericResult_ShouldReturnFailureWithAggregatedMessage()
    {
        // Arrange
        var validator = Substitute.For<IValidator<DummyGenericRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<DummyGenericRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[]
            {
                new ValidationFailure("Name", "Name is required"),
                new ValidationFailure("Name", "Name must be at least 3 characters")
            }));

        var behavior = new ValidationBehavior<DummyGenericRequest, Result<string>>(new[] { validator });
        var request = new DummyGenericRequest(string.Empty);

        // Act
        var result = await behavior.Handle(request, NextGeneric(), CancellationToken.None);

        // Assert
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error.Code, Is.EqualTo("Validation.Failed"));
        Assert.That(result.Error.Message, Does.Contain("Name is required"));
        Assert.That(result.Error.Message, Does.Contain("Name must be at least 3 characters"));
    }

    [Test]
    public async Task Handle_WithValidatorFailures_NonGenericResult_ShouldReturnFailure()
    {
        // Arrange
        var validator = Substitute.For<IValidator<DummyNonGenericRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<DummyNonGenericRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[]
            {
                new ValidationFailure("Name", "Name is required"),
            }));

        var behavior = new ValidationBehavior<DummyNonGenericRequest, Result>(new[] { validator });
        var request = new DummyNonGenericRequest(string.Empty);

        // Act
        var result = await behavior.Handle(request, NextNonGeneric(), CancellationToken.None);

        // Assert
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error.Code, Is.EqualTo("Validation.Failed"));
        Assert.That(result.Error.Message, Does.Contain("Name is required"));
    }

    [Test]
    public async Task Handle_WithValidatorFailures_GenericResult_CalledTwice_ShouldBeConsistent()
    {
        // Arrange
        var validator = Substitute.For<IValidator<DummyGenericRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<DummyGenericRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[]
            {
                new ValidationFailure("Name", "Name is required"),
            }));

        var behavior = new ValidationBehavior<DummyGenericRequest, Result<string>>(new[] { validator });
        var request = new DummyGenericRequest(string.Empty);

        // Act
        var result1 = await behavior.Handle(request, NextGeneric(), CancellationToken.None);
        var result2 = await behavior.Handle(request, NextGeneric(), CancellationToken.None);

        // Assert
        Assert.That(result1.IsFailure, Is.True);
        Assert.That(result2.IsFailure, Is.True);
        Assert.That(result1.Error.Message, Is.EqualTo(result2.Error.Message));
    }
}


