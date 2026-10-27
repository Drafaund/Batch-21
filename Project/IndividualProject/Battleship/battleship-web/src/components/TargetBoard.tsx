import type { Coordinate, EnemyBoardView, EnemyCell } from '../api/types'
import { formatCoordinate } from '../domain/coordinates'
import { Board, type CellSpec } from './Board'

interface TargetBoardProps {
  board: EnemyBoardView
  lastShot?: Coordinate | null
  onFire?: (target: Coordinate) => void
}

const LOOK: Record<EnemyCell, { symbol: string; className: string; text: string }> = {
  Unknown: { symbol: '', className: 'unknown', text: 'not fired at yet' },
  Hit: { symbol: '', className: 'hit', text: 'hit' },
  Miss: { symbol: '•', className: 'miss', text: 'miss' },
  Sunk: { symbol: '', className: 'sunk', text: 'sunk' },
}

export function TargetBoard({ board, lastShot, onFire }: TargetBoardProps) {
  const rows: CellSpec[][] = board.cells.map((row, y) =>
    row.map((value, x) => {
      const look = LOOK[value]
      const isLast = lastShot?.x === x && lastShot?.y === y
      return {
        symbol: look.symbol,
        className: isLast ? `${look.className} last-shot` : look.className,
        label: `Target board ${formatCoordinate({ x, y })}: ${look.text}`,
        interactive: onFire !== undefined && value === 'Unknown',
      }
    }),
  )

  return <Board title="Target board" rows={rows} onCellClick={(x, y) => onFire?.({ x, y })} />
}
