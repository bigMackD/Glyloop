namespace Glyloop.API.Configuration;

/// <summary>
/// Configures authorization policies for the API.
/// </summary>
public static class AuthorizationConfiguration
{
    /// <summary>
    /// Adds authorization services and policies to the dependency injection container.
    /// </summary>
    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Authorization is handled via [Authorize] and [AllowAnonymous] attributes on controllers
            // No FallbackPolicy is set to allow access to non-controller endpoints (like Swagger, health checks)
        });

        return services;
    }
}

