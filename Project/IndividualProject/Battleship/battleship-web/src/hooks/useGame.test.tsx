import { act, renderHook, waitFor } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { ApiError, type GameApi } from '../api/client'
import { loadSession, saveSession } from '../api/session'
import type { ShotResponse } from '../api/types'
import { fleetView, makeFakeApi, makeView } from '../test/fixtures'
import { useGame } from './useGame'

const created = { code: 'K7QF2', playerToken: 'secret', seat: 1, view: makeView() }

function shot(version: number, result: ShotResponse['result'] = 'Miss'): ShotResponse {
  return { result, botShots: [{ target: { x: 9, y: 9 }, result: 'Miss' }], view: fleetView({ version }) }
}

async function startedGame(api: GameApi) {
  const hook = renderHook(() => useGame(api))
  await act(() => hook.result.current.start('Ann', 'Random'))
  return hook
}

describe('useGame', () => {
  it('starts with no game', () => {
    const { result } = renderHook(() => useGame(makeFakeApi()))

    expect(result.current.view).toBeNull()
    expect(result.current.resuming).toBe(false)
    expect(result.current.error).toBeNull()
  })

  it('stores the session when a game starts', async () => {
    const api = makeFakeApi({ createGame: async () => created })

    const { result } = await startedGame(api)

    expect(result.current.view?.code).toBe('K7QF2')
    expect(loadSession()).toEqual({ code: 'K7QF2', token: 'secret' })
  })

  it('shows the message of a failed start and stays without a game', async () => {
    const api = makeFakeApi({ createGame: async () => Promise.reject(new ApiError(400, 'playerName must be 1 to 20 characters.')) })

    const { result } = await startedGame(api)

    expect(result.current.view).toBeNull()
    expect(result.current.error).toBe('playerName must be 1 to 20 characters.')
    expect(result.current.busy).toBe(false)
  })

  it('writes the shots in the log, the player first and then the bot', async () => {
    const api = makeFakeApi({ createGame: async () => created, fire: async () => shot(7, 'Hit') })
    const { result } = await startedGame(api)

    await act(() => result.current.fire({ x: 3, y: 3 }))

    expect(result.current.log.map((entry) => entry.text)).toEqual([
      'You fired at D-4: Hit!',
      'Bot Random fired at J-10: Miss.',
    ])
    expect(result.current.lastPlayerShot).toEqual({ x: 3, y: 3 })
    expect(result.current.lastBotShots).toEqual([{ x: 9, y: 9 }])
  })

  it('keeps only the latest eight messages', async () => {
    let version = 6
    const api = makeFakeApi({ createGame: async () => created, fire: async () => shot(++version) })
    const { result } = await startedGame(api)

    for (let i = 0; i < 6; i++) {
      await act(() => result.current.fire({ x: i, y: 0 }))
    }

    expect(result.current.log).toHaveLength(8)
    expect(result.current.log.at(-1)?.text).toContain('Bot Random fired at')
  })

  it('announces the winner in the log', async () => {
    const finished = { ...shot(9, 'Sunk'), view: fleetView({ version: 9, phase: 'Finished', winner: 'you' as const, yourTurn: false }) }
    const api = makeFakeApi({ createGame: async () => created, fire: async () => finished })
    const { result } = await startedGame(api)

    await act(() => result.current.fire({ x: 0, y: 0 }))

    expect(result.current.log.at(-1)).toMatchObject({ kind: 'info', text: 'You win the game!' })
  })

  it('does not take an older view over a newer one', async () => {
    let call = 0
    const api = makeFakeApi({
      createGame: async () => created,
      fire: async () => shot(call++ === 0 ? 20 : 10),
    })
    const { result } = await startedGame(api)

    await act(() => result.current.fire({ x: 0, y: 0 }))
    await act(() => result.current.fire({ x: 1, y: 0 }))

    expect(result.current.view?.version).toBe(20)
  })

  it('does not mark the last shot when the server says it was invalid', async () => {
    const api = makeFakeApi({
      createGame: async () => created,
      fire: async () => ({ result: 'Invalid' as const, botShots: [], view: fleetView() }),
    })
    const { result } = await startedGame(api)

    await act(() => result.current.fire({ x: 12, y: 0 }))

    expect(result.current.lastPlayerShot).toBeNull()
    expect(result.current.lastBotShots).toEqual([])
  })

  it('forgets the game when the server no longer knows it', async () => {
    const api = makeFakeApi({ createGame: async () => created, fire: async () => Promise.reject(new ApiError(404, 'There is no game with this code.')) })
    const { result } = await startedGame(api)

    await act(() => result.current.fire({ x: 0, y: 0 }))

    expect(result.current.view).toBeNull()
    expect(result.current.error).toMatch(/no longer exists/)
    expect(loadSession()).toBeNull()
  })

  it('keeps the game and shows the message for an ordinary error', async () => {
    const api = makeFakeApi({ createGame: async () => created, placeShipsRandomly: async () => Promise.reject(new ApiError(409, 'Nope.')) })
    const { result } = await startedGame(api)

    await act(() => result.current.placeRandomly())

    expect(result.current.view).not.toBeNull()
    expect(result.current.error).toBe('Nope.')
    expect(loadSession()).not.toBeNull()
  })

  it('shows a plain message for an error it does not know', async () => {
    const api = makeFakeApi({ createGame: async () => created, placeShipsRandomly: async () => Promise.reject(new Error('boom')) })
    const { result } = await startedGame(api)

    await act(() => result.current.placeRandomly())

    expect(result.current.error).toBe('Something went wrong. Please try again.')
  })

  it('clears an error when the next action starts', async () => {
    let fail = true
    const api = makeFakeApi({
      createGame: async () => created,
      placeShipsRandomly: async () => (fail ? Promise.reject(new ApiError(409, 'Nope.')) : fleetView()),
    })
    const { result } = await startedGame(api)
    await act(() => result.current.placeRandomly())
    fail = false

    await act(() => result.current.placeRandomly())

    expect(result.current.error).toBeNull()
  })

  it('can dismiss an error', async () => {
    const api = makeFakeApi({ createGame: async () => Promise.reject(new ApiError(400, 'Bad.')) })
    const { result } = await startedGame(api)

    act(() => result.current.dismissError())

    expect(result.current.error).toBeNull()
  })

  it('resets to the start screen and forgets the session', async () => {
    const api = makeFakeApi({ createGame: async () => created, fire: async () => shot(7) })
    const { result } = await startedGame(api)
    await act(() => result.current.fire({ x: 0, y: 0 }))

    act(() => result.current.reset())

    expect(result.current.view).toBeNull()
    expect(result.current.log).toEqual([])
    expect(result.current.lastPlayerShot).toBeNull()
    expect(loadSession()).toBeNull()
  })

  it('resumes the saved game', async () => {
    saveSession({ code: 'K7QF2', token: 'secret' })
    const api = makeFakeApi({ getGame: async () => fleetView() })

    const { result } = renderHook(() => useGame(api))

    expect(result.current.resuming).toBe(true)
    await waitFor(() => expect(result.current.resuming).toBe(false))
    expect(result.current.view?.phase).toBe('InProgress')
  })

  it('starts fresh when the saved game is gone', async () => {
    saveSession({ code: 'K7QF2', token: 'secret' })
    const api = makeFakeApi({ getGame: async () => Promise.reject(new ApiError(404, 'There is no game with this code.')) })

    const { result } = renderHook(() => useGame(api))

    await waitFor(() => expect(result.current.resuming).toBe(false))
    expect(result.current.view).toBeNull()
    expect(result.current.error).toMatch(/no longer exists/)
    expect(loadSession()).toBeNull()
  })

  it('gives the token and code of the game to every call', async () => {
    const seen: string[] = []
    const api = makeFakeApi({
      createGame: async () => created,
      placeShip: async (code, token) => {
        seen.push(`${code}/${token}`)
        return makeView({ version: 2 })
      },
    })
    const { result } = await startedGame(api)

    await act(() => result.current.placeShip('Carrier', { x: 0, y: 0 }, 'Horizontal'))

    expect(seen).toEqual(['K7QF2/secret'])
  })
})
