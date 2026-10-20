import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { Board, type CellSpec } from './Board'

function rows(size: number, interactive: boolean): CellSpec[][] {
  return Array.from({ length: size }, (_, y) =>
    Array.from({ length: size }, (_, x) => ({ symbol: '~', className: 'water', label: `cell ${x},${y}`, interactive })),
  )
}

describe('Board', () => {
  it('shows row letters and column numbers', () => {
    const { container } = render(<Board title="Test board" rows={rows(3, false)} />)

    const axis = Array.from(container.querySelectorAll('.axis')).map((element) => element.textContent)
    expect(axis).toEqual(['', '1', '2', '3', 'A', 'B', 'C'])
  })

  it('has a heading and a named region', () => {
    render(<Board title="Test board" rows={rows(2, false)} />)

    expect(screen.getByRole('heading', { name: 'Test board' })).toBeInTheDocument()
    expect(screen.getByRole('region', { name: 'Test board' })).toBeInTheDocument()
  })

  it('makes interactive cells buttons and reports the clicked cell', async () => {
    const onCellClick = vi.fn()
    render(<Board title="Test board" rows={rows(3, true)} onCellClick={onCellClick} />)

    expect(screen.getAllByRole('button')).toHaveLength(9)
    await userEvent.click(screen.getByRole('button', { name: 'cell 2,1' }))

    expect(onCellClick).toHaveBeenCalledExactlyOnceWith(2, 1)
  })

  it('makes other cells plain boxes with a label and no buttons', () => {
    render(<Board title="Test board" rows={rows(3, false)} />)

    expect(screen.queryAllByRole('button')).toHaveLength(0)
    expect(screen.getAllByRole('img')).toHaveLength(9)
    expect(screen.getByRole('img', { name: 'cell 0,0' })).toBeInTheDocument()
  })

  it('reports hovering a cell and leaving the board', async () => {
    const onCellHover = vi.fn()
    const onLeave = vi.fn()
    render(<Board title="Test board" rows={rows(2, true)} onCellHover={onCellHover} onLeave={onLeave} />)

    await userEvent.hover(screen.getByRole('button', { name: 'cell 1,0' }))
    expect(onCellHover).toHaveBeenLastCalledWith(1, 0)

    await userEvent.unhover(screen.getByRole('button', { name: 'cell 1,0' }))
    expect(onLeave).toHaveBeenCalled()
  })

  it('applies the class of each cell', () => {
    const cells = rows(2, false)
    cells[1][0] = { symbol: '✕', className: 'hit last-shot', label: 'the hit', interactive: false }

    render(<Board title="Test board" rows={cells} />)

    expect(screen.getByRole('img', { name: 'the hit' })).toHaveClass('cell', 'hit', 'last-shot')
  })
})
