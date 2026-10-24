import { useEffect, useState } from 'react'
import { searchBoards, type Board } from '../../api/boards'
import { errorMessage } from '../../api/http'
import { deleteOrders, productionDownloadUrl, searchOrders, type Order } from '../../api/orders'
import { useNameSearch } from '../../hooks/useNameSearch'
import { useSelection } from '../../hooks/useSelection'
import { DeleteSelection } from '../../ui/DeleteSelection'
import { OrderForm } from './OrderForm'

export function OrdersPage() {
  const { nameFilter, setNameFilter, state, items: orders, reload } = useNameSearch(searchOrders)
  const selection = useSelection(orders)
  const [boards, setBoards] = useState<Board[]>([])
  const [editing, setEditing] = useState<Order | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [boardsError, setBoardsError] = useState<string | null>(null)

  useEffect(() => {
    const controller = new AbortController()
    searchBoards('', controller.signal)
      .then(setBoards)
      .catch((error: unknown) => {
        if (!controller.signal.aborted) {
          setBoardsError(errorMessage(error))
        }
      })
    return () => controller.abort()
  }, [])

  async function deleteSelected() {
    const count = selection.selectedIds.length
    await deleteOrders(selection.selectedIds)
    setNotice(`Deleted ${count} order${count === 1 ? '' : 's'}.`)
    selection.clear()
    setEditing(null)
    reload()
  }

  function handleSaved(order: Order) {
    setNotice(`Saved ${order.name}.`)
    setEditing(null)
    reload()
  }

  return (
    <section className="page">
      <header className="page-header">
        <h1>Orders</h1>
      </header>

      <OrderForm
        key={editing?.id ?? 'new'}
        order={editing}
        boards={boards}
        boardsError={boardsError}
        onSaved={handleSaved}
        onCancel={() => setEditing(null)}
      />

      <div className="panel">
        <div className="toolbar">
          <label className="search">
            Filter by name
            <input
              id="order-name-filter"
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

        <DeleteSelection count={selection.selectedIds.length} noun="order" onDelete={deleteSelected} onClear={selection.clear} />

        {state.kind === 'loading' && <p className="muted">Loading orders…</p>}
        {state.kind === 'error' && <p className="form-error">Could not load orders: {state.message}</p>}
        {state.kind === 'ready' && orders.length === 0 && (
          <p className="muted">{nameFilter.trim() ? `No order name contains “${nameFilter.trim()}”.` : 'No orders yet.'}</p>
        )}
        {orders.length > 0 && (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th className="select-cell">
                    <input
                      id="select-all-orders"
                      type="checkbox"
                      checked={selection.allSelected}
                      aria-label="Select all orders"
                      onChange={selection.toggleAll}
                    />
                  </th>
                  <th>Name</th>
                  <th>Order date</th>
                  <th>Description</th>
                  <th>Boards</th>
                  <th>
                    <span className="visually-hidden">Actions</span>
                  </th>
                </tr>
              </thead>
              <tbody>
                {orders.map((order) => (
                  <tr key={order.id}>
                    <td className="select-cell">
                      <input
                        id={`select-order-${order.id}`}
                        type="checkbox"
                        checked={selection.isSelected(order.id)}
                        aria-label={`Select ${order.name}`}
                        onChange={() => selection.toggle(order.id)}
                      />
                    </td>
                    <td>{order.name}</td>
                    <td>{order.orderDate}</td>
                    <td className="muted">{order.description}</td>
                    <td>{order.boards.map((board) => board.name).join(', ')}</td>
                    <td className="row-actions">
                      <button type="button" className="link-button" onClick={() => setEditing(order)}>
                        Edit
                      </button>
                      <a href={productionDownloadUrl(order.id)} download>
                        Download for production
                      </a>
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
