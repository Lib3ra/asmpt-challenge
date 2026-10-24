import { useState, type FormEvent } from 'react'
import type { Board } from '../../api/boards'
import { ApiError, errorMessage, type FieldErrors } from '../../api/http'
import { createOrder, updateOrder, type Order, type OrderInput } from '../../api/orders'
import { CheckboxList } from '../../ui/CheckboxList'
import { FieldError } from '../../ui/FieldError'

type OrderFormProps = {
  /** The order being edited, or null to create a new one. */
  order: Order | null
  boards: Board[]
  onSaved: (order: Order) => void
  onCancel: () => void
}

type OrderDraft = {
  name: string
  description: string
  orderDate: string
  boardIds: number[]
}

/** Today's date in the user's time zone as yyyy-MM-dd, the format of date inputs and the API. */
function today(): string {
  const now = new Date()
  const month = String(now.getMonth() + 1).padStart(2, '0')
  const day = String(now.getDate()).padStart(2, '0')
  return `${now.getFullYear()}-${month}-${day}`
}

function draftFrom(order: Order | null): OrderDraft {
  return order
    ? { name: order.name, description: order.description, orderDate: order.orderDate, boardIds: order.boards.map((b) => b.id) }
    : { name: '', description: '', orderDate: today(), boardIds: [] }
}

function toInput(draft: OrderDraft): OrderInput {
  return { name: draft.name, description: draft.description, orderDate: draft.orderDate || null, boardIds: draft.boardIds }
}

/** Creates or edits one order. The parent remounts it (key) when switching between orders. */
export function OrderForm({ order, boards, onSaved, onCancel }: OrderFormProps) {
  const [draft, setDraft] = useState<OrderDraft>(() => draftFrom(order))
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setSaving(true)
    setFieldErrors({})
    setFormError(null)
    try {
      const saved = order ? await updateOrder(order.id, toInput(draft)) : await createOrder(toInput(draft))
      setDraft(draftFrom(null))
      onSaved(saved)
    } catch (error) {
      if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
        setFieldErrors(error.fieldErrors)
      } else {
        setFormError(errorMessage(error))
      }
    } finally {
      setSaving(false)
    }
  }

  return (
    <form className="panel" onSubmit={handleSubmit} noValidate>
      <h2>{order ? `Edit ${order.name}` : 'Add order'}</h2>
      <div className="form-grid">
        <label>
          Name
          <input
            id="order-name"
            value={draft.name}
            aria-invalid={Boolean(fieldErrors.name)}
            onChange={(event) => setDraft({ ...draft, name: event.target.value })}
          />
          <FieldError messages={fieldErrors.name} />
        </label>
        <label>
          Description
          <input
            id="order-description"
            value={draft.description}
            aria-invalid={Boolean(fieldErrors.description)}
            onChange={(event) => setDraft({ ...draft, description: event.target.value })}
          />
          <FieldError messages={fieldErrors.description} />
        </label>
        <label>
          Order date
          <input
            id="order-date"
            type="date"
            value={draft.orderDate}
            aria-invalid={Boolean(fieldErrors.orderDate)}
            onChange={(event) => setDraft({ ...draft, orderDate: event.target.value })}
          />
          <FieldError messages={fieldErrors.orderDate} />
        </label>
      </div>
      <CheckboxList
        id="order-board"
        legend="Boards"
        options={boards.map((board) => ({ id: board.id, label: board.name }))}
        selectedIds={draft.boardIds}
        invalid={Boolean(fieldErrors.boardIds)}
        onChange={(boardIds) => setDraft({ ...draft, boardIds })}
      />
      <FieldError messages={fieldErrors.boardIds} />
      {formError && <p className="form-error">{formError}</p>}
      <div className="actions">
        {order && (
          <button type="button" className="secondary" onClick={onCancel} disabled={saving}>
            Cancel
          </button>
        )}
        <button type="submit" disabled={saving}>
          {saving ? 'Saving…' : order ? 'Save changes' : 'Save order'}
        </button>
      </div>
    </form>
  )
}
