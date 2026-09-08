import { useCallback, useRef, useState, type ReactNode } from 'react'
import { AlertTriangleIcon, CheckCircleIcon } from '../components/common/icons'
import { ToastContext, type ToastType } from './toastContextValue'

interface ToastEntry {
  id: number
  message: string
  type: ToastType
}

const AUTO_DISMISS_MS = 5000

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<ToastEntry[]>([])
  const nextId = useRef(0)

  const dismiss = useCallback((id: number) => {
    setToasts((prev) => prev.filter((t) => t.id !== id))
  }, [])

  const showToast = useCallback(
    (message: string, type: ToastType) => {
      const id = nextId.current++
      setToasts((prev) => [...prev, { id, message, type }])
      setTimeout(() => dismiss(id), AUTO_DISMISS_MS)
    },
    [dismiss],
  )

  return (
    <ToastContext.Provider value={{ showToast }}>
      {children}
      <div className="pointer-events-none fixed right-4 top-4 z-[100] flex w-full max-w-sm flex-col gap-2">
        {toasts.map((toast) => (
          <div
            key={toast.id}
            role="alert"
            className={[
              'pointer-events-auto flex items-start gap-2 rounded-lg border p-3 text-sm shadow-lg backdrop-blur-md backdrop-saturate-150',
              toast.type === 'success' ? 'border-success/30 bg-success/20 text-success' : 'border-error/30 bg-error/20 text-error',
            ].join(' ')}
          >
            <span className="mt-0.5 shrink-0">{toast.type === 'success' ? <CheckCircleIcon /> : <AlertTriangleIcon />}</span>
            <span className="flex-1">{toast.message}</span>
            <button
              type="button"
              onClick={() => dismiss(toast.id)}
              className="cursor-pointer text-current opacity-70 hover:opacity-100"
              aria-label="Fechar"
            >
              ✕
            </button>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  )
}
