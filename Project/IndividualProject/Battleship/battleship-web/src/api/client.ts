import type {
  Coordinate,
  CreateGameResponse,
  EmoteCode,
  GameView,
  JoinGameResponse,
  OpponentKind,
  Orientation,
  ShipType,
  ShotResponse,
} from './types'

export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

export interface GameApi {
  createGame(playerName: string, opponent: OpponentKind): Promise<CreateGameResponse>
  joinGame(code: string, playerName: string): Promise<JoinGameResponse>
  getGame(code: string, token: string): Promise<GameView>
  placeShip(code: string, token: string, type: ShipType, start: Coordinate, orientation: Orientation): Promise<GameView>
  placeShipsRandomly(code: string, token: string): Promise<GameView>
  fire(code: string, token: string, target: Coordinate): Promise<ShotResponse>
  sendEmote(code: string, token: string, emote: EmoteCode): Promise<void>
}

const TOKEN_HEADER = 'X-Player-Token'

async function request<T>(method: string, path: string, token?: string, body?: unknown): Promise<T> {
  const headers: Record<string, string> = {}
  if (token) {
    headers[TOKEN_HEADER] = token
  }
  if (body !== undefined) {
    headers['Content-Type'] = 'application/json'
  }

  let response: Response
  try {
    response = await fetch(`/api${path}`, {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
    })
  } catch {
    throw new ApiError(0, 'Cannot reach the server. Check your connection and try again.')
  }

  if (!response.ok) {
    const problem = (await response.json().catch(() => null)) as { detail?: string; title?: string } | null
    throw new ApiError(response.status, problem?.detail ?? problem?.title ?? `The server answered with an error (${response.status}).`)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}

const gamePath = (code: string) => `/games/${encodeURIComponent(code)}`

export const api: GameApi = {
  createGame: (playerName, opponent) => request('POST', '/games', undefined, { playerName, opponent }),
  joinGame: (code, playerName) => request('POST', `${gamePath(code)}/join`, undefined, { playerName }),
  getGame: (code, token) => request('GET', gamePath(code), token),
  placeShip: (code, token, type, start, orientation) =>
    request('POST', `${gamePath(code)}/ships`, token, { type, start, orientation }),
  placeShipsRandomly: (code, token) => request('POST', `${gamePath(code)}/ships/random`, token),
  fire: (code, token, target) => request('POST', `${gamePath(code)}/shots`, token, { target }),
  sendEmote: (code, token, emote) => request('POST', `${gamePath(code)}/emotes`, token, { emote }),
}
