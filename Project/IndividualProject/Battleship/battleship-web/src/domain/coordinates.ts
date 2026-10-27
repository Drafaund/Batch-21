import type { Coordinate } from '../api/types'

export const rowLabel = (y: number): string => String.fromCharCode(65 + y)

export const columnLabel = (x: number): string => String(x + 1)

export const formatCoordinate = (coordinate: Coordinate): string => `${rowLabel(coordinate.y)}-${columnLabel(coordinate.x)}`
