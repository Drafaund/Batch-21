import { HttpError, HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr'
import type { Coordinate, EmoteCode, GameView, ShotResult } from './types'

export type GameEvent =
  | { type: 'PlayerJoined'; name: string; view: GameView }
  | { type: 'GameStarted'; view: GameView }
  | { type: 'ShotResolved'; shooter: 'you' | 'opponent'; target: Coordinate; result: ShotResult; view: GameView }
  | { type: 'GameEnded'; winner: 'you' | 'opponent'; view: GameView }
  | { type: 'OpponentConnection'; connected: boolean; view: GameView }
  | { type: 'EmoteReceived'; emote: EmoteCode }

export type ConnectionStatus = 'connecting' | 'connected' | 'reconnecting' | 'offline'

export interface RealtimeHandlers {
  onEvent(event: GameEvent): void
  onStatus(status: ConnectionStatus): void
  onGone(): void
}

export interface RealtimeConnection {
  stop(): Promise<void>
}

export interface RealtimeClient {
  connect(code: string, token: string, handlers: RealtimeHandlers): RealtimeConnection
}

const EVENT_NAMES = ['PlayerJoined', 'GameStarted', 'ShotResolved', 'GameEnded', 'OpponentConnection', 'EmoteReceived'] as const
const FIRST_CONNECT_RETRY_MS = 2000
const RECONNECT_WINDOW_MS = 120_000
const RECONNECT_DELAY_MS = 2000

function isRejection(error: unknown): boolean {
  return error instanceof HttpError && (error.statusCode === 401 || error.statusCode === 404)
}

export const realtime: RealtimeClient = {
  connect(code, token, handlers) {
    const connection: HubConnection = new HubConnectionBuilder()
      .withUrl(`/hubs/game?code=${encodeURIComponent(code)}`, { accessTokenFactory: () => token })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (context) => (context.elapsedMilliseconds < RECONNECT_WINDOW_MS ? RECONNECT_DELAY_MS : null),
      })
      .configureLogging(LogLevel.None)
      .build()

    let stopped = false
    let retryTimer: ReturnType<typeof setTimeout> | undefined

    for (const name of EVENT_NAMES) {
      connection.on(name, (payload: object) => handlers.onEvent({ type: name, ...payload } as GameEvent))
    }
    connection.onreconnecting(() => handlers.onStatus('reconnecting'))
    connection.onreconnected(() => handlers.onStatus('connected'))
    connection.onclose(() => {
      if (!stopped) {
        handlers.onStatus('offline')
      }
    })

    async function start() {
      handlers.onStatus('connecting')
      try {
        await connection.start()
        if (!stopped) {
          handlers.onStatus('connected')
        }
      } catch (error) {
        if (stopped) {
          return
        }
        if (isRejection(error)) {
          handlers.onGone()
          return
        }
        handlers.onStatus('reconnecting')
        retryTimer = setTimeout(() => void start(), FIRST_CONNECT_RETRY_MS)
      }
    }

    retryTimer = setTimeout(() => void start(), 0)

    return {
      async stop() {
        stopped = true
        clearTimeout(retryTimer)
        if (connection.state !== HubConnectionState.Disconnected) {
          await connection.stop()
        }
      },
    }
  },
}
