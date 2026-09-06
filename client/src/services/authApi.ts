import { apiGet, apiPost } from './apiClient'
import type {
  AuthResponse,
  ForgotPasswordRequest,
  LoginAuditEntry,
  LoginRequest,
  RegisterRequest,
  ResetPasswordRequest,
  UserSummary,
} from '../types/auth'

export const authApi = {
  register: (req: RegisterRequest) => apiPost<AuthResponse>('/api/auth/register', req),
  login: (req: LoginRequest) => apiPost<AuthResponse>('/api/auth/login', req),
  me: () => apiGet<UserSummary>('/api/auth/me'),
  forgotPassword: (req: ForgotPasswordRequest) => apiPost<{ message: string }>('/api/auth/forgot-password', req),
  resetPassword: (req: ResetPasswordRequest) => apiPost<{ message: string }>('/api/auth/reset-password', req),
  getLoginAudit: () => apiGet<LoginAuditEntry[]>('/api/auth/login-audit'),
}
