# Environment Configuration

This folder contains environment-specific configuration files for the application.

## Files

### `environment.ts` (Development)
Used during local development (`ng serve`, `ng build` without production flag).

```typescript
export const environment = {
  production: false,
  apiBaseUrl: 'http://localhost:5000'  // Backend API URL
};
```

### `environment.production.ts` (Production)
Used for production builds (`ng build --configuration=production`).

```typescript
export const environment = {
  production: true,
  apiBaseUrl: ''  // Empty for same-origin (relative URLs)
};
```

## How to Change API URL

### During Development

Edit `environment.ts` and change the `apiBaseUrl`:

```typescript
apiBaseUrl: 'http://localhost:5000'     // Local backend
apiBaseUrl: 'http://localhost:5273'     // Different port
apiBaseUrl: 'https://dev-api.example.com'  // Remote dev server
```

### For Production

Edit `environment.production.ts`:

```typescript
apiBaseUrl: ''  // Same origin (recommended)
apiBaseUrl: 'https://api.example.com'  // Different domain
```

## Usage in Code

The API base URL is automatically injected into services via the `API_CONFIG` token:

```typescript
import { inject } from '@angular/core';
import { API_CONFIG } from '../config/api.config';

export class MyService {
  private readonly apiConfig = inject(API_CONFIG);

  getData() {
    const url = `${this.apiConfig.baseUrl}/api/endpoint`;
    // Makes request to: http://localhost:5000/api/endpoint (dev)
    //                or /api/endpoint (production)
  }
}
```

## Build Commands

```bash
# Development build (uses environment.ts)
npm run build

# Production build (uses environment.production.ts)
ng build --configuration=production

# Serve with development environment
npm start
```

## Adding New Environment Variables

To add new configuration options:

1. Add the property to both environment files:
   ```typescript
   export const environment = {
     production: false,
     apiBaseUrl: 'http://localhost:5000',
     newProperty: 'value'  // Add here
   };
   ```

2. Use it in your code:
   ```typescript
   import { environment } from '../environments/environment';
   
   console.log(environment.newProperty);
   ```

## Best Practices

1. **Never commit sensitive data** (API keys, passwords) to environment files
2. **Use relative URLs in production** when possible (empty `apiBaseUrl`)
3. **Document environment variables** in this README
4. **Use environment variables for**:
   - API base URLs
   - Feature flags
   - External service URLs
   - Debug/logging levels

## Current Configuration

### Development
- API Base URL: `http://localhost:5000`
- Production Mode: `false`

### Production
- API Base URL: `` (same origin)
- Production Mode: `true`

## Troubleshooting

### API calls returning 404
Check that `apiBaseUrl` in `environment.ts` matches your backend server URL and port.

### CORS errors
Ensure your backend is configured to allow requests from the frontend origin (e.g., `http://localhost:4200`).

### Wrong environment in build
Make sure you're using the correct build configuration:
- Development: `ng build` or `npm run build`
- Production: `ng build --configuration=production`

