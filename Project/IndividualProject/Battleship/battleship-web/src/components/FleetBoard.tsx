import type { Coordinate, OwnBoardView, OwnCell } from '../api/types'
import { formatCoordinate } from '../domain/coordinates'
import type { PlacementPreview } from '../domain/ships'
import { Board, type CellSpec } from './Board'

interface FleetBoardProps {
  board: OwnBoardView
  /** Cells to colour while a ship is being placed. */
  preview?: PlacementPreview | null
  /** Cells to mark as the most recent shots at this board. */
  highlight?: Coordinate[]
  /** Cells of the player's own ships that have sunk. The board only says `Hit` for them, so the ships tell. */
  sunkCells?: Coordinate[]
  interactive?: boolean
  onCellClick?: (x: number, y: number) => void
  onCellHover?: (x: number, y: number) => void
  onLeave?: () => void
}

const LOOK: Record<OwnCell, { symbol: string; className: string; text: string }> = {
  Water: { symbol: '', className: 'water', text: 'water' },
  Ship: { symbol: '■', className: 'ship', text: 'ship' },
  // Hit and sunk cells are drawn by CSS (flames and a skull), so they have no text symbol.
  Hit: { symbol: '', className: 'hit', text: 'hit' },
  Miss: { symbol: '•', className: 'miss', text: 'miss' },
}

const SUNK_LOOK = { symbol: '', className: 'sunk', text: 'sunk' }

/** The player's own board: ships are visible. */
export function FleetBoard({ board, preview, highlight = [], sunkCells = [], interactive = false, onCellClick, onCellHover, onLeave }: FleetBoardProps) {
  const previewKeys = new Set(preview?.cells.map((cell) => `${cell.x},${cell.y}`))
  const highlightKeys = new Set(highlight.map((cell) => `${cell.x},${cell.y}`))
  const sunkKeys = new Set(sunkCells.map((cell) => `${cell.x},${cell.y}`))

  const rows: CellSpec[][] = board.cells.map((row, y) =>
    row.map((value, x) => {
      const look = value === 'Hit' && sunkKeys.has(`${x},${y}`) ? SUNK_LOOK : LOOK[value]
      const classes = [look.className]
      if (previewKeys.has(`${x},${y}`)) {
        classes.push(preview?.valid ? 'preview-ok' : 'preview-bad')
      }
      if (highlightKeys.has(`${x},${y}`)) {
        classes.push('last-shot')
      }
      return {
        symbol: look.symbol,
        className: classes.join(' '),
        label: `Your fleet ${formatCoordinate({ x, y })}: ${look.text}`,
        interactive,
      }
    }),
  )

  return <Board title="Your fleet" rows={rows} onCellClick={onCellClick} onCellHover={onCellHover} onLeave={onLeave} />
}
