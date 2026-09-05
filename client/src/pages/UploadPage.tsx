import { useEffect, useRef, useState } from 'react'
import { Button } from '../components/common/Button'
import { Dropzone } from '../components/documents/Dropzone'
import { ProcessingStatus } from '../components/documents/ProcessingStatus'
import { ResultView } from '../components/documents/ResultView'
import { getDocumentResult, getDocumentStatus, listCompanies, uploadDocument } from '../services/documentsApi'
import type {
  Company,
  DocumentResultResponse,
  DocumentType,
  ExtractionStatus,
} from '../types/documents'

const POLL_INTERVAL_MS = 1000

export function UploadPage() {
  const [companies, setCompanies] = useState<Company[]>([])
  const [companiesError, setCompaniesError] = useState<string | null>(null)

  const [file, setFile] = useState<File | null>(null)
  const [companyId, setCompanyId] = useState('')
  const [documentType, setDocumentType] = useState<DocumentType>('BalanceSheet')

  const [documentId, setDocumentId] = useState<string | null>(null)
  const [status, setStatus] = useState<ExtractionStatus | null>(null)
  const [statusError, setStatusError] = useState<string | null>(null)
  const [result, setResult] = useState<DocumentResultResponse | null>(null)
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const pollHandle = useRef<ReturnType<typeof setInterval> | null>(null)

  useEffect(() => {
    listCompanies()
      .then((data) => {
        setCompanies(data)
        if (data.length > 0) setCompanyId(data[0].id)
      })
      .catch((err) => setCompaniesError(err instanceof Error ? err.message : 'Erro ao carregar empresas'))
  }, [])

  useEffect(() => {
    if (!documentId || status === 'Completed' || status === 'Failed') {
      return
    }

    pollHandle.current = setInterval(async () => {
      try {
        const statusResponse = await getDocumentStatus(documentId)
        setStatus(statusResponse.status)
        setStatusError(statusResponse.extractionError)

        if (statusResponse.status === 'Completed') {
          const resultResponse = await getDocumentResult(documentId)
          setResult(resultResponse)
        }
      } catch (err) {
        setStatusError(err instanceof Error ? err.message : 'Erro ao consultar status')
      }
    }, POLL_INTERVAL_MS)

    return () => {
      if (pollHandle.current) clearInterval(pollHandle.current)
    }
  }, [documentId, status])

  const handleSubmit = async () => {
    if (!file || !companyId) return

    setIsSubmitting(true)
    setSubmitError(null)

    try {
      const response = await uploadDocument(file, companyId, documentType)
      setDocumentId(response.documentId)
      setStatus(response.status)
    } catch (err) {
      setSubmitError(err instanceof Error ? err.message : 'Erro ao enviar documento')
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleReset = () => {
    setFile(null)
    setDocumentId(null)
    setStatus(null)
    setStatusError(null)
    setResult(null)
    setSubmitError(null)
  }

  const isProcessing = documentId !== null

  return (
    <div>
      <h1 className="text-2xl font-semibold">Upload de Documento</h1>
      <p className="mt-2 text-neutral">Envie um balanço patrimonial ou DRE em PDF para extração automática.</p>

      {!isProcessing && (
        <div className="mt-6 max-w-xl space-y-4">
          <Dropzone file={file} onFileSelected={setFile} />

          {companiesError && <p className="text-sm text-error">{companiesError}</p>}

          <div>
            <label className="mb-1 block text-sm font-medium">Empresa</label>
            <select
              value={companyId}
              onChange={(event) => setCompanyId(event.target.value)}
              className="w-full rounded-md border border-neutral/30 px-3 py-2 text-sm"
            >
              {companies.length === 0 && <option value="">Nenhuma empresa cadastrada</option>}
              {companies.map((company) => (
                <option key={company.id} value={company.id}>
                  {company.name}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium">Tipo de demonstração</label>
            <select
              value={documentType}
              onChange={(event) => setDocumentType(event.target.value as DocumentType)}
              className="w-full rounded-md border border-neutral/30 px-3 py-2 text-sm"
            >
              <option value="BalanceSheet">Balanço Patrimonial</option>
              <option value="IncomeStatement">DRE</option>
            </select>
          </div>

          {submitError && <p className="text-sm text-error">{submitError}</p>}

          <Button onClick={handleSubmit} disabled={!file || !companyId || isSubmitting} loading={isSubmitting}>
            Fazer Upload
          </Button>
        </div>
      )}

      {isProcessing && (
        <div className="mt-6 max-w-4xl space-y-4">
          {status && <ProcessingStatus status={status} error={statusError} />}

          {result && (
            <>
              <ResultView result={result} />
              <Button variant="secondary" onClick={handleReset}>
                Enviar outro documento
              </Button>
            </>
          )}
        </div>
      )}
    </div>
  )
}
