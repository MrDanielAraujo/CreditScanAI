import { useEffect, useState } from 'react'
import { Button } from '../components/common/Button'
import { DataGrid } from '../components/common/DataGrid/DataGrid'
import type { DataGridColumn } from '../components/common/DataGrid/types'
import { Drawer } from '../components/common/Drawer'
import { DownloadIcon } from '../components/common/icons'
import { FileIcon } from '../components/common/navIcons'
import { downloadDocument, listCompanies, listDocuments, reprocessDocument } from '../services/documentsApi'
import { reportsApi } from '../services/reportsApi'
import type { ClassificationStatus, Company, DocumentListItem, ExtractionStatus } from '../types/documents'
import type { QualityReport } from '../types/reports'

function formatDate(iso: string): string {
  return new Date(iso).toLocaleString('pt-BR')
}

function formatPercent(value: number | null): string {
  return value === null ? '—' : `${(value * 100).toFixed(0)}%`
}

const EXTRACTION_STATUS_LABELS: Record<ExtractionStatus, string> = {
  Pending: 'Pendente',
  Processing: 'Processando',
  Completed: 'Concluída',
  Failed: 'Falhou',
}

const CLASSIFICATION_STATUS_LABELS: Record<ClassificationStatus, string> = {
  NotStarted: 'Não iniciada',
  AwaitingDefaultChartOfAccounts: 'Aguardando plano de contas padrão',
  Processing: 'Processando',
  Completed: 'Concluída',
  Failed: 'Falhou',
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
  const [drawerOpen, setDrawerOpen] = useState(false)
  const [report, setReport] = useState<QualityReport | null>(null)
  const [reportLoading, setReportLoading] = useState(false)
  const [reportError, setReportError] = useState<string | null>(null)

  const [reprocessing, setReprocessing] = useState(false)
  const [reprocessMessage, setReprocessMessage] = useState<string | null>(null)

  const [downloading, setDownloading] = useState(false)
  const [downloadError, setDownloadError] = useState<string | null>(null)

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
    setDrawerOpen(true)
    setReport(null)
    setReportError(null)
    setReprocessMessage(null)
    setDownloadError(null)
    setReportLoading(true)

    reportsApi
      .getQualityReport(documentId)
      .then(setReport)
      .catch((err) => setReportError(err instanceof Error ? err.message : 'Erro ao carregar o relatório de qualidade'))
      .finally(() => setReportLoading(false))
  }

  const extractionStatusOptions = Object.values(EXTRACTION_STATUS_LABELS)
  const classificationStatusOptions = Object.values(CLASSIFICATION_STATUS_LABELS)

  const columns: DataGridColumn<DocumentListItem>[] = [
    { key: 'fileName', label: 'Arquivo' },
    { key: 'companyName', label: 'Empresa' },
    {
      key: 'uploadDate',
      label: 'Enviado em',
      getValue: (row) => row.uploadDate,
      render: (row) => formatDate(row.uploadDate),
    },
    {
      key: 'extractionStatus',
      label: 'Extração',
      filterOptions: extractionStatusOptions,
      getValue: (row) => EXTRACTION_STATUS_LABELS[row.extractionStatus],
    },
    {
      key: 'classificationStatus',
      label: 'Classificação',
      filterOptions: classificationStatusOptions,
      getValue: (row) => CLASSIFICATION_STATUS_LABELS[row.classificationStatus],
    },
  ]

  const handleDownload = async () => {
    if (!selectedId || !report) return
    setDownloading(true)
    setDownloadError(null)
    try {
      await downloadDocument(selectedId, report.fileName)
    } catch (err) {
      setDownloadError(err instanceof Error ? err.message : 'Erro ao baixar documento')
    } finally {
      setDownloading(false)
    }
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
    <div className="flex h-full flex-col">
      <h1 className="flex items-center gap-2 text-2xl font-semibold">
        <FileIcon className="h-6 w-6" />
        Gerenciamento de Documentos
      </h1>
      <p className="mt-2 text-neutral">Todos os documentos já enviados. Selecione um para ver o relatório de qualidade ou reprocessar.</p>

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

        <input
          type="text"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Buscar por nome do arquivo..."
          className="w-full max-w-xs h-10 rounded-md border border-neutral/30 px-3 py-2 text-sm"
        />
      </div>

      {listError && <p className="mt-3 text-sm text-error">{listError}</p>}

      <p className="mb-2 mt-4 text-sm text-neutral">{totalCount} documento(s)</p>
      <div className="min-h-0 flex-1 pb-[10px]">
        <DataGrid
          columns={columns}
          data={items}
          rowKey={(item) => item.id}
          loading={listLoading}
          onRowClick={(item) => selectDocument(item.id)}
          isRowSelected={(item) => item.id === selectedId}
          emptyMessage="Nenhum documento encontrado."
        />
      </div>

      <Drawer
        open={drawerOpen}
        onClose={() => setDrawerOpen(false)}
        title={report?.fileName ?? 'Detalhe do Documento'}
        footer={
          !reportLoading &&
          report && (
            <>
              <Button variant="secondary" onClick={handleDownload} loading={downloading}>
                <DownloadIcon />
                Baixar Documento
              </Button>
              <Button onClick={handleReprocess} loading={reprocessing}>
                Reprocessar Classificação
              </Button>
            </>
          )
        }
      >
        {reportLoading && <p className="text-sm text-neutral">Carregando relatório...</p>}
        {reportError && <p className="text-sm text-error">{reportError}</p>}

        {!reportLoading && report && (
          <div>
            <div className="rounded-md bg-surface-muted p-3">
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
            {downloadError && <p className="mt-4 text-sm text-error">{downloadError}</p>}
          </div>
        )}
      </Drawer>
    </div>
  )
}
