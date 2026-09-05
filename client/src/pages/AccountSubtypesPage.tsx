import { useEffect, useState } from 'react'
import { Button } from '../components/common/Button'
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

  const handleSubmit = async () => {
    setError(null)
    try {
      if (editingId) {
        await accountSubtypesApi.update(editingId, form)
      } else {
        await accountSubtypesApi.create(form)
      }
      setForm({ ...emptyForm, accountTypeId: form.accountTypeId })
      setEditingId(null)
      load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar')
    }
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

  return (
    <div>
      <h1 className="text-2xl font-semibold">Subtipos de Conta</h1>
      <p className="mt-2 text-neutral">
        Ex: Circulante, Não Circulante. Ao criar, a compatibilidade com o Tipo escolhido é registrada automaticamente.
      </p>

      <div className="mt-6 grid max-w-2xl grid-cols-2 gap-3">
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
        <div className="flex gap-2">
          <Button onClick={handleSubmit} disabled={!form.code || !form.name || !form.accountTypeId}>
            {editingId ? 'Salvar' : 'Adicionar'}
          </Button>
          {editingId && (
            <Button
              variant="secondary"
              onClick={() => {
                setEditingId(null)
                setForm({ ...emptyForm, accountTypeId: form.accountTypeId })
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
            <th className="p-2">Código</th>
            <th className="p-2">Nome</th>
            <th className="p-2">Tipo</th>
            <th className="p-2" />
          </tr>
        </thead>
        <tbody>
          {loading && (
            <tr>
              <td className="p-2 text-neutral" colSpan={4}>
                Carregando...
              </td>
            </tr>
          )}
          {!loading &&
            items.map((item) => (
              <tr key={item.id} className="border-b border-neutral/10">
                <td className="p-2">{item.code}</td>
                <td className="p-2">{item.name}</td>
                <td className="p-2">{typeName(item.accountTypeId)}</td>
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
