import { useState } from 'react'
import { deleteComponents, searchComponents, type Component } from '../../api/components'
import { useNameSearch } from '../../hooks/useNameSearch'
import { useSelection } from '../../hooks/useSelection'
import { DeleteSelection } from '../../ui/DeleteSelection'
import { ComponentCreateForm } from './ComponentCreateForm'
import { ComponentRow } from './ComponentRow'

export function ComponentsPage() {
  const { nameFilter, setNameFilter, state, items: components, reload } = useNameSearch(searchComponents)
  const selection = useSelection(components)
  const [notice, setNotice] = useState<string | null>(null)

  async function deleteSelected() {
    const count = selection.selectedIds.length
    await deleteComponents(selection.selectedIds)
    setNotice(`Deleted ${count} component${count === 1 ? '' : 's'}.`)
    selection.clear()
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
          count={selection.selectedIds.length}
          noun="component"
          onDelete={deleteSelected}
          onClear={selection.clear}
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
                      checked={selection.allSelected}
                      aria-label="Select all components"
                      onChange={selection.toggleAll}
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
                    selected={selection.isSelected(component.id)}
                    onToggleSelected={selection.toggle}
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
