import { useCallback, useEffect, useState, type ReactNode } from 'react'
import { authApi } from '../services/authApi'
import { clearStoredToken, getStoredToken, setStoredToken } from '../services/authStorage'
import type { LoginRequest, RegisterRequest, UserSummary } from '../types/auth'
import { AuthContext } from './authContextValue'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserSummary | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (!getStoredToken()) {
      setLoading(false)
      return
    }
    authApi
      .me()
      .then(setUser)
      .catch(() => clearStoredToken())
      .finally(() => setLoading(false))
  }, [])

  const login = useCallback(async (req: LoginRequest) => {
    const response = await authApi.login(req)
    setStoredToken(response.token)
    setUser(response.user)
  }, [])

  const register = useCallback(async (req: RegisterRequest) => {
    const response = await authApi.register(req)
    setStoredToken(response.token)
    setUser(response.user)
  }, [])

  const logout = useCallback(() => {
    clearStoredToken()
    setUser(null)
  }, [])

  return <AuthContext.Provider value={{ user, loading, login, register, logout }}>{children}</AuthContext.Provider>
}
