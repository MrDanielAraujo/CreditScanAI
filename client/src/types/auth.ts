export interface RegisterRequest {
  email: string
  password: string
  name?: string | null
}

export interface LoginRequest {
  email: string
  password: string
}

export interface UserSummary {
  id: string
  email: string
  name: string | null
  role: string
}

export interface AuthResponse {
  token: string
  user: UserSummary
}

export interface ForgotPasswordRequest {
  email: string
}

export interface ResetPasswordRequest {
  email: string
  token: string
  newPassword: string
}

export interface LoginAuditEntry {
  id: string
  email: string
  success: boolean
  failureReason: string | null
  ipAddress: string | null
  attemptedAt: string
}
