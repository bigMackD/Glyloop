using Glyloop.Domain.Common;
using Glyloop.Domain.Repositories;
using Glyloop.Infrastructure.Identity;
using Glyloop.Infrastructure.Persistence;
using Glyloop.Infrastructure.Persistence.Repositories;
using Glyloop.Infrastructure.Services;
using Glyloop.Infrastructure.Services.Dexcom;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace Glyloop.Infrastructure;

/// <summary>
/// Infrastructure layer dependency injection configuration.
/// Registers all Infrastructure services, repositories, DbContext, and external clients.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds all Infrastructure services to the DI container.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // MediatR for domain event dispatching
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        // Database and persistence
        AddPersistence(services, configuration);

        // ASP.NET Core Identity
        AddIdentity(services);

        // Cross-cutting services
        AddServices(services);

        // External API clients
        AddExternalServices(services, configuration);

        return services;
    }

    /// <summary>
    /// Registers database context, repositories, and unit of work.
    /// </summary>
    private static void AddPersistence(
        IServiceCollection services,
        IConfiguration configuration)
    {
        // PostgreSQL DbContext with Npgsql
        services.AddDbContext<GlyloopDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("Default");
            
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                // Retry on transient failures (network issues, etc.)
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);

                // Command timeout
                npgsqlOptions.CommandTimeout(30);

                // Migrations assembly (if migrations are in Infrastructure)
                npgsqlOptions.MigrationsAssembly(typeof(GlyloopDbContext).Assembly.FullName);
            });

            // Development logging (enable sensitive data logging if configured)
            var enableSensitiveLogging = configuration.GetValue<bool>("Logging:EnableSensitiveDataLogging");
            if (enableSensitiveLogging)
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        // Repositories
        services.AddScoped<IDexcomLinkRepository, DexcomLinkRepository>();
        services.AddScoped<IEventRepository, EventRepository>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }

    /// <summary>
    /// Registers ASP.NET Core Identity with custom ApplicationUser.
    /// </summary>
    private static void AddIdentity(IServiceCollection services)
    {
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            // Password policy per PRD
            options.Password.RequiredLength = 12;
            options.Password.RequireDigit = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireNonAlphanumeric = false;

            // Sign-in settings
            options.SignIn.RequireConfirmedEmail = false; // MVP: no email verification
            options.SignIn.RequireConfirmedAccount = false;

            // User settings
            options.User.RequireUniqueEmail = true;
            options.User.AllowedUserNameCharacters =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
        })
        .AddEntityFrameworkStores<GlyloopDbContext>()
        .AddDefaultTokenProviders();
    }

    /// <summary>
    /// Registers cross-cutting services (time provider, encryption).
    /// </summary>
    private static void AddServices(IServiceCollection services)
    {
        // Time Provider
        services.AddSingleton<ITimeProvider, SystemTimeProvider>();

        // Data Protection for token encryption
        // Note: Key persistence and application name should be configured in the API layer
        // using PersistKeysToFileSystem, PersistKeysToAzureKeyVault, etc.
        services.AddDataProtection();

        // Token Encryption Service
        services.AddScoped<ITokenEncryptionService, TokenEncryptionService>();
    }

    /// <summary>
    /// Registers external API clients with resilience policies.
    /// </summary>
    private static void AddExternalServices(
        IServiceCollection services,
        IConfiguration configuration)
    {
        // Dexcom API Client with Polly resilience
        services.AddHttpClient<IDexcomApiClient, DexcomApiClient>(client =>
        {
            var baseUrl = configuration["Dexcom:BaseUrl"] ?? "https://sandbox-api.dexcom.com";
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());
    }

    /// <summary>
    /// Retry policy with exponential backoff for transient HTTP errors.
    /// Retries 3 times with delays: 2s, 4s, 8s.
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError() // 5xx and 408 errors
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests) // 429
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    // Log retry attempt (ILogger will be available via DI in actual usage)
                    Console.WriteLine($"Retry {retryCount} after {timespan.TotalSeconds}s due to {outcome.Result?.StatusCode}");
                });
    }

    /// <summary>
    /// Circuit breaker policy to prevent cascading failures.
    /// Opens circuit after 5 consecutive failures, stays open for 1 minute.
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromMinutes(1),
                onBreak: (outcome, duration) =>
                {
                    Console.WriteLine($"Circuit breaker opened for {duration.TotalSeconds}s");
                },
                onReset: () =>
                {
                    Console.WriteLine("Circuit breaker reset");
                });
    }
}

