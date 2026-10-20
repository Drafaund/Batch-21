import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import type { GameView, ShipType } from '../api/types'
import { makeView, SHIP_TYPES } from '../test/fixtures'
import { PlacementScreen } from './PlacementScreen'

function setup(view: GameView = makeView(), busy = false) {
  const onPlace = vi.fn()
  const onPlaceRandomly = vi.fn()
  const utils = render(<PlacementScreen view={view} busy={busy} onPlace={onPlace} onPlaceRandomly={onPlaceRandomly} />)
  return { onPlace, onPlaceRandomly, ...utils }
}

const cell = (name: string) => screen.getByRole('button', { name })

function viewWithPlaced(...placed: ShipType[]): GameView {
  return makeView({
    version: 1 + placed.length,
    placement: { placed, remaining: SHIP_TYPES.filter((type) => !placed.includes(type)) },
  })
}

describe('PlacementScreen', () => {
  it('starts with the carrier selected and a horizontal direction', () => {
    setup()

    expect(screen.getByRole('button', { name: /Carrier \(5\)/ })).toHaveAttribute('aria-pressed', 'true')
    expect(screen.getByRole('button', { name: /Direction: Horizontal/ })).toBeInTheDocument()
  })

  it('places the selected ship where the board is clicked', async () => {
    const { onPlace } = setup()

    await userEvent.click(cell('Your fleet B-3: water'))

    expect(onPlace).toHaveBeenCalledExactlyOnceWith('Carrier', { x: 2, y: 1 }, 'Horizontal')
  })

  it('places another ship after choosing it', async () => {
    const { onPlace } = setup()

    await userEvent.click(screen.getByRole('button', { name: /Destroyer \(2\)/ }))
    await userEvent.click(cell('Your fleet A-1: water'))

    expect(onPlace).toHaveBeenCalledWith('Destroyer', { x: 0, y: 0 }, 'Horizontal')
  })

  it('rotates with the button', async () => {
    const { onPlace } = setup()

    await userEvent.click(screen.getByRole('button', { name: /Direction: Horizontal/ }))
    expect(screen.getByRole('button', { name: /Direction: Vertical/ })).toBeInTheDocument()
    await userEvent.click(cell('Your fleet A-1: water'))

    expect(onPlace).toHaveBeenCalledWith('Carrier', { x: 0, y: 0 }, 'Vertical')
  })

  it('rotates with the R key, and back again', async () => {
    setup()

    await userEvent.keyboard('r')
    expect(screen.getByRole('button', { name: /Direction: Vertical/ })).toBeInTheDocument()
    await userEvent.keyboard('R')
    expect(screen.getByRole('button', { name: /Direction: Horizontal/ })).toBeInTheDocument()
  })

  it('shows a green preview of the ship under the pointer', async () => {
    const { container } = setup()

    await userEvent.hover(cell('Your fleet C-2: water'))

    expect(container.querySelectorAll('.preview-ok')).toHaveLength(5)
    expect(container.querySelectorAll('.preview-bad')).toHaveLength(0)
  })

  it('shows a red preview when the ship does not fit', async () => {
    const { container } = setup()

    await userEvent.hover(cell('Your fleet A-9: water'))

    expect(container.querySelectorAll('.preview-bad')).toHaveLength(2)
    expect(container.querySelectorAll('.preview-ok')).toHaveLength(0)
  })

  it('does not place a ship that does not fit, and says why', async () => {
    const { onPlace } = setup()

    await userEvent.click(cell('Your fleet A-9: water'))

    expect(onPlace).not.toHaveBeenCalled()
    expect(screen.getByRole('status')).toHaveTextContent('The Carrier does not fit there')
  })

  it('does not place a ship over another ship', async () => {
    const view = viewWithPlaced('Carrier')
    view.ownBoard.cells[0][0] = 'Ship'
    const { onPlace } = setup(view)

    await userEvent.click(cell('Your fleet A-1: ship'))

    expect(onPlace).not.toHaveBeenCalled()
  })

  it('moves on to the next ship when one is placed', () => {
    setup(viewWithPlaced('Carrier'))

    expect(screen.getByRole('button', { name: /Carrier \(5\)/ })).toBeDisabled()
    expect(screen.getByRole('button', { name: /Battleship \(4\)/ })).toHaveAttribute('aria-pressed', 'true')
  })

  it('keeps the chosen ship while it is still to be placed', async () => {
    const { rerender, onPlace, onPlaceRandomly } = setup()
    await userEvent.click(screen.getByRole('button', { name: /Submarine \(3\)/ }))

    rerender(<PlacementScreen view={viewWithPlaced('Carrier')} busy={false} onPlace={onPlace} onPlaceRandomly={onPlaceRandomly} />)

    expect(screen.getByRole('button', { name: /Submarine \(3\)/ })).toHaveAttribute('aria-pressed', 'true')
  })

  it('places all ships randomly before the first ship is placed', async () => {
    const { onPlaceRandomly } = setup()

    await userEvent.click(screen.getByRole('button', { name: 'Place randomly' }))

    expect(onPlaceRandomly).toHaveBeenCalledOnce()
  })

  it('explains why random placement is off after the first ship', () => {
    setup(viewWithPlaced('Cruiser'))

    expect(screen.getByRole('button', { name: 'Place randomly' })).toBeDisabled()
    expect(screen.getByText(/only possible before you place the first ship/)).toBeInTheDocument()
  })

  it('cannot do anything while busy', async () => {
    const { onPlace } = setup(makeView(), true)

    expect(screen.getByRole('button', { name: 'Place randomly' })).toBeDisabled()
    expect(screen.queryAllByRole('button', { name: /^Your fleet/ })).toHaveLength(0)
    expect(onPlace).not.toHaveBeenCalled()
  })

  it('draws the ships that are already placed', () => {
    const view = viewWithPlaced('Destroyer')
    view.ownBoard.cells[4][0] = 'Ship'
    view.ownBoard.cells[4][1] = 'Ship'
    setup(view)

    expect(cell('Your fleet E-1: ship')).toBeInTheDocument()
    expect(cell('Your fleet E-2: ship')).toBeInTheDocument()
  })
})
