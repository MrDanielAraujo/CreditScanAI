import { useLayoutEffect, useRef, useState } from 'react'
import { getColumnValue, useDataGridState, type DataGridGroup } from './useDataGridState'
import type { DataGridColumn, DataGridProps } from './types'

const DEFAULT_PAGE_SIZE = 20

type RowEntry<T> =
  | { kind: 'loading'; key: string }
  | { kind: 'empty'; key: string }
  | { kind: 'group'; key: string; group: DataGridGroup<T> }
  | { kind: 'data'; key: string; row: T }

function SortIcon({ direction }: { direction: 'asc' | 'desc' | undefined }) {
  if (!direction) return <span className="ml-1 inline-block w-3 shrink-0 text-neutral/40">↕</span>
  return <span className="ml-1 inline-block w-3 shrink-0">{direction === 'asc' ? '↑' : '↓'}</span>
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
  const [scrollbarWidth, setScrollbarWidth] = useState(0)
  const [rowHeights, setRowHeights] = useState<Record<string, number>>({})
  const dragKeyRef = useRef<string | null>(null)
  const resizeStateRef = useRef<{ key: string; startX: number; startWidth: number } | null>(null)
  const headerScrollRef = useRef<HTMLDivElement>(null)
  const bodyScrollRef = useRef<HTMLDivElement>(null)
  const frozenBodyScrollRef = useRef<HTMLDivElement>(null)
  const frozenTableRef = useRef<HTMLTableElement>(null)
  const scrollTableRef = useRef<HTMLTableElement>(null)
  const frozenRowRefs = useRef(new Map<string, HTMLTableRowElement>())
  const scrollRowRefs = useRef(new Map<string, HTMLTableRowElement>())

  const groupableColumns = columns.filter((c) => c.groupable !== false)
  const frozenColumns = grid.visibleColumns.filter((c) => c.frozen)
  const scrollableColumns = grid.visibleColumns.filter((c) => !c.frozen)
  const frozenTotalWidth = frozenColumns.reduce((sum, c) => sum + (grid.columnWidths[c.key] ?? 0), 0)

  // Header and body live in two separate, independently-scrolling tables (matching the
  // Syncfusion Grid's split e-gridheader/e-gridcontent architecture) so the header truly
  // never scrolls with the body - no position:sticky, so no sticky+border rendering glitches.
  useLayoutEffect(() => {
    const body = bodyScrollRef.current
    if (!body) return
    const measure = () => setScrollbarWidth(body.offsetWidth - body.clientWidth)
    measure()
    if (typeof ResizeObserver === 'undefined') return
    const observer = new ResizeObserver(measure)
    observer.observe(body)
    return () => observer.disconnect()
  }, [grid.pagedData, grid.groups])

  // Frozen and scrollable panes render the same rows in two separate tables, so a row whose
  // content is taller in one pane (e.g. wrapping action buttons) must force the other pane's
  // row to match - otherwise the two panes drift out of vertical alignment. A ResizeObserver
  // on both tables catches every case that changes a row's natural height (new data, a column
  // resize, an inline form opening inside a cell) without needing this in a render-time effect.
  useLayoutEffect(() => {
    if (typeof ResizeObserver === 'undefined') return
    const measure = () => {
      setRowHeights((current) => {
        let changed = false
        const next: Record<string, number> = { ...current }
        for (const [key, frozenEl] of frozenRowRefs.current) {
          const scrollEl = scrollRowRefs.current.get(key)
          if (!scrollEl) continue
          const height = Math.ceil(Math.max(frozenEl.getBoundingClientRect().height, scrollEl.getBoundingClientRect().height))
          if (next[key] !== height) {
            next[key] = height
            changed = true
          }
        }
        return changed ? next : current
      })
    }
    measure()
    const observer = new ResizeObserver(measure)
    if (frozenTableRef.current) observer.observe(frozenTableRef.current)
    if (scrollTableRef.current) observer.observe(scrollTableRef.current)
    return () => observer.disconnect()
  }, [])

  const handleBodyScroll = () => {
    if (headerScrollRef.current && bodyScrollRef.current) {
      headerScrollRef.current.scrollLeft = bodyScrollRef.current.scrollLeft
    }
    if (frozenBodyScrollRef.current && bodyScrollRef.current) {
      frozenBodyScrollRef.current.scrollTop = bodyScrollRef.current.scrollTop
    }
  }

  const handleFrozenWheel = (event: React.WheelEvent) => {
    if (!bodyScrollRef.current) return
    event.preventDefault()
    bodyScrollRef.current.scrollTop += event.deltaY
  }

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
    const text = value === null || value === undefined ? '—' : String(value)
    return (
      <span className="block truncate" title={text}>
        {text}
      </span>
    )
  }

  const alignClass = (align: DataGridColumn<T>['align']) =>
    align === 'right' ? 'text-right' : align === 'center' ? 'text-center' : 'text-left'

  const renderColGroup = (cols: DataGridColumn<T>[]) => (
    <colgroup>
      {cols.map((column) => (
        <col key={column.key} style={{ width: grid.columnWidths[column.key] }} />
      ))}
    </colgroup>
  )

  const renderHeaderRows = (cols: DataGridColumn<T>[], draggable: boolean) => (
    <thead>
      <tr className="h-10 text-left">
        {cols.map((column) => (
          <th
            key={column.key}
            draggable={draggable}
            onDragStart={
              draggable
                ? () => {
                    dragKeyRef.current = column.key
                  }
                : undefined
            }
            onDragOver={draggable ? (e) => e.preventDefault() : undefined}
            onDrop={
              draggable
                ? () => {
                    if (dragKeyRef.current) grid.reorderColumn(dragKeyRef.current, column.key)
                    dragKeyRef.current = null
                  }
                : undefined
            }
            className={['relative select-none overflow-hidden p-2 font-medium', alignClass(column.align)].join(' ')}
          >
            <button
              type="button"
              onClick={() => column.sortable !== false && grid.toggleSort(column.key)}
              className="inline-flex w-full min-w-0 items-center hover:text-primary"
              disabled={column.sortable === false}
              title={column.label}
            >
              <span className="truncate">{column.label}</span>
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
      <tr className="h-10">
        {cols.map((column) => (
          <th key={column.key} className="border-t border-neutral/20 bg-surface p-1 font-normal">
            {column.filterable !== false &&
              (column.filterOptions ? (
                <select
                  value={grid.filters[column.key] ?? ''}
                  onChange={(e) => grid.setFilter(column.key, e.target.value)}
                  className="h-7 w-full rounded-md border border-neutral/30 px-2 py-1 text-xs"
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
                  className="h-7 w-full rounded-md border border-neutral/30 px-2 py-1 text-xs"
                />
              ))}
          </th>
        ))}
      </tr>
    </thead>
  )

  // Rows are computed once and rendered into both the frozen and scrollable tables, so the two
  // panes always show the exact same rows, in the exact same order, at the exact same index.
  const rowEntries: RowEntry<T>[] = loading
    ? [{ kind: 'loading', key: '__loading__' }]
    : grid.groups
      ? grid.groups.length === 0
        ? [{ kind: 'empty', key: '__empty__' }]
        : grid.groups.flatMap((group): RowEntry<T>[] => [
            { kind: 'group', key: `__group__${group.key}`, group },
            ...(collapsedGroups.has(group.key)
              ? []
              : group.rows.map((row): RowEntry<T> => ({ kind: 'data', key: grid.rowKey(row), row }))),
          ])
      : grid.pagedData.length === 0
        ? [{ kind: 'empty', key: '__empty__' }]
        : grid.pagedData.map((row): RowEntry<T> => ({ kind: 'data', key: grid.rowKey(row), row }))

  const rowClassName = (entry: RowEntry<T>) => {
    if (entry.kind === 'group') return 'border-b border-neutral/20 bg-neutral/10'
    if (entry.kind === 'data') {
      return [
        'border-b border-neutral/10',
        onRowClick ? 'cursor-pointer' : '',
        isRowSelected?.(entry.row) ? 'bg-primary/10' : 'hover:bg-neutral/10',
      ].join(' ')
    }
    return ''
  }

  const renderRowCells = (entry: RowEntry<T>, cols: DataGridColumn<T>[], pane: 'frozen' | 'scrollable') => {
    const span = Math.max(cols.length, 1)
    if (entry.kind === 'loading') {
      return pane === 'scrollable' ? (
        <td className="p-3 text-neutral" colSpan={span}>
          Carregando...
        </td>
      ) : (
        <td className="p-3" colSpan={span} />
      )
    }
    if (entry.kind === 'empty') {
      return pane === 'scrollable' ? (
        <td className="p-3 text-neutral" colSpan={span}>
          {emptyMessage}
        </td>
      ) : (
        <td className="p-3" colSpan={span} />
      )
    }
    if (entry.kind === 'group') {
      if (pane === 'frozen') return <td className="p-2" colSpan={span} />
      return (
        <td className="p-2 font-medium" colSpan={span}>
          <button type="button" onClick={() => toggleGroup(entry.group.key)} className="inline-flex items-center gap-2">
            <span>{collapsedGroups.has(entry.group.key) ? '▶' : '▼'}</span>
            <span>{entry.group.key}</span>
            <span className="font-normal text-neutral">({entry.group.rows.length})</span>
            {Object.entries(entry.group.aggregates).length > 0 && (
              <span className="font-normal text-neutral">
                {Object.entries(entry.group.aggregates)
                  .map(([key, value]) => `${grid.visibleColumns.find((c) => c.key === key)?.label}: ${value}`)
                  .join(' · ')}
              </span>
            )}
          </button>
        </td>
      )
    }
    return cols.map((column) => (
      <td
        key={column.key}
        onClick={column.preventRowClick ? (e) => e.stopPropagation() : undefined}
        className={['p-2', alignClass(column.align), column.preventRowClick ? 'cursor-default bg-surface-muted' : ''].join(' ')}
      >
        {renderCell(column, entry.row)}
      </td>
    ))
  }

  const renderTbody = (cols: DataGridColumn<T>[], pane: 'frozen' | 'scrollable') => {
    const refMap = pane === 'frozen' ? frozenRowRefs : scrollRowRefs
    return (
      <tbody>
        {rowEntries.map((entry) => (
          <tr
            key={entry.key}
            ref={(el) => {
              if (el) refMap.current.set(entry.key, el)
              else refMap.current.delete(entry.key)
            }}
            onClick={entry.kind === 'data' && onRowClick ? () => onRowClick(entry.row) : undefined}
            className={rowClassName(entry)}
            style={frozenColumns.length > 0 ? { height: rowHeights[entry.key] } : undefined}
          >
            {renderRowCells(entry, cols, pane)}
          </tr>
        ))}
      </tbody>
    )
  }

  const renderTfoot = (cols: DataGridColumn<T>[]) =>
    grid.footerAggregates && (
      <tfoot>
        <tr className="border-t border-neutral/20 bg-surface-muted font-medium">
          {cols.map((column) => (
            <td key={column.key} className={['p-2', alignClass(column.align)].join(' ')}>
              {grid.footerAggregates?.[column.key] ?? ''}
            </td>
          ))}
        </tr>
      </tfoot>
    )

  return (
    <div className="flex h-full min-h-0 flex-col">
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
                className="h-8 rounded-md border border-neutral/30 px-2 py-1 text-sm"
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
            className="h-8 rounded-md border border-neutral/30 px-3 py-1 text-sm hover:bg-neutral/10"
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

      <div className="flex min-h-0 flex-1 flex-col overflow-hidden rounded-md border border-neutral/20">
        <div className="flex border-b border-neutral/20 bg-surface-muted">
          {frozenColumns.length > 0 && (
            <div className="shrink-0 overflow-hidden border-r border-neutral/20" style={{ width: frozenTotalWidth }}>
              <table className="w-full table-fixed border-collapse text-sm">
                {renderColGroup(frozenColumns)}
                {renderHeaderRows(frozenColumns, false)}
              </table>
            </div>
          )}
          <div ref={headerScrollRef} className="min-w-0 flex-1 overflow-hidden">
            <table className="w-full table-fixed border-collapse text-sm">
              {renderColGroup(scrollableColumns)}
              {renderHeaderRows(scrollableColumns, true)}
            </table>
          </div>
          <div className="shrink-0 border-l border-neutral/20" style={{ width: scrollbarWidth }} />
        </div>

        <div className="flex min-h-0 flex-1">
          {frozenColumns.length > 0 && (
            <div
              ref={frozenBodyScrollRef}
              onWheel={handleFrozenWheel}
              className="shrink-0 overflow-hidden border-r border-neutral/20"
              style={{ width: frozenTotalWidth }}
            >
              <table ref={frozenTableRef} className="w-full table-fixed border-collapse text-sm">
                {renderColGroup(frozenColumns)}
                {renderTbody(frozenColumns, 'frozen')}
                {renderTfoot(frozenColumns)}
              </table>
            </div>
          )}
          <div ref={bodyScrollRef} onScroll={handleBodyScroll} className="min-h-0 min-w-0 flex-1 overflow-auto">
            <table ref={scrollTableRef} className="w-full table-fixed border-collapse text-sm">
              {renderColGroup(scrollableColumns)}
              {renderTbody(scrollableColumns, 'scrollable')}
              {renderTfoot(scrollableColumns)}
            </table>
          </div>
        </div>
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
