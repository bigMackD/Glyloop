import { InjectionToken } from '@angular/core';

/**
 * API Configuration interface
 */
export interface ApiConfig {
  baseUrl: string;
}

/**
 * Injection token for API configuration
 */
export const API_CONFIG = new InjectionToken<ApiConfig>('api.config');

