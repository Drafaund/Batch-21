import { afterEach, describe, expect, it, vi } from 'vitest'
import { api, ApiError } from './client'
import { makeView } from '../test/fixtures'

function respond(status: number, body: unknown): Response {
  return new Response(typeof body === 'string' ? body : JSON.stringify(body), {
    status,
    headers: { 'Content-Type': status >= 400 ? 'application/problem+json' : 'application/json' },
  })
}

function stubFetch(response: Response | Error) {
  const fetchMock = vi.fn(() => (response instanceof Error ? Promise.reject(response) : Promise.resolve(response)))
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('api', () => {
  it('creates a game with a JSON body and no token', async () => {
    const created = { code: 'K7QF2', playerToken: 'secret', seat: 1, view: makeView() }
    const fetchMock = stubFetch(respond(201, created))

    const result = await api.createGame('Ann', 'HuntTarget')

    expect(result).toEqual(created)
    const [url, init] = fetchMock.mock.calls[0] as unknown as [string, RequestInit]
    expect(url).toBe('/api/games')
    expect(init.method).toBe('POST')
    expect(JSON.parse(init.body as string)).toEqual({ playerName: 'Ann', opponent: 'HuntTarget' })
    expect(init.headers).toEqual({ 'Content-Type': 'application/json' })
  })

  it('reads a game with the token header and no body', async () => {
    const fetchMock = stubFetch(respond(200, makeView()))

    await api.getGame('K7QF2', 'secret')

    const [url, init] = fetchMock.mock.calls[0] as unknown as [string, RequestInit]
    expect(url).toBe('/api/games/K7QF2')
    expect(init.method).toBe('GET')
    expect(init.headers).toEqual({ 'X-Player-Token': 'secret' })
    expect(init.body).toBeUndefined()
  })

  it('sends the ship to place', async () => {
    const fetchMock = stubFetch(respond(200, makeView()))

    await api.placeShip('K7QF2', 'secret', 'Cruiser', { x: 2, y: 4 }, 'Vertical')

    const [url, init] = fetchMock.mock.calls[0] as unknown as [string, RequestInit]
    expect(url).toBe('/api/games/K7QF2/ships')
    expect(JSON.parse(init.body as string)).toEqual({ type: 'Cruiser', start: { x: 2, y: 4 }, orientation: 'Vertical' })
  })

  it('asks for random placement without a body', async () => {
    const fetchMock = stubFetch(respond(200, makeView()))

    await api.placeShipsRandomly('K7QF2', 'secret')

    const [url, init] = fetchMock.mock.calls[0] as unknown as [string, RequestInit]
    expect(url).toBe('/api/games/K7QF2/ships/random')
    expect(init.method).toBe('POST')
    expect(init.body).toBeUndefined()
  })

  it('sends the target of a shot', async () => {
    const fetchMock = stubFetch(respond(200, { result: 'Miss', botShots: [], view: makeView() }))

    const result = await api.fire('K7QF2', 'secret', { x: 3, y: 3 })

    expect(result.result).toBe('Miss')
    const [url, init] = fetchMock.mock.calls[0] as unknown as [string, RequestInit]
    expect(url).toBe('/api/games/K7QF2/shots')
    expect(JSON.parse(init.body as string)).toEqual({ target: { x: 3, y: 3 } })
  })

  it('escapes the game code in the path', async () => {
    const fetchMock = stubFetch(respond(200, makeView()))

    await api.getGame('a/b', 'secret')

    expect((fetchMock.mock.calls[0] as unknown as [string])[0]).toBe('/api/games/a%2Fb')
  })

  it('turns problem details into an ApiError with the detail as the message', async () => {
    stubFetch(respond(409, { title: 'Conflict', status: 409, detail: 'It is not your turn.' }))

    const failure = await api.fire('K7QF2', 'secret', { x: 0, y: 0 }).catch((caught: unknown) => caught)

    expect(failure).toBeInstanceOf(ApiError)
    expect((failure as ApiError).status).toBe(409)
    expect((failure as ApiError).message).toBe('It is not your turn.')
  })

  it('falls back to the title when the problem has no detail', async () => {
    stubFetch(respond(404, { title: 'Not found', status: 404 }))

    const failure = await api.getGame('K7QF2', 'x').catch((caught: unknown) => caught)

    expect((failure as ApiError).message).toBe('Not found')
  })

  it('gives a generic message when the error body is not JSON', async () => {
    stubFetch(respond(502, '<html>Bad gateway</html>'))

    const failure = await api.getGame('K7QF2', 'x').catch((caught: unknown) => caught)

    expect((failure as ApiError).status).toBe(502)
    expect((failure as ApiError).message).toContain('502')
  })

  it('reports an unreachable server with status 0', async () => {
    stubFetch(new TypeError('Failed to fetch'))

    const failure = await api.createGame('Ann', 'Random').catch((caught: unknown) => caught)

    expect(failure).toBeInstanceOf(ApiError)
    expect((failure as ApiError).status).toBe(0)
    expect((failure as ApiError).message).toMatch(/cannot reach the server/i)
  })
})
