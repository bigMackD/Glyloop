using System.Reflection;
using FluentValidation;
using Glyloop.Application.Common.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace Glyloop.Application;

/// <summary>
/// Extension methods for registering Application layer dependencies.
/// Registers MediatR with pipeline behaviors and FluentValidation validators.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Application layer services to the dependency injection container.
    /// Registers MediatR with validation pipeline behavior.
    /// Note: Authentication is handled at the API layer via [Authorize] attribute.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register MediatR with pipeline behaviors
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(assembly);
            
            // Validates before processing
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // Register all FluentValidation validators from this assembly
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}

