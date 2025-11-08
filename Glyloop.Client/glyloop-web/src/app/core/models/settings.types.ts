// DTOs (backend contracts)

export interface TirPreferencesDto {
  tirLowerBound: number;
  tirUpperBound: number;
}

export interface UpdatePreferencesRequestDto {
  tirLowerBound: number;
  tirUpperBound: number;
}

export interface DexcomStatusDto {
  isLinked: boolean;
  linkedAt: string | null;
  tokenExpiresAt: string | null;
  lastSyncAt: string | null;
}

export interface LinkDexcomRequestDto {
  authorizationCode: string;
}

export interface LinkDexcomResponseDto {
  linkId: string;
  linkedAt: string;
  tokenExpiresAt: string;
}

// View models

export interface AccountPreferencesVM {
  lower: number;
  upper: number;
  initialLower: number;
  initialUpper: number;
  isDirty: boolean;
  isValid: boolean;
  errors: ValidationErrors;
  saving: boolean;
}

export interface ValidationErrors {
  lower?: string;
  upper?: string;
  cross?: string;
}

export interface DexcomLinkVM {
  status: DexcomStatusDto | null;
  loading: boolean;
  linking: boolean;
  unlinking: boolean;
  error?: string;
}

export type SettingsRouteKey = 'account' | 'data-sources' | 'display' | 'system';

export interface SystemInfo {
  appVersion: string;
  environment: string;
  healthUrl: string;
}
