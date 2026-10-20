import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { MessageLog } from './MessageLog'

describe('MessageLog', () => {
  it('says so when there are no shots', () => {
    render(<MessageLog entries={[]} />)

    expect(screen.getByText('No shots yet.')).toBeInTheDocument()
    expect(screen.queryByRole('list')).not.toBeInTheDocument()
  })

  it('lists the messages in order', () => {
    render(
      <MessageLog
        entries={[
          { id: 1, kind: 'you', text: 'You fired at A-1: Miss.' },
          { id: 2, kind: 'bot', text: 'Bot Random fired at C-3: Hit!' },
        ]}
      />,
    )

    const items = screen.getAllByRole('listitem')
    expect(items.map((item) => item.textContent)).toEqual(['You fired at A-1: Miss.', 'Bot Random fired at C-3: Hit!'])
  })
})
