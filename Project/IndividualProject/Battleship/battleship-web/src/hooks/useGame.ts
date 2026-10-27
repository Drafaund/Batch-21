import { useCallback, useEffect, useRef, useState } from 'react'
import { ApiError, type GameApi } from '../api/client'
import { realtime as realRealtime, type ConnectionStatus, type GameEvent, type RealtimeClient } from '../api/realtime'
import { clearSession, loadSession, saveSession, type Session } from '../api/session'
import type { Coordinate, EmoteCode, GameView, OpponentKind, Orientation, ShipType } from '../api/types'
import { formatCoordinate } from '../domain/coordinates'
import { describeResult } from '../domain/ships'

export interface LogEntry {
  id: number
  kind: 'you' | 'bot' | 'info'
  text: string
}

export interface IncomingEmote {
  id: number
  emote: EmoteCode
}

export interface GameController {
  view: GameView | null
  busy: boolean
  resuming: boolean
  online: boolean
  connection: ConnectionStatus
  error: string | null
  log: LogEntry[]
  lastPlayerShot: Coordinate | null
  lastBotShots: Coordinate[]
  incomingEmote: IncomingEmote | null
  start(playerName: string, opponent: OpponentKind): Promise<void>
  join(playerName: string, code: string): Promise<void>
  reconnect(): void
  placeShip(type: ShipType, start: Coordinate, orientation: Orientation): Promise<void>
  placeRandomly(): Promise<void>
  fire(target: Coordinate): Promise<void>
  sendEmote(emote: EmoteCode): Promise<void>
  reset(): void
  dismissError(): void
}

const MAX_LOG_ENTRIES = 8
const GAME_GONE = 'This game no longer exists. Start a new game.'

