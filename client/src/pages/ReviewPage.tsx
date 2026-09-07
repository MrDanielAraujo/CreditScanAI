import { useEffect, useState } from 'react'
import { Button } from '../components/common/Button'
import { DataGrid } from '../components/common/DataGrid/DataGrid'
import type { DataGridColumn } from '../components/common/DataGrid/types'
import { Drawer } from '../components/common/Drawer'
import { CheckSquareIcon } from '../components/common/navIcons'
import { classificationsApi } from '../services/classificationsApi'
import { listCompanies } from '../services/documentsApi'
import { standardAccountsApi } from '../services/registrationsApi'
import type { ClassificationDetail, PendingClassification } from '../types/classifications'
import type { Company } from '../types/documents'
import type { StandardAccount } from '../types/registrations'

export function ReviewPage() {
  const [companies, setCompanies] = useState<Company[]>([])
  const [companyId, setCompanyId] = useState('')
  const [showAll, setShowAll] = useState(false)

  const [items, setItems] = useState<PendingClassification[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [listLoading, setListLoading] = useState(true)
  const [listError, setListError] = useState<string | null>(null)

  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [drawerOpen, setDrawerOpen] = useState(false)
  const [detail, setDetail] = useState<ClassificationDetail | null>(null)
  const [detailLoading, setDetailLoading] = useState(false)
  const [standardAccounts, setStandardAccounts] = useState<StandardAccount[]>([])
  const [overrideAccountId, setOverrideAccountId] = useState('')
  const [reason, setReason] = useState('')

  const [actionLoading, setActionLoading] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)

  const loadPending = () => {
    setListLoading(true)
    setListError(null)
    classificationsApi
      .listPending({ companyId: companyId || undefined, status: showAll ? 'all' : 'needs_review', limit: 100 })
      .then((res) => {
        setItems(res.items)
        setTotalCount(res.totalCount)
      })
      .catch((err) => setListError(err instanceof Error ? err.message : 'Erro ao carregar a fila'))
      .finally(() => setListLoading(false))
  }

  useEffect(() => {
    listCompanies()
      .then(setCompanies)
      .catch(() => setCompanies([]))
  }, [])

  useEffect(loadPending, [companyId, showAll])

  const selectItem = (classificationId: string) => {
    setSelectedId(classificationId)
    setDrawerOpen(true)
    setDetail(null)
    setStandardAccounts([])
    setOverrideAccountId('')
    setReason('')
    setActionError(null)
    setDetailLoading(true)

    classificationsApi
      .getById(classificationId)
      .then((d) => {
        setDetail(d)
        return standardAccountsApi.list(d.chartOfAccountsId)
      })
      .then((accounts) => setStandardAccounts(accounts))
      .catch((err) => setActionError(err instanceof Error ? err.message : 'Erro ao carregar o detalhe'))
      .finally(() => setDetailLoading(false))
  }

  const afterAction = () => {
    setSelectedId(null)
    setDrawerOpen(false)
    setDetail(null)
    loadPending()
  }

  const handleApprove = async () => {
    if (!detail) return
    setActionLoading(true)
    setActionError(null)
    try {
      await classificationsApi.approve(detail.classificationId, { notes: reason || null })
      afterAction()
    } catch (err) {
      setActionError(err instanceof Error ? err.message : 'Erro ao aprovar')
    } finally {
      setActionLoading(false)
    }
  }

  const handleReject = async () => {
    if (!detail) return
    setActionLoading(true)
    setActionError(null)
    try {
      await classificationsApi.reject(detail.classificationId, { reason: reason || null })
      afterAction()
    } catch (err) {
      setActionError(err instanceof Error ? err.message : 'Erro ao rejeitar')
    } finally {
      setActionLoading(false)
    }
  }

  const handleOverride = async () => {
    if (!detail || !overrideAccountId) return
    setActionLoading(true)
    setActionError(null)
    try {
      await classificationsApi.override(detail.classificationId, { newStandardAccountId: overrideAccountId, reason: reason || null })
      afterAction()
    } catch (err) {
      setActionError(err instanceof Error ? err.message : 'Erro ao aplicar o override')
    } finally {
      setActionLoading(false)
    }
  }

  const methodOptions = ['EXACT_MATCH', 'PATTERN_MATCH', 'AI', 'AI_UNAVAILABLE', 'HISTORICAL_DECISION', 'CROSS_COMPANY_PATTERN', 'UNKNOWN']
  const statusOptions = ['Pending', 'NeedsReview', 'Approved', 'Rejected', 'Overridden']

  const columns: DataGridColumn<PendingClassification>[] = [
    { key: 'sourceAccountName', label: 'Conta Original' },
    { key: 'suggestedStandardAccountName', label: 'Sugerida' },
    {
      key: 'confidenceScore',
      label: 'Confiança',
      align: 'right',
      aggregate: 'avg',
      getValue: (row) => row.confidenceScore * 100,
      render: (row) => `${(row.confidenceScore * 100).toFixed(0)}%`,
    },
    { key: 'classificationMethod', label: 'Método', filterOptions: methodOptions },
    ...(showAll ? [{ key: 'reviewStatus', label: 'Status', filterOptions: statusOptions } as DataGridColumn<PendingClassification>] : []),
  ]

  return (
    <div className="flex h-full flex-col">
      <h1 className="flex items-center gap-2 text-2xl font-semibold">
        <CheckSquareIcon className="h-6 w-6" />
        Fila de Revisão
      </h1>
      <p className="mt-2 text-neutral">
        {showAll
          ? 'Todas as classificações, incluindo as já auto-aprovadas com confiança alta - confirme ou corrija qualquer uma.'
          : 'Contas classificadas com baixa confiança, ou sem nenhuma correspondência.'}{' '}
        Selecione um item para aprovar, corrigir ou rejeitar.
      </p>

      <div className="mt-4 flex flex-wrap items-center gap-3">
        <select
          value={companyId}
          onChange={(e) => setCompanyId(e.target.value)}
          className="w-full max-w-xs h-10 rounded-md border border-neutral/30 px-3 py-2 text-sm"
        >
          <option value="">Todas as empresas</option>
          {companies.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
            </option>
          ))}
        </select>

        <label className="flex items-center gap-2 text-sm text-neutral">
          <input type="checkbox" checked={showAll} onChange={(e) => setShowAll(e.target.checked)} />
          Mostrar todas as classificações (não só as pendentes de revisão)
        </label>
      </div>

      {listError && <p className="mt-3 text-sm text-error">{listError}</p>}

      <p className="mb-2 mt-4 text-sm text-neutral">
        {totalCount} {showAll ? 'classificação(ões) no servidor' : 'pendente(s) no servidor'} (máx. 100 carregadas por vez)
      </p>
      <div className="min-h-0 flex-1 pb-[10px]">
        <DataGrid
          columns={columns}
          data={items}
          rowKey={(item) => item.classificationId}
          loading={listLoading}
          onRowClick={(item) => selectItem(item.classificationId)}
          isRowSelected={(item) => item.classificationId === selectedId}
          emptyMessage={showAll ? 'Nenhuma classificação encontrada.' : 'Nenhum item pendente de revisão.'}
        />
      </div>

      <Drawer
        open={drawerOpen}
        onClose={() => setDrawerOpen(false)}
        title={detail?.sourceAccount.originalName ?? 'Detalhe da Classificação'}
        footer={
          !detailLoading &&
          detail && (
            <>
              <Button onClick={handleApprove} loading={actionLoading} disabled={!detail.suggestedStandardAccount}>
                Aprovar
              </Button>
              <Button variant="secondary" onClick={handleOverride} loading={actionLoading} disabled={!overrideAccountId}>
                Aplicar Correção
              </Button>
              <Button variant="danger" onClick={handleReject} loading={actionLoading}>
                Rejeitar
              </Button>
            </>
          )
        }
      >
        {detailLoading && <p className="text-sm text-neutral">Carregando detalhe...</p>}
        {!detailLoading && detail && (
          <div>
            <p className="text-xs text-neutral">
              Tipo: {detail.sourceAccount.inferredType ?? '—'} / Subtipo: {detail.sourceAccount.inferredSubtype ?? '—'}
            </p>

            <div className="mt-4 rounded-md bg-surface-muted p-3">
              <p className="text-xs font-semibold uppercase text-neutral">Sugestão</p>
              <p className="mt-1 text-sm font-medium">{detail.suggestedStandardAccount?.name ?? 'Nenhuma'}</p>
              <p className="text-xs text-neutral">
                {detail.classificationMethod} · {(detail.confidenceScore * 100).toFixed(0)}% de confiança
              </p>
              {detail.evidence && <p className="mt-2 text-xs text-neutral">{detail.evidence}</p>}
            </div>

            <label className="mt-4 block text-xs font-semibold uppercase text-neutral">Corrigir para</label>
            <select
              value={overrideAccountId}
              onChange={(e) => setOverrideAccountId(e.target.value)}
              className="mt-1 w-full max-w-md h-10 rounded-md border border-neutral/30 px-3 py-2 text-sm"
            >
              <option value="">Selecione uma conta padrão...</option>
              {standardAccounts.map((a) => (
                <option key={a.id} value={a.id}>
                  {a.name}
                </option>
              ))}
            </select>

            <label className="mt-3 block text-xs font-semibold uppercase text-neutral">Observação (opcional)</label>
            <textarea
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              rows={2}
              className="mt-1 w-full max-w-md rounded-md border border-neutral/30 px-3 py-2 text-sm"
            />

            {actionError && <p className="mt-3 text-sm text-error">{actionError}</p>}
          </div>
        )}
      </Drawer>
    </div>
  )
}
