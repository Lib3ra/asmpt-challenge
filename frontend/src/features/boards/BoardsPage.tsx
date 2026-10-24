import { useEffect, useState } from 'react'
import { deleteBoards, searchBoards, type Board } from '../../api/boards'
import { searchComponents, type Component } from '../../api/components'
import { errorMessage } from '../../api/http'
import { useNameSearch } from '../../hooks/useNameSearch'
import { useSelection } from '../../hooks/useSelection'
import { DeleteSelection } from '../../ui/DeleteSelection'
import { BoardForm } from './BoardForm'
import { componentLabel } from './componentLabel'

export function BoardsPage() {
  const { nameFilter, setNameFilter, state, items: boards, reload } = useNameSearch(searchBoards)
  const selection = useSelection(boards)
  const [components, setComponents] = useState<Component[]>([])
  const [editing, setEditing] = useState<Board | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [componentsError, setComponentsError] = useState<string | null>(null)

  useEffect(() => {
    const controller = new AbortController()
    searchComponents('', controller.signal)
      .then(setComponents)
      .catch((error: unknown) => {
        if (!controller.signal.aborted) {
          setComponentsError(errorMessage(error))
        }
      })
    return () => controller.abort()
  }, [])

  async function deleteSelected() {
    const count = selection.selectedIds.length
    await deleteBoards(selection.selectedIds)
    setNotice(`Deleted ${count} board${count === 1 ? '' : 's'}.`)
    selection.clear()
    setEditing(null)
    reload()
  }

  function handleSaved(board: Board) {
    setNotice(`Saved ${board.name}.`)
    setEditing(null)
    reload()
  }

  return (
    <section className="page">
      <header className="page-header">
        <h1>Boards</h1>
      </header>

      <BoardForm
        key={editing?.id ?? 'new'}
        board={editing}
        components={components}
        componentsError={componentsError}
        onSaved={handleSaved}
        onCancel={() => setEditing(null)}
      />

      <div className="panel">
        <div className="toolbar">
          <label className="search">
            Filter by name
            <input
              id="board-name-filter"
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

        <DeleteSelection count={selection.selectedIds.length} noun="board" onDelete={deleteSelected} onClear={selection.clear} />

        {state.kind === 'loading' && <p className="muted">Loading boards…</p>}
        {state.kind === 'error' && <p className="form-error">Could not load boards: {state.message}</p>}
        {state.kind === 'ready' && boards.length === 0 && (
          <p className="muted">{nameFilter.trim() ? `No board name contains “${nameFilter.trim()}”.` : 'No boards yet.'}</p>
        )}
        {boards.length > 0 && (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th className="select-cell">
                    <input
                      id="select-all-boards"
                      type="checkbox"
                      checked={selection.allSelected}
                      aria-label="Select all boards"
                      onChange={selection.toggleAll}
                    />
                  </th>
                  <th>Name</th>
                  <th>Description</th>
                  <th className="number">Size (mm)</th>
                  <th>Components</th>
                  <th>
                    <span className="visually-hidden">Actions</span>
                  </th>
                </tr>
              </thead>
              <tbody>
                {boards.map((board) => (
                  <tr key={board.id}>
                    <td className="select-cell">
                      <input
                        id={`select-board-${board.id}`}
                        type="checkbox"
                        checked={selection.isSelected(board.id)}
                        aria-label={`Select ${board.name}`}
                        onChange={() => selection.toggle(board.id)}
                      />
                    </td>
                    <td>{board.name}</td>
                    <td className="muted">{board.description}</td>
                    <td className="number">
                      {board.lengthMm} × {board.widthMm}
                    </td>
                    <td>{board.components.map(componentLabel).join(', ')}</td>
                    <td className="row-actions">
                      <button type="button" className="link-button" onClick={() => setEditing(board)}>
                        Edit
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </section>
  )
}
