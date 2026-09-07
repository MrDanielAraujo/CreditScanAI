import type { ReactNode } from 'react'

export type SortDirection = 'asc' | 'desc'
export type AggregateType = 'sum' | 'avg' | 'count'

export interface DataGridColumn<T> {
  /** Chave estável da coluna - usada para ordenar, filtrar, agrupar, reordenar e mostrar/ocultar. */
  key: string
  label: string
  /** Como renderizar a célula. Sem isso, usa String(getValue(row)). */
  render?: (row: T) => ReactNode
  /** Valor "cru" da coluna para esta linha - usado por ordenação, filtro e agregados. Padrão: (row as any)[key]. */
  getValue?: (row: T) => string | number | null | undefined
  /** Lista fixa de opções para filtrar esta coluna com um <select> em vez de texto livre. */
  filterOptions?: string[]
  width?: number
  sortable?: boolean
  filterable?: boolean
  groupable?: boolean
  aggregate?: AggregateType
  align?: 'left' | 'right' | 'center'
  /** Congela a coluna num painel à esquerda que não rola horizontalmente. Não é arrastável/reordenável. */
  frozen?: boolean
  /**
   * Para colunas de ação: a célula não reage ao hover/clique da linha (só o
   * conteúdo que ela renderiza - normalmente um ícone - responde a isso).
   */
  preventRowClick?: boolean
}

export interface DataGridProps<T> {
  columns: DataGridColumn<T>[]
  data: T[]
  rowKey: (row: T) => string
  pageSize?: number
  onRowClick?: (row: T) => void
  isRowSelected?: (row: T) => boolean
  emptyMessage?: string
  loading?: boolean
}
