import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  DexcomStatusDto,
  LinkDexcomRequestDto,
  LinkDexcomResponseDto
} from '../models/settings.types';
import { API_CONFIG } from '../config/api.config';

/**
 * Service for Dexcom integration API operations
 */
@Injectable({ providedIn: 'root' })
export class DexcomApiService {
  private readonly http = inject(HttpClient);
  private readonly apiConfig = inject(API_CONFIG);

  private buildUrl(endpoint: string): string {
    return `${this.apiConfig.baseUrl}${endpoint}`;
  }

  /**
   * GET /api/dexcom/status
   * Retrieves the current Dexcom link status
   */
  getDexcomStatus(): Observable<DexcomStatusDto> {
    return this.http.get<DexcomStatusDto>(
      this.buildUrl('/api/dexcom/status'),
      { withCredentials: true }
    );
  }

  /**
   * POST /api/dexcom/link
   * Links a Dexcom account using an authorization code
   */
  linkDexcom(code: string): Observable<LinkDexcomResponseDto> {
    const request: LinkDexcomRequestDto = { authorizationCode: code };
    return this.http.post<LinkDexcomResponseDto>(
      this.buildUrl('/api/dexcom/link'),
      request,
      { withCredentials: true }
    );
  }

  /**
   * DELETE /api/dexcom/unlink
   * Unlinks the connected Dexcom account
   */
  unlinkDexcom(): Observable<void> {
    return this.http.delete<void>(
      this.buildUrl('/api/dexcom/unlink'),
      { withCredentials: true }
    );
  }

  /**
   * Redirects to the Dexcom OAuth authorization flow
   */
  startOAuthFlow(): void {
    window.location.href = this.buildUrl('/api/dexcom/authorize');
  }
}
