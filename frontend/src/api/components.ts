import { apiRequest } from './http'

export type Component = {
  id: number
  name: string
  description: string
  quantity: number
}

export type ComponentInput = Omit<Component, 'id'>

/** Mirrors the server-side rules so inputs can limit length; the server remains the authority. */
export const componentLimits = {
  nameMaxLength: 256,
  descriptionMaxLength: 2000,
  minQuantity: 1,
  maxQuantity: 10_000,
} as const

const route = '/api/components'

export function searchComponents(name: string, signal?: AbortSignal): Promise<Component[]> {
  const query = name.trim() ? `?name=${encodeURIComponent(name.trim())}` : ''
  return apiRequest<Component[]>(`${route}${query}`, { signal })
}

/** Creates one or more components; all-or-nothing on the server. */
export function createComponents(inputs: ComponentInput[]): Promise<Component[]> {
  return apiRequest<Component[]>(route, { method: 'POST', body: JSON.stringify(inputs) })
}

export function updateComponent(id: number, input: ComponentInput): Promise<Component> {
  return apiRequest<Component>(`${route}/${id}`, { method: 'PUT', body: JSON.stringify(input) })
}

/** Deletes one or more components; all-or-nothing on the server. */
export function deleteComponents(ids: number[]): Promise<void> {
  const query = ids.map((id) => `ids=${id}`).join('&')
  return apiRequest<void>(`${route}?${query}`, { method: 'DELETE' })
}
