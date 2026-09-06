import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { DataGrid } from './DataGrid'
import type { DataGridColumn } from './types'

interface Row {
  id: string
  name: string
  category: string
  amount: number
}

const rows: Row[] = [
  { id: '1', name: 'Banana', category: 'Fruta', amount: 10 },
  { id: '2', name: 'Maçã', category: 'Fruta', amount: 20 },
  { id: '3', name: 'Cenoura', category: 'Legume', amount: 5 },
]

const columns: DataGridColumn<Row>[] = [
  { key: 'name', label: 'Nome' },
  { key: 'category', label: 'Categoria', filterOptions: ['Fruta', 'Legume'] },
  { key: 'amount', label: 'Quantidade', align: 'right', aggregate: 'sum' },
]

describe('DataGrid', () => {
  it('renders all rows and columns', () => {
    render(<DataGrid columns={columns} data={rows} rowKey={(r) => r.id} />)

    expect(screen.getByText('Banana')).toBeInTheDocument()
    expect(screen.getByText('Maçã')).toBeInTheDocument()
    expect(screen.getByText('Cenoura')).toBeInTheDocument()
  })

  it('shows the empty message when there is no data', () => {
    render(<DataGrid columns={columns} data={[]} rowKey={(r) => r.id} emptyMessage="Sem dados" />)

    expect(screen.getByText('Sem dados')).toBeInTheDocument()
  })

  it('sorts rows when clicking a column header', async () => {
    render(<DataGrid columns={columns} data={rows} rowKey={(r) => r.id} />)

    await userEvent.click(screen.getByRole('button', { name: /Nome/ }))

    const bodyRows = screen.getAllByRole('row').slice(2) // pula cabeçalho + linha de filtro
    expect(within(bodyRows[0]).getByText('Banana')).toBeInTheDocument()
    expect(within(bodyRows[1]).getByText('Cenoura')).toBeInTheDocument()
    expect(within(bodyRows[2]).getByText('Maçã')).toBeInTheDocument()
  })

  it('filters rows using the text filter input', async () => {
    render(<DataGrid columns={columns} data={rows} rowKey={(r) => r.id} />)

    const filterInput = screen.getAllByPlaceholderText('Filtrar...')[0]
    await userEvent.type(filterInput, 'ban')

    expect(screen.getByText('Banana')).toBeInTheDocument()
    expect(screen.queryByText('Maçã')).not.toBeInTheDocument()
    expect(screen.queryByText('Cenoura')).not.toBeInTheDocument()
  })

  it('filters rows using a select-based filter for columns with filterOptions', async () => {
    render(<DataGrid columns={columns} data={rows} rowKey={(r) => r.id} />)

    const selects = screen.getAllByRole('combobox')
    const categoryFilter = selects[selects.length - 1]
    await userEvent.selectOptions(categoryFilter, 'Legume')

    expect(screen.getByText('Cenoura')).toBeInTheDocument()
    expect(screen.queryByText('Banana')).not.toBeInTheDocument()
  })

  it('paginates rows according to pageSize', () => {
    render(<DataGrid columns={columns} data={rows} rowKey={(r) => r.id} pageSize={2} />)

    expect(screen.getByText('Banana')).toBeInTheDocument()
    expect(screen.getByText('Maçã')).toBeInTheDocument()
    expect(screen.queryByText('Cenoura')).not.toBeInTheDocument()
    expect(screen.getByText('Página 1 de 2')).toBeInTheDocument()
  })

  it('groups rows and shows per-group counts and aggregates', async () => {
    render(<DataGrid columns={columns} data={rows} rowKey={(r) => r.id} />)

    const groupBySelect = screen.getByRole('combobox', { name: /agrupar por/i })
    await userEvent.selectOptions(groupBySelect, 'category')

    expect(screen.getByRole('button', { name: /Fruta.*2/ })).toBeInTheDocument()
  })

  it('calls onRowClick with the clicked row', async () => {
    const handleRowClick = vi.fn()
    render(<DataGrid columns={columns} data={rows} rowKey={(r) => r.id} onRowClick={handleRowClick} />)

    await userEvent.click(screen.getByText('Banana'))

    expect(handleRowClick).toHaveBeenCalledWith(rows[0])
  })

  it('renders a frozen column for every row, independent from row clicks', async () => {
    const handleRowClick = vi.fn()
    const handleDelete = vi.fn()
    const columnsWithFrozenActions: DataGridColumn<Row>[] = [
      ...columns,
      {
        key: 'actions',
        label: '',
        frozen: true,
        sortable: false,
        filterable: false,
        groupable: false,
        render: (row) => (
          <button
            onClick={(e) => {
              e.stopPropagation()
              handleDelete(row.id)
            }}
          >
            Excluir
          </button>
        ),
      },
    ]

    render(<DataGrid columns={columnsWithFrozenActions} data={rows} rowKey={(r) => r.id} onRowClick={handleRowClick} />)

    const deleteButtons = screen.getAllByRole('button', { name: 'Excluir' })
    expect(deleteButtons).toHaveLength(rows.length)

    await userEvent.click(deleteButtons[0])

    expect(handleDelete).toHaveBeenCalledWith(rows[0].id)
    expect(handleRowClick).not.toHaveBeenCalled()
  })
})
