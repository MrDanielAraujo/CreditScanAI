import type { ExtractionStatus } from '../../types/documents'

interface ProcessingStatusProps {
  status: ExtractionStatus
  error?: string | null
}

const labels: Record<ExtractionStatus, string> = {
  Pending: 'Na fila para processamento...',
  Processing: 'Extraindo dados do PDF...',
  Completed: 'Processamento concluído',
  Failed: 'Falha no processamento',
}

export function ProcessingStatus({ status, error }: ProcessingStatusProps) {
  const isActive = status === 'Pending' || status === 'Processing'

  return (
    <div className="flex items-center gap-3 rounded-lg border border-neutral/20 bg-surface p-4">
      {isActive && (
        <span className="h-5 w-5 shrink-0 animate-spin rounded-full border-2 border-primary border-t-transparent" />
      )}
      {status === 'Completed' && <span className="shrink-0 text-success">✓</span>}
      {status === 'Failed' && <span className="shrink-0 text-error">✕</span>}

      <div>
        <p className="font-medium">{labels[status]}</p>
        {status === 'Failed' && error && <p className="mt-1 text-sm text-error">{error}</p>}
      </div>
    </div>
  )
}
