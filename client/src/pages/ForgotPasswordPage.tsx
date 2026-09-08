import { useState } from 'react'
import { Link } from 'react-router-dom'
import { Button } from '../components/common/Button'
import { useToast } from '../contexts/toastContextValue'
import { authApi } from '../services/authApi'

export function ForgotPasswordPage() {
  const { showToast } = useToast()
  const [email, setEmail] = useState('')
  const [loading, setLoading] = useState(false)

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()
    setLoading(true)
    try {
      const res = await authApi.forgotPassword({ email })
      showToast(res.message, 'success')
    } catch (err) {
      showToast(err instanceof Error ? err.message : 'Erro ao solicitar redefinição', 'error')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="flex h-screen items-center justify-center bg-surface-muted">
      <form onSubmit={handleSubmit} className="w-full max-w-sm rounded-lg border border-neutral/20 bg-surface p-6 shadow-sm">
        <h1 className="text-xl font-semibold">Esqueceu sua senha?</h1>
        <p className="mt-1 text-sm text-neutral">Informe seu email para receber um link de redefinição.</p>

        <label className="mt-6 block text-sm font-medium">Email</label>
        <input
          type="email"
          required
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          className="mt-1 w-full h-10 rounded-md border border-neutral/30 px-3 py-2 text-sm"
        />

        <Button type="submit" className="mt-6 w-full" loading={loading}>
          Enviar link de redefinição
        </Button>

        <p className="mt-4 text-center text-sm text-neutral">
          <Link to="/login" className="text-primary hover:underline">
            Voltar para o login
          </Link>
        </p>
      </form>
    </div>
  )
}
