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

export function DownloadIcon(props: IconProps) {
  return (
    <svg {...base} className="h-4 w-4" {...props}>
      <path d="M12 4v11" />
      <path d="M8 11l4 4 4-4" />
      <path d="M4 19h16" />
    </svg>
  )
}

export function ScaleIcon(props: IconProps) {
  return (
    <svg {...base} className="h-[18px] w-[18px]" {...props}>
      <path d="M12 3v18" />
      <path d="M7 21h10" />
      <path d="M4 7h6" />
      <path d="M14 7h6" />
      <path d="M4 7l-2.5 5a2.5 2.5 0 0 0 5 0z" />
      <path d="M20 7l-2.5 5a2.5 2.5 0 0 0 5 0z" />
    </svg>
  )
}

export function TrendingUpIcon(props: IconProps) {
  return (
    <svg {...base} className="h-[18px] w-[18px]" {...props}>
      <path d="M4 16l6-6 4 4 6-7" />
      <path d="M14 7h6v6" />
    </svg>
  )
}

export function CheckCircleIcon(props: IconProps) {
  return (
    <svg {...base} className="h-4 w-4" {...props}>
      <circle cx="12" cy="12" r="9" />
      <path d="M8 12.5l2.5 2.5L16 9.5" />
    </svg>
  )
}

export function AlertTriangleIcon(props: IconProps) {
  return (
    <svg {...base} className="h-4 w-4" {...props}>
      <path d="M12 4L2.5 20h19z" />
      <path d="M12 10v4" />
      <path d="M12 17.5v.01" />
    </svg>
  )
}
