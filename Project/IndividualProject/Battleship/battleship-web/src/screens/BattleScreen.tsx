import type { Coordinate, GameView } from '../api/types'
import { FleetBoard } from '../components/FleetBoard'
import { FleetStatus } from '../components/FleetStatus'
import { MessageLog } from '../components/MessageLog'
import { TargetBoard } from '../components/TargetBoard'
import { useCountdown } from '../hooks/useCountdown'
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

/** What the banner says when the game is over. */
function describeEnd(view: GameView, opponentName: string): string {
  const won = view.winner === 'you'
  switch (view.endReason) {
    case 'Forfeit':
      return won ? `${opponentName} left the game. You win!` : 'You were away too long. You lose.'
    case 'Timeout':
      return won ? `${opponentName} ran out of time. You win!` : 'You ran out of time. You lose.'
    default:
      return won ? 'You win!' : `${opponentName} wins.`
  }
}

export function BattleScreen({ view, busy, log, lastPlayerShot, lastBotShots, onFire, onPlayAgain }: BattleScreenProps) {
  const finished = view.phase === 'Finished'
  const canFire = view.phase === 'InProgress' && view.yourTurn && !busy
  const opponentName = view.opponent?.name ?? 'The bot'
  const opponentAway = !finished && view.opponent !== null && !view.opponent.connected
  // Counts down from the seconds the server reported with this view; only a battle between two humans has a clock.
  const secondsLeft = useCountdown(finished ? null : view.turnSecondsRemaining, view.version)

  return (
    <div className="battle-screen">
      {finished ? (
        <div role="status" className={view.winner === 'you' ? 'banner win' : 'banner lose'}>
          <strong>{describeEnd(view, opponentName)}</strong>
          <button type="button" className="primary" onClick={onPlayAgain}>
            Play again
          </button>
        </div>
      ) : (
        <p className="turn-info" role="status">
          {busy ? 'Firing…' : canFire ? 'Your turn: click a cell on the target board.' : `Waiting for ${opponentName}…`}
          {secondsLeft !== null && !busy && (
            <span className={secondsLeft <= 10 ? 'countdown urgent' : 'countdown'} aria-label={`${secondsLeft} seconds left`}>
              {' '}
              {secondsLeft}s
            </span>
          )}
        </p>
      )}
      {opponentAway && (
        <p role="status" className="hint">
          {opponentName} lost the connection. The game waits for them to come back.
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
