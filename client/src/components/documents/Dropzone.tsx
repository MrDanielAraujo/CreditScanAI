import { useRef, useState, type DragEvent } from 'react'

interface DropzoneProps {
  file: File | null
  onFileSelected: (file: File) => void
  disabled?: boolean
}

const MAX_SIZE_BYTES = 100 * 1024 * 1024

export function Dropzone({ file, onFileSelected, disabled = false }: DropzoneProps) {
  const inputRef = useRef<HTMLInputElement>(null)
  const [isDragging, setIsDragging] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const validateAndSelect = (candidate: File) => {
    if (!candidate.name.toLowerCase().endsWith('.pdf')) {
      setError('Apenas arquivos PDF são aceitos.')
      return
    }
    if (candidate.size > MAX_SIZE_BYTES) {
      setError('Tamanho máximo de 100MB excedido.')
      return
    }
    setError(null)
    onFileSelected(candidate)
  }

  const handleDrop = (event: DragEvent<HTMLDivElement>) => {
    event.preventDefault()
    setIsDragging(false)
    if (disabled) return

    const dropped = event.dataTransfer.files[0]
    if (dropped) validateAndSelect(dropped)
  }

  return (
    <div>
      <div
        onDragOver={(event) => {
          event.preventDefault()
          if (!disabled) setIsDragging(true)
        }}
        onDragLeave={() => setIsDragging(false)}
        onDrop={handleDrop}
        onClick={() => !disabled && inputRef.current?.click()}
        className={[
          'flex flex-col items-center justify-center gap-2 rounded-lg border-2 border-dashed p-10 text-center transition-colors',
          disabled ? 'cursor-not-allowed opacity-60' : 'cursor-pointer',
          isDragging ? 'border-primary bg-primary/5' : 'border-neutral/30 hover:border-primary/50',
        ].join(' ')}
      >
        <input
          ref={inputRef}
          type="file"
          accept="application/pdf,.pdf"
          className="hidden"
          disabled={disabled}
          onChange={(event) => {
            const selected = event.target.files?.[0]
            if (selected) validateAndSelect(selected)
          }}
        />
        {file ? (
          <>
            <span className="font-medium">{file.name}</span>
            <span className="text-sm text-neutral">{(file.size / 1024 / 1024).toFixed(2)} MB</span>
          </>
        ) : (
          <>
            <span className="font-medium">Arraste um PDF aqui ou clique para selecionar</span>
            <span className="text-sm text-neutral">Máximo 100MB</span>
          </>
        )}
      </div>
      {error && <p className="mt-2 text-sm text-error">{error}</p>}
    </div>
  )
}
