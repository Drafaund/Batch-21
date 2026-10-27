import type { ShipView } from '../api/types'

interface FleetStatusProps {
  ships: ShipView[]
  enemyShipsRemaining: number
}

export function FleetStatus({ ships, enemyShipsRemaining }: FleetStatusProps) {
  return (
    <section className="fleet-status" aria-label="Fleet status">
      <h2>Fleets</h2>
      <ul>
        {ships.map((ship) => (
          <li key={ship.type} className={ship.isSunk ? 'sunk' : undefined}>
            <span>{ship.type}</span>
            <span aria-label={ship.isSunk ? 'sunk' : `${ship.length - ship.hitCount} of ${ship.length} cells intact`}>
              {Array.from({ length: ship.length }, (_, index) => (index < ship.hitCount ? '✕' : '■')).join('')}
            </span>
          </li>
        ))}
      </ul>
      <p>
        Your ships afloat: <strong>{ships.filter((ship) => !ship.isSunk).length}</strong>
      </p>
      <p>
        Enemy ships left: <strong>{enemyShipsRemaining}</strong>
      </p>
    </section>
  )
}
