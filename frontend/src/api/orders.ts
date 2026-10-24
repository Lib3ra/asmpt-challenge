import { apiRequest } from './http'

export type OrderBoard = {
  id: number
  name: string
}

export type Order = {
  id: number
  name: string
  description: string
  /** Date only, yyyy-MM-dd. */
  orderDate: string
  boards: OrderBoard[]
}

export type OrderInput = {
  name: string
  description: string
  orderDate: string | null
  boardIds: number[]
}

const route = '/api/orders'

export function searchOrders(name: string, signal?: AbortSignal): Promise<Order[]> {
  const query = name.trim() ? `?name=${encodeURIComponent(name.trim())}` : ''
  return apiRequest<Order[]>(`${route}${query}`, { method: 'GET', signal })
}

export function createOrder(input: OrderInput): Promise<Order> {
  return apiRequest<Order>(route, { method: 'POST', body: JSON.stringify(input) })
}

export function updateOrder(id: number, input: OrderInput): Promise<Order> {
  return apiRequest<Order>(`${route}/${id}`, { method: 'PUT', body: JSON.stringify(input) })
}

/**
 * Address of the production download: the server sends the order with its boards and components as a JSON
 * file (Content-Disposition attachment), so a plain link saves it without loading it into the page.
 */
export function productionDownloadUrl(id: number): string {
  return `${route}/${id}/production-download`
}

/** Deletes one or more orders; all-or-nothing on the server. */
export function deleteOrders(ids: number[]): Promise<void> {
  const query = ids.map((id) => `ids=${id}`).join('&')
  return apiRequest<void>(`${route}?${query}`, { method: 'DELETE' })
}
