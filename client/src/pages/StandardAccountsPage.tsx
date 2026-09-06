import { useEffect, useState } from 'react'
import { Button } from '../components/common/Button'
import { DataGrid } from '../components/common/DataGrid/DataGrid'
import type { DataGridColumn } from '../components/common/DataGrid/types'
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
  const [items, setItems] = useState<StandardAccount[]>([])
  const [charts, setCharts] = useState<ChartOfAccounts[]>([])
  const [types, setTypes] = useState<AccountType[]>([])
  const [subtypes, setSubtypes] = useState<AccountSubtype[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [form, setForm] = useState<UpsertStandardAccountRequest>(buildEmptyForm('', '', ''))
  const [editingId, setEditingId] = useState<string | null>(null)

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
      .catch((err) => setError(err instanceof Error ? err.message : 'Erro ao carregar'))
      .finally(() => setLoading(false))
  }

  useEffect(load, [])

  const chartName = (id: string) => charts.find((c) => c.id === id)?.name ?? id
  const typeName = (id: string) => types.find((t) => t.id === id)?.name ?? id
  const subtypeName = (id: string) => subtypes.find((s) => s.id === id)?.name ?? id

  const handleSubmit = async () => {
    setError(null)
    try {
      if (editingId) {
        await standardAccountsApi.update(editingId, form)
      } else {
        await standardAccountsApi.create(form)
      }
      setForm({ ...form, code: '', name: '', description: '' })
      setEditingId(null)
      load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar')
    }
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
  }

  const handleDelete = async (id: string) => {
    if (!confirm('Excluir esta Conta?')) return
    setError(null)
    try {
      await standardAccountsApi.remove(id)
      load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao excluir')
    }
  }

  const columns: DataGridColumn<StandardAccount>[] = [
    { key: 'code', label: 'Código' },
    { key: 'name', label: 'Nome' },
    {
      key: 'typeSubtype',
      label: 'Tipo / Subtipo',
      getValue: (row) => `${typeName(row.accountTypeId)} / ${subtypeName(row.accountSubtypeId)}`,
    },
    { key: 'chartOfAccountsId', label: 'Plano', getValue: (row) => chartName(row.chartOfAccountsId) },
    {
      key: 'actions',
      label: '',
      sortable: false,
      filterable: false,
      groupable: false,
      align: 'right',
      render: (row) => (
        <>
          <button className="mr-3 text-primary" onClick={() => handleEdit(row)}>
            Editar
          </button>
          <button className="text-error" onClick={() => handleDelete(row.id)}>
            Excluir
          </button>
        </>
      ),
    },
  ]

  return (
    <div>
      <h1 className="text-2xl font-semibold">Contas</h1>
      <p className="mt-2 text-neutral">Ex: Caixa e Equivalentes de Caixa, Fornecedores. Tipo e Subtipo precisam ser compatíveis.</p>

      <div className="mt-6 grid max-w-3xl grid-cols-3 gap-3">
        <select
          value={form.chartOfAccountsId}
          onChange={(e) => setForm({ ...form, chartOfAccountsId: e.target.value })}
          className="col-span-3 rounded-md border border-neutral/30 px-3 py-2 text-sm"
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
          className="rounded-md border border-neutral/30 px-3 py-2 text-sm"
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
          className="rounded-md border border-neutral/30 px-3 py-2 text-sm"
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
          className="rounded-md border border-neutral/30 px-3 py-2 text-sm"
        />
        <input
          placeholder="Nome"
          value={form.name}
          onChange={(e) => setForm({ ...form, name: e.target.value })}
          className="col-span-2 rounded-md border border-neutral/30 px-3 py-2 text-sm"
        />
        <input
          placeholder="Descrição"
          value={form.description ?? ''}
          onChange={(e) => setForm({ ...form, description: e.target.value || null })}
          className="col-span-3 rounded-md border border-neutral/30 px-3 py-2 text-sm"
        />
        <div className="col-span-3 flex gap-2">
          <Button
            onClick={handleSubmit}
            disabled={!form.code || !form.name || !form.chartOfAccountsId || !form.accountTypeId || !form.accountSubtypeId}
          >
            {editingId ? 'Salvar' : 'Adicionar'}
          </Button>
          {editingId && (
            <Button
              variant="secondary"
              onClick={() => {
                setEditingId(null)
                setForm({ ...form, code: '', name: '', description: '' })
              }}
            >
              Cancelar
            </Button>
          )}
        </div>
      </div>

      {error && <p className="mt-3 text-sm text-error">{error}</p>}

      <div className="mt-6 max-w-3xl">
        <DataGrid columns={columns} data={items} rowKey={(item) => item.id} loading={loading} emptyMessage="Nenhuma conta cadastrada." />
      </div>
    </div>
  )
}
