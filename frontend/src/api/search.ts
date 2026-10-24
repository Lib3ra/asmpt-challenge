import { apiRequest } from './http'

export type SearchType = 'orders' | 'boards' | 'components'

export type SearchHit = {
  id: number
  name: string
  description: string
}

/** Matches grouped by type; a group is empty when nothing matches or the type was filtered out. */
export type SearchResult = Record<SearchType, SearchHit[]>

/** Searches names across all record types, or only `type` when given. The term must not be blank. */
export function searchAll(term: string, type: SearchType | null, signal?: AbortSignal): Promise<SearchResult> {
  const query = new URLSearchParams({ term: term.trim() })
  if (type) query.set('type', type)
  return apiRequest<SearchResult>(`/api/search?${query}`, { method: 'GET', signal })
}
