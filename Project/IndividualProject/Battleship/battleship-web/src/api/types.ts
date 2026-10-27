
export type ShipType = 'Carrier' | 'Battleship' | 'Cruiser' | 'Submarine' | 'Destroyer'
export type Orientation = 'Horizontal' | 'Vertical'
export type ShotResult = 'Hit' | 'Miss' | 'Sunk' | 'Invalid'
export type GamePhase = 'Lobby' | 'Placing' | 'InProgress' | 'Finished'
export type OwnCell = 'Water' | 'Ship' | 'Hit' | 'Miss'

export type EnemyCell = 'Unknown' | 'Hit' | 'Miss' | 'Sunk'

export type OpponentKind = 'Random' | 'HuntTarget' | 'Human'

export type EmoteCode = 'thumbsUp' | 'laugh' | 'wow' | 'angry' | 'target' | 'wave' | 'handshake' | 'clap'

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
  endReason?: 'Forfeit' | 'Timeout' | null
  turnSecondsRemaining?: number | null
}

export interface CreateGameResponse {
  code: string
  playerToken: string
  seat: number
  view: GameView
}

export interface JoinGameResponse {
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
