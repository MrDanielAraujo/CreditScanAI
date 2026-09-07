import { useEffect, useState } from 'react'
import { Button } from '../components/common/Button'
import { DataGrid } from '../components/common/DataGrid/DataGrid'
import type { DataGridColumn } from '../components/common/DataGrid/types'
import { Drawer } from '../components/common/Drawer'
import { TrashIcon } from '../components/common/icons'
import { BookIcon } from '../components/common/navIcons'
import { chartOfAccountsApi } from '../services/registrationsApi'
import type { ChartOfAccounts, UpsertChartOfAccountsRequest } from '../types/registrations'

const emptyForm: UpsertChartOfAccountsRequest = { name: '', description: '' }

export function ChartOfAccountsPage() {
  const [items, setItems] = useState<ChartOfAccounts[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [form, setForm] = useState<UpsertChartOfAccountsRequest>(emptyForm)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [drawerOpen, setDrawerOpen] = useState(false)

  const load = () => {
    setLoading(true)
    chartOfAccountsApi
      .list()
      .then(setItems)
      .catch((err) => setError(err instanceof Error ? err.message : 'Erro ao carregar'))
      .finally(() => setLoading(false))
  }

  useEffect(load, [])

  const openCreateDrawer = () => {
    setEditingId(null)
    setForm(emptyForm)
    setDrawerOpen(true)
  }

  const handleEdit = (item: ChartOfAccounts) => {
    setEditingId(item.id)
    setForm({ name: item.name, description: item.description })
    setDrawerOpen(true)
  }

  const handleSubmit = async () => {
    setError(null)
    try {
      if (editingId) {
        await chartOfAccountsApi.update(editingId, form)
      } else {
        await chartOfAccountsApi.create(form)
      }
      setDrawerOpen(false)
      load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar')
    }
  }

  const handleDelete = async (id: string) => {
    if (!confirm('Excluir este Plano de Contas?')) return
    setError(null)
    try {
      await chartOfAccountsApi.remove(id)
      load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao excluir')
    }
  }

  const handleSetDefault = async (id: string) => {
    setError(null)
    try {
      await chartOfAccountsApi.setDefault(id)
      load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao definir padrão')
    }
  }

  const columns: DataGridColumn<ChartOfAccounts>[] = [
    { key: 'name', label: 'Nome' },
    {
      key: 'isDefault',
      label: 'Padrão',
      filterOptions: ['Padrão', 'Não padrão'],
      getValue: (row) => (row.isDefault ? 'Padrão' : 'Não padrão'),
      render: (row) =>
        row.isDefault ? (
          <span className="rounded-full bg-success/10 px-2 py-0.5 text-xs font-medium text-success">Padrão</span>
        ) : (
          <button
            className="text-sm text-primary"
            onClick={(e) => {
              e.stopPropagation()
              handleSetDefault(row.id)
            }}
          >
            Definir como padrão
          </button>
        ),
    },
    {
      key: 'actions',
      label: 'Ações',
      width: 70,
      sortable: false,
      filterable: false,
      groupable: false,
      frozen: true,
      preventRowClick: true,
      render: (row) => (
        <button
          className="cursor-pointer text-error hover:text-red-700"
          title="Excluir"
          aria-label="Excluir"
          onClick={(e) => {
            e.stopPropagation()
            handleDelete(row.id)
          }}
        >
          <TrashIcon />
        </button>
      ),
    },
  ]

  return (
    <div className="flex h-full flex-col">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="flex items-center gap-2 text-2xl font-semibold">
            <BookIcon className="h-6 w-6" />
            Planos de Contas
          </h1>
          <p className="mt-2 text-neutral">
            O plano marcado como <strong>padrão</strong> é o usado automaticamente na classificação de documentos.
          </p>
        </div>
        <Button onClick={openCreateDrawer}>Novo Plano</Button>
      </div>

      {error && <p className="mt-3 text-sm text-error">{error}</p>}

      <div className="mt-6 min-h-0 flex-1 pb-[10px]">
        <DataGrid
          columns={columns}
          data={items}
          rowKey={(item) => item.id}
          loading={loading}
          onRowClick={handleEdit}
          emptyMessage="Nenhum plano de contas cadastrado."
        />
      </div>

      <Drawer open={drawerOpen} onClose={() => setDrawerOpen(false)} title={editingId ? 'Editar Plano de Contas' : 'Novo Plano de Contas'}>
        <div className="grid max-w-2xl grid-cols-2 gap-3">
          <input
            placeholder="Nome"
            value={form.name}
            onChange={(e) => setForm({ ...form, name: e.target.value })}
            className="rounded-md border border-neutral/30 px-3 py-2 text-sm"
          />
          <input
            placeholder="Descrição"
            value={form.description ?? ''}
            onChange={(e) => setForm({ ...form, description: e.target.value || null })}
            className="rounded-md border border-neutral/30 px-3 py-2 text-sm"
          />
          <div className="col-span-2 flex gap-2">
            <Button onClick={handleSubmit} disabled={!form.name}>
              {editingId ? 'Salvar' : 'Adicionar'}
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
