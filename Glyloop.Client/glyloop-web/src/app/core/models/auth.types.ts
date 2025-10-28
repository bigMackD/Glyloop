// Backend API contracts
export interface RegisterRequest {
  email: string;
  password: string; // min 12 chars
}

export interface RegisterResponse {
  userId: string; // Guid
  email: string;
  registeredAt: string; // ISO Date
}

// View models
export interface RegisterFormModel {
  email: string;
  password: string;
}

export interface RegisterUiState {
  isSubmitting: boolean;
  serverError?: string; // top-level error (network/unknown)
  emailTaken?: boolean; // 409 specialization
  success?: boolean;
}

