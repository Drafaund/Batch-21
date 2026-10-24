import { act, renderHook, waitFor } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { ApiError } from '../api/client'
import { loadSession, saveSession } from '../api/session'
import { fleetView, makeFakeApi, makeFakeRealtime, makeView } from '../test/fixtures'
import { useGame } from './useGame'

const lobby = makeView({ phase: 'Lobby', opponent: null, you: { seat: 1, name: 'Ann' } })
const created = { code: 'K7QF2', playerToken: 'secret', seat: 1, view: lobby }
const placing = makeView({ version: 3, phase: 'Placing', opponent: { name: 'Bob', connected: false } })

/** Ann opens a room: she is in the lobby and the game is connected to the fake server. */
async function openedRoom(overrides = {}) {
  const api = makeFakeApi({ createGame: async () => created, getGame: async () => lobby, ...overrides })
  const server = makeFakeRealtime()
  const hook = renderHook(() => useGame(api, server.client))
  await act(() => hook.result.current.start('Ann', 'Human'))
  return { api, server, hook }
}

describe('useGame in a room with another human', () => {
  it('opens a room, remembers it as online, and connects with the code and token', async () => {
    const { api, server, hook } = await openedRoom()

    expect(api.createGame).toHaveBeenCalledWith('Ann', 'Human')
    expect(hook.result.current.online).toBe(true)
    expect(hook.result.current.view?.phase).toBe('Lobby')
    expect(loadSession()).toEqual({ code: 'K7QF2', token: 'secret', online: true })
    expect(server.connect).toHaveBeenCalledWith('K7QF2', 'secret', expect.any(Object))
  })

  it('does not open a connection for a game against a bot', async () => {
    const api = makeFakeApi({ createGame: async () => ({ ...created, view: makeView() }) })
    const server = makeFakeRealtime()
    const hook = renderHook(() => useGame(api, server.client))

    await act(() => hook.result.current.start('Ann', 'Random'))

    expect(hook.result.current.online).toBe(false)
    expect(server.connect).not.toHaveBeenCalled()
    expect(loadSession()).toEqual({ code: 'K7QF2', token: 'secret' })
  })

  it('joins a room with the code in capitals and connects', async () => {
    const api = makeFakeApi({ joinGame: async () => ({ playerToken: 'guest-token', seat: 2, view: placing }) })
    const server = makeFakeRealtime()
    const hook = renderHook(() => useGame(api, server.client))

    await act(() => hook.result.current.join('Bob', ' k7qf2 '))

    expect(api.joinGame).toHaveBeenCalledWith('K7QF2', 'Bob')
    expect(hook.result.current.view?.phase).toBe('Placing')
    expect(loadSession()).toEqual({ code: 'K7QF2', token: 'guest-token', online: true })
    expect(server.connect).toHaveBeenCalledWith('K7QF2', 'guest-token', expect.any(Object))
  })

  it('shows the message when the room is full and stays on the start screen', async () => {
    const api = makeFakeApi({ joinGame: async () => Promise.reject(new ApiError(409, 'This room is already full.')) })
    const server = makeFakeRealtime()
    const hook = renderHook(() => useGame(api, server.client))

    await act(() => hook.result.current.join('Eve', 'K7QF2'))

    expect(hook.result.current.error).toBe('This room is already full.')
    expect(hook.result.current.view).toBeNull()
    expect(server.connect).not.toHaveBeenCalled()
  })

  it('shows the message of the server for a code nobody opened instead of saying the game is gone', async () => {
    const api = makeFakeApi({ joinGame: async () => Promise.reject(new ApiError(404, 'There is no game with this code.')) })
    const hook = renderHook(() => useGame(api, makeFakeRealtime().client))

    await act(() => hook.result.current.join('Eve', 'ZZZZZ'))

    expect(hook.result.current.error).toBe('There is no game with this code.')
    expect(hook.result.current.view).toBeNull()
  })

  it('moves on when the opponent joins and writes it in the log', async () => {
    const { server, hook } = await openedRoom()

    await server.emit({ type: 'PlayerJoined', name: 'Bob', view: placing })

    expect(hook.result.current.view?.phase).toBe('Placing')
    expect(hook.result.current.log.at(-1)).toMatchObject({ kind: 'info', text: 'Bob joined the room.' })
  })

  it('starts the battle when the server says both fleets are ready', async () => {
    const { server, hook } = await openedRoom()

    await server.emit({ type: 'GameStarted', view: fleetView({ version: 6, opponent: { name: 'Bob', connected: true } }) })

    expect(hook.result.current.view?.phase).toBe('InProgress')
    expect(hook.result.current.view?.yourTurn).toBe(true)
  })

  it('logs and highlights a shot of the opponent but not one of its own', async () => {
    const { server, hook } = await openedRoom()
    const view = fleetView({ version: 7, opponent: { name: 'Bob', connected: true } })

    await server.emit({ type: 'ShotResolved', shooter: 'opponent', target: { x: 3, y: 3 }, result: 'Hit', view })
    await server.emit({ type: 'ShotResolved', shooter: 'you', target: { x: 0, y: 0 }, result: 'Miss', view: { ...view, version: 8 } })

    expect(hook.result.current.log.map((entry) => entry.text)).toEqual(['Bob fired at D-4: Hit!'])
    expect(hook.result.current.lastBotShots).toEqual([{ x: 3, y: 3 }])
    expect(hook.result.current.view?.version).toBe(8)
  })

  it('writes its own shot in the log from the answer and keeps the highlight of the opponent shot', async () => {
    const { server, hook } = await openedRoom({
      fire: async () => ({ result: 'Miss' as const, botShots: [], view: fleetView({ version: 9, yourTurn: false, opponent: { name: 'Bob', connected: true } }) }),
    })
    await server.emit({
      type: 'ShotResolved',
      shooter: 'opponent',
      target: { x: 3, y: 3 },
      result: 'Miss',
      view: fleetView({ version: 7, opponent: { name: 'Bob', connected: true } }),
    })

    await act(() => hook.result.current.fire({ x: 5, y: 5 }))

    expect(hook.result.current.log.map((entry) => entry.text)).toEqual(['Bob fired at D-4: Miss.', 'You fired at F-6: Miss.'])
    expect(hook.result.current.lastPlayerShot).toEqual({ x: 5, y: 5 })
    expect(hook.result.current.lastBotShots).toEqual([{ x: 3, y: 3 }])
  })

  it('ignores an event that is older than the view on screen', async () => {
    const { server, hook } = await openedRoom()
    await server.emit({ type: 'GameStarted', view: fleetView({ version: 6 }) })

    await server.emit({ type: 'OpponentConnection', connected: true, view: placing })

    expect(hook.result.current.view?.version).toBe(6)
  })

  it('tells who won and who left', async () => {
    const { server, hook } = await openedRoom()
    const finished = fleetView({ version: 12, phase: 'Finished', yourTurn: false, opponent: { name: 'Bob', connected: true } })

    await server.emit({ type: 'GameEnded', winner: 'opponent', view: { ...finished, winner: 'opponent' } })
    await server.emit({ type: 'GameEnded', winner: 'you', view: { ...finished, version: 13, winner: 'you', endReason: 'Forfeit' } })

    expect(hook.result.current.log.map((entry) => entry.text)).toEqual(['Bob wins the game.', 'Bob left the game. You win!'])
  })

  it('tells who ran out of time', async () => {
    const { server, hook } = await openedRoom()
    const finished = fleetView({ version: 12, phase: 'Finished', yourTurn: false, endReason: 'Timeout', opponent: { name: 'Bob', connected: true } })

    await server.emit({ type: 'GameEnded', winner: 'you', view: { ...finished, winner: 'you' } })
    await server.emit({ type: 'GameEnded', winner: 'opponent', view: { ...finished, version: 13, winner: 'opponent' } })

    expect(hook.result.current.log.map((entry) => entry.text)).toEqual(['Bob ran out of time. You win!', 'You ran out of time and lost the game.'])
  })

  it('writes the loss of the connection of the opponent in the log', async () => {
    const { server, hook } = await openedRoom()

    await server.emit({ type: 'OpponentConnection', connected: false, view: { ...placing, version: 4 } })
    await server.emit({ type: 'OpponentConnection', connected: true, view: { ...placing, version: 5, opponent: { name: 'Bob', connected: true } } })

    expect(hook.result.current.log.map((entry) => entry.text)).toEqual(['Bob lost the connection.', 'Bob connected.'])
  })

  it('follows the state of the connection', async () => {
    const { server, hook } = await openedRoom()

    await server.status('reconnecting')
    expect(hook.result.current.connection).toBe('reconnecting')

    await server.status('connected')
    expect(hook.result.current.connection).toBe('connected')

    await server.status('offline')
    expect(hook.result.current.connection).toBe('offline')
  })

  it('asks the server for the current state every time the connection is back', async () => {
    const newer = fleetView({ version: 20, opponent: { name: 'Bob', connected: true } })
    const { api, server, hook } = await openedRoom({ getGame: async () => newer })

    await server.status('connected')

    await waitFor(() => expect(hook.result.current.view?.version).toBe(20))
    expect(api.getGame).toHaveBeenLastCalledWith('K7QF2', 'secret')
  })

  it('forgets the game when the server refuses the connection', async () => {
    const { server, hook } = await openedRoom()

    await server.gone()

    expect(hook.result.current.view).toBeNull()
    expect(hook.result.current.error).toMatch(/no longer exists/)
    expect(loadSession()).toBeNull()
    expect(server.stop).toHaveBeenCalled()
  })

  it('opens the connection again on request', async () => {
    const { server, hook } = await openedRoom()
    expect(server.connect).toHaveBeenCalledTimes(1)

    act(() => hook.result.current.reconnect())

    expect(server.stop).toHaveBeenCalledTimes(1)
    expect(server.connect).toHaveBeenCalledTimes(2)
  })

  it('closes the connection when the player leaves the room', async () => {
    const { server, hook } = await openedRoom()

    act(() => hook.result.current.reset())

    expect(server.stop).toHaveBeenCalled()
    expect(hook.result.current.online).toBe(false)
    expect(hook.result.current.view).toBeNull()
  })

  it('reconnects to a saved room after a reload', async () => {
    saveSession({ code: 'K7QF2', token: 'secret', online: true })
    const api = makeFakeApi({ getGame: async () => fleetView({ version: 15, opponent: { name: 'Bob', connected: true } }) })
    const server = makeFakeRealtime()

    const hook = renderHook(() => useGame(api, server.client))

    await waitFor(() => expect(hook.result.current.view?.version).toBe(15))
    expect(server.connect).toHaveBeenCalledWith('K7QF2', 'secret', expect.any(Object))
  })
})
