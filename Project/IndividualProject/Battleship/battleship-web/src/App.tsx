import { api as realApi, type GameApi } from './api/client'
import { ErrorBanner } from './components/ErrorBanner'
import { useGame } from './hooks/useGame'
import { BattleScreen } from './screens/BattleScreen'
import { PlacementScreen } from './screens/PlacementScreen'
import { StartScreen } from './screens/StartScreen'

interface AppProps {
  /** The server to talk to. Tests pass a fake. */
  api?: GameApi
}

export default function App({ api = realApi }: AppProps) {
  const game = useGame(api)
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

      {!view && <StartScreen busy={game.busy} onStart={game.start} />}

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
