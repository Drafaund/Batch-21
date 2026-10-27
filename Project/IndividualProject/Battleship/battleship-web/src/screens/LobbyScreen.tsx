import type { GameView } from '../api/types'

interface LobbyScreenProps {
  view: GameView
  onLeave: () => void
}

export function LobbyScreen({ view, onLeave }: LobbyScreenProps) {
  return (
    <section className="panel lobby-screen">
      <h1>Waiting for an opponent</h1>
      <p className="muted">Send this code to a friend. They enter it on the start screen under “Play a friend online”.</p>
      <p className="room-code" aria-label={`Room code ${view.code.split('').join(' ')}`}>
        {view.code}
      </p>
      <p role="status" className="muted">
        The game moves on to ship placement as soon as they join.
      </p>
      <div className="actions">
        <button type="button" onClick={onLeave}>
          Leave room
        </button>
      </div>
    </section>
  )
}
