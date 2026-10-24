import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { loadSession } from './api/session'
import App from './App'
import { fleetView, makeFakeApi, makeFakeRealtime, makeView } from './test/fixtures'

const lobby = makeView({ phase: 'Lobby', opponent: null })
const bob = { name: 'Bob', connected: true }

async function openRoom() {
  const api = makeFakeApi({ createGame: async () => ({ code: 'K7QF2', playerToken: 'secret', seat: 1, view: lobby }), getGame: async () => lobby })
  const server = makeFakeRealtime()
  render(<App api={api} realtime={server.client} />)
  await userEvent.type(screen.getByLabelText('Your name'), 'Ann')
  await userEvent.click(screen.getByRole('button', { name: 'Create a room' }))
  await screen.findByRole('heading', { name: 'Waiting for an opponent' })
  return { api, server }
}

describe('App with a friend online', () => {
  it('opens a room and shows its code while waiting', async () => {
    const { api } = await openRoom()

    expect(api.createGame).toHaveBeenCalledWith('Ann', 'Human')
    expect(screen.getByText('K7QF2')).toBeInTheDocument()
    expect(loadSession()).toEqual({ code: 'K7QF2', token: 'secret', online: true })
  })

  it('moves on to ship placement when the friend joins', async () => {
    const { server } = await openRoom()

    await server.emit({ type: 'PlayerJoined', name: 'Bob', view: makeView({ version: 2, phase: 'Placing', opponent: { name: 'Bob', connected: false } }) })

    expect(await screen.findByRole('heading', { name: 'Place your ships' })).toBeInTheDocument()
  })

  it('joins the room of a friend from the start screen', async () => {
    const placing = makeView({ version: 2, phase: 'Placing', you: { seat: 2, name: 'Bob' }, opponent: { name: 'Ann', connected: true } })
    const api = makeFakeApi({ joinGame: async () => ({ playerToken: 'guest', seat: 2, view: placing }), getGame: async () => placing })
    const server = makeFakeRealtime()
    render(<App api={api} realtime={server.client} />)

    await userEvent.type(screen.getByLabelText('Your name'), 'Bob')
    await userEvent.type(screen.getByLabelText('Room code'), 'k7qf2')
    await userEvent.click(screen.getByRole('button', { name: 'Join room' }))

    expect(await screen.findByRole('heading', { name: 'Place your ships' })).toBeInTheDocument()
    expect(api.joinGame).toHaveBeenCalledWith('K7QF2', 'Bob')
  })

  it('tells a player with a complete fleet to wait for the other fleet', async () => {
    const { server } = await openRoom()

    await server.emit({
      type: 'PlayerJoined',
      name: 'Bob',
      view: makeView({ version: 2, phase: 'Placing', opponent: bob, placement: { placed: ['Carrier', 'Battleship', 'Cruiser', 'Submarine', 'Destroyer'], remaining: [] } }),
    })

    expect(await screen.findByText(/Waiting for Bob to place theirs/)).toBeInTheDocument()
  })

  it('plays: the battle starts by itself and the opponent shot arrives as an event', async () => {
    const { server } = await openRoom()

    await server.emit({ type: 'GameStarted', view: fleetView({ version: 6, yourTurn: false, opponent: bob }) })
    expect(await screen.findByText('Waiting for Bob…')).toBeInTheDocument()

    await server.emit({
      type: 'ShotResolved',
      shooter: 'opponent',
      target: { x: 0, y: 0 },
      result: 'Hit',
      view: fleetView({ version: 7, yourTurn: true, opponent: bob }),
    })

    expect(await screen.findByText(/Your turn/)).toBeInTheDocument()
    expect(screen.getByText('Bob fired at A-1: Hit!')).toBeInTheDocument()
  })

  it('says when the opponent lost the connection', async () => {
    const { server } = await openRoom()
    await server.emit({ type: 'GameStarted', view: fleetView({ version: 6, opponent: bob }) })

    await server.emit({ type: 'OpponentConnection', connected: false, view: fleetView({ version: 7, opponent: { name: 'Bob', connected: false } }) })

    expect(await screen.findByText(/Bob lost the connection\. The game waits/)).toBeInTheDocument()
  })

  it('shows the win when the opponent left', async () => {
    const { server } = await openRoom()
    await server.emit({ type: 'GameStarted', view: fleetView({ version: 6, opponent: bob }) })

    await server.emit({
      type: 'GameEnded',
      winner: 'you',
      view: fleetView({ version: 8, phase: 'Finished', yourTurn: false, winner: 'you', endReason: 'Forfeit', opponent: { name: 'Bob', connected: false } }),
    })

    expect(await screen.findByText('Bob left the game. You win!', { selector: 'strong' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Play again' })).toBeInTheDocument()
  })

  it('shows the loss when the player was away too long', async () => {
    const { server } = await openRoom()
    await server.emit({ type: 'GameStarted', view: fleetView({ version: 6, opponent: bob }) })

    await server.emit({
      type: 'GameEnded',
      winner: 'opponent',
      view: fleetView({ version: 8, phase: 'Finished', yourTurn: false, winner: 'opponent', endReason: 'Forfeit', opponent: bob }),
    })

    expect(await screen.findByText('You were away too long. You lose.')).toBeInTheDocument()
  })

  it('shows the seconds left to fire and counts down', async () => {
    const { server } = await openRoom()

    await server.emit({ type: 'GameStarted', view: fleetView({ version: 6, opponent: bob, turnSecondsRemaining: 30 }) })

    expect(await screen.findByLabelText('30 seconds left')).toHaveTextContent('30s')
  })

  it('shows no clock in a game without one', async () => {
    const { server } = await openRoom()

    await server.emit({ type: 'GameStarted', view: fleetView({ version: 6, opponent: bob }) })

    await screen.findByText(/Your turn/)
    expect(screen.queryByLabelText(/seconds left/)).not.toBeInTheDocument()
  })

  it('shows the win when the opponent ran out of time', async () => {
    const { server } = await openRoom()
    await server.emit({ type: 'GameStarted', view: fleetView({ version: 6, opponent: bob, turnSecondsRemaining: 30 }) })

    await server.emit({
      type: 'GameEnded',
      winner: 'you',
      view: fleetView({ version: 8, phase: 'Finished', yourTurn: false, winner: 'you', endReason: 'Timeout', opponent: bob, turnSecondsRemaining: null }),
    })

    expect(await screen.findByText('Bob ran out of time. You win!', { selector: 'strong' })).toBeInTheDocument()
    expect(screen.queryByLabelText(/seconds left/)).not.toBeInTheDocument()
  })

  it('shows the loss when the player ran out of time', async () => {
    const { server } = await openRoom()
    await server.emit({ type: 'GameStarted', view: fleetView({ version: 6, opponent: bob, turnSecondsRemaining: 30 }) })

    await server.emit({
      type: 'GameEnded',
      winner: 'opponent',
      view: fleetView({ version: 8, phase: 'Finished', yourTurn: false, winner: 'opponent', endReason: 'Timeout', opponent: bob }),
    })

    expect(await screen.findByText('You ran out of time. You lose.')).toBeInTheDocument()
  })

  it('shows that it is reconnecting and offers a manual retry when the connection is lost', async () => {
    const { server } = await openRoom()

    await server.status('reconnecting')
    expect(screen.getByText('Connecting to the game…')).toBeInTheDocument()

    await server.status('offline')
    await userEvent.click(screen.getByRole('button', { name: 'Reconnect' }))

    expect(server.connect).toHaveBeenCalledTimes(2)
  })

  it('hides the connection notice while connected', async () => {
    const { server } = await openRoom()

    await server.status('connected')

    expect(screen.queryByText('Connecting to the game…')).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Reconnect' })).not.toBeInTheDocument()
  })

  it('leaves the room and goes back to the start screen', async () => {
    const { server } = await openRoom()

    await userEvent.click(screen.getByRole('button', { name: 'Leave room' }))

    expect(screen.getByRole('button', { name: 'Start game' })).toBeInTheDocument()
    expect(server.stop).toHaveBeenCalled()
    expect(loadSession()).toBeNull()
  })
})
