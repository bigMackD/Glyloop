import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

/**
 * HTTP interceptor that handles 401 Unauthorized responses.
 * Redirects to login page with optional redirect parameter.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);

  return next(req).pipe(
    catchError((error) => {
      if (error.status === 401) {
        // Get current URL for redirect after login
        const currentUrl = router.url;

        // Clear any cached data
        // Services will be notified via the navigation

        // Redirect to login with redirect parameter (unless already on login/register)
        if (!currentUrl.includes('/login') && !currentUrl.includes('/register')) {
          router.navigate(['/login'], {
            queryParams: {
              reason: 'sessionExpired',
              redirect: currentUrl
            }
          });
        }
      }

      return throwError(() => error);
    })
  );
};
