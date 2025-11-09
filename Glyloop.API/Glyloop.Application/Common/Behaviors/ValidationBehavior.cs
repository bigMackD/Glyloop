using System.Collections.Concurrent;
using FluentValidation;
using Glyloop.Domain.Common;
using MediatR;

namespace Glyloop.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that automatically validates requests using FluentValidation.
/// Runs before the request handler and returns validation errors as a Result.Failure.
/// Uses cached delegates to avoid reflection overhead on every request.
/// </summary>
/// <typeparam name="TRequest">The request type</typeparam>
/// <typeparam name="TResponse">The response type (must be Result or Result&lt;T&gt;)</typeparam>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    // Cache delegates per TResponse type to avoid reflection on every call
    private static readonly ConcurrentDictionary<Type, Func<Error, object>> FailureFactoryCache = new();

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .Where(r => !r.IsValid)
            .SelectMany(r => r.Errors)
            .ToList();

        if (failures.Any())
        {
            var errorMessage = string.Join("; ", failures.Select(f => f.ErrorMessage));
            var error = Error.Create("Validation.Failed", errorMessage);

            // Use cached delegate to create Result.Failure<T> without reflection on every call
            return (TResponse)CreateFailureResult(error);
        }

        // Proceed to next behavior/handler
        return await next();
    }

    /// <summary>
    /// Creates a failure result using a cached delegate to avoid reflection overhead.
    /// The delegate is created once per TResponse type and reused for all subsequent calls.
    /// </summary>
    private static object CreateFailureResult(Error error)
    {
        var factory = FailureFactoryCache.GetOrAdd(typeof(TResponse), resultType =>
        {
            // TODO: This is a hack to avoid reflection overhead on every call.
            // We should find a better way to do this.
            if (resultType.IsGenericType)
            {
                var genericArg = resultType.GetGenericArguments()[0];
                // Resolve the generic Result.Failure<T>(Error) overload explicitly to avoid ambiguity
                var failureMethod = typeof(Result)
                    .GetMethods()
                    .First(m =>
                        m.Name == nameof(Result.Failure) &&
                        m.IsGenericMethodDefinition &&
                        m.GetParameters().Length == 1 &&
                        m.GetParameters()[0].ParameterType == typeof(Error))
                    .MakeGenericMethod(genericArg);

                // Create and cache a delegate for future calls
                return (Func<Error, object>)Delegate.CreateDelegate(
                    typeof(Func<Error, object>),
                    failureMethod);
            }

            // For non-generic Result, create a simple wrapper
            return (Error err) => Result.Failure(err);
        });

        return factory(error);
    }
}

