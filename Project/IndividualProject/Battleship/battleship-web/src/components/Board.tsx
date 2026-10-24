import { columnLabel, rowLabel } from '../domain/coordinates'

/** How one cell looks and what a screen reader says about it. */
export interface CellSpec {
  symbol: string
  className: string
  label: string
  /** Cells that can be clicked are buttons; the others are plain boxes. */
  interactive: boolean
}

interface BoardProps {
  title: string
  /** rows[y][x]: first the row, then the column. */
  rows: CellSpec[][]
  onCellClick?: (x: number, y: number) => void
  onCellHover?: (x: number, y: number) => void
  onLeave?: () => void
}

/** A square board with row letters and column numbers. It knows nothing about the game. */
export function Board({ title, rows, onCellClick, onCellHover, onLeave }: BoardProps) {
  const size = rows.length

  return (
    <section className="board-panel" aria-label={title}>
      <h2>{title}</h2>
      <div
        className="board"
        style={{
          gridTemplateColumns: `auto repeat(${size}, var(--cell-size))`,
          // Rows are explicit too, so the sea layer's "2 / -1" reaches the last row.
          gridTemplateRows: `auto repeat(${size}, var(--cell-size))`,
        }}
        onMouseLeave={onLeave}
      >
        {/* Animated sea behind the cells. Cells are re-created when they turn into buttons or boxes; this layer is not. */}
        <div className="sea" aria-hidden="true" />
        <span className="axis" aria-hidden="true" />
        {Array.from({ length: size }, (_, x) => (
          <span key={`column-${x}`} className="axis" aria-hidden="true">
            {columnLabel(x)}
          </span>
        ))}
        {rows.map((row, y) => [
          <span key={`row-${y}`} className="axis" aria-hidden="true">
            {rowLabel(y)}
          </span>,
          ...row.map((cell, x) =>
            cell.interactive ? (
              <button
                key={`${x}-${y}`}
                type="button"
                className={`cell ${cell.className}`}
                aria-label={cell.label}
                onClick={() => onCellClick?.(x, y)}
                onMouseEnter={() => onCellHover?.(x, y)}
                onFocus={() => onCellHover?.(x, y)}
              >
                <span aria-hidden="true">{cell.symbol}</span>
              </button>
            ) : (
              <div
                key={`${x}-${y}`}
                className={`cell ${cell.className}`}
                role="img"
                aria-label={cell.label}
                onMouseEnter={() => onCellHover?.(x, y)}
              >
                <span aria-hidden="true">{cell.symbol}</span>
              </div>
            ),
          ),
        ])}
      </div>
    </section>
  )
}
