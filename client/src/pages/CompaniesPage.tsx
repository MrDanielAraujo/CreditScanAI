import { useEffect, useState } from 'react'
import { Button } from '../components/common/Button'
import { DataGrid } from '../components/common/DataGrid/DataGrid'
import type { DataGridColumn } from '../components/common/DataGrid/types'
import { Drawer } from '../components/common/Drawer'
import { TrashIcon } from '../components/common/icons'
import { companiesApi } from '../services/companiesApi'
import type { Company, UpsertCompanyRequest } from '../types/documents'

function buildEmptyForm(): UpsertCompanyRequest {
  return { code: '', name: '', legalName: '', cnpj: '', industry: '', fiscalYearEnd: null, reportingCurrency: 'BRL' }
}

export function CompaniesPage() {
  const [items, setItems] = useState<Company[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [form, setForm] = useState<UpsertCompanyRequest>(buildEmptyForm())
  const [editingId, setEditingId] = useState<string | null>(null)
  const [drawerOpen, setDrawerOpen] = useState(false)

  const load = () => {
    setLoading(true)
    companiesApi
      .list()
      .then(setItems)
      .catch((err) => setError(err instanceof Error ? err.message : 'Erro ao carregar'))
      .finally(() => setLoading(false))
  }

  useEffect(load, [])

  const openCreateDrawer = () => {
    setEditingId(null)
    setForm(buildEmptyForm())
    setDrawerOpen(true)
  }

  const handleEdit = (item: Company) => {
    setEditingId(item.id)
    setForm({
      code: item.code,
      name: item.name,
      legalName: item.legalName ?? '',
      cnpj: item.cnpj ?? '',
      industry: item.industry ?? '',
      fiscalYearEnd: item.fiscalYearEnd,
      reportingCurrency: item.reportingCurrency,
    })
    setDrawerOpen(true)
  }

  const handleSubmit = async () => {
    setError(null)
    try {
      if (editingId) {
        await companiesApi.update(editingId, form)
      } else {
        await companiesApi.create(form)
      }
      setDrawerOpen(false)
      load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar')
    }
  }

  const handleDelete = async (id: string) => {
    if (!confirm('Excluir esta Empresa?')) return
    setError(null)
    try {
      await companiesApi.remove(id)
      load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao excluir')
    }
  }

  const columns: DataGridColumn<Company>[] = [
    { key: 'code', label: 'Código' },
    { key: 'name', label: 'Nome' },
    { key: 'cnpj', label: 'CNPJ', getValue: (row) => row.cnpj ?? '—' },
    { key: 'reportingCurrency', label: 'Moeda' },
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
          <h1 className="text-2xl font-semibold">Empresas</h1>
          <p className="mt-2 text-neutral">Empresas cujos documentos podem ser classificados, calculados e consolidados.</p>
        </div>
        <Button onClick={openCreateDrawer}>Nova Empresa</Button>
      </div>

      {error && <p className="mt-3 text-sm text-error">{error}</p>}

      <div className="mt-6 min-h-0 flex-1 pb-[10px]">
        <DataGrid
          columns={columns}
          data={items}
          rowKey={(item) => item.id}
          loading={loading}
          onRowClick={handleEdit}
          emptyMessage="Nenhuma empresa cadastrada."
        />
      </div>

      <Drawer open={drawerOpen} onClose={() => setDrawerOpen(false)} title={editingId ? 'Editar Empresa' : 'Nova Empresa'}>
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
            placeholder="Razão Social"
            value={form.legalName ?? ''}
            onChange={(e) => setForm({ ...form, legalName: e.target.value || null })}
            className="col-span-2 rounded-md border border-neutral/30 px-3 py-2 text-sm"
          />
          <input
            placeholder="CNPJ"
            value={form.cnpj ?? ''}
            onChange={(e) => setForm({ ...form, cnpj: e.target.value || null })}
            className="rounded-md border border-neutral/30 px-3 py-2 text-sm"
          />
          <input
            placeholder="Setor"
            value={form.industry ?? ''}
            onChange={(e) => setForm({ ...form, industry: e.target.value || null })}
            className="rounded-md border border-neutral/30 px-3 py-2 text-sm"
          />
          <input
            placeholder="Moeda (ex: BRL)"
            value={form.reportingCurrency ?? ''}
            onChange={(e) => setForm({ ...form, reportingCurrency: e.target.value || null })}
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
