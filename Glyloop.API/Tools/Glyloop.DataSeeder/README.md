# Glyloop Data Seeder

A command-line tool for seeding test data into the Glyloop database for e2e testing purposes.

## Purpose

This tool creates test user accounts with known credentials to support automated end-to-end testing with Playwright. It ensures that the required test data exists in the database before running tests.

## Features

- **Idempotent**: Safe to run multiple times - skips user creation if the user already exists
- **Environment-Safe**: Only allows seeding in `Test` or `Development` environments to prevent accidental production data modification
- **Auto-Migration**: Automatically creates database and applies EF Core migrations before seeding
- **Database Validation**: Verifies database connectivity before attempting to seed
- **Automatic Integration**: Runs automatically before Playwright e2e tests via global setup

## Test User Credentials

The tool creates a single test user with the following credentials:

- **Email**: `test@test.com`
- **Password**: `TestPassword123!`
- **Email Confirmed**: `true` (bypasses email verification)
- **TIR Bounds**: 70-180 mg/dL (default)

## Usage

### Manual Execution

Run the seeder manually from the repository root:

```bash
# Seed test database (default)
dotnet run --project Glyloop.API/Tools/Glyloop.DataSeeder

# Explicitly specify environment
dotnet run --project Glyloop.API/Tools/Glyloop.DataSeeder --environment Test

# Seed development database
dotnet run --project Glyloop.API/Tools/Glyloop.DataSeeder --environment Development
```

### From Angular Project

When working in the `glyloop-web` directory, use npm scripts:

```bash
# Seed test database
npm run seed:test

# Seed development database
npm run seed:dev
```

### Automatic Execution with Playwright

The seeder runs automatically when executing Playwright tests in normal mode:

```bash
# Standard e2e test execution (runs seeder via global setup)
npm run e2e
```

**UI, Headed, and Debug modes skip seeding** to speed up development:

```bash
# Interactive UI mode (SKIPS seeding)
npm run e2e:ui

# Headed mode - visible browser (SKIPS seeding)
npm run e2e:headed

# Debug mode (SKIPS seeding)
npm run e2e:debug
```

The global setup in `e2e/global-setup.ts` automatically detects UI/debug mode and skips seeding. You can manually seed before using UI mode:

```bash
# Seed manually, then use UI mode
npm run seed:test
npm run e2e:ui
```

You can also force skip seeding with an environment variable:

```bash
# Skip seeding even in normal test mode
SKIP_SEEDING=true npm run e2e
```

## Configuration

The seeder uses `appsettings.json` configuration files:

- `appsettings.json` - Default configuration (test database)
- `appsettings.Test.json` - Test environment overrides
- `appsettings.Development.json` - Development environment overrides

### Database Connection

Update the connection string in the appropriate `appsettings.{Environment}.json` file:

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Database=glyloop_test;Username=postgres;Password=postgres;Port=5432"
  }
}
```

**Important**: The database will be created automatically if it doesn't exist. The seeder will:
1. Check if the database exists
2. Create it if needed
3. Apply all EF Core migrations
4. Seed the test user

You only need to ensure PostgreSQL is running and the credentials are correct.

## Environment Restrictions

For safety, the seeder **only** allows execution in these environments:
- `Test`
- `Development`

Attempting to run in any other environment (e.g., `Production`, `Staging`) will fail with an error message.

## Exit Codes

The seeder returns standard exit codes for scripting:

- `0` - Success (user created or already exists)
- `1` - Error (database connection failure, user creation error, invalid environment)

## Architecture

The seeder leverages the existing Glyloop infrastructure:

- **Infrastructure Layer**: Reuses `GlyloopDbContext`, `ApplicationUser`, and Identity configuration
- **Dependency Injection**: Uses Microsoft.Extensions.Hosting for DI setup
- **ASP.NET Core Identity**: Uses `UserManager<ApplicationUser>` for user creation
- **Logging**: Microsoft.Extensions.Logging for console output

## Integration with E2E Tests

The seeder is integrated into the Playwright test pipeline:

1. **Global Setup** (`e2e/global-setup.ts`): Executes seeder before test suite
2. **Playwright Config** (`playwright.config.ts`): References global setup
3. **Package Scripts** (`package.json`): Provides convenient npm commands

### Test Workflow

```
npm run e2e
  ↓
Playwright starts
  ↓
Global setup runs
  ↓
Seeder executes (creates test user if needed)
  ↓
Tests execute (user exists in database)
  ↓
Report results
```

## Troubleshooting

### Database Connection Error

If you see "Cannot connect to database":

1. Ensure PostgreSQL is running
2. Verify connection string in `appsettings.{Environment}.json`
3. Verify PostgreSQL credentials have appropriate permissions

**Note**: The database will be created automatically if it doesn't exist. You don't need to manually create it or run migrations separately.

### User Creation Fails

If user creation fails with validation errors:

1. Check password meets requirements (12+ characters)
2. Verify email format is valid
3. Check Identity configuration in `Glyloop.Infrastructure/DependencyInjection.cs`

### Environment Not Allowed

If you see "Environment 'X' is not allowed":

- Only `Test` and `Development` environments are permitted
- Use `--environment Test` or `--environment Development`

## Development Notes

### Adding New Test Users

To add additional test users:

1. Open `Services/TestDataSeeder.cs`
2. Add new user creation logic to `SeedTestUserAsync()` method
3. Follow the same idempotent pattern (check existence first)

### Adding Test Data Beyond Users

To seed additional data (events, preferences, etc.):

1. Create new methods in `TestDataSeeder` class
2. Call from `SeedTestUserAsync()` or add new public methods
3. Update `Program.cs` to execute new seeding methods
4. Maintain idempotency (check before creating)

## Related Documentation

- [Playwright E2E Tests](../../../Glyloop.Client/glyloop-web/e2e/README.md)
- [Database Plan](../../../.ai/plans/database/db-plan.md)
- [Test Plan](../../../TEST_PLAN.md)

