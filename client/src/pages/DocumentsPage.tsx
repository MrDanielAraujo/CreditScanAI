import { useEffect, useState } from 'react'
import { Button } from '../components/common/Button'
import { listCompanies, listDocuments, reprocessDocument } from '../services/documentsApi'
import { reportsApi } from '../services/reportsApi'
import type { Company, DocumentListItem } from '../types/documents'
import type { QualityReport } from '../types/reports'

function formatDate(iso: string): string {
  return new Date(iso).toLocaleString('pt-BR')
}

function formatPercent(value: number | null): string {
  return value === null ? '—' : `${(value * 100).toFixed(0)}%`
}

export function DocumentsPage() {
  const [companies, setCompanies] = useState<Company[]>([])
  const [companyId, setCompanyId] = useState('')
  const [search, setSearch] = useState('')

  const [items, setItems] = useState<DocumentListItem[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [listLoading, setListLoading] = useState(true)
  const [listError, setListError] = useState<string | null>(null)

  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [report, setReport] = useState<QualityReport | null>(null)
  const [reportLoading, setReportLoading] = useState(false)
  const [reportError, setReportError] = useState<string | null>(null)

  const [reprocessing, setReprocessing] = useState(false)
  const [reprocessMessage, setReprocessMessage] = useState<string | null>(null)

  const loadDocuments = () => {
    setListLoading(true)
    setListError(null)
    listDocuments({ companyId: companyId || undefined, search: search || undefined, limit: 100 })
      .then((res) => {
        setItems(res.items)
        setTotalCount(res.totalCount)
      })
      .catch((err) => setListError(err instanceof Error ? err.message : 'Erro ao carregar documentos'))
      .finally(() => setListLoading(false))
  }

  useEffect(() => {
    listCompanies()
      .then(setCompanies)
      .catch(() => setCompanies([]))
  }, [])

  useEffect(loadDocuments, [companyId, search])

  const selectDocument = (documentId: string) => {
    setSelectedId(documentId)
    setReport(null)
    setReportError(null)
    setReprocessMessage(null)
    setReportLoading(true)

    reportsApi
      .getQualityReport(documentId)
      .then(setReport)
      .catch((err) => setReportError(err instanceof Error ? err.message : 'Erro ao carregar o relatório de qualidade'))
      .finally(() => setReportLoading(false))
  }

  const handleReprocess = async () => {
    if (!selectedId) return
    setReprocessing(true)
    setReprocessMessage(null)
    try {
      await reprocessDocument(selectedId)
      setReprocessMessage('Reprocessamento iniciado - a classificação será refeita em segundo plano.')
      loadDocuments()
    } catch (err) {
      setReprocessMessage(err instanceof Error ? err.message : 'Erro ao reprocessar')
    } finally {
      setReprocessing(false)
    }
  }

  return (
    <div>
      <h1 className="text-2xl font-semibold">Gerenciamento de Documentos</h1>
      <p className="mt-2 text-neutral">Todos os documentos já enviados. Selecione um para ver o relatório de qualidade ou reprocessar.</p>

      <div className="mt-4 flex flex-wrap items-center gap-3">
        <select
          value={companyId}
          onChange={(e) => setCompanyId(e.target.value)}
          className="w-full max-w-xs rounded-md border border-neutral/30 px-3 py-2 text-sm"
        >
          <option value="">Todas as empresas</option>
          {companies.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
            </option>
          ))}
        </select>

        <input
          type="text"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Buscar por nome do arquivo..."
          className="w-full max-w-xs rounded-md border border-neutral/30 px-3 py-2 text-sm"
        />
      </div>

      {listError && <p className="mt-3 text-sm text-error">{listError}</p>}

      <div className="mt-6 flex gap-6">
        <div className="flex-1">
          <p className="mb-2 text-sm text-neutral">{totalCount} documento(s)</p>
          <table className="w-full border-collapse text-sm">
            <thead>
              <tr className="border-b border-neutral/20 bg-surface-muted text-left">
                <th className="p-2">Arquivo</th>
                <th className="p-2">Empresa</th>
                <th className="p-2">Enviado em</th>
                <th className="p-2">Extração</th>
                <th className="p-2">Classificação</th>
              </tr>
            </thead>
            <tbody>
              {listLoading && (
                <tr>
                  <td className="p-2 text-neutral" colSpan={5}>
                    Carregando...
                  </td>
                </tr>
              )}
              {!listLoading && items.length === 0 && (
                <tr>
                  <td className="p-2 text-neutral" colSpan={5}>
                    Nenhum documento encontrado.
                  </td>
                </tr>
              )}
              {!listLoading &&
                items.map((item) => (
                  <tr
                    key={item.id}
                    onClick={() => selectDocument(item.id)}
                    className={[
                      'cursor-pointer border-b border-neutral/10',
                      item.id === selectedId ? 'bg-primary/10' : 'hover:bg-neutral/10',
                    ].join(' ')}
                  >
                    <td className="p-2">{item.fileName}</td>
                    <td className="p-2 text-neutral">{item.companyName}</td>
                    <td className="p-2 text-neutral">{formatDate(item.uploadDate)}</td>
                    <td className="p-2 text-neutral">{item.extractionStatus}</td>
                    <td className="p-2 text-neutral">{item.classificationStatus}</td>
                  </tr>
                ))}
            </tbody>
          </table>
        </div>

        <div className="w-96 shrink-0 rounded-md border border-neutral/20 p-4">
          {!selectedId && <p className="text-sm text-neutral">Selecione um documento na lista para ver o detalhe.</p>}
          {selectedId && reportLoading && <p className="text-sm text-neutral">Carregando relatório...</p>}
          {selectedId && reportError && <p className="text-sm text-error">{reportError}</p>}

          {selectedId && !reportLoading && report && (
            <div>
              <h2 className="text-lg font-semibold">{report.fileName}</h2>

              <div className="mt-4 rounded-md bg-surface-muted p-3">
                <p className="text-xs font-semibold uppercase text-neutral">Classificações</p>
                <p className="mt-1 text-sm">Total: {report.totalClassifiedAccounts}</p>
                <p className="text-sm text-neutral">
                  Aprovadas: {report.approvedCount} · Corrigidas: {report.overriddenCount} · Rejeitadas: {report.rejectedCount}
                </p>
                <p className="text-sm text-neutral">
                  Auto-aprovadas: {report.pendingCount} · Precisam revisão: {report.needsReviewCount}
                </p>
                <p className="mt-1 text-sm text-neutral">Confiança média: {formatPercent(report.averageConfidence)}</p>
              </div>

              <div className="mt-4">
                <p className="text-xs font-semibold uppercase text-neutral">Equação por período</p>
                {report.periodEquationStatus.length === 0 && <p className="mt-1 text-sm text-neutral">Sem períodos detectados.</p>}
                <ul className="mt-1 flex flex-col gap-1 text-sm">
                  {report.periodEquationStatus.map((p) => (
                    <li key={p.periodId}>
                      {p.periodLabel}:{' '}
                      {p.equationBalanced === null ? (
                        <span className="text-neutral">ainda não calculado</span>
                      ) : p.equationBalanced ? (
                        <span className="text-success">balanceada</span>
                      ) : (
                        <span className="text-error">desbalanceada</span>
                      )}
                    </li>
                  ))}
                </ul>
              </div>

              {reprocessMessage && <p className="mt-4 text-sm text-neutral">{reprocessMessage}</p>}

              <Button className="mt-4" onClick={handleReprocess} loading={reprocessing}>
                Reprocessar Classificação
              </Button>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
