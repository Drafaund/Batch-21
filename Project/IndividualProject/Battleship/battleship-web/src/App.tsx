import { api as realApi, type GameApi } from './api/client'
import { realtime as realRealtime, type RealtimeClient } from './api/realtime'
import { ErrorBanner } from './components/ErrorBanner'
import { useGame } from './hooks/useGame'
import { BattleScreen } from './screens/BattleScreen'
import { LobbyScreen } from './screens/LobbyScreen'
import { PlacementScreen } from './screens/PlacementScreen'
import { StartScreen } from './screens/StartScreen'

interface AppProps {
  /** The server to talk to. Tests pass a fake. */
  api?: GameApi
  /** The real-time channel for games against another human. Tests pass a fake. */
  realtime?: RealtimeClient
}

export default function App({ api = realApi, realtime = realRealtime }: AppProps) {
  const game = useGame(api, realtime)
  const { view } = game

  if (game.resuming) {
    return (
      <main>
        <p role="status" className="muted">
          Loading your game…
        </p>
      </main>
    )
  }

  return (
    <main>
      {game.error && <ErrorBanner message={game.error} onDismiss={game.dismissError} />}

      {game.online && view && game.connection !== 'connected' && (
        <p role="status" className="hint connection-status">
          {game.connection === 'offline' ? (
            <>
              The connection to the game was lost.{' '}
              <button type="button" onClick={game.reconnect}>
                Reconnect
              </button>
            </>
          ) : (
            'Connecting to the game…'
          )}
        </p>
      )}

      {!view && <StartScreen busy={game.busy} onStart={game.start} onJoin={game.join} />}

      {view?.phase === 'Lobby' && <LobbyScreen view={view} onLeave={game.reset} />}

      {view?.phase === 'Placing' && (
        <PlacementScreen
          view={view}
          busy={game.busy}
          onPlace={(type, start, orientation) => void game.placeShip(type, start, orientation)}
          onPlaceRandomly={() => void game.placeRandomly()}
        />
      )}

      {(view?.phase === 'InProgress' || view?.phase === 'Finished') && (
        <BattleScreen
          view={view}
          busy={game.busy}
          log={game.log}
          lastPlayerShot={game.lastPlayerShot}
          lastBotShots={game.lastBotShots}
          onFire={(target) => void game.fire(target)}
          onPlayAgain={game.reset}
        />
      )}
    </main>
  )
}
