import type { EmoteCode } from '../api/types'

export interface Emote {
  code: EmoteCode
  glyph: string
  label: string
}

/** The emotes a player can send. The server accepts only these codes (`EmoteCatalog` in Battleship.Api). */
export const EMOTES: readonly Emote[] = [
  { code: 'thumbsUp', glyph: '👍', label: 'Thumbs up' },
  { code: 'laugh', glyph: '😂', label: 'Laughing' },
  { code: 'wow', glyph: '😮', label: 'Wow' },
  { code: 'angry', glyph: '😡', label: 'Angry' },
  { code: 'target', glyph: '🎯', label: 'Bullseye' },
  { code: 'wave', glyph: '🌊', label: 'Wave' },
  { code: 'handshake', glyph: '🤝', label: 'Handshake' },
  { code: 'clap', glyph: '👏', label: 'Applause' },
]

/** The server makes a seat wait this long between two emotes (`Game:EmoteCooldown`); the buttons wait the same. */
export const EMOTE_COOLDOWN_MS = 2000

/** How long an emote from the opponent stays on screen. */
export const EMOTE_VISIBLE_MS = 3000

export function findEmote(code: EmoteCode): Emote | undefined {
  return EMOTES.find((emote) => emote.code === code)
}
