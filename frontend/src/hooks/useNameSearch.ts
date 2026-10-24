import { useEffect, useState } from 'react'
import { useSearchParams } from 'react-router'
import { errorMessage } from '../api/http'

const searchDelayMs = 250

export type SearchState<T> = { kind: 'loading' } | { kind: 'ready'; items: T[] } | { kind: 'error'; message: string }

/**
 * Loads a list filtered by name. Typing waits briefly before searching, and outdated requests are cancelled.
 * The filter lives in the URL (`?name=`), so a global search result can open the list already filtered.
 * Call reload() after a change to fetch the list again.
 */
export function useNameSearch<T>(search: (name: string, signal: AbortSignal) => Promise<T[]>) {
  const [searchParams, setSearchParams] = useSearchParams()
  const nameFilter = searchParams.get('name') ?? ''
  const [reloadToken, setReloadToken] = useState(0)
  const [state, setState] = useState<SearchState<T>>({ kind: 'loading' })

  useEffect(() => {
    const controller = new AbortController()
    const timer = setTimeout(() => {
      search(nameFilter, controller.signal)
        .then((items) => setState({ kind: 'ready', items }))
        .catch((error: unknown) => {
          if (!controller.signal.aborted) setState({ kind: 'error', message: errorMessage(error) })
        })
    }, searchDelayMs)
    return () => {
      clearTimeout(timer)
      controller.abort()
    }
  }, [search, nameFilter, reloadToken])

  return {
    nameFilter,
    setNameFilter: (name: string) => setSearchParams(name ? { name } : {}, { replace: true }),
    state,
    items: state.kind === 'ready' ? state.items : [],
    reload: () => setReloadToken((token) => token + 1),
  }
}
