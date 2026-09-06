import { useEffect, useState } from 'react'
import { authApi } from '../services/authApi'
import type { LoginAuditEntry } from '../types/auth'

function formatDate(iso: string): string {
  return new Date(iso).toLocaleString('pt-BR')
}

export function LoginAuditPage() {
  const [entries, setEntries] = useState<LoginAuditEntry[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    authApi
      .getLoginAudit()
      .then(setEntries)
      .catch((err) => setError(err instanceof Error ? err.message : 'Erro ao carregar auditoria'))
      .finally(() => setLoading(false))
  }, [])

  return (
    <div>
      <h1 className="text-2xl font-semibold">Auditoria de Login</h1>
      <p className="mt-2 text-neutral">Tentativas de login recentes, com sucesso ou falha. Só Admin e Compliance têm acesso.</p>

      {loading && <p className="mt-6 text-sm text-neutral">Carregando...</p>}
      {error && <p className="mt-6 text-sm text-error">{error}</p>}

      {!loading && !error && (
        <table className="mt-6 w-full max-w-3xl border-collapse text-sm">
          <thead>
            <tr className="border-b border-neutral/20 bg-surface-muted text-left">
              <th className="p-2">Email</th>
              <th className="p-2">Status</th>
              <th className="p-2">Motivo da falha</th>
              <th className="p-2">IP</th>
              <th className="p-2">Data/Hora</th>
            </tr>
          </thead>
          <tbody>
            {entries.length === 0 && (
              <tr>
                <td className="p-2 text-neutral" colSpan={5}>
                  Nenhuma tentativa de login registrada ainda.
                </td>
              </tr>
            )}
            {entries.map((entry) => (
              <tr key={entry.id} className="border-b border-neutral/10">
                <td className="p-2">{entry.email}</td>
                <td className={['p-2', entry.success ? 'text-success' : 'text-error'].join(' ')}>
                  {entry.success ? 'Sucesso' : 'Falha'}
                </td>
                <td className="p-2 text-neutral">{entry.failureReason ?? '—'}</td>
                <td className="p-2 text-neutral">{entry.ipAddress ?? '—'}</td>
                <td className="p-2 text-neutral">{formatDate(entry.attemptedAt)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  )
}
