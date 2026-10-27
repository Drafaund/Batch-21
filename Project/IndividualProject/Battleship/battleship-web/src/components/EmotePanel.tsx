import { useEffect, useState } from 'react'
import type { EmoteCode } from '../api/types'
import { EMOTE_COOLDOWN_MS, EMOTE_VISIBLE_MS, EMOTES, findEmote } from '../domain/emotes'
import type { IncomingEmote } from '../hooks/useGame'

interface EmotePanelProps {
  incoming: IncomingEmote | null
  opponentName: string
  onSend: (emote: EmoteCode) => void
}

const HIDE_KEY = 'battleship.hideEmotes'

function loadHidden(): boolean {
  try {
    return window.localStorage.getItem(HIDE_KEY) === 'true'
  } catch {
    return false
  }
}

function saveHidden(hidden: boolean): void {
  try {
    window.localStorage.setItem(HIDE_KEY, String(hidden))
  } catch {
  }
}

export function EmotePanel({ incoming, opponentName, onSend }: EmotePanelProps) {
  const [hidden, setHidden] = useState(loadHidden)
  const [cooling, setCooling] = useState(false)
  const [dismissedId, setDismissedId] = useState(0)

  useEffect(() => {
    if (!cooling) {
      return
    }
    const timer = setTimeout(() => setCooling(false), EMOTE_COOLDOWN_MS)
    return () => clearTimeout(timer)
  }, [cooling])

  useEffect(() => {
    if (!incoming) {
      return
    }
    const timer = setTimeout(() => setDismissedId(incoming.id), EMOTE_VISIBLE_MS)
    return () => clearTimeout(timer)
  }, [incoming])

  const shown = incoming && incoming.id !== dismissedId && !hidden ? findEmote(incoming.emote) : undefined

  function send(emote: EmoteCode) {
    setCooling(true)
    onSend(emote)
  }

  function toggleHidden(next: boolean) {
    setHidden(next)
    saveHidden(next)
  }

  return (
    <section className="emote-panel" aria-label="Emotes">
      <div className="emote-buttons" role="group" aria-label="Send an emote">
        {EMOTES.map((emote) => (
          <button key={emote.code} type="button" className="emote-button" disabled={cooling} aria-label={emote.label} onClick={() => send(emote.code)}>
            <span aria-hidden="true">{emote.glyph}</span>
          </button>
        ))}
      </div>

      <label className="emote-toggle">
        <input type="checkbox" checked={hidden} onChange={(event) => toggleHidden(event.target.checked)} />
        Hide emotes from {opponentName}
      </label>

      <div className="emote-stage" role="status">
        {shown && (
          <span key={incoming?.id} className="emote-bubble">
            <span aria-hidden="true" className="emote-glyph">
              {shown.glyph}
            </span>
            <span className="emote-caption">
              {opponentName}: {shown.label}
            </span>
          </span>
        )}
      </div>
    </section>
  )
}
