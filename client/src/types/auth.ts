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
