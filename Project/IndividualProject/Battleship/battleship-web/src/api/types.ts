// Shapes of the API described in WEB_DESIGN.md. Enums are plain strings, as the server sends them.

export type ShipType = 'Carrier' | 'Battleship' | 'Cruiser' | 'Submarine' | 'Destroyer'
export type Orientation = 'Horizontal' | 'Vertical'
export type ShotResult = 'Hit' | 'Miss' | 'Sunk' | 'Invalid'
export type GamePhase = 'Lobby' | 'Placing' | 'InProgress' | 'Finished'
export type OwnCell = 'Water' | 'Ship' | 'Hit' | 'Miss'

/** There is deliberately no value for a ship: the server never says where enemy ships are. */
export type EnemyCell = 'Unknown' | 'Hit' | 'Miss'

export type OpponentKind = 'Random' | 'HuntTarget'

/** x is the column and y is the row, both zero-based. */
export interface Coordinate {
  x: number
  y: number
}

export interface ShipView {
  type: ShipType
  length: number
  orientation: Orientation
  positions: Coordinate[]
  hitCount: number
  isSunk: boolean
}

/** Boards are indexed cells[y][x]: first the row, then the column. */
export interface OwnBoardView {
  size: number
  cells: OwnCell[][]
}

export interface EnemyBoardView {
  size: number
  cells: EnemyCell[][]
}

export interface GameView {
  code: string
  version: number
  phase: GamePhase
  you: { seat: number; name: string }
  opponent: { name: string; connected: boolean } | null
  yourTurn: boolean
  winner: 'you' | 'opponent' | null
  placement: { placed: ShipType[]; remaining: ShipType[] }
  ownBoard: OwnBoardView
  ownShips: ShipView[]
  enemyBoard: EnemyBoardView
  enemyShipsRemaining: number
}

export interface CreateGameResponse {
  code: string
  playerToken: string
  seat: number
  view: GameView
}

export interface BotShot {
  target: Coordinate
  result: ShotResult
}

export interface ShotResponse {
  result: ShotResult
  botShots: BotShot[]
  view: GameView
}
