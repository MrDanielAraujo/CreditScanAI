import { apiGet, apiPost } from './apiClient'
import type { AuthResponse, LoginRequest, RegisterRequest, UserSummary } from '../types/auth'

export const authApi = {
  register: (req: RegisterRequest) => apiPost<AuthResponse>('/api/auth/register', req),
  login: (req: LoginRequest) => apiPost<AuthResponse>('/api/auth/login', req),
  me: () => apiGet<UserSummary>('/api/auth/me'),
}
