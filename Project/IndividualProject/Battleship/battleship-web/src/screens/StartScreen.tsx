import { useState, type FormEvent } from 'react'
import type { OpponentKind } from '../api/types'

interface StartScreenProps {
  busy: boolean
  onStart: (playerName: string, opponent: OpponentKind) => void
}

const OPPONENTS: { value: OpponentKind; name: string; hint: string }[] = [
  { value: 'Random', name: 'Random AI', hint: 'shoots at random cells' },
  { value: 'HuntTarget', name: 'Hunt-Target AI', hint: 'hunts at random, then finishes off a ship it hit' },
]

export function StartScreen({ busy, onStart }: StartScreenProps) {
  const [name, setName] = useState('')
  const [opponent, setOpponent] = useState<OpponentKind>('Random')

  function handleSubmit(event: FormEvent) {
    event.preventDefault()
    const trimmed = name.trim()
    if (trimmed) {
      onStart(trimmed, opponent)
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

      <button type="submit" className="primary" disabled={busy || name.trim() === ''}>
        {busy ? 'Starting…' : 'Start game'}
      </button>
    </form>
  )
}
