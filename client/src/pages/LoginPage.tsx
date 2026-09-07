import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Button } from '../components/common/Button'
import { useAuth } from '../contexts/authContextValue'
import { consumeSessionExpiredFlag } from '../services/authStorage'

export function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const [sessionExpired] = useState(consumeSessionExpiredFlag)

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()
    setLoading(true)
    setError(null)
    try {
      await login({ email, password })
      navigate('/')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao entrar')
      setPassword('')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="flex h-screen items-center justify-center bg-surface-muted">
      <form onSubmit={handleSubmit} className="w-full max-w-sm rounded-lg border border-neutral/20 bg-surface p-6 shadow-sm">
        <h1 className="text-xl font-semibold">CreditScanAI</h1>
        <p className="mt-1 text-sm text-neutral">Entre com sua conta.</p>

        {sessionExpired && (
          <p className="mt-3 rounded-md bg-warning/10 p-2 text-sm text-warning">
            Sua sessão expirou. Faça login novamente.
          </p>
        )}

        <label className="mt-6 block text-sm font-medium">Email</label>
        <input
          type="email"
          required
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          className="mt-1 w-full h-10 rounded-md border border-neutral/30 px-3 py-2 text-sm"
        />

        <label className="mt-4 block text-sm font-medium">Senha</label>
        <input
          type="password"
          required
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          className="mt-1 w-full h-10 rounded-md border border-neutral/30 px-3 py-2 text-sm"
        />

        {error && <p className="mt-3 text-sm text-error">{error}</p>}

        <Button type="submit" className="mt-6 w-full" loading={loading}>
          Entrar
        </Button>

        <p className="mt-3 text-center text-sm">
          <Link to="/forgot-password" className="text-primary hover:underline">
            Esqueceu sua senha?
          </Link>
        </p>

        <p className="mt-2 text-center text-sm text-neutral">
          Não tem conta?{' '}
          <Link to="/register" className="text-primary hover:underline">
            Cadastre-se
          </Link>
        </p>
      </form>
    </div>
  )
}
