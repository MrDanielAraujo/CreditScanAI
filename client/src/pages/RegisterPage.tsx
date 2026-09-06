import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Button } from '../components/common/Button'
import { useAuth } from '../contexts/authContextValue'

export function RegisterPage() {
  const { register } = useAuth()
  const navigate = useNavigate()

  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()
    setLoading(true)
    setError(null)
    try {
      await register({ email, password, name: name || null })
      navigate('/')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao cadastrar')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="flex h-screen items-center justify-center bg-surface-muted">
      <form onSubmit={handleSubmit} className="w-full max-w-sm rounded-lg border border-neutral/20 bg-surface p-6 shadow-sm">
        <h1 className="text-xl font-semibold">Criar conta</h1>
        <p className="mt-1 text-sm text-neutral">CreditScanAI</p>

        <label className="mt-6 block text-sm font-medium">Nome</label>
        <input
          type="text"
          value={name}
          onChange={(e) => setName(e.target.value)}
          className="mt-1 w-full rounded-md border border-neutral/30 px-3 py-2 text-sm"
        />

        <label className="mt-4 block text-sm font-medium">Email</label>
        <input
          type="email"
          required
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          className="mt-1 w-full rounded-md border border-neutral/30 px-3 py-2 text-sm"
        />

        <label className="mt-4 block text-sm font-medium">Senha</label>
        <input
          type="password"
          required
          minLength={8}
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          className="mt-1 w-full rounded-md border border-neutral/30 px-3 py-2 text-sm"
        />
        <p className="mt-1 text-xs text-neutral">Mínimo de 8 caracteres.</p>

        {error && <p className="mt-3 text-sm text-error">{error}</p>}

        <Button type="submit" className="mt-6 w-full" loading={loading}>
          Cadastrar
        </Button>

        <p className="mt-4 text-center text-sm text-neutral">
          Já tem conta?{' '}
          <Link to="/login" className="text-primary hover:underline">
            Entrar
          </Link>
        </p>
      </form>
    </div>
  )
}
