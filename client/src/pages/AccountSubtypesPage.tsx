import { useEffect, useState } from 'react'
import { Button } from '../components/common/Button'
import { DataGrid } from '../components/common/DataGrid/DataGrid'
import type { DataGridColumn } from '../components/common/DataGrid/types'
import { Drawer } from '../components/common/Drawer'
import { TrashIcon } from '../components/common/icons'
import { BookmarkIcon } from '../components/common/navIcons'
import { accountSubtypesApi, accountTypesApi } from '../services/registrationsApi'
import type { AccountSubtype, AccountType, UpsertAccountSubtypeRequest } from '../types/registrations'

const emptyForm: UpsertAccountSubtypeRequest = { accountTypeId: '', code: '', name: '', description: '', sequenceOrder: null }

export function AccountSubtypesPage() {
  const [items, setItems] = useState<AccountSubtype[]>([])
  const [types, setTypes] = useState<AccountType[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [form, setForm] = useState<UpsertAccountSubtypeRequest>(emptyForm)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [drawerOpen, setDrawerOpen] = useState(false)

  const load = () => {
    setLoading(true)
    Promise.all([accountSubtypesApi.list(), accountTypesApi.list()])
      .then(([subtypes, accountTypes]) => {
        setItems(subtypes)
        setTypes(accountTypes)
        setForm((prev) => (prev.accountTypeId ? prev : { ...prev, accountTypeId: accountTypes[0]?.id ?? '' }))
      })
      .catch((err) => setError(err instanceof Error ? err.message : 'Erro ao carregar'))
      .finally(() => setLoading(false))
  }

  useEffect(load, [])

  const typeName = (id: string) => types.find((t) => t.id === id)?.name ?? id

  const openCreateDrawer = () => {
    setEditingId(null)
    setForm({ ...emptyForm, accountTypeId: form.accountTypeId })
    setDrawerOpen(true)
  }

  const handleEdit = (item: AccountSubtype) => {
    setEditingId(item.id)
    setForm({
      accountTypeId: item.accountTypeId,
      code: item.code,
      name: item.name,
      description: item.description,
      sequenceOrder: item.sequenceOrder,
    })
    setDrawerOpen(true)
  }

  const handleSubmit = async () => {
    setError(null)
    try {
      if (editingId) {
        await accountSubtypesApi.update(editingId, form)
      } else {
        await accountSubtypesApi.create(form)
      }
      setForm({ ...emptyForm, accountTypeId: form.accountTypeId })
      setDrawerOpen(false)
      load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar')
    }
  }

  const handleDelete = async (id: string) => {
    if (!confirm('Excluir este Subtipo?')) return
    setError(null)
    try {
      await accountSubtypesApi.remove(id)
      load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao excluir')
    }
  }

  const columns: DataGridColumn<AccountSubtype>[] = [
    { key: 'code', label: 'Código' },
    { key: 'name', label: 'Nome' },
    { key: 'accountTypeId', label: 'Tipo', getValue: (row) => typeName(row.accountTypeId) },
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
            <BookmarkIcon className="h-6 w-6" />
            Subtipos de Conta
          </h1>
          <p className="mt-2 text-neutral">
            Ex: Circulante, Não Circulante. Ao criar, a compatibilidade com o Tipo escolhido é registrada automaticamente.
          </p>
        </div>
        <Button onClick={openCreateDrawer}>Novo Subtipo</Button>
      </div>

      {error && <p className="mt-3 text-sm text-error">{error}</p>}

      <div className="mt-6 min-h-0 flex-1 pb-[10px]">
        <DataGrid
          columns={columns}
          data={items}
          rowKey={(item) => item.id}
          loading={loading}
          onRowClick={handleEdit}
          emptyMessage="Nenhum subtipo cadastrado."
        />
      </div>

      <Drawer
        open={drawerOpen}
        onClose={() => setDrawerOpen(false)}
        title={editingId ? 'Editar Subtipo' : 'Novo Subtipo'}
        footer={
          <>
            <Button onClick={handleSubmit} disabled={!form.code || !form.name || !form.accountTypeId}>
              {editingId ? 'Salvar' : 'Adicionar'}
            </Button>
            <Button variant="secondary" onClick={() => setDrawerOpen(false)}>
              Cancelar
            </Button>
          </>
        }
      >
        <div className="grid max-w-2xl grid-cols-2 gap-3">
          <select
            value={form.accountTypeId}
            onChange={(e) => setForm({ ...form, accountTypeId: e.target.value })}
            className="col-span-2 rounded-md border border-neutral/30 px-3 py-2 text-sm"
          >
            {types.map((t) => (
              <option key={t.id} value={t.id}>
                {t.name}
              </option>
            ))}
          </select>
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
        </div>
      </Drawer>
    </div>
  )
}
