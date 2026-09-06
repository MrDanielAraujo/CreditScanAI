import { Fragment, useRef, useState } from 'react'
import { getColumnValue, useDataGridState } from './useDataGridState'
import type { DataGridColumn, DataGridProps } from './types'

const DEFAULT_PAGE_SIZE = 20

function SortIcon({ direction }: { direction: 'asc' | 'desc' | undefined }) {
  if (!direction) return <span className="ml-1 inline-block w-3 text-neutral/40">↕</span>
  return <span className="ml-1 inline-block w-3">{direction === 'asc' ? '↑' : '↓'}</span>
}

export function DataGrid<T>({
  columns,
  data,
  rowKey,
  pageSize = DEFAULT_PAGE_SIZE,
  onRowClick,
  isRowSelected,
  emptyMessage = 'Nenhum registro encontrado.',
  loading = false,
}: DataGridProps<T>) {
  const grid = useDataGridState(columns, data, rowKey, pageSize)
  const [collapsedGroups, setCollapsedGroups] = useState<Set<string>>(new Set())
  const [columnChooserOpen, setColumnChooserOpen] = useState(false)
  const dragKeyRef = useRef<string | null>(null)
  const resizeStateRef = useRef<{ key: string; startX: number; startWidth: number } | null>(null)

  const groupableColumns = columns.filter((c) => c.groupable !== false)

  const toggleGroup = (key: string) => {
    setCollapsedGroups((current) => {
      const next = new Set(current)
      if (next.has(key)) next.delete(key)
      else next.add(key)
      return next
    })
  }

  const handleResizeStart = (key: string, event: React.PointerEvent) => {
    event.preventDefault()
    event.stopPropagation()
    const th = (event.target as HTMLElement).closest('th')
    const startWidth = grid.columnWidths[key] ?? th?.offsetWidth ?? 150
    resizeStateRef.current = { key, startX: event.clientX, startWidth }

    const handleMove = (moveEvent: PointerEvent) => {
      if (!resizeStateRef.current) return
      const delta = moveEvent.clientX - resizeStateRef.current.startX
      grid.resizeColumn(resizeStateRef.current.key, resizeStateRef.current.startWidth + delta)
    }
    const handleUp = () => {
      resizeStateRef.current = null
      window.removeEventListener('pointermove', handleMove)
      window.removeEventListener('pointerup', handleUp)
    }
    window.addEventListener('pointermove', handleMove)
    window.addEventListener('pointerup', handleUp)
  }

  const renderCell = (column: DataGridColumn<T>, row: T) => {
    if (column.render) return column.render(row)
    const value = getColumnValue(column, row)
    return value === null || value === undefined ? '—' : String(value)
  }

  const alignClass = (align: DataGridColumn<T>['align']) =>
    align === 'right' ? 'text-right' : align === 'center' ? 'text-center' : 'text-left'

  const colSpan = grid.visibleColumns.length

  return (
    <div>
      <div className="mb-2 flex flex-wrap items-center justify-between gap-2">
        <div className="flex items-center gap-2 text-sm">
          {groupableColumns.length > 0 && (
            <>
              <label htmlFor="datagrid-group-by" className="text-xs font-semibold uppercase text-neutral">
                Agrupar por
              </label>
              <select
                id="datagrid-group-by"
                aria-label="Agrupar por"
                value={grid.groupByKey ?? ''}
                onChange={(e) => grid.setGroupByKey(e.target.value || null)}
                className="rounded-md border border-neutral/30 px-2 py-1 text-sm"
              >
                <option value="">Nenhum</option>
                {groupableColumns.map((c) => (
                  <option key={c.key} value={c.key}>
                    {c.label}
                  </option>
                ))}
              </select>
            </>
          )}
        </div>

        <div className="relative">
          <button
            type="button"
            onClick={() => setColumnChooserOpen((v) => !v)}
            className="rounded-md border border-neutral/30 px-3 py-1 text-sm hover:bg-neutral/10"
          >
            Colunas
          </button>
          {columnChooserOpen && (
            <div className="absolute right-0 z-10 mt-1 w-56 rounded-md border border-neutral/20 bg-surface p-2 shadow-md">
              {grid.orderedColumns.map((c) => (
                <label key={c.key} className="flex items-center gap-2 rounded px-2 py-1 text-sm hover:bg-neutral/10">
                  <input
                    type="checkbox"
                    checked={!grid.hiddenKeys.has(c.key)}
                    onChange={() => grid.toggleColumnVisibility(c.key)}
                  />
                  {c.label}
                </label>
              ))}
            </div>
          )}
        </div>
      </div>

      <div className="overflow-x-auto rounded-md border border-neutral/20">
        <table className="w-full border-collapse text-sm">
          <thead>
            <tr className="border-b border-neutral/20 bg-surface-muted text-left">
              {grid.visibleColumns.map((column) => (
                <th
                  key={column.key}
                  style={{ width: grid.columnWidths[column.key] }}
                  draggable
                  onDragStart={() => {
                    dragKeyRef.current = column.key
                  }}
                  onDragOver={(e) => e.preventDefault()}
                  onDrop={() => {
                    if (dragKeyRef.current) grid.reorderColumn(dragKeyRef.current, column.key)
                    dragKeyRef.current = null
                  }}
                  className={['relative select-none p-2 font-medium', alignClass(column.align)].join(' ')}
                >
                  <button
                    type="button"
                    onClick={() => column.sortable !== false && grid.toggleSort(column.key)}
                    className="inline-flex items-center hover:text-primary"
                    disabled={column.sortable === false}
                  >
                    {column.label}
                    {column.sortable !== false && (
                      <SortIcon direction={grid.sort?.key === column.key ? grid.sort.direction : undefined} />
                    )}
                  </button>
                  <span
                    onPointerDown={(e) => handleResizeStart(column.key, e)}
                    className="absolute right-0 top-0 h-full w-1 cursor-col-resize hover:bg-primary/40"
                  />
                </th>
              ))}
            </tr>
            <tr className="border-b border-neutral/20">
              {grid.visibleColumns.map((column) => (
                <th key={column.key} className="p-1 font-normal">
                  {column.filterable !== false &&
                    (column.filterOptions ? (
                      <select
                        value={grid.filters[column.key] ?? ''}
                        onChange={(e) => grid.setFilter(column.key, e.target.value)}
                        className="w-full rounded-md border border-neutral/30 px-2 py-1 text-xs"
                      >
                        <option value="">Todos</option>
                        {column.filterOptions.map((option) => (
                          <option key={option} value={option}>
                            {option}
                          </option>
                        ))}
                      </select>
                    ) : (
                      <input
                        type="text"
                        placeholder="Filtrar..."
                        value={grid.filters[column.key] ?? ''}
                        onChange={(e) => grid.setFilter(column.key, e.target.value)}
                        className="w-full rounded-md border border-neutral/30 px-2 py-1 text-xs"
                      />
                    ))}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {loading && (
              <tr>
                <td className="p-3 text-neutral" colSpan={colSpan}>
                  Carregando...
                </td>
              </tr>
            )}

            {!loading && grid.groups && grid.groups.length === 0 && (
              <tr>
                <td className="p-3 text-neutral" colSpan={colSpan}>
                  {emptyMessage}
                </td>
              </tr>
            )}

            {!loading &&
              grid.groups &&
              grid.groups.map((group) => {
                const isCollapsed = collapsedGroups.has(group.key)
                return (
                  <Fragment key={group.key}>
                    <tr className="border-b border-neutral/20 bg-neutral/10">
                      <td className="p-2 font-medium" colSpan={colSpan}>
                        <button type="button" onClick={() => toggleGroup(group.key)} className="inline-flex items-center gap-2">
                          <span>{isCollapsed ? '▶' : '▼'}</span>
                          <span>{group.key}</span>
                          <span className="font-normal text-neutral">({group.rows.length})</span>
                          {Object.entries(group.aggregates).length > 0 && (
                            <span className="font-normal text-neutral">
                              {Object.entries(group.aggregates)
                                .map(([key, value]) => `${grid.visibleColumns.find((c) => c.key === key)?.label}: ${value}`)
                                .join(' · ')}
                            </span>
                          )}
                        </button>
                      </td>
                    </tr>
                    {!isCollapsed &&
                      group.rows.map((row) => (
                        <tr
                          key={grid.rowKey(row)}
                          onClick={() => onRowClick?.(row)}
                          className={[
                            'border-b border-neutral/10',
                            onRowClick ? 'cursor-pointer' : '',
                            isRowSelected?.(row) ? 'bg-primary/10' : 'hover:bg-neutral/10',
                          ].join(' ')}
                        >
                          {grid.visibleColumns.map((column) => (
                            <td key={column.key} className={['p-2', alignClass(column.align)].join(' ')}>
                              {renderCell(column, row)}
                            </td>
                          ))}
                        </tr>
                      ))}
                  </Fragment>
                )
              })}

            {!loading && !grid.groups && grid.pagedData.length === 0 && (
              <tr>
                <td className="p-3 text-neutral" colSpan={colSpan}>
                  {emptyMessage}
                </td>
              </tr>
            )}

            {!loading &&
              !grid.groups &&
              grid.pagedData.map((row) => (
                <tr
                  key={grid.rowKey(row)}
                  onClick={() => onRowClick?.(row)}
                  className={[
                    'border-b border-neutral/10',
                    onRowClick ? 'cursor-pointer' : '',
                    isRowSelected?.(row) ? 'bg-primary/10' : 'hover:bg-neutral/10',
                  ].join(' ')}
                >
                  {grid.visibleColumns.map((column) => (
                    <td key={column.key} className={['p-2', alignClass(column.align)].join(' ')}>
                      {renderCell(column, row)}
                    </td>
                  ))}
                </tr>
              ))}
          </tbody>
          {grid.footerAggregates && (
            <tfoot>
              <tr className="border-t border-neutral/20 bg-surface-muted font-medium">
                {grid.visibleColumns.map((column) => (
                  <td key={column.key} className={['p-2', alignClass(column.align)].join(' ')}>
                    {grid.footerAggregates?.[column.key] ?? ''}
                  </td>
                ))}
              </tr>
            </tfoot>
          )}
        </table>
      </div>

      {!grid.groups && (
        <div className="mt-2 flex items-center justify-between text-sm text-neutral">
          <span>{grid.totalCount} registro(s)</span>
          <div className="flex items-center gap-2">
            <button
              type="button"
              disabled={grid.page <= 1}
              onClick={() => grid.setPage(grid.page - 1)}
              className="rounded-md border border-neutral/30 px-2 py-1 disabled:cursor-not-allowed disabled:opacity-50"
            >
              Anterior
            </button>
            <span>
              Página {grid.page} de {grid.totalPages}
            </span>
            <button
              type="button"
              disabled={grid.page >= grid.totalPages}
              onClick={() => grid.setPage(grid.page + 1)}
              className="rounded-md border border-neutral/30 px-2 py-1 disabled:cursor-not-allowed disabled:opacity-50"
            >
              Próxima
            </button>
          </div>
        </div>
      )}
    </div>
  )
}
