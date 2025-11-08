/**
 * Playwright Global Setup
 * Runs once before all tests to seed test data into the database
 * 
 * This script executes the Glyloop.DataSeeder tool to ensure
 * the test user exists before running e2e tests.
 * 
 * SKIPS seeding in UI/debug mode (set SKIP_SEEDING=true to skip manually)
 */

import { execSync } from 'child_process';
import * as path from 'path';

async function globalSetup() {
  // Skip seeding if explicitly disabled or in UI/headed/debug mode
  const skipSeeding = process.env.SKIP_SEEDING === 'true' || 
                      process.argv.includes('--ui') ||
                      process.argv.includes('--headed') ||
                      process.argv.includes('--debug');

  if (skipSeeding) {
    console.log('\n⏭️  Skipping test data seeding (UI/debug mode or SKIP_SEEDING=true)\n');
    console.log('💡 To seed manually, run: npm run seed:test\n');
    return;
  }

  console.log('\n🌱 Seeding test data...\n');

  try {
    // Path to the DataSeeder project (relative to glyloop-web)
    const seederProjectPath = path.join(
      __dirname,
      '..',
      '..',
      '..',
      'Glyloop.API',
      'Tools',
      'Glyloop.DataSeeder'
    );

    // Execute the seeder tool
    const command = `dotnet run --project "${seederProjectPath}" --environment Test`;
    
    console.log(`Executing: ${command}\n`);
    
    execSync(command, {
      stdio: 'inherit', // Show output in console
      cwd: path.join(__dirname, '..', '..', '..'), // Run from repo root
    });

    console.log('\n✓ Test data seeding completed\n');
  } catch (error) {
    console.error('\n✗ Failed to seed test data:', error);
    process.exit(1); // Fail the test run if seeding fails
  }
}

export default globalSetup;

