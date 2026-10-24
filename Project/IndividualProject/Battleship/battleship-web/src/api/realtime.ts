import { HttpError, HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr'
import type { Coordinate, EmoteCode, GameView, ShotResult } from './types'

/** The events of WEB_DESIGN.md. Every one except `EmoteReceived` carries the full view of this player, so a client can just replace its state. */
export type GameEvent =
  | { type: 'PlayerJoined'; name: string; view: GameView }
  | { type: 'GameStarted'; view: GameView }
  | { type: 'ShotResolved'; shooter: 'you' | 'opponent'; target: Coordinate; result: ShotResult; view: GameView }
  | { type: 'GameEnded'; winner: 'you' | 'opponent'; view: GameView }
  | { type: 'OpponentConnection'; connected: boolean; view: GameView }
  /** An emote does not change the game, so it has no view. */
  | { type: 'EmoteReceived'; emote: EmoteCode }

export type ConnectionStatus = 'connecting' | 'connected' | 'reconnecting' | 'offline'

export interface RealtimeHandlers {
  onEvent(event: GameEvent): void
  /** Called with `connected` at the first connection and after every reconnect, so the client can resynchronise. */
  onStatus(status: ConnectionStatus): void
  /** The server refused the connection because the game or the token is unknown. */
  onGone(): void
}

export interface RealtimeConnection {
  stop(): Promise<void>
}

/** What the game needs from SignalR. Tests replace it with a fake. */
export interface RealtimeClient {
  connect(code: string, token: string, handlers: RealtimeHandlers): RealtimeConnection
}

const EVENT_NAMES = ['PlayerJoined', 'GameStarted', 'ShotResolved', 'GameEnded', 'OpponentConnection', 'EmoteReceived'] as const
const FIRST_CONNECT_RETRY_MS = 2000
// After a drop the client keeps trying for two minutes, as long as the server waits for a missing player.
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
        // The server may be waking up or the network may be down: keep trying.
        handlers.onStatus('reconnecting')
        retryTimer = setTimeout(() => void start(), FIRST_CONNECT_RETRY_MS)
      }
    }

    // Starting one tick later means a component that React mounts, unmounts, and mounts again (StrictMode in
    // development) opens one connection, not two. The opponent would otherwise see a drop and a return.
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
