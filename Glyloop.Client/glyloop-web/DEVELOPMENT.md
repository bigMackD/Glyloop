# Development Guide

## Running the Application

### Start Development Server

**Important:** Make sure you're in the correct directory!

```bash
# Navigate to the web app directory
cd Glyloop.Client/glyloop-web

# Start the development server
npm start
```

The application will be available at: `http://localhost:4200`

### Common Commands

All commands should be run from `Glyloop.Client/glyloop-web/`:

```bash
# Start development server
npm start

# Build for production
npm run build

# Run linter
npm run lint

# Fix linting issues
npm run lint:fix

# Extract i18n messages
ng extract-i18n --output-path src/locale
```

## Project Structure

```
Glyloop.Client/glyloop-web/
├── src/
│   ├── app/
│   │   ├── core/
│   │   │   ├── config/       # Configuration (API, etc.)
│   │   │   ├── models/       # TypeScript types
│   │   │   └── services/     # API services
│   │   ├── features/
│   │   │   └── auth/
│   │   │       ├── login/    # Login feature
│   │   │       └── register/ # Registration feature
│   │   ├── app.config.ts     # Application configuration
│   │   ├── app.routes.ts     # Route definitions
│   │   └── app.ts            # Root component
│   ├── environments/         # Environment configurations
│   ├── locale/               # Translation files
│   ├── styles/               # Global styles
│   └── main.ts               # Application entry point
├── docs/                     # Documentation
├── angular.json              # Angular CLI configuration
├── package.json              # NPM dependencies
└── tsconfig.app.json         # TypeScript configuration
```

## API Configuration

### Configuring Backend URL

The API base URL is configured in environment files:

**Development (`src/environments/environment.ts`):**
```typescript
export const environment = {
  production: false,
  apiBaseUrl: 'http://localhost:5000'  // Change this to your backend URL
};
```

**Production (`src/environments/environment.production.ts`):**
```typescript
export const environment = {
  production: true,
  apiBaseUrl: ''  // Empty for same-origin (relative URLs)
};
```

### Changing the Backend URL

1. **For local development**, edit `src/environments/environment.ts`:
   ```typescript
   apiBaseUrl: 'http://localhost:5000'     // ASP.NET default
   apiBaseUrl: 'http://localhost:5273'     // Different port
   apiBaseUrl: 'https://dev.example.com'   // Remote server
   ```

2. **Restart the dev server** after changing:
   ```bash
   # Stop server (Ctrl+C)
   npm start
   ```

3. The frontend will now make API calls to:
   - Development: `http://localhost:5000/api/auth/register`
   - Production: `/api/auth/register` (same origin)

See `src/environments/README.md` for detailed environment configuration guide.

## Troubleshooting

### Wrong Directory Error

If you see:
```
npm error enoent Could not read package.json
```

**Solution:** You're in the wrong directory. Navigate to the correct one:
```bash
cd Glyloop.Client/glyloop-web
```

### $localize is not defined

**Solution:** Ensure `src/main.ts` has the import at the very top:
```typescript
import '@angular/localize/init';
```

Then restart the dev server:
```bash
# Stop the server (Ctrl+C)
npm start
```

### Schema Validation Error

If you see: `must NOT have additional properties(polyfills)`

**Solution:** Remove the `polyfills` option from the `serve` section in `angular.json`. It should only be in the `build` section.

### Port Already in Use

If port 4200 is already in use:
```bash
# Use a different port
ng serve --port 4201
```

## Testing Routes

After starting the dev server, test these URLs:

- `http://localhost:4200/` - Redirects to `/register`
- `http://localhost:4200/register` - Registration page
- `http://localhost:4200/login` - Login page (placeholder)

## Development Workflow

1. **Make changes** to code
2. **Save** - Hot reload will update the browser automatically
3. **Check console** for errors
4. **Run linter** before committing:
   ```bash
   npm run lint
   ```
5. **Build** to verify production build works:
   ```bash
   npm run build
   ```

## Build Output

After building, check the output in `dist/glyloop-web/browser/`:

```
dist/glyloop-web/browser/
├── chunk-*.js         # Application code chunks
├── main-*.js          # Main application bundle
├── polyfills-*.js     # Polyfills (includes @angular/localize)
├── styles-*.css       # Compiled styles
├── index.html         # Entry HTML
└── favicon.ico        # Application icon
```

## IDE Setup

### VS Code

Recommended extensions:
- Angular Language Service
- ESLint
- Prettier
- XLIFF Sync (for translations)

### WebStorm/IntelliJ

Angular support is built-in. Enable:
- Angular Language Service
- ESLint integration
- Prettier integration

## Next Steps

- See `docs/I18N_IMPLEMENTATION.md` for internationalization guide
- See `src/locale/README.md` for translation workflow
- See `.cursor/rules/frontend.md` for coding standards

