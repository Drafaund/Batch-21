import type { Coordinate, EmoteCode, GameView } from '../api/types'
import { EmotePanel } from '../components/EmotePanel'
import { FleetBoard } from '../components/FleetBoard'
import { FleetStatus } from '../components/FleetStatus'
import { MessageLog } from '../components/MessageLog'
import { TargetBoard } from '../components/TargetBoard'
import { useCountdown } from '../hooks/useCountdown'
import type { IncomingEmote, LogEntry } from '../hooks/useGame'

interface BattleScreenProps {
  view: GameView
  busy: boolean
  log: LogEntry[]
  lastPlayerShot: Coordinate | null
  lastBotShots: Coordinate[]
  onFire: (target: Coordinate) => void
  onPlayAgain: () => void
  emotes?: { incoming: IncomingEmote | null; onSend: (emote: EmoteCode) => void }
}

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

export function BattleScreen({ view, busy, log, lastPlayerShot, lastBotShots, onFire, onPlayAgain, emotes }: BattleScreenProps) {
  const finished = view.phase === 'Finished'
  const canFire = view.phase === 'InProgress' && view.yourTurn && !busy
  const opponentName = view.opponent?.name ?? 'The bot'
  const opponentAway = !finished && view.opponent !== null && !view.opponent.connected
  const secondsLeft = useCountdown(finished ? null : view.turnSecondsRemaining, view.version)
  const sunkCells = view.ownShips.filter((ship) => ship.isSunk).flatMap((ship) => ship.positions)

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

      {emotes && <EmotePanel incoming={emotes.incoming} opponentName={opponentName} onSend={emotes.onSend} />}

      <div className="boards">
        <TargetBoard board={view.enemyBoard} lastShot={lastPlayerShot} onFire={canFire ? onFire : undefined} />
        <FleetBoard board={view.ownBoard} highlight={lastBotShots} sunkCells={sunkCells} />
      </div>

      <div className="side-panels">
        <FleetStatus ships={view.ownShips} enemyShipsRemaining={view.enemyShipsRemaining} />
        <MessageLog entries={log} />
      </div>
    </div>
  )
}
