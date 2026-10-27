import { act } from '@testing-library/react'
import { vi } from 'vitest'
import type { GameApi } from '../api/client'
import type { ConnectionStatus, GameEvent, RealtimeClient, RealtimeConnection, RealtimeHandlers } from '../api/realtime'
import type { EnemyCell, GameView, OwnCell, ShipType, ShipView } from '../api/types'

export const SHIP_TYPES: ShipType[] = ['Carrier', 'Battleship', 'Cruiser', 'Submarine', 'Destroyer']
const LENGTHS: Record<ShipType, number> = { Carrier: 5, Battleship: 4, Cruiser: 3, Submarine: 3, Destroyer: 2 }

export function grid<T>(value: T, size = 10): T[][] {
  return Array.from({ length: size }, () => Array.from({ length: size }, () => value))
}

export function makeView(overrides: Partial<GameView> = {}): GameView {
  return {
    code: 'K7QF2',
    version: 1,
    phase: 'Placing',
    you: { seat: 1, name: 'Ann' },
    opponent: { name: 'Bot Random', connected: true },
    yourTurn: false,
    winner: null,
    placement: { placed: [], remaining: [...SHIP_TYPES] },
    ownBoard: { size: 10, cells: grid<OwnCell>('Water') },
    ownShips: [],
    enemyBoard: { size: 10, cells: grid<EnemyCell>('Unknown') },
    enemyShipsRemaining: 5,
    ...overrides,
  }
}

export function fleetView(overrides: Partial<GameView> = {}): GameView {
  const cells = grid<OwnCell>('Water')
  const ships: ShipView[] = SHIP_TYPES.map((type, y) => {
    const positions = Array.from({ length: LENGTHS[type] }, (_, x) => ({ x, y }))
    positions.forEach(({ x }) => {
      cells[y][x] = 'Ship'
    })
    return { type, length: LENGTHS[type], orientation: 'Horizontal', positions, hitCount: 0, isSunk: false }
  })

  return makeView({
    version: 6,
    phase: 'InProgress',
    yourTurn: true,
    placement: { placed: [...SHIP_TYPES], remaining: [] },
    ownBoard: { size: 10, cells },
    ownShips: ships,
    ...overrides,
  })
}

export function withEnemyCell(view: GameView, x: number, y: number, value: EnemyCell): GameView {
  const cells = view.enemyBoard.cells.map((row) => [...row])
  cells[y][x] = value
  return { ...view, enemyBoard: { size: view.enemyBoard.size, cells } }
}

export function makeFakeApi(overrides: Partial<GameApi> = {}): GameApi {
  const unexpected = () => Promise.reject(new Error('Unexpected API call'))
  return {
    createGame: vi.fn(overrides.createGame ?? unexpected),
    joinGame: vi.fn(overrides.joinGame ?? unexpected),
    getGame: vi.fn(overrides.getGame ?? unexpected),
    placeShip: vi.fn(overrides.placeShip ?? unexpected),
    placeShipsRandomly: vi.fn(overrides.placeShipsRandomly ?? unexpected),
    fire: vi.fn(overrides.fire ?? unexpected),
    sendEmote: vi.fn(overrides.sendEmote ?? unexpected),
  }
}

export function makeFakeRealtime() {
  const stop = vi.fn(async () => {})
  let handlers: RealtimeHandlers | null = null
  const connect = vi.fn((_code: string, _token: string, given: RealtimeHandlers): RealtimeConnection => {
    handlers = given
    return { stop }
  })
  const current = () => {
    if (!handlers) {
      throw new Error('The game has not connected yet')
    }
    return handlers
  }

  return {
    client: { connect } as RealtimeClient,
    connect,
    stop,
    emit: (event: GameEvent) => act(() => current().onEvent(event)),
    status: (value: ConnectionStatus) => act(() => current().onStatus(value)),
    gone: () => act(() => current().onGone()),
  }
}
