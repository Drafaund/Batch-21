import { useEffect, useState } from 'react'
import type { Coordinate, GameView, Orientation, ShipType } from '../api/types'
import { FleetBoard } from '../components/FleetBoard'
import { previewPlacement, SHIP_LENGTHS } from '../domain/ships'

interface PlacementScreenProps {
  view: GameView
  busy: boolean
  onPlace: (type: ShipType, start: Coordinate, orientation: Orientation) => void
  onPlaceRandomly: () => void
}

const SHIP_ORDER = Object.keys(SHIP_LENGTHS) as ShipType[]

export function PlacementScreen({ view, busy, onPlace, onPlaceRandomly }: PlacementScreenProps) {
  const [orientation, setOrientation] = useState<Orientation>('Horizontal')
  const [chosen, setChosen] = useState<ShipType | null>(null)
  const [hover, setHover] = useState<Coordinate | null>(null)
  const [hint, setHint] = useState<string | null>(null)

  const { placed, remaining } = view.placement
  const selected = chosen && remaining.includes(chosen) ? chosen : remaining[0]
  const canPlaceRandomly = placed.length === 0
  // Against a human, the game starts only when the other fleet is complete too.
  const waitingForOpponent = remaining.length === 0

  useEffect(() => {
    function handleKey(event: KeyboardEvent) {
      const element = event.target as HTMLElement
      if (element.tagName === 'INPUT' || element.tagName === 'TEXTAREA') {
        return
      }
      if (event.key === 'r' || event.key === 'R') {
        setOrientation((current) => (current === 'Horizontal' ? 'Vertical' : 'Horizontal'))
      }
    }

    window.addEventListener('keydown', handleKey)
    return () => window.removeEventListener('keydown', handleKey)
  }, [])

  const preview = selected && hover ? previewPlacement(view.ownBoard.cells, hover, selected, orientation) : null

  function handleCellClick(x: number, y: number) {
    if (!selected || busy) {
      return
    }
    const start = { x, y }
    if (!previewPlacement(view.ownBoard.cells, start, selected, orientation).valid) {
      setHint(`The ${selected} does not fit there: it must stay on the board and not overlap another ship.`)
      return
    }
    setHint(null)
    onPlace(selected, start, orientation)
  }

  return (
    <div className="placement-screen">
      <section className="panel">
        <h1>Place your ships</h1>
        <p className="muted">
          Pick a ship, choose its direction, then click the board where its first cell should go. A horizontal ship extends to the
          right and a vertical ship downward.
        </p>

        {waitingForOpponent && (
          <p role="status" className="turn-info">
            All your ships are placed. Waiting for {view.opponent?.name ?? 'your opponent'} to place theirs…
          </p>
        )}

        <ul className="ship-picker" aria-label="Ships to place">
          {SHIP_ORDER.map((type) => {
            const isPlaced = placed.includes(type)
            return (
              <li key={type}>
                <button
                  type="button"
                  className={type === selected ? 'ship-choice selected' : 'ship-choice'}
                  aria-pressed={type === selected}
                  disabled={isPlaced || busy}
                  onClick={() => setChosen(type)}
                >
                  {type} ({SHIP_LENGTHS[type]}){isPlaced ? ' ✓' : ''}
                </button>
              </li>
            )
          })}
        </ul>

        <div className="actions">
          <button type="button" onClick={() => setOrientation(orientation === 'Horizontal' ? 'Vertical' : 'Horizontal')}>
            Direction: {orientation} (press R to rotate)
          </button>
          <button type="button" onClick={onPlaceRandomly} disabled={busy || !canPlaceRandomly}>
            Place randomly
          </button>
        </div>
        {!canPlaceRandomly && <p className="muted">Random placement is only possible before you place the first ship.</p>}
        {hint && (
          <p role="status" className="hint">
            {hint}
          </p>
        )}
      </section>

      <FleetBoard
        board={view.ownBoard}
        preview={preview}
        interactive={!busy}
        onCellClick={handleCellClick}
        onCellHover={(x, y) => setHover({ x, y })}
        onLeave={() => setHover(null)}
      />
    </div>
  )
}
