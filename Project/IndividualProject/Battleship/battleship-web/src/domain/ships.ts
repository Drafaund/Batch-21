import type { Coordinate, OwnCell, Orientation, ShipType, ShotResult } from '../api/types'

export const SHIP_LENGTHS: Record<ShipType, number> = {
  Carrier: 5,
  Battleship: 4,
  Cruiser: 3,
  Submarine: 3,
  Destroyer: 2,
}

/** The cells a ship would cover. Horizontal extends to the right and vertical downward, like the server. */
export function shipCells(start: Coordinate, length: number, orientation: Orientation): Coordinate[] {
  return Array.from({ length }, (_, offset) =>
    orientation === 'Horizontal' ? { x: start.x + offset, y: start.y } : { x: start.x, y: start.y + offset },
  )
}

export interface PlacementPreview {
  /** The cells of the ship that lie on the board. */
  cells: Coordinate[]
  /** True when the whole ship is on the board and only on water. The server still has the last word. */
  valid: boolean
}

export function previewPlacement(
  board: OwnCell[][],
  start: Coordinate,
  type: ShipType,
  orientation: Orientation,
): PlacementPreview {
  const size = board.length
  const all = shipCells(start, SHIP_LENGTHS[type], orientation)
  const inside = all.filter((cell) => cell.x >= 0 && cell.x < size && cell.y >= 0 && cell.y < size)
  const valid = inside.length === all.length && inside.every((cell) => board[cell.y][cell.x] === 'Water')
  return { cells: inside, valid }
}

export function describeResult(result: ShotResult): string {
  switch (result) {
    case 'Hit':
      return 'Hit!'
    case 'Sunk':
      return 'Hit and sunk a ship!'
    case 'Miss':
      return 'Miss.'
    default:
      return 'Invalid shot.'
  }
}
