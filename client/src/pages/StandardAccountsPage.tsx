import { useEffect, useState } from 'react'
import { Button } from '../components/common/Button'
import { DataGrid } from '../components/common/DataGrid/DataGrid'
import type { DataGridColumn } from '../components/common/DataGrid/types'
import { Drawer } from '../components/common/Drawer'
import { TrashIcon } from '../components/common/icons'
import { TagIcon } from '../components/common/navIcons'
import { useToast } from '../contexts/toastContextValue'
import { accountSubtypesApi, accountTypesApi, chartOfAccountsApi, standardAccountsApi } from '../services/registrationsApi'
import type {
  AccountSubtype,
  AccountType,
  ChartOfAccounts,
  StandardAccount,
  UpsertStandardAccountRequest,
} from '../types/registrations'

function buildEmptyForm(chartId: string, typeId: string, subtypeId: string): UpsertStandardAccountRequest {
  return { chartOfAccountsId: chartId, accountTypeId: typeId, accountSubtypeId: subtypeId, code: '', name: '', description: '' }
}

export function StandardAccountsPage() {
  const { showToast } = useToast()
  const [items, setItems] = useState<StandardAccount[]>([])
  const [charts, setCharts] = useState<ChartOfAccounts[]>([])
  const [types, setTypes] = useState<AccountType[]>([])
  const [subtypes, setSubtypes] = useState<AccountSubtype[]>([])
  const [loading, setLoading] = useState(true)
  const [form, setForm] = useState<UpsertStandardAccountRequest>(buildEmptyForm('', '', ''))
  const [editingId, setEditingId] = useState<string | null>(null)
  const [drawerOpen, setDrawerOpen] = useState(false)

  const load = () => {
    setLoading(true)
    Promise.all([standardAccountsApi.list(), chartOfAccountsApi.list(), accountTypesApi.list(), accountSubtypesApi.list()])
      .then(([accounts, chartList, typeList, subtypeList]) => {
        setItems(accounts)
        setCharts(chartList)
        setTypes(typeList)
        setSubtypes(subtypeList)
        setForm((prev) =>
          prev.chartOfAccountsId
            ? prev
            : buildEmptyForm(chartList.find((c) => c.isDefault)?.id ?? chartList[0]?.id ?? '', typeList[0]?.id ?? '', subtypeList[0]?.id ?? ''),
        )
      })
      .catch((err) => showToast(err instanceof Error ? err.message : 'Erro ao carregar', 'error'))
      .finally(() => setLoading(false))
  }

  useEffect(load, [showToast])

  const chartName = (id: string) => charts.find((c) => c.id === id)?.name ?? id
  const typeName = (id: string) => types.find((t) => t.id === id)?.name ?? id
  const subtypeName = (id: string) => subtypes.find((s) => s.id === id)?.name ?? id

  const openCreateDrawer = () => {
    setEditingId(null)
    setForm((prev) => ({ ...prev, code: '', name: '', description: '' }))
    setDrawerOpen(true)
  }

  const handleEdit = (item: StandardAccount) => {
    setEditingId(item.id)
    setForm({
      chartOfAccountsId: item.chartOfAccountsId,
      accountTypeId: item.accountTypeId,
      accountSubtypeId: item.accountSubtypeId,
      code: item.code,
      name: item.name,
      description: item.description,
    })
    setDrawerOpen(true)
  }

  const handleSubmit = async () => {
    try {
      if (editingId) {
        await standardAccountsApi.update(editingId, form)
      } else {
        await standardAccountsApi.create(form)
      }
      setForm({ ...form, code: '', name: '', description: '' })
      setDrawerOpen(false)
      load()
    } catch (err) {
      showToast(err instanceof Error ? err.message : 'Erro ao salvar', 'error')
    }
  }

  const handleDelete = async (id: string) => {
    if (!confirm('Excluir esta Conta?')) return
    try {
      await standardAccountsApi.remove(id)
      load()
    } catch (err) {
      showToast(err instanceof Error ? err.message : 'Erro ao excluir', 'error')
    }
  }

  const columns: DataGridColumn<StandardAccount>[] = [
    { key: 'code', label: 'Código' },
    { key: 'name', label: 'Nome' },
    {
      key: 'type',
      label: 'Tipo',
      filterOptions: types.map((t) => t.name),
      getValue: (row) => typeName(row.accountTypeId),
    },
    {
      key: 'subtype',
      label: 'Subtipo',
      filterOptions: subtypes.map((s) => s.name),
      getValue: (row) => subtypeName(row.accountSubtypeId),
    },
    { key: 'chartOfAccountsId', label: 'Plano', getValue: (row) => chartName(row.chartOfAccountsId) },
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
          className="cursor-pointer text-error hover:text-red-700 dark:hover:text-red-400"
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
            <TagIcon className="h-6 w-6" />
            Contas
          </h1>
          <p className="mt-2 text-neutral">Ex: Caixa e Equivalentes de Caixa, Fornecedores. Tipo e Subtipo precisam ser compatíveis.</p>
        </div>
        <Button onClick={openCreateDrawer}>Nova Conta</Button>
      </div>

      <div className="mt-6 min-h-0 flex-1 pb-[10px]">
        <DataGrid
          columns={columns}
          data={items}
          rowKey={(item) => item.id}
          loading={loading}
          onRowClick={handleEdit}
          emptyMessage="Nenhuma conta cadastrada."
        />
      </div>

      <Drawer
        open={drawerOpen}
        onClose={() => setDrawerOpen(false)}
        title={editingId ? 'Editar Conta' : 'Nova Conta'}
        footer={
          <>
            <Button
              onClick={handleSubmit}
              disabled={!form.code || !form.name || !form.chartOfAccountsId || !form.accountTypeId || !form.accountSubtypeId}
            >
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
            value={form.chartOfAccountsId}
            onChange={(e) => setForm({ ...form, chartOfAccountsId: e.target.value })}
            className="col-span-2 h-10 rounded-md border border-neutral/30 px-3 py-2 text-sm"
          >
            {charts.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
                {c.isDefault ? ' (padrão)' : ''}
              </option>
            ))}
          </select>
          <select
            value={form.accountTypeId}
            onChange={(e) => setForm({ ...form, accountTypeId: e.target.value })}
            className="h-10 rounded-md border border-neutral/30 px-3 py-2 text-sm"
          >
            {types.map((t) => (
              <option key={t.id} value={t.id}>
                {t.name}
              </option>
            ))}
          </select>
          <select
            value={form.accountSubtypeId}
            onChange={(e) => setForm({ ...form, accountSubtypeId: e.target.value })}
            className="h-10 rounded-md border border-neutral/30 px-3 py-2 text-sm"
          >
            {subtypes.map((s) => (
              <option key={s.id} value={s.id}>
                {s.name}
              </option>
            ))}
          </select>
          <input
            placeholder="Código"
            value={form.code}
            onChange={(e) => setForm({ ...form, code: e.target.value })}
            className="h-10 rounded-md border border-neutral/30 px-3 py-2 text-sm"
          />
          <input
            placeholder="Nome"
            value={form.name}
            onChange={(e) => setForm({ ...form, name: e.target.value })}
            className="h-10 rounded-md border border-neutral/30 px-3 py-2 text-sm"
          />
          <input
            placeholder="Descrição"
            value={form.description ?? ''}
            onChange={(e) => setForm({ ...form, description: e.target.value || null })}
            className="col-span-2 h-10 rounded-md border border-neutral/30 px-3 py-2 text-sm"
          />
        </div>
      </Drawer>
    </div>
  )
}
