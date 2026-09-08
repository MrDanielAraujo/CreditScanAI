import { useEffect, useState } from 'react'
import { Button } from '../components/common/Button'
import { DataGrid } from '../components/common/DataGrid/DataGrid'
import type { DataGridColumn } from '../components/common/DataGrid/types'
import { Drawer } from '../components/common/Drawer'
import { TrashIcon } from '../components/common/icons'
import { BuildingIcon } from '../components/common/navIcons'
import { useToast } from '../contexts/toastContextValue'
import { companiesApi } from '../services/companiesApi'
import type { Company, UpsertCompanyRequest } from '../types/documents'

function buildEmptyForm(): UpsertCompanyRequest {
  return { code: '', name: '', legalName: '', cnpj: '', industry: '', fiscalYearEnd: null, reportingCurrency: 'BRL' }
}

export function CompaniesPage() {
  const { showToast } = useToast()
  const [items, setItems] = useState<Company[]>([])
  const [loading, setLoading] = useState(true)
  const [form, setForm] = useState<UpsertCompanyRequest>(buildEmptyForm())
  const [editingId, setEditingId] = useState<string | null>(null)
  const [drawerOpen, setDrawerOpen] = useState(false)

  const load = () => {
    setLoading(true)
    companiesApi
      .list()
      .then(setItems)
      .catch((err) => showToast(err instanceof Error ? err.message : 'Erro ao carregar', 'error'))
      .finally(() => setLoading(false))
  }

  useEffect(load, [showToast])

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
    try {
      if (editingId) {
        await companiesApi.update(editingId, form)
      } else {
        await companiesApi.create(form)
      }
      setDrawerOpen(false)
      load()
    } catch (err) {
      showToast(err instanceof Error ? err.message : 'Erro ao salvar', 'error')
    }
  }

  const handleDelete = async (id: string) => {
    if (!confirm('Excluir esta Empresa?')) return
    try {
      await companiesApi.remove(id)
      load()
    } catch (err) {
      showToast(err instanceof Error ? err.message : 'Erro ao excluir', 'error')
    }
  }

  const columns: DataGridColumn<Company>[] = [
    { key: 'code', label: 'Código' },
    { key: 'name', label: 'Nome' },
    { key: 'cnpj', label: 'CNPJ', getValue: (row) => row.cnpj ?? '—' },
    { key: 'reportingCurrency', label: 'Moeda' },
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
            <BuildingIcon className="h-6 w-6" />
            Empresas
          </h1>
          <p className="mt-2 text-neutral">Empresas cujos documentos podem ser classificados, calculados e consolidados.</p>
        </div>
        <Button onClick={openCreateDrawer}>Nova Empresa</Button>
      </div>

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

      <Drawer
        open={drawerOpen}
        onClose={() => setDrawerOpen(false)}
        title={editingId ? 'Editar Empresa' : 'Nova Empresa'}
        footer={
          <>
            <Button onClick={handleSubmit} disabled={!form.code || !form.name}>
              {editingId ? 'Salvar' : 'Adicionar'}
            </Button>
            <Button variant="secondary" onClick={() => setDrawerOpen(false)}>
              Cancelar
            </Button>
          </>
        }
      >
        <div className="grid max-w-2xl grid-cols-2 gap-3">
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
            placeholder="Razão Social"
            value={form.legalName ?? ''}
            onChange={(e) => setForm({ ...form, legalName: e.target.value || null })}
            className="col-span-2 h-10 rounded-md border border-neutral/30 px-3 py-2 text-sm"
          />
          <input
            placeholder="CNPJ"
            value={form.cnpj ?? ''}
            onChange={(e) => setForm({ ...form, cnpj: e.target.value || null })}
            className="h-10 rounded-md border border-neutral/30 px-3 py-2 text-sm"
          />
          <input
            placeholder="Setor"
            value={form.industry ?? ''}
            onChange={(e) => setForm({ ...form, industry: e.target.value || null })}
            className="h-10 rounded-md border border-neutral/30 px-3 py-2 text-sm"
          />
          <input
            placeholder="Moeda (ex: BRL)"
            value={form.reportingCurrency ?? ''}
            onChange={(e) => setForm({ ...form, reportingCurrency: e.target.value || null })}
            className="h-10 rounded-md border border-neutral/30 px-3 py-2 text-sm"
          />
        </div>
      </Drawer>
    </div>
  )
}
