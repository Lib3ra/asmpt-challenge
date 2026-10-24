import { apiRequest } from './http'

export type HealthResponse = {
  status: string
}

export function fetchHealth(signal?: AbortSignal): Promise<HealthResponse> {
  return apiRequest<HealthResponse>('/api/health', { signal })
}
