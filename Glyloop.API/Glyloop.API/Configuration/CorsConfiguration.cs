namespace Glyloop.API.Configuration;

/// <summary>
/// Configures CORS (Cross-Origin Resource Sharing) for the API.
/// </summary>
public static class CorsConfiguration
{
    public const string PolicyName = "GlyloopCorsPolicy";

    /// <summary>
    /// Adds CORS services with configured allowed origins.
    /// </summary>
    public static IServiceCollection AddCorsPolicy(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var corsSettings = configuration.GetSection("CorsSettings");
        var allowedOrigins = corsSettings.GetSection("AllowedOrigins")
            .Get<string[]>() ?? Array.Empty<string>();

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, builder =>
            {
                builder
                    .WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials() // Required for cookies
                    .SetIsOriginAllowedToAllowWildcardSubdomains();
            });
        });

        return services;
    }
}

