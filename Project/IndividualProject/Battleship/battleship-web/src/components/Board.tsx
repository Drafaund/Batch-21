import { columnLabel, rowLabel } from '../domain/coordinates'

export interface CellSpec {
  symbol: string
  className: string
  label: string
  interactive: boolean
}

interface BoardProps {
  title: string
  rows: CellSpec[][]
  onCellClick?: (x: number, y: number) => void
  onCellHover?: (x: number, y: number) => void
  onLeave?: () => void
}

export function Board({ title, rows, onCellClick, onCellHover, onLeave }: BoardProps) {
  const size = rows.length

  return (
    <section className="board-panel" aria-label={title}>
      <h2>{title}</h2>
      <div
        className="board"
        style={{
          gridTemplateColumns: `auto repeat(${size}, var(--cell-size))`,
          gridTemplateRows: `auto repeat(${size}, var(--cell-size))`,
        }}
        onMouseLeave={onLeave}
      >
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
