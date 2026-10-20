import type { LogEntry } from '../hooks/useGame'

/** The latest shots in words, read out by screen readers as they arrive. */
export function MessageLog({ entries }: { entries: LogEntry[] }) {
  return (
    <section className="message-log" aria-label="Shots">
      <h2>Shots</h2>
      {entries.length === 0 ? (
        <p className="muted">No shots yet.</p>
      ) : (
        <ol aria-live="polite">
          {entries.map((entry) => (
            <li key={entry.id} className={entry.kind}>
              {entry.text}
            </li>
          ))}
        </ol>
      )}
    </section>
  )
}