export function useGame(api: GameApi, realtime: RealtimeClient = realRealtime): GameController {
  const [session, setSession] = useState<Session | null>(() => loadSession())
  const [view, setView] = useState<GameView | null>(null)
  const [busy, setBusy] = useState(false)
  const [resuming, setResuming] = useState(() => loadSession() !== null)
  const [error, setError] = useState<string | null>(null)
  const [log, setLog] = useState<LogEntry[]>([])
  const [lastPlayerShot, setLastPlayerShot] = useState<Coordinate | null>(null)
  const [lastBotShots, setLastBotShots] = useState<Coordinate[]>([])
  const [connection, setConnection] = useState<ConnectionStatus>('connecting')
  const [connectAttempt, setConnectAttempt] = useState(0)
  const [incomingEmote, setIncomingEmote] = useState<IncomingEmote | null>(null)
  const nextLogId = useRef(1)
  const nextEmoteId = useRef(1)

  const addLog = useCallback((entries: Omit<LogEntry, 'id'>[]) => {
    setLog((previous) => [...previous, ...entries.map((entry) => ({ ...entry, id: nextLogId.current++ }))].slice(-MAX_LOG_ENTRIES))
  }, [])

  const applyView = useCallback((next: GameView) => {
    setView((previous) => (previous && previous.code === next.code && next.version < previous.version ? previous : next))
  }, [])

  const forgetGame = useCallback((message: string | null) => {
    clearSession()
    setSession(null)
    setView(null)
    setLog([])
    setLastPlayerShot(null)
    setLastBotShots([])
    setIncomingEmote(null)
    setConnection('connecting')
    setError(message)
  }, [])

  const handleError = useCallback(
    (caught: unknown) => {
      if (caught instanceof ApiError && (caught.status === 404 || caught.status === 401)) {
        forgetGame(GAME_GONE)
      } else {
        setError(caught instanceof ApiError ? caught.message : 'Something went wrong. Please try again.')
      }
    },
    [forgetGame],
  )

  const run = useCallback(
    async (action: () => Promise<void>) => {
      setBusy(true)
      setError(null)
      try {
        await action()
      } catch (caught) {
        handleError(caught)
      } finally {
        setBusy(false)
      }
    },
    [handleError],
  )

  useEffect(() => {
    if (!session) {
      return
    }
    let cancelled = false
    api
      .getGame(session.code, session.token)
      .then((loaded) => {
        if (!cancelled) {
          applyView(loaded)
        }
      })
      .catch((caught) => {
        if (!cancelled) {
          handleError(caught)
        }
      })
      .finally(() => {
        if (!cancelled) {
          setResuming(false)
        }
      })
    return () => {
      cancelled = true
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const handleEvent = useCallback(
    (event: GameEvent) => {
      if (event.type === 'EmoteReceived') {
        setIncomingEmote({ id: nextEmoteId.current++, emote: event.emote })
        return
      }

      applyView(event.view)
      const opponentName = event.view.opponent?.name ?? 'Your opponent'
      switch (event.type) {
        case 'PlayerJoined':
          addLog([{ kind: 'info', text: `${event.name} joined the room.` }])
          break
        case 'GameStarted':
          addLog([{ kind: 'info', text: 'Both fleets are ready. The battle begins!' }])
          break
        case 'OpponentConnection':
          addLog([{ kind: 'info', text: event.connected ? `${opponentName} connected.` : `${opponentName} lost the connection.` }])
          break
        case 'ShotResolved':
          if (event.shooter === 'opponent') {
            addLog([{ kind: 'bot', text: `${opponentName} fired at ${formatCoordinate(event.target)}: ${describeResult(event.result)}` }])
            setLastBotShots([event.target])
          }
          break
        case 'GameEnded':
          if (event.view.endReason === 'Forfeit') {
            addLog([{ kind: 'info', text: event.winner === 'you' ? `${opponentName} left the game. You win!` : 'You were away too long and lost the game.' }])
          } else if (event.view.endReason === 'Timeout') {
            addLog([{ kind: 'info', text: event.winner === 'you' ? `${opponentName} ran out of time. You win!` : 'You ran out of time and lost the game.' }])
          } else if (event.winner === 'opponent') {
            addLog([{ kind: 'info', text: `${opponentName} wins the game.` }])
          }
          break
      }
    },
    [addLog, applyView],
  )

  const online = session?.online === true
  const sessionCode = session?.code
  const sessionToken = session?.token

  useEffect(() => {
    if (!online || !sessionCode || !sessionToken) {
      return
    }

    const connectionToGame = realtime.connect(sessionCode, sessionToken, {
      onEvent: (event) => handleEvent(event),
      onStatus: (status) => {
        setConnection(status)
        if (status === 'connected') {
          api
            .getGame(sessionCode, sessionToken)
            .then(applyView)
            .catch(handleError)
        }
      },
      onGone: () => forgetGame(GAME_GONE),
    })
    return () => {
      void connectionToGame.stop()
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [online, sessionCode, sessionToken, connectAttempt, realtime, api])

  const requireSession = useCallback((): Session => {
    if (!session) {
      throw new ApiError(404, GAME_GONE)
    }
    return session
  }, [session])

  const start = useCallback(
    (playerName: string, opponent: OpponentKind) =>
      run(async () => {
        const created = await api.createGame(playerName, opponent)
        const next: Session = { code: created.code, token: created.playerToken, ...(opponent === 'Human' ? { online: true } : {}) }
        saveSession(next)
        setSession(next)
        setView(created.view)
        setLog([])
        setLastPlayerShot(null)
        setLastBotShots([])
      }),
    [api, run],
  )

  const join = useCallback(
    (playerName: string, code: string) =>
      run(async () => {
        let joined
        try {
          joined = await api.joinGame(code.trim().toUpperCase(), playerName)
        } catch (caught) {
          if (caught instanceof ApiError) {
            setError(caught.message)
            return
          }
          throw caught
        }
        const next: Session = { code: joined.view.code, token: joined.playerToken, online: true }
        saveSession(next)
        setSession(next)
        setView(joined.view)
        setLog([])
        setLastPlayerShot(null)
        setLastBotShots([])
      }),
    [api, run],
  )

  const reconnect = useCallback(() => setConnectAttempt((attempt) => attempt + 1), [])

  const placeShip = useCallback(
    (type: ShipType, startCell: Coordinate, orientation: Orientation) =>
      run(async () => {
        const { code, token } = requireSession()
        applyView(await api.placeShip(code, token, type, startCell, orientation))
      }),
    [api, applyView, requireSession, run],
  )

  const placeRandomly = useCallback(
    () =>
      run(async () => {
        const { code, token } = requireSession()
        applyView(await api.placeShipsRandomly(code, token))
      }),
    [api, applyView, requireSession, run],
  )

  const fire = useCallback(
    (target: Coordinate) =>
      run(async () => {
        const current = requireSession()
        const { code, token } = current
        const response = await api.fire(code, token, target)
        applyView(response.view)

        const opponentName = response.view.opponent?.name ?? 'The bot'
        const entries: Omit<LogEntry, 'id'>[] = [
          { kind: 'you', text: `You fired at ${formatCoordinate(target)}: ${describeResult(response.result)}` },
          ...response.botShots.map((shot) => ({
            kind: 'bot' as const,
            text: `${opponentName} fired at ${formatCoordinate(shot.target)}: ${describeResult(shot.result)}`,
          })),
        ]
        if (response.view.winner) {
          entries.push({ kind: 'info', text: response.view.winner === 'you' ? 'You win the game!' : `${opponentName} wins the game.` })
        }
        addLog(entries)

        if (response.result !== 'Invalid') {
          setLastPlayerShot(target)
          if (!current.online) {
            setLastBotShots(response.botShots.map((shot) => shot.target))
          }
        }
      }),
    [addLog, api, applyView, requireSession, run],
  )

  const sendEmote = useCallback(
    async (emote: EmoteCode) => {
      try {
        const { code, token } = requireSession()
        await api.sendEmote(code, token, emote)
      } catch (caught) {
        if (caught instanceof ApiError && caught.status === 429) {
          return
        }
        handleError(caught)
      }
    },
    [api, handleError, requireSession],
  )

  const reset = useCallback(() => forgetGame(null), [forgetGame])
  const dismissError = useCallback(() => setError(null), [])

  return {
    view,
    busy,
    resuming,
    online,
    connection,
    error,
    log,
    lastPlayerShot,
    lastBotShots,
    incomingEmote,
    start,
    join,
    reconnect,
    placeShip,
    placeRandomly,
    fire,
    sendEmote,
    reset,
    dismissError,
  }
}
