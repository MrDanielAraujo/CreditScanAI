import { createContext, useContext } from 'react'
import type { LoginRequest, RegisterRequest, UserSummary } from '../types/auth'

export interface AuthContextValue {
  user: UserSummary | null
  loading: boolean
  login: (req: LoginRequest) => Promise<void>
  register: (req: RegisterRequest) => Promise<void>
  logout: () => void
}

export const AuthContext = createContext<AuthContextValue | null>(null)

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}
