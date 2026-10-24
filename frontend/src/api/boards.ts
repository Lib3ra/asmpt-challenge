import type { Component } from './components'
import { apiRequest } from './http'

export type Board = {
  id: number
  name: string
  description: string
  lengthMm: number
  widthMm: number
  components: Component[]
}

export type BoardInput = {
  name: string
  description: string
  lengthMm: number
  widthMm: number
  componentIds: number[]
}

const route = '/api/boards'

export function searchBoards(name: string, signal?: AbortSignal): Promise<Board[]> {
  const query = name.trim() ? `?name=${encodeURIComponent(name.trim())}` : ''
  return apiRequest<Board[]>(`${route}${query}`, { method: 'GET', signal })
}

export function createBoard(input: BoardInput): Promise<Board> {
  return apiRequest<Board>(route, { method: 'POST', body: JSON.stringify(input) })
}

export function updateBoard(id: number, input: BoardInput): Promise<Board> {
  return apiRequest<Board>(`${route}/${id}`, { method: 'PUT', body: JSON.stringify(input) })
}

/** Deletes one or more boards; all-or-nothing on the server. */
export function deleteBoards(ids: number[]): Promise<void> {
  const query = ids.map((id) => `ids=${id}`).join('&')
  return apiRequest<void>(`${route}?${query}`, { method: 'DELETE' })
}
