import { useEffect, useRef, useState, type ReactNode } from 'react'

interface DrawerProps {
  open: boolean
  onClose: () => void
  title?: string
  footer?: ReactNode
  children: ReactNode
}

export function Drawer({ open, onClose, title, footer, children }: DrawerProps) {
  const [mounted, setMounted] = useState(open)
  const [visible, setVisible] = useState(false)
  const rafRef = useRef<number | null>(null)

  useEffect(() => {
    if (open) {
      setMounted(true)
      // Double rAF: guarantees the browser paints the closed position first,
      // so the transition to visible actually animates instead of snapping open.
      rafRef.current = requestAnimationFrame(() => {
        rafRef.current = requestAnimationFrame(() => setVisible(true))
      })
      return () => {
        if (rafRef.current !== null) cancelAnimationFrame(rafRef.current)
      }
    }
    setVisible(false)
    const timeout = setTimeout(() => setMounted(false), 300)
    return () => clearTimeout(timeout)
  }, [open])

  useEffect(() => {
    if (!open) return
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose()
    }
    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [open, onClose])

  if (!mounted) return null

  return (
    <div className="fixed inset-0 z-50">
      <div
        className={['absolute inset-0 bg-black/40 transition-opacity duration-300', visible ? 'opacity-100' : 'opacity-0'].join(' ')}
        onClick={onClose}
      />
      <div
        role="dialog"
        aria-modal="true"
        className={[
          'absolute right-0 top-0 flex h-full w-[70vw] max-w-[70vw] min-w-[320px] flex-col bg-surface shadow-xl',
          'transition-transform duration-300 ease-in-out',
          visible ? 'translate-x-0' : 'translate-x-full',
        ].join(' ')}
      >
        <div className="flex shrink-0 items-center justify-between border-b border-neutral/20 p-4">
          {title && <h2 className="text-lg font-semibold">{title}</h2>}
          <button type="button" onClick={onClose} className="cursor-pointer text-neutral hover:text-error" aria-label="Fechar">
            ✕
          </button>
        </div>
        <div className="min-h-0 flex-1 overflow-auto p-4">{children}</div>
        {footer && <div className="flex shrink-0 flex-wrap gap-2 border-t border-neutral/20 p-4">{footer}</div>}
      </div>
    </div>
  )
}
