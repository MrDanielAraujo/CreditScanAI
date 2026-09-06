import { useMemo, useState } from 'react'
import type { AggregateType, DataGridColumn, SortDirection } from './types'

function defaultGetValue<T>(row: T, key: string): string | number | null | undefined {
  return (row as Record<string, unknown>)[key] as string | number | null | undefined
}

export function getColumnValue<T>(column: DataGridColumn<T>, row: T): string | number | null | undefined {
  return column.getValue ? column.getValue(row) : defaultGetValue(row, column.key)
}

function compareValues(a: string | number | null | undefined, b: string | number | null | undefined): number {
  if (a === b) return 0
  if (a === null || a === undefined) return -1
  if (b === null || b === undefined) return 1
  if (typeof a === 'number' && typeof b === 'number') return a - b
  return String(a).localeCompare(String(b), 'pt-BR', { sensitivity: 'base' })
}

function computeAggregate(values: (string | number | null | undefined)[], type: AggregateType): string {
  const numeric = values.map((v) => (typeof v === 'number' ? v : Number(v))).filter((v) => !Number.isNaN(v))
  if (type === 'count') return String(values.length)
  if (numeric.length === 0) return '—'
  const sum = numeric.reduce((acc, v) => acc + v, 0)
  if (type === 'sum') return sum.toLocaleString('pt-BR', { maximumFractionDigits: 2 })
  return (sum / numeric.length).toLocaleString('pt-BR', { maximumFractionDigits: 2 })
}

export interface DataGridGroup<T> {
  key: string
  rows: T[]
  aggregates: Record<string, string>
}

export const DEFAULT_COLUMN_WIDTH = 160

export function useDataGridState<T>(columns: DataGridColumn<T>[], data: T[], rowKey: (row: T) => string, pageSize: number) {
  const [sort, setSort] = useState<{ key: string; direction: SortDirection } | null>(null)
  const [filters, setFilters] = useState<Record<string, string>>({})
  const [page, setPage] = useState(1)
  const [columnOrder, setColumnOrder] = useState<string[]>(() => columns.map((c) => c.key))
  const [columnWidths, setColumnWidths] = useState<Record<string, number>>(() =>
    Object.fromEntries(columns.map((c) => [c.key, c.width ?? DEFAULT_COLUMN_WIDTH])),
  )
  const [hiddenKeys, setHiddenKeys] = useState<Set<string>>(new Set())
  const [groupByKey, setGroupByKey] = useState<string | null>(null)

  const columnsByKey = useMemo(() => new Map(columns.map((c) => [c.key, c])), [columns])

  const orderedColumns = useMemo(
    () => columnOrder.map((key) => columnsByKey.get(key)).filter((c): c is DataGridColumn<T> => c !== undefined),
    [columnOrder, columnsByKey],
  )
  const visibleColumns = useMemo(() => orderedColumns.filter((c) => !hiddenKeys.has(c.key)), [orderedColumns, hiddenKeys])

  const filteredData = useMemo(() => {
    const activeFilters = Object.entries(filters).filter(([, value]) => value !== '')
    if (activeFilters.length === 0) return data

    return data.filter((row) =>
      activeFilters.every(([key, filterValue]) => {
        const column = columnsByKey.get(key)
        if (!column) return true
        const value = getColumnValue(column, row)
        return String(value ?? '').toLowerCase().includes(filterValue.toLowerCase())
      }),
    )
  }, [data, filters, columnsByKey])

  const sortedData = useMemo(() => {
    if (!sort) return filteredData
    const column = columnsByKey.get(sort.key)
    if (!column) return filteredData

    const copy = [...filteredData]
    copy.sort((a, b) => {
      const result = compareValues(getColumnValue(column, a), getColumnValue(column, b))
      return sort.direction === 'asc' ? result : -result
    })
    return copy
  }, [filteredData, sort, columnsByKey])

  const groupColumn = groupByKey ? columnsByKey.get(groupByKey) : undefined

  const groups = useMemo((): DataGridGroup<T>[] | null => {
    if (!groupColumn) return null

    const map = new Map<string, T[]>()
    for (const row of sortedData) {
      const key = String(getColumnValue(groupColumn, row) ?? '—')
      const bucket = map.get(key)
      if (bucket) bucket.push(row)
      else map.set(key, [row])
    }

    return Array.from(map.entries()).map(([key, rows]) => ({
      key,
      rows,
      aggregates: Object.fromEntries(
        visibleColumns
          .filter((c) => c.aggregate)
          .map((c) => [c.key, computeAggregate(rows.map((r) => getColumnValue(c, r)), c.aggregate!)]),
      ),
    }))
  }, [sortedData, groupColumn, visibleColumns])

  const totalPages = groups ? 1 : Math.max(1, Math.ceil(sortedData.length / pageSize))
  const clampedPage = Math.min(page, totalPages)

  const pagedData = useMemo(() => {
    if (groups) return sortedData
    const start = (clampedPage - 1) * pageSize
    return sortedData.slice(start, start + pageSize)
  }, [groups, sortedData, clampedPage, pageSize])

  const footerAggregates = useMemo(() => {
    if (groups) return null
    const aggregateColumns = visibleColumns.filter((c) => c.aggregate)
    if (aggregateColumns.length === 0) return null
    return Object.fromEntries(
      aggregateColumns.map((c) => [c.key, computeAggregate(filteredData.map((r) => getColumnValue(c, r)), c.aggregate!)]),
    )
  }, [groups, visibleColumns, filteredData])

  const toggleSort = (key: string) => {
    setPage(1)
    setSort((current) => {
      if (!current || current.key !== key) return { key, direction: 'asc' }
      if (current.direction === 'asc') return { key, direction: 'desc' }
      return null
    })
  }

  const setFilter = (key: string, value: string) => {
    setPage(1)
    setFilters((current) => ({ ...current, [key]: value }))
  }

  const toggleColumnVisibility = (key: string) => {
    setHiddenKeys((current) => {
      const next = new Set(current)
      if (next.has(key)) next.delete(key)
      else next.add(key)
      return next
    })
  }

  const resizeColumn = (key: string, width: number) => {
    setColumnWidths((current) => ({ ...current, [key]: Math.max(60, width) }))
  }

  const reorderColumn = (draggedKey: string, targetKey: string) => {
    if (draggedKey === targetKey) return
    setColumnOrder((current) => {
      const next = current.filter((k) => k !== draggedKey)
      const targetIndex = next.indexOf(targetKey)
      next.splice(targetIndex, 0, draggedKey)
      return next
    })
  }

  return {
    sort,
    filters,
    page: clampedPage,
    totalPages,
    columnWidths,
    hiddenKeys,
    groupByKey,
    orderedColumns,
    visibleColumns,
    sortedData,
    pagedData,
    groups,
    footerAggregates,
    totalCount: filteredData.length,
    toggleSort,
    setFilter,
    setPage,
    toggleColumnVisibility,
    resizeColumn,
    reorderColumn,
    setGroupByKey,
    rowKey,
  }
}
