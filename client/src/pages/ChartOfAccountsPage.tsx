import { useEffect, useState } from 'react'
import { Button } from '../components/common/Button'
import { chartOfAccountsApi } from '../services/registrationsApi'
import type { ChartOfAccounts, UpsertChartOfAccountsRequest } from '../types/registrations'

const emptyForm: UpsertChartOfAccountsRequest = { name: '', description: '' }

export function ChartOfAccountsPage() {
  const [items, setItems] = useState<ChartOfAccounts[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [form, setForm] = useState<UpsertChartOfAccountsRequest>(emptyForm)
  const [editingId, setEditingId] = useState<string | null>(null)

  const load = () => {
    setLoading(true)
    chartOfAccountsApi
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
        await chartOfAccountsApi.update(editingId, form)
      } else {
        await chartOfAccountsApi.create(form)
      }
      setForm(emptyForm)
      setEditingId(null)
      load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar')
    }
  }

  const handleEdit = (item: ChartOfAccounts) => {
    setEditingId(item.id)
    setForm({ name: item.name, description: item.description })
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

  return (
    <div>
      <h1 className="text-2xl font-semibold">Planos de Contas</h1>
      <p className="mt-2 text-neutral">
        O plano marcado como <strong>padrão</strong> é o usado automaticamente na classificação de documentos.
      </p>

      <div className="mt-6 grid max-w-2xl grid-cols-2 gap-3">
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
        <div className="flex gap-2">
          <Button onClick={handleSubmit} disabled={!form.name}>
            {editingId ? 'Salvar' : 'Adicionar'}
          </Button>
          {editingId && (
            <Button
              variant="secondary"
              onClick={() => {
                setEditingId(null)
                setForm(emptyForm)
              }}
            >
              Cancelar
            </Button>
          )}
        </div>
      </div>

      {error && <p className="mt-3 text-sm text-error">{error}</p>}

      <table className="mt-6 w-full max-w-2xl border-collapse text-sm">
        <thead>
          <tr className="border-b border-neutral/20 bg-surface-muted text-left">
            <th className="p-2">Nome</th>
            <th className="p-2">Padrão</th>
            <th className="p-2" />
          </tr>
        </thead>
        <tbody>
          {loading && (
            <tr>
              <td className="p-2 text-neutral" colSpan={3}>
                Carregando...
              </td>
            </tr>
          )}
          {!loading &&
            items.map((item) => (
              <tr key={item.id} className="border-b border-neutral/10">
                <td className="p-2">{item.name}</td>
                <td className="p-2">
                  {item.isDefault ? (
                    <span className="rounded-full bg-success/10 px-2 py-0.5 text-xs font-medium text-success">Padrão</span>
                  ) : (
                    <button className="text-sm text-primary" onClick={() => handleSetDefault(item.id)}>
                      Definir como padrão
                    </button>
                  )}
                </td>
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
