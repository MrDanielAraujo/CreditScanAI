import type { ComponentType, SVGProps } from 'react'
import { accentBadgeClasses, accentTopBorderClasses, type Accent } from '../common/accentColors'
import { formatFinancialValue, type ValueDefinition } from './financialValueDefinitions'

interface FinancialValueGridProps {
  title: string
  values: Record<string, number>
  definitions: ValueDefinition[]
  icon: ComponentType<SVGProps<SVGSVGElement>>
  accent: Accent
}

export function FinancialValueGrid({ title, values, definitions, icon: Icon, accent }: FinancialValueGridProps) {
  return (
    <div className="mt-8">
      <div className="mb-3 flex items-center gap-2">
        <span className={['flex h-8 w-8 shrink-0 items-center justify-center rounded-lg', accentBadgeClasses[accent]].join(' ')}>
          <Icon className="h-[18px] w-[18px]" />
        </span>
        <h2 className="text-sm font-semibold uppercase tracking-wide text-neutral">{title}</h2>
      </div>
      <div className="grid min-w-0 grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
        {definitions.map((v) => (
          <div
            key={v.key}
            className={[
              'min-w-0 rounded-lg border border-t-4 border-neutral/15 bg-surface p-4 shadow-sm transition-shadow hover:shadow-md',
              accentTopBorderClasses[accent],
            ].join(' ')}
          >
            <p className="truncate text-xs font-medium uppercase tracking-wide text-neutral" title={v.label}>
              {v.label}
            </p>
            <p className="mt-1.5 break-words text-xl font-bold tabular-nums text-surface-dark">
              {formatFinancialValue(values[v.key], v.format)}
            </p>
          </div>
        ))}
      </div>
    </div>
  )
}
