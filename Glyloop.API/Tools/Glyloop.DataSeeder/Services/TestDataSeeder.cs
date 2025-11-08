using Glyloop.Infrastructure.Identity;
using Glyloop.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Glyloop.DataSeeder.Services;

/// <summary>
/// Service responsible for seeding test user data into the database for e2e testing.
/// Implements idempotent operations - safe to run multiple times.
/// </summary>
public class TestDataSeeder
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly GlyloopDbContext _context;
    private readonly ILogger<TestDataSeeder> _logger;

    // Test user credentials
    private const string TestUserEmail = "test@test.com";
    private const string TestUserPassword = "TestPassword123!";

    public TestDataSeeder(
        UserManager<ApplicationUser> userManager,
        GlyloopDbContext context,
        ILogger<TestDataSeeder> logger)
    {
        _userManager = userManager;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seeds the test user account into the database.
    /// Idempotent - skips if user already exists.
    /// </summary>
    /// <returns>True if user was created, false if already exists</returns>
    public async Task<bool> SeedTestUserAsync()
    {
        _logger.LogInformation("Starting test data seeding...");

        // Check if test user already exists (idempotent)
        var existingUser = await _userManager.FindByEmailAsync(TestUserEmail);
        if (existingUser != null)
        {
            _logger.LogInformation("Test user '{Email}' already exists. Skipping creation.", TestUserEmail);
            return false;
        }

        // Create test user
        var testUser = new ApplicationUser
        {
            UserName = TestUserEmail,
            Email = TestUserEmail,
            EmailConfirmed = true, // Bypass email confirmation for testing
            CreatedAt = DateTimeOffset.UtcNow,
            TirLowerBound = 70,  // Default TIR lower bound
            TirUpperBound = 180  // Default TIR upper bound
        };

        _logger.LogInformation("Creating test user '{Email}'...", TestUserEmail);

        var result = await _userManager.CreateAsync(testUser, TestUserPassword);

        if (result.Succeeded)
        {
            _logger.LogInformation(
                "✓ Test user '{Email}' created successfully with ID: {UserId}",
                TestUserEmail,
                testUser.Id);
            return true;
        }
        else
        {
            _logger.LogError("Failed to create test user '{Email}'. Errors:", TestUserEmail);
            foreach (var error in result.Errors)
            {
                _logger.LogError("  - {Code}: {Description}", error.Code, error.Description);
            }
            throw new InvalidOperationException(
                $"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }

    /// <summary>
    /// Verifies database connectivity before seeding.
    /// </summary>
    public async Task<bool> VerifyDatabaseConnectionAsync()
    {
        try
        {
            _logger.LogInformation("Verifying database connection...");
            await _context.Database.CanConnectAsync();
            _logger.LogInformation("✓ Database connection verified");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "✗ Failed to connect to database");
            return false;
        }
    }

    /// <summary>
    /// Ensures database is created and all migrations are applied.
    /// Creates the database if it doesn't exist and applies pending migrations.
    /// </summary>
    public async Task<bool> EnsureDatabaseAsync()
    {
        try
        {
            _logger.LogInformation("Checking database state...");

            // Check if there are any pending migrations
            var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
            var pendingList = pendingMigrations.ToList();

            if (pendingList.Any())
            {
                _logger.LogInformation("Found {Count} pending migration(s). Applying migrations...", pendingList.Count);
                foreach (var migration in pendingList)
                {
                    _logger.LogInformation("  - {Migration}", migration);
                }

                // Apply all pending migrations (creates database if needed)
                await _context.Database.MigrateAsync();
                _logger.LogInformation("✓ Database migrations applied successfully");
            }
            else
            {
                // Database exists and is up to date
                _logger.LogInformation("✓ Database is up to date (no pending migrations)");
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "✗ Failed to ensure database is ready");
            return false;
        }
    }
}

