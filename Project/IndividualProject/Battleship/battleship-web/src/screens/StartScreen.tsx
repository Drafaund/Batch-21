import { useState, type FormEvent } from 'react'
import type { OpponentKind } from '../api/types'

interface StartScreenProps {
  busy: boolean
  onStart: (playerName: string, opponent: OpponentKind) => void
  onJoin: (playerName: string, code: string) => void
}

const OPPONENTS: { value: OpponentKind; name: string; hint: string }[] = [
  { value: 'Random', name: 'Random AI', hint: 'shoots at random cells' },
  { value: 'HuntTarget', name: 'Hunt-Target AI', hint: 'hunts at random, then finishes off a ship it hit' },
]

export function StartScreen({ busy, onStart, onJoin }: StartScreenProps) {
  const [name, setName] = useState('')
  const [code, setCode] = useState('')
  const [opponent, setOpponent] = useState<OpponentKind>('Random')

  function handleSubmit(event: FormEvent) {
    event.preventDefault()
    const trimmed = name.trim()
    if (trimmed) {
      onStart(trimmed, opponent)
    }
  }

  const trimmedName = name.trim()
  const trimmedCode = code.trim()

  function handleJoin() {
    if (trimmedName && trimmedCode) {
      onJoin(trimmedName, trimmedCode)
    }
  }

  return (
    <form className="panel start-screen" onSubmit={handleSubmit}>
      <h1>Battleship</h1>
      <p className="muted">Sink all five enemy ships before the bot sinks yours.</p>

      <label htmlFor="player-name">Your name</label>
      <input
        id="player-name"
        value={name}
        onChange={(event) => setName(event.target.value)}
        maxLength={20}
        required
        autoComplete="nickname"
        autoFocus
      />

      <fieldset>
        <legend>Opponent</legend>
        {OPPONENTS.map((option) => (
          <label key={option.value} className="choice">
            <input
              type="radio"
              name="opponent"
              value={option.value}
              checked={opponent === option.value}
              onChange={() => setOpponent(option.value)}
            />
            <span>
              {option.name} <span className="muted">({option.hint})</span>
            </span>
          </label>
        ))}
      </fieldset>

      <button type="submit" className="primary" disabled={busy || trimmedName === ''}>
        {busy ? 'Starting…' : 'Start game'}
      </button>

      <fieldset>
        <legend>Play a friend online</legend>
        <p className="muted">Open a room and share its code, or enter the code of a room a friend opened. You need your name above.</p>
        <div className="actions">
          <button type="button" onClick={() => onStart(trimmedName, 'Human')} disabled={busy || trimmedName === ''}>
            Create a room
          </button>
        </div>
        <div className="actions">
          <label htmlFor="room-code">Room code</label>
          <input
            id="room-code"
            value={code}
            onChange={(event) => setCode(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === 'Enter') {
                event.preventDefault()
                handleJoin()
              }
            }}
            maxLength={5}
            autoComplete="off"
            autoCapitalize="characters"
          />
          <button type="button" onClick={handleJoin} disabled={busy || trimmedName === '' || trimmedCode === ''}>
            Join room
          </button>
        </div>
      </fieldset>
    </form>
  )
}
