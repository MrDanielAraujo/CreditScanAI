import { useEffect, useState } from 'react'
import { Button } from '../components/common/Button'
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

  const load = () => {
    setLoading(true)
    companiesApi
      .list()
      .then(setItems)
      .catch((err) => setError(err instanceof Error ? err.message : 'Erro ao carregar'))
      .finally(() => setLoading(false))
  }

  useEffect(load, [])

  const handleSubmit = async () => {
    setError(null)
    try {
      if (editingId) {
        await companiesApi.update(editingId, form)
      } else {
        await companiesApi.create(form)
      }
      setForm(buildEmptyForm())
      setEditingId(null)
      load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar')
    }
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

  return (
    <div>
      <h1 className="text-2xl font-semibold">Empresas</h1>
      <p className="mt-2 text-neutral">Empresas cujos documentos podem ser classificados, calculados e consolidados.</p>

      <div className="mt-6 grid max-w-3xl grid-cols-3 gap-3">
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
        <div className="col-span-3 flex gap-2">
          <Button onClick={handleSubmit} disabled={!form.code || !form.name}>
            {editingId ? 'Salvar' : 'Adicionar'}
          </Button>
          {editingId && (
            <Button
              variant="secondary"
              onClick={() => {
                setEditingId(null)
                setForm(buildEmptyForm())
              }}
            >
              Cancelar
            </Button>
          )}
        </div>
      </div>

      {error && <p className="mt-3 text-sm text-error">{error}</p>}

      <table className="mt-6 w-full max-w-3xl border-collapse text-sm">
        <thead>
          <tr className="border-b border-neutral/20 bg-surface-muted text-left">
            <th className="p-2">Código</th>
            <th className="p-2">Nome</th>
            <th className="p-2">CNPJ</th>
            <th className="p-2">Moeda</th>
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
                <td className="p-2">{item.code}</td>
                <td className="p-2">{item.name}</td>
                <td className="p-2 text-neutral">{item.cnpj ?? '—'}</td>
                <td className="p-2 text-neutral">{item.reportingCurrency}</td>
                <td className="p-2 text-right">
                  <button className="mr-3 text-primary" onClick={() => handleEdit(item)}>
                    Editar
                  </button>
                  <button className="text-error" onClick={() => handleDelete(item.id)}>
                    Excluir
                  </button>
                </td>
              </tr>
            ))}
        </tbody>
      </table>
    </div>
  )
}
