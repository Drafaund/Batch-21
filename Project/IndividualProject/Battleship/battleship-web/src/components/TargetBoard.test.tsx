import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import type { EnemyCell } from '../api/types'
import { grid } from '../test/fixtures'
import { TargetBoard } from './TargetBoard'

function board(changes: [number, number, EnemyCell][] = []) {
  const cells = grid<EnemyCell>('Unknown')
  changes.forEach(([x, y, value]) => {
    cells[y][x] = value
  })
  return { size: 10, cells }
}

describe('TargetBoard', () => {
  it('describes the cells that were fired at', () => {
    render(
      <TargetBoard
        board={board([
          [0, 0, 'Hit'],
          [1, 0, 'Miss'],
        ])}
      />,
    )

    expect(screen.getByRole('img', { name: 'Target board A-1: hit' })).toBeInTheDocument()
    expect(screen.getByRole('img', { name: 'Target board A-2: miss' })).toBeInTheDocument()
    expect(screen.getByRole('img', { name: 'Target board A-3: not fired at yet' })).toBeInTheDocument()
  })

  it('never describes a ship', () => {
    render(<TargetBoard board={board([[4, 4, 'Hit']])} />)

    expect(screen.queryByRole('img', { name: /ship/i })).not.toBeInTheDocument()
  })

  it('cannot be clicked without a fire handler', () => {
    render(<TargetBoard board={board()} />)

    expect(screen.queryAllByRole('button')).toHaveLength(0)
  })

  it('lets only the cells not fired at be clicked', async () => {
    const onFire = vi.fn()
    render(
      <TargetBoard
        board={board([
          [0, 0, 'Hit'],
          [1, 0, 'Miss'],
        ])}
        onFire={onFire}
      />,
    )

    expect(screen.getAllByRole('button')).toHaveLength(98)
    await userEvent.click(screen.getByRole('button', { name: 'Target board D-4: not fired at yet' }))

    expect(onFire).toHaveBeenCalledExactlyOnceWith({ x: 3, y: 3 })
  })

  it('marks the last shot', () => {
    render(<TargetBoard board={board([[2, 5, 'Miss']])} lastShot={{ x: 2, y: 5 }} />)

    expect(screen.getByRole('img', { name: 'Target board F-3: miss' })).toHaveClass('last-shot')
  })
})
