import { describe, expect, it } from 'vitest'
import { columnLabel, formatCoordinate, rowLabel } from './coordinates'

describe('coordinates', () => {
  it('writes rows as letters starting at A', () => {
    expect(rowLabel(0)).toBe('A')
    expect(rowLabel(3)).toBe('D')
    expect(rowLabel(9)).toBe('J')
  })

  it('writes columns as numbers starting at 1', () => {
    expect(columnLabel(0)).toBe('1')
    expect(columnLabel(9)).toBe('10')
  })

  it.each([
    [{ x: 3, y: 3 }, 'D-4'],
    [{ x: 0, y: 0 }, 'A-1'],
    [{ x: 9, y: 9 }, 'J-10'],
    [{ x: 0, y: 9 }, 'J-1'],
    [{ x: 9, y: 0 }, 'A-10'],
  ])('formats %o as %s, like the console game', (coordinate, text) => {
    expect(formatCoordinate(coordinate)).toBe(text)
  })
})
