import { useEffect, useRef, useState } from 'react'
import { Button } from '../components/common/Button'
import { UploadIcon } from '../components/common/navIcons'
import { Dropzone } from '../components/documents/Dropzone'
import { ProcessingStatus } from '../components/documents/ProcessingStatus'
import { ResultView } from '../components/documents/ResultView'
import { getDocumentResult, getDocumentStatus, uploadDocument } from '../services/documentsApi'
import type { DocumentResultResponse, ExtractionStatus } from '../types/documents'

const POLL_INTERVAL_MS = 1000

function formatCnpj(rawValue: string): string {
  const digits = rawValue.replace(/\D/g, '').slice(0, 14)
  const parts = [digits.slice(0, 2), digits.slice(2, 5), digits.slice(5, 8), digits.slice(8, 12), digits.slice(12, 14)]
  let formatted = parts[0]
  if (parts[1]) formatted += `.${parts[1]}`
  if (parts[2]) formatted += `.${parts[2]}`
  if (parts[3]) formatted += `/${parts[3]}`
  if (parts[4]) formatted += `-${parts[4]}`
  return formatted
}

export function UploadPage() {
  const [file, setFile] = useState<File | null>(null)
  const [cnpj, setCnpj] = useState('')

  const [documentId, setDocumentId] = useState<string | null>(null)
  const [status, setStatus] = useState<ExtractionStatus | null>(null)
  const [statusError, setStatusError] = useState<string | null>(null)
  const [result, setResult] = useState<DocumentResultResponse | null>(null)
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [submitMessage, setSubmitMessage] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const pollHandle = useRef<ReturnType<typeof setInterval> | null>(null)

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

  const cnpjDigits = cnpj.replace(/\D/g, '')

  const handleSubmit = async () => {
    if (!file || cnpjDigits.length !== 14) return

    setIsSubmitting(true)
    setSubmitError(null)
    setSubmitMessage(null)

    try {
      const response = await uploadDocument(file, cnpj)
      setDocumentId(response.documentId)
      setStatus(response.status)
      if (response.companyCreated) {
        setSubmitMessage('Nenhuma empresa tinha esse CNPJ - cadastramos uma nova automaticamente.')
      }
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
    setSubmitMessage(null)
  }

  const isProcessing = documentId !== null

  return (
    <div>
      <h1 className="flex items-center gap-2 text-2xl font-semibold">
        <UploadIcon className="h-6 w-6" />
        Upload de Documento
      </h1>
      <p className="mt-2 text-neutral">
        Envie um balanço patrimonial, DRE, ou um PDF com os dois - o sistema identifica sozinho.
      </p>

      {!isProcessing && (
        <div className="mt-6 max-w-xl space-y-4">
          <Dropzone file={file} onFileSelected={setFile} />

          <div>
            <label className="mb-1 block text-sm font-medium">CNPJ da empresa</label>
            <input
              type="text"
              value={cnpj}
              onChange={(event) => setCnpj(formatCnpj(event.target.value))}
              placeholder="00.000.000/0000-00"
              className="w-full h-10 rounded-md border border-neutral/30 px-3 py-2 text-sm"
            />
            <p className="mt-1 text-xs text-neutral">
              Se ainda não existir uma empresa com esse CNPJ, ela será cadastrada automaticamente.
            </p>
          </div>

          {submitError && <p className="text-sm text-error">{submitError}</p>}

          <Button onClick={handleSubmit} disabled={!file || cnpjDigits.length !== 14 || isSubmitting} loading={isSubmitting}>
            Fazer Upload
          </Button>
        </div>
      )}

      {isProcessing && (
        <div className="mt-6 max-w-4xl space-y-4">
          {submitMessage && <p className="text-sm text-success">{submitMessage}</p>}
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
