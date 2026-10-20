import type { Coordinate } from '../api/types'

/** Rows are letters: 0 is A. */
export const rowLabel = (y: number): string => String.fromCharCode(65 + y)

/** Columns are numbers from 1: 0 is 1. */
export const columnLabel = (x: number): string => String(x + 1)

/** The way a player reads a cell, such as D-4 for column 4 of row D. */
export const formatCoordinate = (coordinate: Coordinate): string => `${rowLabel(coordinate.y)}-${columnLabel(coordinate.x)}`
