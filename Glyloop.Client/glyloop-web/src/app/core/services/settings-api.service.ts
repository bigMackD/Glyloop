import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  TirPreferencesDto,
  UpdatePreferencesRequestDto
} from '../models/settings.types';
import { API_CONFIG } from '../config/api.config';

/**
 * Service for account preferences API operations
 */
@Injectable({ providedIn: 'root' })
export class SettingsApiService {
  private readonly http = inject(HttpClient);
  private readonly apiConfig = inject(API_CONFIG);

  private buildUrl(endpoint: string): string {
    return `${this.apiConfig.baseUrl}${endpoint}`;
  }

  /**
   * GET /api/account/preferences
   * Retrieves the user's TIR preferences
   */
  getPreferences(): Observable<TirPreferencesDto> {
    return this.http.get<TirPreferencesDto>(
      this.buildUrl('/api/account/preferences'),
      { withCredentials: true }
    );
  }

  /**
   * PUT /api/account/preferences
   * Updates the user's TIR preferences
   */
  updatePreferences(req: UpdatePreferencesRequestDto): Observable<void> {
    return this.http.put<void>(
      this.buildUrl('/api/account/preferences'),
      req,
      { withCredentials: true }
    );
  }
}
