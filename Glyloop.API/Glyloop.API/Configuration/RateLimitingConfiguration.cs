using System.Threading.RateLimiting;

namespace Glyloop.API.Configuration;

/// <summary>
/// Configures rate limiting for API endpoints.
/// </summary>
public static class RateLimitingConfiguration
{
    public const string AuthPolicy = "auth";
    public const string EventsPolicy = "events";
    public const string ChartPolicy = "chart";

    /// <summary>
    /// Adds rate limiting services with policies for different endpoint types.
    /// </summary>
    public static IServiceCollection AddRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var rateLimitConfig = configuration.GetSection("RateLimiting");

        services.AddRateLimiter(options =>
        {
            // Auth endpoints: 5 requests per minute
            options.AddPolicy(AuthPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitConfig.GetValue<int>("AuthEndpoints:PermitLimit", 5),
                        Window = TimeSpan.FromSeconds(rateLimitConfig.GetValue<int>("AuthEndpoints:Window", 60)),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0 // No queuing for auth
                    }));

            // Event endpoints: 30 requests per minute
            options.AddPolicy(EventsPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitConfig.GetValue<int>("EventEndpoints:PermitLimit", 30),
                        Window = TimeSpan.FromSeconds(rateLimitConfig.GetValue<int>("EventEndpoints:Window", 60)),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 2
                    }));

            // Chart endpoints: 60 requests per minute
            options.AddPolicy(ChartPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitConfig.GetValue<int>("ChartEndpoints:PermitLimit", 60),
                        Window = TimeSpan.FromSeconds(rateLimitConfig.GetValue<int>("ChartEndpoints:Window", 60)),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 5
                    }));

            // Global fallback
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1)
                    });
            });

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        return services;
    }
}

