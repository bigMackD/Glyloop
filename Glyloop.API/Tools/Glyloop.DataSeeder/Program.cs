using Glyloop.DataSeeder.Services;
using Glyloop.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Glyloop.DataSeeder;

/// <summary>
/// Data seeding tool for e2e testing.
/// Seeds test user accounts into the database.
/// 
/// Usage: 
///   dotnet run --project Tools/Glyloop.DataSeeder
///   dotnet run --project Tools/Glyloop.DataSeeder --environment Test
///   dotnet run --project Tools/Glyloop.DataSeeder --environment Development
/// </summary>
public class Program
{
    private static readonly string[] AllowedEnvironments = { "Test", "Development" };

    public static async Task<int> Main(string[] args)
    {
        try
        {
            // Parse environment argument
            var environment = GetEnvironmentFromArgs(args);

            // Validate environment (safety check)
            if (!IsEnvironmentAllowed(environment))
            {
                Console.Error.WriteLine($"ERROR: Environment '{environment}' is not allowed for seeding.");
                Console.Error.WriteLine($"Allowed environments: {string.Join(", ", AllowedEnvironments)}");
                Console.Error.WriteLine("This is a safety measure to prevent accidental production data seeding.");
                return 1;
            }

            Console.WriteLine($"=== Glyloop Test Data Seeder ===");
            Console.WriteLine($"Environment: {environment}");
            Console.WriteLine();

            // Build host with DI
            var host = CreateHostBuilder(args, environment).Build();

            // Get seeder service and execute
            using var scope = host.Services.CreateScope();
            var seeder = scope.ServiceProvider.GetRequiredService<TestDataSeeder>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            // Verify database connection
            logger.LogInformation("Checking database connectivity...");
            var canConnect = await seeder.VerifyDatabaseConnectionAsync();
            if (!canConnect)
            {
                logger.LogError("Cannot connect to database. Please check connection string and ensure PostgreSQL is running.");
                return 1;
            }

            // Ensure database exists and migrations are applied
            logger.LogInformation("Ensuring database is ready...");
            var isDatabaseReady = await seeder.EnsureDatabaseAsync();
            if (!isDatabaseReady)
            {
                logger.LogError("Failed to prepare database. Check migrations and database permissions.");
                return 1;
            }

            // Execute seeding
            logger.LogInformation("Starting seeding process...");
            var wasCreated = await seeder.SeedTestUserAsync();

            // Report results
            Console.WriteLine();
            if (wasCreated)
            {
                Console.WriteLine("✓ Test data seeding completed successfully!");
                Console.WriteLine("  - Test user created: test@test.com");
            }
            else
            {
                Console.WriteLine("✓ Test data seeding completed (user already exists)");
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"✗ FATAL ERROR: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    /// <summary>
    /// Creates and configures the host builder with DI and configuration.
    /// </summary>
    private static IHostBuilder CreateHostBuilder(string[] args, string environment)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                // Set environment
                context.HostingEnvironment.EnvironmentName = environment;

                // Load configuration files
                config.SetBasePath(Directory.GetCurrentDirectory());
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
                config.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false);
                config.AddEnvironmentVariables();
                config.AddCommandLine(args);
            })
            .ConfigureServices((context, services) =>
            {
                // Register Infrastructure layer (DbContext, Identity, Repositories)
                services.AddInfrastructure(context.Configuration);

                // Register seeding service
                services.AddScoped<TestDataSeeder>();
            })
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            });
    }

    /// <summary>
    /// Extracts environment name from command line arguments.
    /// Default: "Test"
    /// </summary>
    private static string GetEnvironmentFromArgs(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals("--environment", StringComparison.OrdinalIgnoreCase) ||
                args[i].Equals("-e", StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return "Test"; // Default environment
    }

    /// <summary>
    /// Validates that the environment is allowed for seeding.
    /// Only Test and Development environments are permitted.
    /// </summary>
    private static bool IsEnvironmentAllowed(string environment)
    {
        return AllowedEnvironments.Contains(environment, StringComparer.OrdinalIgnoreCase);
    }
}

