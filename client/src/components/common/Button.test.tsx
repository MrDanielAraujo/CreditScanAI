import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { Button } from './Button'

describe('Button', () => {
  it('renders its children', () => {
    render(<Button>Click me</Button>)
    expect(screen.getByText('Click me')).toBeInTheDocument()
  })

  it('calls onClick when clicked', async () => {
    const handleClick = vi.fn()
    render(<Button onClick={handleClick}>Click me</Button>)

    await userEvent.click(screen.getByRole('button', { name: 'Click me' }))

    expect(handleClick).toHaveBeenCalledOnce()
  })

  it('is disabled when the disabled prop is true', () => {
    render(<Button disabled>Click me</Button>)
    expect(screen.getByRole('button', { name: 'Click me' })).toBeDisabled()
  })

  it('is disabled and shows a spinner when loading', () => {
    render(<Button loading>Submit</Button>)
    const button = screen.getByRole('button', { name: /Submit/ })

    expect(button).toBeDisabled()
    expect(button).toHaveAttribute('aria-busy', 'true')
    expect(screen.getByRole('status', { name: 'loading' })).toBeInTheDocument()
  })
})
