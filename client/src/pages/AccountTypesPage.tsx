import { useEffect, useState } from 'react'
import { Button } from '../components/common/Button'
import { DataGrid } from '../components/common/DataGrid/DataGrid'
import type { DataGridColumn } from '../components/common/DataGrid/types'
import { Drawer } from '../components/common/Drawer'
import { TrashIcon } from '../components/common/icons'
import { accountTypesApi } from '../services/registrationsApi'
import type { AccountType, UpsertAccountTypeRequest } from '../types/registrations'

const emptyForm: UpsertAccountTypeRequest = { code: '', name: '', description: '', sequenceOrder: null }

export function AccountTypesPage() {
  const [items, setItems] = useState<AccountType[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [form, setForm] = useState<UpsertAccountTypeRequest>(emptyForm)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [drawerOpen, setDrawerOpen] = useState(false)

  const load = () => {
    setLoading(true)
    accountTypesApi
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

  const handleEdit = (item: AccountType) => {
    setEditingId(item.id)
    setForm({ code: item.code, name: item.name, description: item.description, sequenceOrder: item.sequenceOrder })
    setDrawerOpen(true)
  }

  const handleSubmit = async () => {
    setError(null)
    try {
      if (editingId) {
        await accountTypesApi.update(editingId, form)
      } else {
        await accountTypesApi.create(form)
      }
      setDrawerOpen(false)
      load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar')
    }
  }

  const handleDelete = async (id: string) => {
    if (!confirm('Excluir este Tipo?')) return
    setError(null)
    try {
      await accountTypesApi.remove(id)
      load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao excluir')
    }
  }

  const columns: DataGridColumn<AccountType>[] = [
    { key: 'code', label: 'Código' },
    { key: 'name', label: 'Nome' },
    { key: 'sequenceOrder', label: 'Ordem', align: 'right', getValue: (row) => row.sequenceOrder ?? '-' },
    {
      key: 'actions',
      label: '',
      width: 56,
      sortable: false,
      filterable: false,
      groupable: false,
      frozen: true,
      render: (row) => (
        <button
          className="text-error hover:text-red-700"
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
          <h1 className="text-2xl font-semibold">Tipos de Conta</h1>
          <p className="mt-2 text-neutral">Ex: Ativo, Passivo, DRE.</p>
        </div>
        <Button onClick={openCreateDrawer}>Novo Tipo</Button>
      </div>

      {error && <p className="mt-3 text-sm text-error">{error}</p>}

      <div className="mt-6 min-h-0 flex-1 pb-[10px]">
        <DataGrid
          columns={columns}
          data={items}
          rowKey={(item) => item.id}
          loading={loading}
          onRowClick={handleEdit}
          emptyMessage="Nenhum tipo cadastrado."
        />
      </div>

      <Drawer open={drawerOpen} onClose={() => setDrawerOpen(false)} title={editingId ? 'Editar Tipo' : 'Novo Tipo'}>
        <div className="grid max-w-2xl grid-cols-2 gap-3">
          <input
            placeholder="Código"
            value={form.code}
            onChange={(e) => setForm({ ...form, code: e.target.value })}
            className="rounded-md border border-neutral/30 px-3 py-2 text-sm"
          />
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
            className="col-span-2 rounded-md border border-neutral/30 px-3 py-2 text-sm"
          />
          <input
            placeholder="Ordem"
            type="number"
            value={form.sequenceOrder ?? ''}
            onChange={(e) => setForm({ ...form, sequenceOrder: e.target.value ? Number(e.target.value) : null })}
            className="rounded-md border border-neutral/30 px-3 py-2 text-sm"
          />
          <div className="col-span-2 flex gap-2">
            <Button onClick={handleSubmit} disabled={!form.code || !form.name}>
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
