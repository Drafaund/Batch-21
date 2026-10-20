import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { ApiError, type GameApi } from './api/client'
import { loadSession, saveSession } from './api/session'
import type { ShotResponse } from './api/types'
import App from './App'
import { fleetView, makeFakeApi, makeView, withEnemyCell } from './test/fixtures'

const created = { code: 'K7QF2', playerToken: 'secret', seat: 1, view: makeView() }

async function startGame(api: GameApi, name = 'Ann') {
  render(<App api={api} />)
  await userEvent.type(screen.getByLabelText('Your name'), name)
  await userEvent.click(screen.getByRole('button', { name: 'Start game' }))
}

describe('App', () => {
  it('begins at the start screen', () => {
    render(<App api={makeFakeApi()} />)

    expect(screen.getByRole('heading', { name: 'Battleship' })).toBeInTheDocument()
    expect(screen.getByLabelText('Your name')).toBeInTheDocument()
  })

  it('creates the game and moves on to ship placement', async () => {
    const api = makeFakeApi({ createGame: async () => created })

    await startGame(api)

    expect(await screen.findByRole('heading', { name: 'Place your ships' })).toBeInTheDocument()
    expect(api.createGame).toHaveBeenCalledWith('Ann', 'Random')
    expect(loadSession()).toEqual({ code: 'K7QF2', token: 'secret' })
  })

  it('places ships one by one with the code and token of the game', async () => {
    const placed = makeView({ version: 2, placement: { placed: ['Carrier'], remaining: ['Battleship', 'Cruiser', 'Submarine', 'Destroyer'] } })
    const api = makeFakeApi({ createGame: async () => created, placeShip: async () => placed })
    await startGame(api)

    await userEvent.click(await screen.findByRole('button', { name: 'Your fleet B-2: water' }))

    expect(api.placeShip).toHaveBeenCalledWith('K7QF2', 'secret', 'Carrier', { x: 1, y: 1 }, 'Horizontal')
    expect(await screen.findByRole('button', { name: /Battleship \(4\)/ })).toHaveAttribute('aria-pressed', 'true')
  })

  it('places everything at random and goes to the battle', async () => {
    const api = makeFakeApi({ createGame: async () => created, placeShipsRandomly: async () => fleetView() })
    await startGame(api)

    await userEvent.click(await screen.findByRole('button', { name: 'Place randomly' }))

    expect(await screen.findByText(/Your turn/)).toBeInTheDocument()
    expect(api.placeShipsRandomly).toHaveBeenCalledWith('K7QF2', 'secret')
    expect(screen.getByRole('img', { name: 'Your fleet A-1: ship' })).toBeInTheDocument()
  })

  it('fires, shows the result and the reply of the bot', async () => {
    const reply: ShotResponse = {
      result: 'Hit',
      botShots: [{ target: { x: 5, y: 6 }, result: 'Miss' }],
      view: withEnemyCell(fleetView({ version: 8 }), 3, 3, 'Hit'),
    }
    reply.view.ownBoard.cells[6][5] = 'Miss'
    const api = makeFakeApi({
      createGame: async () => created,
      placeShipsRandomly: async () => fleetView(),
      fire: async () => reply,
    })
    await startGame(api)
    await userEvent.click(await screen.findByRole('button', { name: 'Place randomly' }))

    await userEvent.click(await screen.findByRole('button', { name: 'Target board D-4: not fired at yet' }))

    expect(api.fire).toHaveBeenCalledWith('K7QF2', 'secret', { x: 3, y: 3 })
    expect(await screen.findByText('You fired at D-4: Hit!')).toBeInTheDocument()
    expect(screen.getByText('Bot Random fired at G-6: Miss.')).toBeInTheDocument()
    expect(screen.getByRole('img', { name: 'Target board D-4: hit' })).toHaveClass('last-shot')
    expect(screen.getByRole('img', { name: 'Your fleet G-6: miss' })).toHaveClass('last-shot')
  })

  it('shows the message of the server when an action is refused, and lets it be dismissed', async () => {
    const api = makeFakeApi({
      createGame: async () => created,
      placeShip: async () => Promise.reject(new ApiError(409, 'The ship does not fit there (outside the board or overlapping another ship).')),
    })
    await startGame(api)

    await userEvent.click(await screen.findByRole('button', { name: 'Your fleet A-1: water' }))

    const alert = await screen.findByRole('alert')
    expect(alert).toHaveTextContent('does not fit there')
    expect(screen.getByRole('heading', { name: 'Place your ships' })).toBeInTheDocument()

    await userEvent.click(within(alert).getByRole('button', { name: 'Dismiss the message' }))
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })

  it('stays on the start screen with a message when the server cannot be reached', async () => {
    const api = makeFakeApi({ createGame: async () => Promise.reject(new ApiError(0, 'Cannot reach the server. Check your connection and try again.')) })

    await startGame(api)

    expect(await screen.findByRole('alert')).toHaveTextContent('Cannot reach the server')
    expect(screen.getByLabelText('Your name')).toBeInTheDocument()
  })

  it('shows the reason when the name is refused', async () => {
    const api = makeFakeApi({ createGame: async () => Promise.reject(new ApiError(400, 'playerName must be 1 to 20 characters without control characters.')) })

    await startGame(api)

    expect(await screen.findByRole('alert')).toHaveTextContent('playerName must be 1 to 20 characters')
  })

  it('resumes a saved game after a reload', async () => {
    saveSession({ code: 'K7QF2', token: 'secret' })
    const api = makeFakeApi({ getGame: async () => fleetView() })

    render(<App api={api} />)

    expect(screen.getByText('Loading your game…')).toBeInTheDocument()
    expect(await screen.findByText(/Your turn/)).toBeInTheDocument()
    expect(api.getGame).toHaveBeenCalledWith('K7QF2', 'secret')
  })

  it('goes back to the start screen when the saved game no longer exists', async () => {
    saveSession({ code: 'K7QF2', token: 'secret' })
    const api = makeFakeApi({ getGame: async () => Promise.reject(new ApiError(404, 'There is no game with this code.')) })

    render(<App api={api} />)

    expect(await screen.findByRole('alert')).toHaveTextContent('no longer exists')
    expect(screen.getByLabelText('Your name')).toBeInTheDocument()
    expect(loadSession()).toBeNull()
  })

  it('goes back to the start screen when a shot finds the game gone', async () => {
    saveSession({ code: 'K7QF2', token: 'secret' })
    const api = makeFakeApi({
      getGame: async () => fleetView(),
      fire: async () => Promise.reject(new ApiError(404, 'There is no game with this code.')),
    })
    render(<App api={api} />)

    await userEvent.click(await screen.findByRole('button', { name: 'Target board A-1: not fired at yet' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('no longer exists')
    expect(screen.getByLabelText('Your name')).toBeInTheDocument()
  })

  it('shows the winner and starts over with Play again', async () => {
    saveSession({ code: 'K7QF2', token: 'secret' })
    const finished = fleetView({ phase: 'Finished', yourTurn: false, winner: 'you', enemyShipsRemaining: 0 })
    const api = makeFakeApi({ getGame: async () => finished })
    render(<App api={api} />)

    expect(await screen.findByText('You win!')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Play again' }))

    expect(screen.getByLabelText('Your name')).toBeInTheDocument()
    expect(loadSession()).toBeNull()
  })

  it('plays a whole game against a scripted server', async () => {
    let version = 6
    let shots = 0
    const total = 3
    const api = makeFakeApi({
      createGame: async () => created,
      placeShipsRandomly: async () => fleetView(),
      fire: async (_code, _token, target) => {
        shots++
        const done = shots === total
        const view = withEnemyCell(
          fleetView({ version: ++version, ...(done ? { phase: 'Finished' as const, winner: 'you' as const, yourTurn: false, enemyShipsRemaining: 0 } : {}) }),
          target.x,
          target.y,
          'Hit',
        )
        return { result: done ? ('Sunk' as const) : ('Hit' as const), botShots: done ? [] : [{ target: { x: 9, y: shots }, result: 'Miss' as const }], view }
      },
    })
    await startGame(api)
    await userEvent.click(await screen.findByRole('button', { name: 'Place randomly' }))

    await userEvent.click(await screen.findByRole('button', { name: 'Target board A-1: not fired at yet' }))
    await userEvent.click(await screen.findByRole('button', { name: 'Target board A-2: not fired at yet' }))
    await userEvent.click(await screen.findByRole('button', { name: 'Target board A-3: not fired at yet' }))

    await waitFor(() => expect(screen.getByText('You win!')).toBeInTheDocument())
    expect(screen.getByText('You win the game!')).toBeInTheDocument()
    expect(api.fire).toHaveBeenCalledTimes(3)
  })
})
