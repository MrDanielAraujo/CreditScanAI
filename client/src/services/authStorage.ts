const TOKEN_KEY = 'creditscanai_auth_token'
const SESSION_EXPIRED_KEY = 'creditscanai_session_expired'

/** Disparado quando uma chamada autenticada volta 401 - AuthContext escuta isso pra deslogar. */
export const AUTH_UNAUTHORIZED_EVENT = 'creditscanai:auth-unauthorized'

export function getStoredToken(): string | null {
  try {
    return localStorage.getItem(TOKEN_KEY)
  } catch {
    return null
  }
}

export function setStoredToken(token: string): void {
  try {
    localStorage.setItem(TOKEN_KEY, token)
  } catch {
    // Private/blocked storage - the session just won't persist across reloads.
  }
}

export function clearStoredToken(): void {
  try {
    localStorage.removeItem(TOKEN_KEY)
  } catch {
    // Nothing to do if storage is unavailable.
  }
}

/**
 * Chamado pelo apiClient quando uma chamada autenticada volta 401 com token
 * ausente/expirado/inválido (nunca pra senha errada no login, ou pra 403 de
 * permissão insuficiente - só o caso "essa sessão não vale mais"). Limpa o
 * token, marca a flag que a tela de login lê pra mostrar o aviso, e avisa
 * quem estiver ouvindo (AuthContext) pra deslogar - o que dispara o redirect
 * automático do ProtectedRoute.
 */
export function handleUnauthorized(): void {
  clearStoredToken()
  try {
    sessionStorage.setItem(SESSION_EXPIRED_KEY, '1')
  } catch {
    // Sem storage, a tela de login só não mostra o aviso - não é crítico.
  }
  window.dispatchEvent(new Event(AUTH_UNAUTHORIZED_EVENT))
}

/** Lido uma vez pela LoginPage ao montar, pra mostrar "sua sessão expirou" - consome a flag. */
export function consumeSessionExpiredFlag(): boolean {
  try {
    const flagged = sessionStorage.getItem(SESSION_EXPIRED_KEY) === '1'
    sessionStorage.removeItem(SESSION_EXPIRED_KEY)
    return flagged
  } catch {
    return false
  }
}
