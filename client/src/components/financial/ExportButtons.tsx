import { DownloadIcon } from '../common/icons'

interface ExportButtonsProps {
  onExportPdf: () => void
  onExportExcel: () => void
}

/** PDF em vermelho e Excel em verde - mesma associação de cor que os ícones oficiais desses formatos, então funciona como sinal reconhecível em vez de decoração. */
export function ExportButtons({ onExportPdf, onExportExcel }: ExportButtonsProps) {
  return (
    <div className="flex gap-2">
      <button
        type="button"
        onClick={onExportPdf}
        className="inline-flex cursor-pointer items-center gap-1.5 rounded-md border border-error/30 bg-error/5 px-3 py-1.5 text-sm font-medium text-error transition-colors hover:bg-error/10"
      >
        <DownloadIcon />
        Baixar PDF
      </button>
      <button
        type="button"
        onClick={onExportExcel}
        className="inline-flex cursor-pointer items-center gap-1.5 rounded-md border border-success/30 bg-success/5 px-3 py-1.5 text-sm font-medium text-success transition-colors hover:bg-success/10"
      >
        <DownloadIcon />
        Baixar Excel
      </button>
    </div>
  )
}
