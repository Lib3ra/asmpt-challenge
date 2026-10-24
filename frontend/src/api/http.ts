/** Field errors as returned by ASP.NET Core validation problems, e.g. { "components[1].name": ["Name is required."] }. */
export type FieldErrors = Record<string, string[]>

/** An error response from the API, with the problem details message and any field errors. */
export class ApiError extends Error {
  readonly status: number
  readonly fieldErrors: FieldErrors

  constructor(status: number, message: string, fieldErrors: FieldErrors = {}) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.fieldErrors = fieldErrors
  }
}

type ProblemDetails = { title?: string; detail?: string; errors?: FieldErrors }

/** Sends a request to the API and returns the parsed JSON body; throws ApiError for non-2xx responses. */
export async function apiRequest<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = init.body ? { 'Content-Type': 'application/json', ...init.headers } : init.headers
  const response = await fetch(path, { ...init, headers })

  if (!response.ok) {
    throw await toApiError(response)
  }
  if (response.status === 204) {
    return undefined as T
  }
  return (await response.json()) as T
}

async function toApiError(response: Response): Promise<ApiError> {
  const fallback = `Request failed with HTTP ${response.status}`
  try {
    const problem = (await response.json()) as ProblemDetails
    return new ApiError(response.status, problem.detail ?? problem.title ?? fallback, problem.errors ?? {})
  } catch {
    return new ApiError(response.status, fallback)
  }
}

export function errorMessage(error: unknown): string {
  return error instanceof Error ? error.message : String(error)
}
