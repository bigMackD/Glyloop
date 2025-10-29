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
  confirmPassword: string;
}

export interface RegisterUiState {
  isSubmitting: boolean;
  serverError?: string; // top-level error (network/unknown)
  emailTaken?: boolean; // 409 specialization
  success?: boolean;
}

// Login API contracts
export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  userId: string; // Guid
  email: string;
}

// Login view models
export interface LoginFormModel {
  email: string;
  password: string;
}

export interface LoginUiState {
  isSubmitting: boolean;
  serverError?: string; // general error to show above form
  fieldErrors?: {
    email?: string;
    password?: string;
  };
}

