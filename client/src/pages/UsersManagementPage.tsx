import { useEffect, useState } from 'react'
import { Button } from '../components/common/Button'
import { DataGrid } from '../components/common/DataGrid/DataGrid'
import type { DataGridColumn } from '../components/common/DataGrid/types'
import { Drawer } from '../components/common/Drawer'
import { KeyIcon, LockIcon, UnlockIcon } from '../components/common/icons'
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
  const [drawerOpen, setDrawerOpen] = useState(false)

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

  const openCreateDrawer = () => {
    setForm(buildEmptyForm())
    setDrawerOpen(true)
  }

  const handleCreate = async () => {
    setError(null)
    try {
      await usersApi.create(form)
      setDrawerOpen(false)
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

  const statusOptions = ['Ativo', 'Bloqueado']

  const columns: DataGridColumn<UserListItem>[] = [
    { key: 'email', label: 'Email' },
    { key: 'name', label: 'Nome', getValue: (row) => row.name ?? '—' },
    {
      key: 'role',
      label: 'Papel',
      filterOptions: ROLES,
      getValue: (row) => row.role,
      render: (row) => (
        <select
          value={row.role}
          onChange={(e) => handleRoleChange(row.id, e.target.value as UserRole)}
          className="rounded-md border border-neutral/30 px-2 py-1 text-sm"
        >
          {ROLES.map((role) => (
            <option key={role} value={role}>
              {role}
            </option>
          ))}
        </select>
      ),
    },
    {
      key: 'status',
      label: 'Status',
      filterOptions: statusOptions,
      getValue: (row) => (row.isLockedOut ? 'Bloqueado' : 'Ativo'),
      render: (row) => <span className={row.isLockedOut ? 'text-error' : 'text-success'}>{row.isLockedOut ? 'Bloqueado' : 'Ativo'}</span>,
    },
    {
      key: 'actions',
      label: 'Ações',
      width: 180,
      sortable: false,
      filterable: false,
      groupable: false,
      frozen: true,
      preventRowClick: true,
      render: (row) => (
        <div className="flex flex-wrap items-center gap-3">
          <button
            className="cursor-pointer text-primary hover:text-blue-700"
            title="Redefinir senha"
            aria-label="Redefinir senha"
            onClick={() => setResetPasswordFor(row.id === resetPasswordFor ? null : row.id)}
          >
            <KeyIcon />
          </button>
          <button
            className={['cursor-pointer', row.isLockedOut ? 'text-success hover:text-green-700' : 'text-error hover:text-red-700'].join(' ')}
            title={row.isLockedOut ? 'Desbloquear' : 'Revogar acesso'}
            aria-label={row.isLockedOut ? 'Desbloquear' : 'Revogar acesso'}
            onClick={() => handleToggleLock(row)}
          >
            {row.isLockedOut ? <UnlockIcon /> : <LockIcon />}
          </button>
          {resetPasswordFor === row.id && (
            <div className="flex items-center gap-2">
              <input
                type="password"
                placeholder="Nova senha"
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
                className="rounded-md border border-neutral/30 px-2 py-1 text-sm"
              />
              <Button size="small" onClick={() => handleResetPassword(row.id)} disabled={newPassword.length < 8}>
                Confirmar
              </Button>
            </div>
          )}
        </div>
      ),
    },
  ]

  return (
    <div className="flex h-full flex-col">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold">Gerenciamento de Usuários</h1>
          <p className="mt-2 text-neutral">Crie usuários com o papel certo, ajuste papéis, redefina senhas e revogue acesso. Só Admin.</p>
        </div>
        <Button onClick={openCreateDrawer}>Novo Usuário</Button>
      </div>

      {error && <p className="mt-3 text-sm text-error">{error}</p>}
      {actionMessage && <p className="mt-3 text-sm text-success">{actionMessage}</p>}
      {actionError && <p className="mt-3 text-sm text-error">{actionError}</p>}

      <div className="mt-6 min-h-0 flex-1 pb-[10px]">
        <DataGrid
          columns={columns}
          data={items}
          rowKey={(item) => item.id}
          loading={loading}
          emptyMessage="Nenhum usuário encontrado."
        />
      </div>

      <Drawer open={drawerOpen} onClose={() => setDrawerOpen(false)} title="Novo Usuário">
        <div className="grid max-w-2xl grid-cols-2 gap-3">
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
          <div className="col-span-2 flex gap-2">
            <Button onClick={handleCreate} disabled={!form.email || form.password.length < 8}>
              Criar Usuário
            </Button>
            <Button variant="secondary" onClick={() => setDrawerOpen(false)}>
              Cancelar
            </Button>
          </div>
        </div>
      </Drawer>
    </div>
  )
}
