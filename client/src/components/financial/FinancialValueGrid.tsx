import { formatFinancialValue, type ValueDefinition } from './financialValueDefinitions'

export function FinancialValueGrid({ title, values, definitions }: { title: string; values: Record<string, number>; definitions: ValueDefinition[] }) {
  return (
    <div className="mt-6">
      <h2 className="text-sm font-semibold uppercase text-neutral">{title}</h2>
      <div className="mt-2 grid grid-cols-2 gap-3 md:grid-cols-4">
        {definitions.map((v) => (
          <div key={v.key} className="rounded-md border border-neutral/20 p-3">
            <p className="text-xs text-neutral">{v.label}</p>
            <p className="mt-1 text-lg font-semibold">{formatFinancialValue(values[v.key], v.format)}</p>
          </div>
        ))}
      </div>
    </div>
  )
}
