import { useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { Button } from '../components/common/Button'
import { authApi } from '../services/authApi'

export function ResetPasswordPage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const email = searchParams.get('email') ?? ''
  const token = searchParams.get('token') ?? ''

  const [newPassword, setNewPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [message, setMessage] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  const missingLinkData = !email || !token

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()
    setLoading(true)
    setError(null)
    try {
      const res = await authApi.resetPassword({ email, token, newPassword })
      setMessage(res.message)
      setTimeout(() => navigate('/login'), 1500)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao redefinir a senha')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="flex h-screen items-center justify-center bg-surface-muted">
      <form onSubmit={handleSubmit} className="w-full max-w-sm rounded-lg border border-neutral/20 bg-surface p-6 shadow-sm">
        <h1 className="text-xl font-semibold">Redefinir senha</h1>

        {missingLinkData && (
          <p className="mt-3 text-sm text-error">
            Link inválido ou incompleto. Solicite um novo em{' '}
            <Link to="/forgot-password" className="text-primary hover:underline">
              Esqueceu sua senha?
            </Link>
          </p>
        )}

        {!missingLinkData && (
          <>
            <p className="mt-1 text-sm text-neutral">Definindo nova senha para {email}.</p>

            <label className="mt-6 block text-sm font-medium">Nova senha</label>
            <input
              type="password"
              required
              minLength={8}
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              className="mt-1 w-full h-10 rounded-md border border-neutral/30 px-3 py-2 text-sm"
            />
            <p className="mt-1 text-xs text-neutral">Mínimo de 8 caracteres.</p>

            {message && <p className="mt-3 text-sm text-success">{message}</p>}
            {error && <p className="mt-3 text-sm text-error">{error}</p>}

            <Button type="submit" className="mt-6 w-full" loading={loading}>
              Redefinir senha
            </Button>
          </>
        )}
      </form>
    </div>
  )
}
