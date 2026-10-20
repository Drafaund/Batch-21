import { describe, expect, it } from 'vitest'
import type { OwnCell } from '../api/types'
import { grid } from '../test/fixtures'
import { describeResult, previewPlacement, SHIP_LENGTHS, shipCells } from './ships'

describe('SHIP_LENGTHS', () => {
  it('matches the rules of the game', () => {
    expect(SHIP_LENGTHS).toEqual({ Carrier: 5, Battleship: 4, Cruiser: 3, Submarine: 3, Destroyer: 2 })
  })
})

describe('shipCells', () => {
  it('extends a horizontal ship to the right', () => {
    expect(shipCells({ x: 2, y: 4 }, 3, 'Horizontal')).toEqual([
      { x: 2, y: 4 },
      { x: 3, y: 4 },
      { x: 4, y: 4 },
    ])
  })

  it('extends a vertical ship downward', () => {
    expect(shipCells({ x: 7, y: 8 }, 2, 'Vertical')).toEqual([
      { x: 7, y: 8 },
      { x: 7, y: 9 },
    ])
  })
})

describe('previewPlacement', () => {
  const water = grid<OwnCell>('Water')

  it('accepts a ship that fits on water', () => {
    const preview = previewPlacement(water, { x: 0, y: 0 }, 'Carrier', 'Horizontal')

    expect(preview.valid).toBe(true)
    expect(preview.cells).toHaveLength(5)
  })

  it('accepts a ship that ends on the last cell', () => {
    expect(previewPlacement(water, { x: 5, y: 0 }, 'Carrier', 'Horizontal').valid).toBe(true)
    expect(previewPlacement(water, { x: 0, y: 5 }, 'Carrier', 'Vertical').valid).toBe(true)
  })

  it('rejects a ship that sticks out and only lists the cells on the board', () => {
    const preview = previewPlacement(water, { x: 8, y: 0 }, 'Carrier', 'Horizontal')

    expect(preview.valid).toBe(false)
    expect(preview.cells).toEqual([
      { x: 8, y: 0 },
      { x: 9, y: 0 },
    ])
  })

  it('rejects a ship that starts outside the board', () => {
    expect(previewPlacement(water, { x: -1, y: 0 }, 'Destroyer', 'Horizontal').valid).toBe(false)
    expect(previewPlacement(water, { x: 0, y: 10 }, 'Destroyer', 'Vertical').valid).toBe(false)
  })

  it('rejects a ship that overlaps another ship', () => {
    const board = grid<OwnCell>('Water')
    board[0][3] = 'Ship'

    expect(previewPlacement(board, { x: 0, y: 0 }, 'Carrier', 'Horizontal').valid).toBe(false)
  })

  it('accepts a ship that only touches another ship', () => {
    const board = grid<OwnCell>('Water')
    board[0][5] = 'Ship'

    expect(previewPlacement(board, { x: 0, y: 0 }, 'Carrier', 'Horizontal').valid).toBe(true)
  })

  it('rejects cells that were already shot at', () => {
    const board = grid<OwnCell>('Water')
    board[0][1] = 'Miss'

    expect(previewPlacement(board, { x: 0, y: 0 }, 'Destroyer', 'Horizontal').valid).toBe(false)
  })
})

describe('describeResult', () => {
  it.each([
    ['Hit', 'Hit!'],
    ['Miss', 'Miss.'],
    ['Sunk', 'Hit and sunk a ship!'],
    ['Invalid', 'Invalid shot.'],
  ] as const)('describes %s', (result, text) => {
    expect(describeResult(result)).toBe(text)
  })
})
