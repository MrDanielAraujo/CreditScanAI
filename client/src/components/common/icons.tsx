import type { SVGProps } from 'react'

type IconProps = SVGProps<SVGSVGElement>

const base = {
  viewBox: '0 0 24 24',
  fill: 'none',
  stroke: 'currentColor',
  strokeWidth: 1.75,
  strokeLinecap: 'round' as const,
  strokeLinejoin: 'round' as const,
}

export function TrashIcon(props: IconProps) {
  return (
    <svg {...base} className="h-4 w-4" {...props}>
      <path d="M4 7h16" />
      <path d="M9 7V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v3" />
      <path d="M6 7l1 12a2 2 0 0 0 2 2h6a2 2 0 0 0 2-2l1-12" />
      <path d="M10 11v6" />
      <path d="M14 11v6" />
    </svg>
  )
}

export function KeyIcon(props: IconProps) {
  return (
    <svg {...base} className="h-4 w-4" {...props}>
      <circle cx="7" cy="15" r="3" />
      <path d="M9.5 12.5L20 2" />
      <path d="M17 5l2 2" />
      <path d="M14 8l2 2" />
    </svg>
  )
}

export function LockIcon(props: IconProps) {
  return (
    <svg {...base} className="h-4 w-4" {...props}>
      <rect x="5" y="11" width="14" height="9" rx="2" />
      <path d="M8 11V7a4 4 0 0 1 8 0v4" />
    </svg>
  )
}

export function UnlockIcon(props: IconProps) {
  return (
    <svg {...base} className="h-4 w-4" {...props}>
      <rect x="5" y="11" width="14" height="9" rx="2" />
      <path d="M8 11V7a4 4 0 0 1 7.75-1.4" />
    </svg>
  )
}
