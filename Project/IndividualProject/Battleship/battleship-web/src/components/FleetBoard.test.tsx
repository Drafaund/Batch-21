import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import type { OwnCell } from '../api/types'
import { grid } from '../test/fixtures'
import { FleetBoard } from './FleetBoard'

function board(changes: [number, number, OwnCell][] = []) {
  const cells = grid<OwnCell>('Water')
  changes.forEach(([x, y, value]) => {
    cells[y][x] = value
  })
  return { size: 10, cells }
}

describe('FleetBoard', () => {
  it('describes every kind of cell by its coordinate', () => {
    render(
      <FleetBoard
        board={board([
          [0, 0, 'Ship'],
          [1, 0, 'Hit'],
          [2, 0, 'Miss'],
        ])}
      />,
    )

    expect(screen.getByRole('img', { name: 'Your fleet A-1: ship' })).toBeInTheDocument()
    expect(screen.getByRole('img', { name: 'Your fleet A-2: hit' })).toBeInTheDocument()
    expect(screen.getByRole('img', { name: 'Your fleet A-3: miss' })).toBeInTheDocument()
    expect(screen.getByRole('img', { name: 'Your fleet J-10: water' })).toBeInTheDocument()
  })

  it('shows the hit cells of a sunk ship as sunk and keeps the other hits as hits', () => {
    render(
      <FleetBoard
        board={board([
          [0, 4, 'Hit'],
          [1, 4, 'Hit'],
          [5, 5, 'Hit'],
        ])}
        sunkCells={[
          { x: 0, y: 4 },
          { x: 1, y: 4 },
        ]}
      />,
    )

    expect(screen.getByRole('img', { name: 'Your fleet E-1: sunk' })).toHaveClass('sunk')
    expect(screen.getByRole('img', { name: 'Your fleet E-2: sunk' })).toHaveClass('sunk')
    expect(screen.getByRole('img', { name: 'Your fleet F-6: hit' })).toHaveClass('hit')
  })

  it('treats every hit as a plain hit when no ship has sunk', () => {
    render(<FleetBoard board={board([[0, 4, 'Hit']])} />)

    expect(screen.getByRole('img', { name: 'Your fleet E-1: hit' })).not.toHaveClass('sunk')
  })

  it('marks the latest shots at this board', () => {
    render(<FleetBoard board={board([[3, 3, 'Miss']])} highlight={[{ x: 3, y: 3 }]} />)

    expect(screen.getByRole('img', { name: 'Your fleet D-4: miss' })).toHaveClass('last-shot')
    expect(screen.getByRole('img', { name: 'Your fleet A-1: water' })).not.toHaveClass('last-shot')
  })

  it('colours a valid preview green and an invalid one red', () => {
    const { rerender, container } = render(
      <FleetBoard board={board()} preview={{ cells: [{ x: 0, y: 0 }, { x: 1, y: 0 }], valid: true }} />,
    )
    expect(container.querySelectorAll('.preview-ok')).toHaveLength(2)

    rerender(<FleetBoard board={board()} preview={{ cells: [{ x: 0, y: 0 }], valid: false }} />)
    expect(container.querySelectorAll('.preview-ok')).toHaveLength(0)
    expect(container.querySelectorAll('.preview-bad')).toHaveLength(1)
  })

  it('has clickable cells only when asked to', () => {
    const { rerender } = render(<FleetBoard board={board()} />)
    expect(screen.queryAllByRole('button')).toHaveLength(0)

    rerender(<FleetBoard board={board()} interactive />)
    expect(screen.getAllByRole('button')).toHaveLength(100)
  })
})
