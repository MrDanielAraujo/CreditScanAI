export type Accent = 'blue' | 'violet' | 'amber' | 'emerald'

export const accentBadgeClasses: Record<Accent, string> = {
  blue: 'bg-blue-50 text-blue-600 dark:bg-blue-500/15 dark:text-blue-300',
  violet: 'bg-violet-50 text-violet-600 dark:bg-violet-500/15 dark:text-violet-300',
  amber: 'bg-amber-50 text-amber-600 dark:bg-amber-500/15 dark:text-amber-300',
  emerald: 'bg-emerald-50 text-emerald-600 dark:bg-emerald-500/15 dark:text-emerald-300',
}

export const accentTopBorderClasses: Record<Accent, string> = {
  blue: 'border-t-blue-500 dark:border-t-blue-400',
  violet: 'border-t-violet-500 dark:border-t-violet-400',
  amber: 'border-t-amber-500 dark:border-t-amber-400',
  emerald: 'border-t-emerald-500 dark:border-t-emerald-400',
}
