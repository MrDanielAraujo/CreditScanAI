import { useMemo } from 'react'
import type { DocumentResultResponse, PeriodDto, SourceAccountDto } from '../../types/documents'

interface ResultViewProps {
  result: DocumentResultResponse
}

interface TreeNode extends SourceAccountDto {
  children: TreeNode[]
}

function buildOrderedRows(accounts: SourceAccountDto[]): TreeNode[] {
  const byId = new Map<string, TreeNode>(accounts.map((a) => [a.id, { ...a, children: [] }]))
  const roots: TreeNode[] = []

  for (const node of byId.values()) {
    if (node.parentId && byId.has(node.parentId)) {
      byId.get(node.parentId)!.children.push(node)
    } else {
      roots.push(node)
    }
  }

  const ordered: TreeNode[] = []
  const visit = (node: TreeNode) => {
    ordered.push(node)
    node.children.forEach(visit)
  }
  roots.forEach(visit)

  return ordered
}

function periodLabel(period: PeriodDto): string {
  return period.periodType === 'Annual' ? `${period.year} (Anual)` : `${period.year} T${period.quarter}`
}

function formatValue(value: number | null): string {
  if (value === null) return '-'
  return value.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

export function ResultView({ result }: ResultViewProps) {
  const orderedRows = useMemo(() => buildOrderedRows(result.accounts), [result.accounts])

  const periodColumns = useMemo(
    () => [...result.periods].sort((a, b) => a.endDate.localeCompare(b.endDate)),
    [result.periods],
  )

  const rawColumnLabels = useMemo(() => {
    const labels = new Set<string>()
    for (const account of result.accounts) {
      for (const value of account.values) {
        if (value.rawColumnLabel) labels.add(value.rawColumnLabel)
      }
    }
    return Array.from(labels).sort()
  }, [result.accounts])

  return (
    <div className="overflow-x-auto rounded-lg border border-neutral/20">
      <table className="w-full border-collapse text-sm">
        <thead>
          <tr className="border-b border-neutral/20 bg-surface-muted text-left">
            <th className="p-3 font-medium">Conta</th>
            {periodColumns.map((period) => (
              <th key={period.id} className="p-3 text-right font-medium">
                {periodLabel(period)}
              </th>
            ))}
            {rawColumnLabels.map((label) => (
              <th key={label} className="p-3 text-right font-medium text-neutral">
                {label}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {orderedRows.map((row) => (
            <tr key={row.id} className="border-b border-neutral/10 last:border-0">
              <td className="p-3" style={{ paddingLeft: `${12 + row.hierarchyLevel * 20}px` }}>
                <span className={row.hierarchyLevel <= 1 ? 'font-semibold' : ''}>{row.originalName}</span>
                {row.inferredType && (
                  <span className="ml-2 text-xs text-neutral">
                    {row.inferredType}
                    {row.inferredSubtype ? ` / ${row.inferredSubtype}` : ''}
                  </span>
                )}
              </td>
              {periodColumns.map((period) => {
                const value = row.values.find((v) => v.periodId === period.id)
                return (
                  <td key={period.id} className="p-3 text-right tabular-nums">
                    {formatValue(value?.rawValue ?? null)}
                  </td>
                )
              })}
              {rawColumnLabels.map((label) => {
                const value = row.values.find((v) => v.rawColumnLabel === label)
                return (
                  <td key={label} className="p-3 text-right tabular-nums text-neutral">
                    {formatValue(value?.rawValue ?? null)}
                  </td>
                )
              })}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
