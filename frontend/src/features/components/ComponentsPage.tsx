import { useEffect, useState } from 'react'
import { deleteComponents, searchComponents, type Component } from '../../api/components'
import { errorMessage } from '../../api/http'
import { DeleteSelection } from '../../ui/DeleteSelection'
import { ComponentCreateForm } from './ComponentCreateForm'
import { ComponentRow } from './ComponentRow'

const searchDelayMs = 250

type LoadState = { kind: 'loading' } | { kind: 'ready'; components: Component[] } | { kind: 'error'; message: string }

export function ComponentsPage() {
  const [nameFilter, setNameFilter] = useState('')
  const [reloadToken, setReloadToken] = useState(0)
  const [state, setState] = useState<LoadState>({ kind: 'loading' })
  const [selectedIds, setSelectedIds] = useState<Set<number>>(new Set())
  const [notice, setNotice] = useState<string | null>(null)

  useEffect(() => {
    const controller = new AbortController()
    const timer = setTimeout(() => {
      searchComponents(nameFilter, controller.signal)
        .then((components) => setState({ kind: 'ready', components }))
        .catch((error: unknown) => {
          if (!controller.signal.aborted) setState({ kind: 'error', message: errorMessage(error) })
        })
    }, searchDelayMs)
    return () => {
      clearTimeout(timer)
      controller.abort()
    }
  }, [nameFilter, reloadToken])

  const components = state.kind === 'ready' ? state.components : []
  const visibleSelectedIds = components.filter((component) => selectedIds.has(component.id)).map((c) => c.id)
  const allSelected = components.length > 0 && visibleSelectedIds.length === components.length

  function reload() {
    setReloadToken((token) => token + 1)
  }

  function toggleSelected(id: number) {
    setSelectedIds((current) => {
      const next = new Set(current)
      if (!next.delete(id)) next.add(id)
      return next
    })
  }

  function toggleAll() {
    setSelectedIds(allSelected ? new Set() : new Set(components.map((component) => component.id)))
  }

  async function deleteSelected() {
    await deleteComponents(visibleSelectedIds)
    setNotice(`Deleted ${visibleSelectedIds.length} component${visibleSelectedIds.length === 1 ? '' : 's'}.`)
    setSelectedIds(new Set())
    reload()
  }

  function handleCreated(created: Component[]) {
    setNotice(`Saved ${created.length} component${created.length === 1 ? '' : 's'}.`)
    reload()
  }

  function handleSaved(updated: Component) {
    setNotice(`Saved ${updated.name}.`)
    reload()
  }

  return (
    <section className="page">
      <header className="page-header">
        <h1>Components</h1>
        <p className="muted">Quantity is the number of copies of a component placed on one board.</p>
      </header>

      <ComponentCreateForm onCreated={handleCreated} />

      <div className="panel">
        <div className="toolbar">
          <label className="search">
            Filter by name
            <input
              id="component-name-filter"
              type="search"
              value={nameFilter}
              placeholder="e.g. resistor"
              onChange={(event) => setNameFilter(event.target.value)}
            />
          </label>
          {notice && (
            <p className="notice" role="status">
              {notice}
            </p>
          )}
        </div>

        <DeleteSelection
          count={visibleSelectedIds.length}
          noun="component"
          onDelete={deleteSelected}
          onClear={() => setSelectedIds(new Set())}
        />

        {state.kind === 'loading' && <p className="muted">Loading components…</p>}
        {state.kind === 'error' && <p className="form-error">Could not load components: {state.message}</p>}
        {state.kind === 'ready' && components.length === 0 && (
          <p className="muted">
            {nameFilter.trim() ? `No component name contains “${nameFilter.trim()}”.` : 'No components yet. Add one above.'}
          </p>
        )}
        {components.length > 0 && (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th className="select-cell">
                    <input
                      id="select-all-components"
                      type="checkbox"
                      checked={allSelected}
                      aria-label="Select all components"
                      onChange={toggleAll}
                    />
                  </th>
                  <th>Name</th>
                  <th>Description</th>
                  <th className="number">Qty / board</th>
                  <th>
                    <span className="visually-hidden">Actions</span>
                  </th>
                </tr>
              </thead>
              <tbody>
                {components.map((component) => (
                  <ComponentRow
                    key={component.id}
                    component={component}
                    selected={selectedIds.has(component.id)}
                    onToggleSelected={toggleSelected}
                    onSaved={handleSaved}
                  />
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </section>
  )
}
