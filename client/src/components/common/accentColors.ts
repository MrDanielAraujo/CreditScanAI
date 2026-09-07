export type Accent = 'blue' | 'violet' | 'amber' | 'emerald'

export const accentBadgeClasses: Record<Accent, string> = {
  blue: 'bg-blue-50 text-blue-600',
  violet: 'bg-violet-50 text-violet-600',
  amber: 'bg-amber-50 text-amber-600',
  emerald: 'bg-emerald-50 text-emerald-600',
}

export const accentTopBorderClasses: Record<Accent, string> = {
  blue: 'border-t-blue-500',
  violet: 'border-t-violet-500',
  amber: 'border-t-amber-500',
  emerald: 'border-t-emerald-500',
}
