import { useEffect, useState } from 'react'
import { DataGrid } from '../components/common/DataGrid/DataGrid'
import type { DataGridColumn } from '../components/common/DataGrid/types'
import { ShieldIcon } from '../components/common/navIcons'
import { useToast } from '../contexts/toastContextValue'
import { authApi } from '../services/authApi'
import type { LoginAuditEntry } from '../types/auth'

function formatDate(iso: string): string {
  return new Date(iso).toLocaleString('pt-BR')
}

const STATUS_OPTIONS = ['Sucesso', 'Falha']

export function LoginAuditPage() {
  const { showToast } = useToast()
  const [entries, setEntries] = useState<LoginAuditEntry[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    authApi
      .getLoginAudit()
      .then(setEntries)
      .catch((err) => showToast(err instanceof Error ? err.message : 'Erro ao carregar auditoria', 'error'))
      .finally(() => setLoading(false))
  }, [showToast])

  const columns: DataGridColumn<LoginAuditEntry>[] = [
    { key: 'email', label: 'Email' },
    {
      key: 'status',
      label: 'Status',
      filterOptions: STATUS_OPTIONS,
      getValue: (row) => (row.success ? 'Sucesso' : 'Falha'),
      render: (row) => <span className={row.success ? 'text-success' : 'text-error'}>{row.success ? 'Sucesso' : 'Falha'}</span>,
    },
    { key: 'failureReason', label: 'Motivo da falha', getValue: (row) => row.failureReason ?? '—' },
    { key: 'ipAddress', label: 'IP', getValue: (row) => row.ipAddress ?? '—' },
    { key: 'attemptedAt', label: 'Data/Hora', getValue: (row) => formatDate(row.attemptedAt) },
  ]

  return (
    <div className="flex h-full flex-col">
      <h1 className="flex items-center gap-2 text-2xl font-semibold">
        <ShieldIcon className="h-6 w-6" />
        Auditoria de Login
      </h1>
      <p className="mt-2 text-neutral">Tentativas de login recentes, com sucesso ou falha. Só Admin e Compliance têm acesso.</p>

      <div className="mt-6 min-h-0 flex-1 pb-[10px]">
        <DataGrid
          columns={columns}
          data={entries}
          rowKey={(entry) => entry.id}
          loading={loading}
          emptyMessage="Nenhuma tentativa de login registrada ainda."
        />
      </div>
    </div>
  )
}
