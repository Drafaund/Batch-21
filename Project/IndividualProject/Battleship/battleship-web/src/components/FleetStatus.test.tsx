import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { fleetView } from '../test/fixtures'
import { FleetStatus } from './FleetStatus'

describe('FleetStatus', () => {
  it('lists the ships and counts both fleets', () => {
    const view = fleetView()

    render(<FleetStatus ships={view.ownShips} enemyShipsRemaining={4} />)

    expect(screen.getAllByRole('listitem')).toHaveLength(5)
    expect(screen.getByText('Carrier')).toBeInTheDocument()
    expect(screen.getByText(/Your ships afloat/)).toHaveTextContent('Your ships afloat: 5')
    expect(screen.getByText(/Enemy ships left/)).toHaveTextContent('Enemy ships left: 4')
  })

  it('shows damage and sunk ships', () => {
    const view = fleetView()
    const ships = view.ownShips.map((ship) =>
      ship.type === 'Destroyer' ? { ...ship, hitCount: 2, isSunk: true } : ship.type === 'Carrier' ? { ...ship, hitCount: 2 } : ship,
    )

    render(<FleetStatus ships={ships} enemyShipsRemaining={5} />)

    expect(screen.getByLabelText('sunk')).toHaveTextContent('✕✕')
    expect(screen.getByLabelText('3 of 5 cells intact')).toHaveTextContent('✕✕■■■')
    expect(screen.getByText(/Your ships afloat/)).toHaveTextContent('Your ships afloat: 4')
  })
})
