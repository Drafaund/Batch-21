import type { Coordinate, OwnBoardView, OwnCell } from '../api/types'
import { formatCoordinate } from '../domain/coordinates'
import type { PlacementPreview } from '../domain/ships'
import { Board, type CellSpec } from './Board'

interface FleetBoardProps {
  board: OwnBoardView
  preview?: PlacementPreview | null
  highlight?: Coordinate[]
  sunkCells?: Coordinate[]
  interactive?: boolean
  onCellClick?: (x: number, y: number) => void
  onCellHover?: (x: number, y: number) => void
  onLeave?: () => void
}

const LOOK: Record<OwnCell, { symbol: string; className: string; text: string }> = {
  Water: { symbol: '', className: 'water', text: 'water' },
  Ship: { symbol: '■', className: 'ship', text: 'ship' },
  Hit: { symbol: '', className: 'hit', text: 'hit' },
  Miss: { symbol: '•', className: 'miss', text: 'miss' },
}

const SUNK_LOOK = { symbol: '', className: 'sunk', text: 'sunk' }

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
