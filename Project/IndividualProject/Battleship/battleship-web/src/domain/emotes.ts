import type { EmoteCode } from '../api/types'

export interface Emote {
  code: EmoteCode
  glyph: string
  label: string
}

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

export const EMOTE_COOLDOWN_MS = 2000

export const EMOTE_VISIBLE_MS = 3000

export function findEmote(code: EmoteCode): Emote | undefined {
  return EMOTES.find((emote) => emote.code === code)
}
