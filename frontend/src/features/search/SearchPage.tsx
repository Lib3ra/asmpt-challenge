import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router'
import { errorMessage } from '../../api/http'
import { searchAll, type SearchResult, type SearchType } from '../../api/search'

const searchDelayMs = 250

/** Result groups in display order, with the list page that shows the full record. */
const groups: { type: SearchType; title: string; path: string }[] = [
  { type: 'orders', title: 'Orders', path: '/orders' },
  { type: 'boards', title: 'Boards', path: '/boards' },
  { type: 'components', title: 'Components', path: '/components' },
]

type SearchState =
  | { kind: 'loading' }
  | { kind: 'ready'; query: string; result: SearchResult }
  | { kind: 'error'; message: string }

function toSearchType(value: string | null): SearchType | null {
  return groups.some((group) => group.type === value) ? (value as SearchType) : null
}

/** Searches names across orders, boards and components. Term and type live in the URL. */
export function SearchPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const term = searchParams.get('term') ?? ''
  const type = toSearchType(searchParams.get('type'))
  const [state, setState] = useState<SearchState>({ kind: 'loading' })
  // Results belong to one term and type; while a newer search is pending they count as loading.
  const query = `${term.trim()}|${type ?? ''}`

  useEffect(() => {
    if (!term.trim()) return
    const controller = new AbortController()
    const timer = setTimeout(() => {
      searchAll(term, type, controller.signal)
        .then((result) => setState({ kind: 'ready', query, result }))
        .catch((error: unknown) => {
          if (!controller.signal.aborted) setState({ kind: 'error', message: errorMessage(error) })
        })
    }, searchDelayMs)
    return () => {
      clearTimeout(timer)
      controller.abort()
    }
  }, [term, type, query])

  function updateQuery(nextTerm: string, nextType: SearchType | null) {
    const query: Record<string, string> = {}
    if (nextTerm) query.term = nextTerm
    if (nextType) query.type = nextType
    setSearchParams(query, { replace: true })
  }

  const hasTerm = term.trim() !== ''
  const current: SearchState = state.kind === 'ready' && state.query !== query ? { kind: 'loading' } : state
  const visibleGroups = groups.filter((group) => type === null || group.type === type)
  const total = current.kind === 'ready' ? visibleGroups.reduce((sum, group) => sum + current.result[group.type].length, 0) : 0

  return (
    <section className="page">
      <header className="page-header">
        <h1>Search</h1>
      </header>

      <div className="panel">
        <div className="toolbar toolbar-start">
          <label className="search">
            Name contains
            <input
              id="global-search-term"
              type="search"
              value={term}
              onChange={(event) => updateQuery(event.target.value, type)}
            />
          </label>
          <label>
            Type
            <select
              id="global-search-type"
              value={type ?? ''}
              onChange={(event) => updateQuery(term, toSearchType(event.target.value))}
            >
              <option value="">All types</option>
              {groups.map((group) => (
                <option key={group.type} value={group.type}>
                  {group.title}
                </option>
              ))}
            </select>
          </label>
        </div>

        {!hasTerm && <p className="muted">Enter part of a name to search orders, boards and components.</p>}
        {hasTerm && current.kind === 'loading' && <p className="muted">Searching…</p>}
        {hasTerm && current.kind === 'error' && <p className="form-error">Search failed: {current.message}</p>}
        {hasTerm && current.kind === 'ready' && total === 0 && <p className="muted">No name contains “{term.trim()}”.</p>}
        {hasTerm &&
          current.kind === 'ready' &&
          total > 0 &&
          visibleGroups.map((group) => {
            const hits = current.result[group.type]
            return (
              <section key={group.type} className="search-group" aria-label={group.title}>
                <h2>
                  {group.title} ({hits.length})
                </h2>
                {hits.length === 0 ? (
                  <p className="muted">No matches.</p>
                ) : (
                  <ul>
                    {hits.map((hit) => (
                      <li key={hit.id}>
                        <Link to={`${group.path}?${new URLSearchParams({ name: hit.name })}`}>{hit.name}</Link>
                        {hit.description && <span className="muted"> · {hit.description}</span>}
                      </li>
                    ))}
                  </ul>
                )}
              </section>
            )
          })}
      </div>
    </section>
  )
}
