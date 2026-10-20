import type { Coordinate, GameView } from '../api/types'
import { FleetBoard } from '../components/FleetBoard'
import { FleetStatus } from '../components/FleetStatus'
import { MessageLog } from '../components/MessageLog'
import { TargetBoard } from '../components/TargetBoard'
import type { LogEntry } from '../hooks/useGame'

interface BattleScreenProps {
  view: GameView
  busy: boolean
  log: LogEntry[]
  lastPlayerShot: Coordinate | null
  lastBotShots: Coordinate[]
  onFire: (target: Coordinate) => void
  onPlayAgain: () => void
}

export function BattleScreen({ view, busy, log, lastPlayerShot, lastBotShots, onFire, onPlayAgain }: BattleScreenProps) {
  const finished = view.phase === 'Finished'
  const canFire = view.phase === 'InProgress' && view.yourTurn && !busy
  const opponentName = view.opponent?.name ?? 'The bot'

  return (
    <div className="battle-screen">
      {finished ? (
        <div role="status" className={view.winner === 'you' ? 'banner win' : 'banner lose'}>
          <strong>{view.winner === 'you' ? 'You win!' : `${opponentName} wins.`}</strong>
          <button type="button" className="primary" onClick={onPlayAgain}>
            Play again
          </button>
        </div>
      ) : (
        <p className="turn-info" role="status">
          {busy ? 'Firing…' : canFire ? 'Your turn: click a cell on the target board.' : 'Waiting…'}
        </p>
      )}

      <div className="boards">
        <TargetBoard board={view.enemyBoard} lastShot={lastPlayerShot} onFire={canFire ? onFire : undefined} />
        <FleetBoard board={view.ownBoard} highlight={lastBotShots} />
      </div>

      <div className="side-panels">
        <FleetStatus ships={view.ownShips} enemyShipsRemaining={view.enemyShipsRemaining} />
        <MessageLog entries={log} />
      </div>
    </div>
  )
}
