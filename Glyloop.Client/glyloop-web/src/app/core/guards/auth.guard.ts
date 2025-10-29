import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { catchError, map, of } from 'rxjs';
import { API_CONFIG } from '../config/api.config';

/**
 * Auth guard to protect routes that require authentication.
 * Checks if the user has a valid session by calling /api/auth/session.
 * On 401 or error, redirects to /login with optional redirect parameter.
 */
export const authGuard: CanActivateFn = (route, state) => {
  const router = inject(Router);
  const http = inject(HttpClient);
  const apiConfig = inject(API_CONFIG);

  const sessionUrl = `${apiConfig.baseUrl}/api/auth/session`;

  return http.get(sessionUrl, { withCredentials: true }).pipe(
    map(() => true), // Session valid
    catchError((err) => {
      // Session invalid or error - redirect to login
      const redirectUrl = state.url;
      router.navigate(['/login'], {
        queryParams: redirectUrl !== '/' ? { redirect: redirectUrl } : {}
      });
      return of(false);
    })
  );
};
