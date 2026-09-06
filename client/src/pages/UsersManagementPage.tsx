import { useEffect, useState } from 'react'
import { Button } from '../components/common/Button'
import { usersApi } from '../services/usersApi'
import type { CreateUserRequest, UserListItem, UserRole } from '../types/users'

const ROLES: UserRole[] = ['Analyst', 'Reviewer', 'CFO', 'Admin', 'Compliance']

function buildEmptyForm(): CreateUserRequest {
  return { email: '', password: '', name: '', role: 'Analyst' }
}

export function UsersManagementPage() {
  const [items, setItems] = useState<UserListItem[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [form, setForm] = useState<CreateUserRequest>(buildEmptyForm())

  const [resetPasswordFor, setResetPasswordFor] = useState<string | null>(null)
  const [newPassword, setNewPassword] = useState('')
  const [actionError, setActionError] = useState<string | null>(null)
  const [actionMessage, setActionMessage] = useState<string | null>(null)

  const load = () => {
    setLoading(true)
    usersApi
      .list()
      .then(setItems)
      .catch((err) => setError(err instanceof Error ? err.message : 'Erro ao carregar usuários'))
      .finally(() => setLoading(false))
  }

  useEffect(load, [])

  const handleCreate = async () => {
    setError(null)
    try {
      await usersApi.create(form)
      setForm(buildEmptyForm())
      load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao criar usuário')
    }
  }

  const handleRoleChange = async (id: string, role: UserRole) => {
    setActionError(null)
    setActionMessage(null)
    try {
      await usersApi.updateRole(id, { role })
      load()
    } catch (err) {
      setActionError(err instanceof Error ? err.message : 'Erro ao alterar papel')
    }
  }

  const handleToggleLock = async (item: UserListItem) => {
    setActionError(null)
    setActionMessage(null)
    try {
      if (item.isLockedOut) {
        await usersApi.unlock(item.id)
      } else {
        await usersApi.lock(item.id)
      }
      load()
    } catch (err) {
      setActionError(err instanceof Error ? err.message : 'Erro ao alterar acesso')
    }
  }

  const handleResetPassword = async (id: string) => {
    setActionError(null)
    setActionMessage(null)
    try {
      const res = await usersApi.resetPassword(id, { newPassword })
      setActionMessage(res.message)
      setResetPasswordFor(null)
      setNewPassword('')
    } catch (err) {
      setActionError(err instanceof Error ? err.message : 'Erro ao redefinir senha')
    }
  }

  return (
    <div>
      <h1 className="text-2xl font-semibold">Gerenciamento de Usuários</h1>
      <p className="mt-2 text-neutral">Crie usuários com o papel certo, ajuste papéis, redefina senhas e revogue acesso. Só Admin.</p>

      <div className="mt-6 grid max-w-2xl grid-cols-2 gap-3">
        <input
          placeholder="Email"
          type="email"
          value={form.email}
          onChange={(e) => setForm({ ...form, email: e.target.value })}
          className="rounded-md border border-neutral/30 px-3 py-2 text-sm"
        />
        <input
          placeholder="Nome"
          value={form.name ?? ''}
          onChange={(e) => setForm({ ...form, name: e.target.value || null })}
          className="rounded-md border border-neutral/30 px-3 py-2 text-sm"
        />
        <input
          placeholder="Senha (mínimo 8 caracteres)"
          type="password"
          value={form.password}
          onChange={(e) => setForm({ ...form, password: e.target.value })}
          className="rounded-md border border-neutral/30 px-3 py-2 text-sm"
        />
        <select
          value={form.role}
          onChange={(e) => setForm({ ...form, role: e.target.value as UserRole })}
          className="rounded-md border border-neutral/30 px-3 py-2 text-sm"
        >
          {ROLES.map((role) => (
            <option key={role} value={role}>
              {role}
            </option>
          ))}
        </select>
        <div className="col-span-2">
          <Button onClick={handleCreate} disabled={!form.email || form.password.length < 8}>
            Criar Usuário
          </Button>
        </div>
      </div>

      {error && <p className="mt-3 text-sm text-error">{error}</p>}
      {actionMessage && <p className="mt-3 text-sm text-success">{actionMessage}</p>}
      {actionError && <p className="mt-3 text-sm text-error">{actionError}</p>}

      <table className="mt-6 w-full max-w-4xl border-collapse text-sm">
        <thead>
          <tr className="border-b border-neutral/20 bg-surface-muted text-left">
            <th className="p-2">Email</th>
            <th className="p-2">Nome</th>
            <th className="p-2">Papel</th>
            <th className="p-2">Status</th>
            <th className="p-2" />
          </tr>
        </thead>
        <tbody>
          {loading && (
            <tr>
              <td className="p-2 text-neutral" colSpan={5}>
                Carregando...
              </td>
            </tr>
          )}
          {!loading &&
            items.map((item) => (
              <tr key={item.id} className="border-b border-neutral/10">
                <td className="p-2">{item.email}</td>
                <td className="p-2 text-neutral">{item.name ?? '—'}</td>
                <td className="p-2">
                  <select
                    value={item.role}
                    onChange={(e) => handleRoleChange(item.id, e.target.value as UserRole)}
                    className="rounded-md border border-neutral/30 px-2 py-1 text-sm"
                  >
                    {ROLES.map((role) => (
                      <option key={role} value={role}>
                        {role}
                      </option>
                    ))}
                  </select>
                </td>
                <td className={['p-2', item.isLockedOut ? 'text-error' : 'text-success'].join(' ')}>
                  {item.isLockedOut ? 'Bloqueado' : 'Ativo'}
                </td>
                <td className="p-2 text-right">
                  <button className="mr-3 text-primary" onClick={() => setResetPasswordFor(item.id === resetPasswordFor ? null : item.id)}>
                    Redefinir senha
                  </button>
                  <button className={item.isLockedOut ? 'text-success' : 'text-error'} onClick={() => handleToggleLock(item)}>
                    {item.isLockedOut ? 'Desbloquear' : 'Revogar acesso'}
                  </button>
                  {resetPasswordFor === item.id && (
                    <div className="mt-2 flex items-center justify-end gap-2">
                      <input
                        type="password"
                        placeholder="Nova senha"
                        value={newPassword}
                        onChange={(e) => setNewPassword(e.target.value)}
                        className="rounded-md border border-neutral/30 px-2 py-1 text-sm"
                      />
                      <Button size="small" onClick={() => handleResetPassword(item.id)} disabled={newPassword.length < 8}>
                        Confirmar
                      </Button>
                    </div>
                  )}
                </td>
              </tr>
            ))}
        </tbody>
      </table>
    </div>
  )
}
