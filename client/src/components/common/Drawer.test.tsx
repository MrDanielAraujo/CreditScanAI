import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { Drawer } from './Drawer'

describe('Drawer', () => {
  it('renders nothing when closed', () => {
    render(
      <Drawer open={false} onClose={vi.fn()} title="Teste">
        <p>Conteúdo</p>
      </Drawer>,
    )
    expect(screen.queryByText('Conteúdo')).not.toBeInTheDocument()
  })

  it('renders title and children when open', () => {
    render(
      <Drawer open onClose={vi.fn()} title="Teste">
        <p>Conteúdo</p>
      </Drawer>,
    )
    expect(screen.getByText('Teste')).toBeInTheDocument()
    expect(screen.getByText('Conteúdo')).toBeInTheDocument()
  })

  it('calls onClose when the close button is clicked', async () => {
    const handleClose = vi.fn()
    render(
      <Drawer open onClose={handleClose} title="Teste">
        <p>Conteúdo</p>
      </Drawer>,
    )
    await userEvent.click(screen.getByLabelText('Fechar'))
    expect(handleClose).toHaveBeenCalled()
  })

  it('calls onClose when the backdrop is clicked', async () => {
    const handleClose = vi.fn()
    const { container } = render(
      <Drawer open onClose={handleClose} title="Teste">
        <p>Conteúdo</p>
      </Drawer>,
    )
    const backdrop = container.querySelector('.bg-black\\/40')
    expect(backdrop).not.toBeNull()
    await userEvent.click(backdrop as Element)
    expect(handleClose).toHaveBeenCalled()
  })

  it('calls onClose when Escape is pressed', async () => {
    const handleClose = vi.fn()
    render(
      <Drawer open onClose={handleClose} title="Teste">
        <p>Conteúdo</p>
      </Drawer>,
    )
    await userEvent.keyboard('{Escape}')
    expect(handleClose).toHaveBeenCalled()
  })
})
