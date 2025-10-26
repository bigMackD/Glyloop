using HealthChecks.NpgSql;

namespace Glyloop.API.Configuration;

/// <summary>
/// Configures health checks for the API.
/// </summary>
public static class HealthCheckConfiguration
{
    /// <summary>
    /// Adds health check services for database and application health.
    /// </summary>
    public static IServiceCollection AddHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Database connection string is not configured");

        services.AddHealthChecks()
            .AddNpgSql(
                connectionString,
                name: "postgresql",
                tags: new[] { "database", "postgresql" })
            .AddCheck<FeatureFlagHealthCheck>("feature-flags", tags: new[] { "ready" });

        return services;
    }
}

/// <summary>
/// Custom health check to verify feature flags are loaded.
/// </summary>
public class FeatureFlagHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    private readonly IConfiguration _configuration;

    public FeatureFlagHealthCheck(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var featureFlags = _configuration.GetSection("FeatureFlags");
        
        if (featureFlags.Exists())
        {
            return Task.FromResult(
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(
                    "Feature flags are loaded"));
        }

        return Task.FromResult(
            Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded(
                "Feature flags section not found"));
    }
}

