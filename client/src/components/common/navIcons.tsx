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

export function HomeIcon(props: IconProps) {
  return (
    <svg {...base} className="h-[18px] w-[18px]" {...props}>
      <path d="M4 11l8-6 8 6" />
      <path d="M6 10v9a1 1 0 0 0 1 1h3v-6h4v6h3a1 1 0 0 0 1-1v-9" />
    </svg>
  )
}

export function UploadIcon(props: IconProps) {
  return (
    <svg {...base} className="h-[18px] w-[18px]" {...props}>
      <path d="M12 15V4" />
      <path d="M8 8l4-4 4 4" />
      <path d="M4 15v3a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-3" />
    </svg>
  )
}

export function FileIcon(props: IconProps) {
  return (
    <svg {...base} className="h-[18px] w-[18px]" {...props}>
      <path d="M7 3h7l4 4v13a1 1 0 0 1-1 1H7a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1z" />
      <path d="M14 3v4h4" />
    </svg>
  )
}

export function CheckSquareIcon(props: IconProps) {
  return (
    <svg {...base} className="h-[18px] w-[18px]" {...props}>
      <rect x="4" y="4" width="16" height="16" rx="2" />
      <path d="M8 12l2.5 2.5L16 9" />
    </svg>
  )
}

export function LayersIcon(props: IconProps) {
  return (
    <svg {...base} className="h-[18px] w-[18px]" {...props}>
      <path d="M12 4l8 4-8 4-8-4z" />
      <path d="M4 12l8 4 8-4" />
      <path d="M4 16l8 4 8-4" />
    </svg>
  )
}

export function LightbulbIcon(props: IconProps) {
  return (
    <svg {...base} className="h-[18px] w-[18px]" {...props}>
      <path d="M9 18h6" />
      <path d="M10 21h4" />
      <path d="M7 10a5 5 0 1 1 10 0c0 2-1.5 3-2.5 4.5-.4.6-.5 1-.5 1.5H10c0-.5-.1-.9-.5-1.5C8.5 13 7 12 7 10z" />
    </svg>
  )
}

export function ShieldIcon(props: IconProps) {
  return (
    <svg {...base} className="h-[18px] w-[18px]" {...props}>
      <path d="M12 3l7 3v6c0 4.5-3 7.5-7 9-4-1.5-7-4.5-7-9V6z" />
      <path d="M9.5 12l1.8 1.8L14.5 10" />
    </svg>
  )
}

export function UsersIcon(props: IconProps) {
  return (
    <svg {...base} className="h-[18px] w-[18px]" {...props}>
      <circle cx="9" cy="8" r="3" />
      <path d="M3 20c0-3 2.7-5 6-5s6 2 6 5" />
      <path d="M16 4.5a3 3 0 0 1 0 6" />
      <path d="M15 15c2.8.3 6 1.9 6 5" />
    </svg>
  )
}

export function FolderIcon(props: IconProps) {
  return (
    <svg {...base} className="h-[18px] w-[18px]" {...props}>
      <path d="M4 6a1 1 0 0 1 1-1h4l2 2h8a1 1 0 0 1 1 1v10a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1z" />
    </svg>
  )
}

export function ChevronDownIcon(props: IconProps) {
  return (
    <svg {...base} className="h-4 w-4" {...props}>
      <path d="M6 9l6 6 6-6" />
    </svg>
  )
}

export function BuildingIcon(props: IconProps) {
  return (
    <svg {...base} className="h-[18px] w-[18px]" {...props}>
      <rect x="5" y="3" width="10" height="18" />
      <path d="M15 8h4v13h-4" />
      <path d="M8 7h1M11 7h1M8 11h1M11 11h1M8 15h1M11 15h1" />
    </svg>
  )
}

export function BookIcon(props: IconProps) {
  return (
    <svg {...base} className="h-[18px] w-[18px]" {...props}>
      <path d="M5 4h11a2 2 0 0 1 2 2v14H7a2 2 0 0 1-2-2z" />
      <path d="M5 18a2 2 0 0 1 2-2h11" />
    </svg>
  )
}

export function TagIcon(props: IconProps) {
  return (
    <svg {...base} className="h-[18px] w-[18px]" {...props}>
      <path d="M12 3h6a1 1 0 0 1 1 1v6l-9 9-7-7z" />
      <circle cx="16.5" cy="7.5" r="1.25" />
    </svg>
  )
}

export function GridIcon(props: IconProps) {
  return (
    <svg {...base} className="h-[18px] w-[18px]" {...props}>
      <rect x="4" y="4" width="7" height="7" rx="1" />
      <rect x="13" y="4" width="7" height="7" rx="1" />
      <rect x="4" y="13" width="7" height="7" rx="1" />
      <rect x="13" y="13" width="7" height="7" rx="1" />
    </svg>
  )
}

export function BookmarkIcon(props: IconProps) {
  return (
    <svg {...base} className="h-[18px] w-[18px]" {...props}>
      <path d="M7 3h10a1 1 0 0 1 1 1v16l-6-4-6 4V4a1 1 0 0 1 1-1z" />
    </svg>
  )
}
